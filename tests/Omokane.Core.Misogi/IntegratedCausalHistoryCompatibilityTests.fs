module IntegratedCausalHistoryCompatibilityTests

open Expecto
open Omokane.Core.Domain
open IntegratedCausalLedgerTests.TestData

let private 統合再生成功を得る = function
    | 統合因果台帳再生結果.成功(更新, 信号一覧) -> 更新, 信号一覧
    | 統合因果台帳再生結果.失敗(_, index, id, 理由一覧) ->
        failtestf "統合Replayが失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

let private 既存再生成功を得る = function
    | 再生成功 更新 -> 更新
    | 再生失敗(_, index, id, 理由一覧) ->
        failtestf "既存Replayが失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

let private 位置変更操作 id tick targetID newPosition events : エンティティ位置変更操作 =
    {
        因果 =
            {
                ID = 因果操作ID id
                Tick = tick
                種別 = 状態変更
                原因因果ID = None
                実行者ID = Some プレイヤーID
                対象EntityID = Some targetID
                概要 = "位置を変更する"
                発行Event一覧 = events
            }
        変更後位置 = newPosition
    }

let private 地盤振動操作 id signalID tick events : 地盤振動発生操作 =
    {
        因果 =
            {
                ID = 因果操作ID id
                Tick = tick
                種別 = 伝播信号生成
                原因因果ID = None
                実行者ID = Some 発生者ID
                対象EntityID = None
                概要 = "地面を踏み鳴らす"
                発行Event一覧 = events
            }
        信号ID = 伝播信号ID signalID
        発生位置 = 位置 8.0 8.0
        方向 = Some { X = 1.0; Y = 0.0 }
        強度 = 10.0
        方式 = 波動
        寿命Tick = 5L
    }

let private 感知設定: 感知設定 =
    {
        種別 = 地盤感知
        対象量種別 = 地盤振動
        対象媒体 = 地盤
        最大距離 = 10.0
        感度倍率 = 1.0
        感知閾値 = 0.0
        確信飽和値 = 10.0
    }

let private 警戒設定: 警戒意図設定 = { 発生閾値 = 0.75 }

let private 認識入力 tick signal : 地盤振動警戒入力 =
    {
        現在Tick = tick
        観測者ID = 観測者ID
        観測位置 = 位置 8.0 8.0
        感知設定 = 感知設定
        信号 = signal
        観測概要 = "地面が強く揺れた"
        仮説 = "近くに危険がある"
        事前確率 = 0.5
        警戒設定 = 警戒設定
    }

let private 地盤振動発生成功を得る = function
    | 発生成功(更新, 信号, 記録) -> 更新, 信号, 記録
    | 発生失敗(_, 分類, 理由一覧) -> failtestf "地盤振動発生失敗: %A %A" 分類 理由一覧

[<Tests>]
let 全テスト =
    testList
        "統合因果履歴の互換性と決定論"
        [
            testCase "既存Replayと移行済み統合Replayが成功と失敗で等価" <| fun _ ->
                let candidates =
                    [
                        位置変更候補 "legacy.1" 10L 対象A (位置 1.0 2.0) (位置 3.0 4.0) [ Tick進行 10L ]
                        位置変更候補 "legacy.2" 12L 対象A (位置 3.0 4.0) (位置 5.0 6.0) [ ダメージ発生(対象A, 1) ]
                    ]
                let legacy = 既存台帳を作る candidates
                let integrated = 統合因果台帳.既存因果台帳から移行する legacy
                let initial = 状態 10L
                let legacySuccess = 因果台帳再生.再生する initial legacy |> 既存再生成功を得る
                let integratedSuccess, signals = 統合因果台帳再生.再生する initial integrated |> 統合再生成功を得る
                Expect.equal integratedSuccess legacySuccess "成功結果"
                Expect.isEmpty signals "位置専用では信号なし"

                let missingState = { initial with エンティティ一覧 = initial.エンティティ一覧 |> List.filter (fun entity -> entity.ID <> 対象A) }
                let legacyFailure =
                    match 因果台帳再生.再生する missingState legacy with
                    | 再生失敗(state, index, id, reasons) -> state, index, id, reasons
                    | result -> failtestf "既存Replayが成功しました: %A" result
                let integratedFailure =
                    match 統合因果台帳再生.再生する missingState integrated with
                    | 統合因果台帳再生結果.失敗(state, index, id, reasons) -> state, index, id, reasons
                    | result -> failtestf "統合Replayが成功しました: %A" result
                Expect.equal integratedFailure legacyFailure "失敗index、ID、理由"

            testCase "位置変更だけの統合Replayでは信号一覧が空" <| fun _ ->
                let ledger = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目; 位置変更候補 "p2" 10L 対象B (位置 10.0 20.0) (位置 11.0 21.0) [] |> 位置項目 ]
                let _, signals = 統合因果台帳再生.再生する (状態 10L) ledger |> 統合再生成功を得る
                Expect.isEmpty signals "信号なし"

            testCase "地盤振動発生実行と統合Replayが等価" <| fun _ ->
                let initial = 状態 10L
                let emittedUpdate, emittedSignal, emittedRecord = 地盤振動発生実行.適用する initial (地盤振動操作 "v1" "s1" 10L [ Tick進行 10L ]) |> 地盤振動発生成功を得る
                let ledger = 統合台帳を作る [ 振動項目 emittedRecord ]
                let replayUpdate, signals = 統合因果台帳再生.再生する initial ledger |> 統合再生成功を得る
                Expect.equal replayUpdate emittedUpdate "状態とEvent"
                Expect.equal signals [ emittedSignal ] "正式信号"

            testCase "実行結果から作った混在履歴をReplayできる" <| fun _ ->
                let initial = 状態 10L
                let positionResult = 因果操作実行.適用する initial (位置変更操作 "p1" 10L 対象A (位置 3.0 4.0) [ Tick進行 1L ])
                let positionUpdate, positionRecord =
                    match positionResult with
                    | 適用成功(update, record) -> update, record
                    | failure -> failtestf "位置変更失敗: %A" failure
                let vibrationUpdate, signal, vibrationRecord = 地盤振動発生実行.適用する positionUpdate.状態 (地盤振動操作 "v1" "s1" 10L [ Tick進行 2L ]) |> 地盤振動発生成功を得る
                let ledger = 統合台帳を作る [ 位置項目 positionRecord; 振動項目 vibrationRecord ]
                let replayUpdate, signals = 統合因果台帳再生.再生する initial ledger |> 統合再生成功を得る
                Expect.equal replayUpdate.状態 vibrationUpdate.状態 "実行後状態"
                Expect.equal replayUpdate.イベント一覧 (positionUpdate.イベント一覧 @ vibrationUpdate.イベント一覧) "実行Event列"
                Expect.equal signals [ signal ] "生成信号"

            testCase "Replay信号から地盤振動警戒経路を再実行できる" <| fun _ ->
                let initial = 状態 10L
                let _, _, record = 地盤振動発生実行.適用する initial (地盤振動操作 "v1" "s1" 10L []) |> 地盤振動発生成功を得る
                let update, signals = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する initial |> 統合再生成功を得る
                match 地盤振動警戒経路.実行する (認識入力 update.状態.Tick (List.exactlyOne signals)) with
                | Ok(警戒成立 _) -> ()
                | result -> failtestf "警戒成立ではありません: %A" result

            testCase "Replay信号から行動候補まで再構築できる" <| fun _ ->
                let initial = 状態 10L
                let _, _, record = 地盤振動発生実行.適用する initial (地盤振動操作 "v1" "s1" 10L []) |> 地盤振動発生成功を得る
                let update, signals = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する initial |> 統合再生成功を得る
                let intent =
                    match 地盤振動警戒経路.実行する (認識入力 update.状態.Tick (List.exactlyOne signals)) with
                    | Ok(警戒成立(_, _, _, intent)) -> intent
                    | result -> failtestf "警戒成立ではありません: %A" result
                match 行動候補生成.警戒意図から作る (行動候補ID "replay.action") intent with
                | Ok candidate -> Expect.equal candidate.種別 行動候補種別.その場警戒 "行動候補"
                | Error reasons -> failtestf "行動候補生成失敗: %A" reasons

            testCase "元の因果認識閉ループとReplay後の認識結果が一致する" <| fun _ ->
                let initial = 状態 10L
                let operation = 地盤振動操作 "v1" "s1" 10L []
                let loopInput: 因果地盤振動行動入力 =
                    {
                        発生操作 = operation
                        観測者ID = 観測者ID
                        観測位置 = 位置 8.0 8.0
                        感知設定 = 感知設定
                        観測概要 = "地面が強く揺れた"
                        仮説 = "近くに危険がある"
                        事前確率 = 0.5
                        警戒設定 = 警戒設定
                        行動候補ID = 行動候補ID "original.action"
                    }
                let originalSignal, originalIntent =
                    match 因果地盤振動行動経路.実行する initial loopInput with
                    | Ok(因果地盤振動行動結果.行動候補成立(_, _, signal, _, _, _, intent, _)) -> signal, intent
                    | result -> failtestf "元経路が成立しません: %A" result
                let _, _, record = 地盤振動発生実行.適用する initial operation |> 地盤振動発生成功を得る
                let update, replaySignals = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する initial |> 統合再生成功を得る
                let replaySignal = List.exactlyOne replaySignals
                let replayIntent =
                    match 地盤振動警戒経路.実行する (認識入力 update.状態.Tick replaySignal) with
                    | Ok(警戒成立(_, _, _, intent)) -> intent
                    | result -> failtestf "再生後経路が成立しません: %A" result
                Expect.equal replaySignal originalSignal "信号"
                Expect.equal replayIntent originalIntent "認識結果"

            testCase "216混在履歴Matrixが決定論的である" <| fun _ ->
                let initialTicks = [ 0L; 10L; 20L ]
                let positionCounts = [ 0; 1; 2 ]
                let vibrationCounts = [ 0; 1; 2; 3 ]
                let sameTickModes = [ true; false ]
                let eventModes = [ 0; 1; 2 ]

                let combinations =
                    [
                        for initialTick in initialTicks do
                            for positionCount in positionCounts do
                                for vibrationCount in vibrationCounts do
                                    for sameTick in sameTickModes do
                                        for eventMode in eventModes do
                                            yield initialTick, positionCount, vibrationCount, sameTick, eventMode
                    ]

                Expect.isGreaterThanOrEqual combinations.Length 100 "100履歴以上"

                combinations
                |> List.iteri (fun combinationIndex (initialTick, positionCount, vibrationCount, sameTick, eventMode) ->
                    let maxCount = max positionCount vibrationCount

                    let tags =
                        [
                            for ordinal in 0 .. maxCount - 1 do
                                if ordinal < positionCount then
                                    yield Choice1Of2 ordinal
                                if ordinal < vibrationCount then
                                    yield Choice2Of2 ordinal
                        ]

                    let events historyIndex =
                        match eventMode with
                        | 0 -> []
                        | 1 -> [ Tick進行(int64 historyIndex) ]
                        | _ -> [ Tick進行(int64 historyIndex); ダメージ発生(対象B, historyIndex) ]

                    let items =
                        tags
                        |> List.mapi (fun historyIndex tag ->
                            let tick = if sameTick then initialTick else initialTick + int64 historyIndex
                            match tag with
                            | Choice1Of2 ordinal ->
                                位置変更候補
                                    $"matrix.{combinationIndex}.p.{ordinal}"
                                    tick
                                    対象A
                                    (位置 (1.0 + float ordinal * 2.0) (2.0 + float ordinal * 2.0))
                                    (位置 (3.0 + float ordinal * 2.0) (4.0 + float ordinal * 2.0))
                                    (events historyIndex)
                                |> 位置項目
                            | Choice2Of2 ordinal ->
                                地盤振動記録
                                    $"matrix.{combinationIndex}.v.{ordinal}"
                                    $"matrix.signal.{combinationIndex}.{ordinal}"
                                    tick
                                    (Some 発生者ID)
                                    (events historyIndex)
                                |> 振動項目)

                    let initial = 状態 initialTick
                    let ledger = 統合台帳を作る items
                    let first = 統合因果台帳再生.再生する initial ledger
                    let second = 統合因果台帳再生.再生する initial ledger
                    Expect.equal second first $"履歴{combinationIndex}の構造的決定論"

                    match first, second with
                    | 統合因果台帳再生結果.成功(firstUpdate, firstSignals), 統合因果台帳再生結果.成功(secondUpdate, secondSignals) ->
                        Expect.equal secondUpdate.イベント一覧 firstUpdate.イベント一覧 $"履歴{combinationIndex}のEvent順"
                        Expect.equal secondSignals firstSignals $"履歴{combinationIndex}の信号順"
                    | _ -> failtestf "有効履歴のReplayが失敗しました: index=%d result=%A" combinationIndex first)
        ]
