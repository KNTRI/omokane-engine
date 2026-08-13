module IntegratedEntityReactionHistoryTests

open Expecto
open Omokane.Core.Domain
open EntityReactionStateCausalTests.TestData

let private 反応項目 record =
    統合因果追記項目.エンティティ反応状態変更記録 record

let private 位置項目 candidate =
    統合因果追記項目.位置変更候補 candidate

let private 振動項目 record =
    統合因果追記項目.地盤振動記録 record

let private 台帳成功を得る = function
    | 統合因果台帳追記結果.成功 ledger -> ledger
    | 統合因果台帳追記結果.失敗(_, index, id, reasons) ->
        failtestf "統合台帳追記失敗: index=%d id=%A reasons=%A" index id reasons

let private 台帳失敗を得る = function
    | 統合因果台帳追記結果.失敗(ledger, index, id, reasons) -> ledger, index, id, reasons
    | 統合因果台帳追記結果.成功 ledger ->
        failtestf "統合台帳追記成功: %A" (統合因果台帳.記録一覧 ledger)

let private Replay成功を得る = function
    | 統合因果台帳再生結果.成功(update, signals) -> update, signals
    | 統合因果台帳再生結果.失敗(_, index, id, reasons) ->
        failtestf "統合Replay失敗: index=%d id=%A reasons=%A" index id reasons

let private Replay失敗を得る = function
    | 統合因果台帳再生結果.失敗(state, index, id, reasons) -> state, index, id, reasons
    | 統合因果台帳再生結果.成功(update, signals) ->
        failtestf "統合Replay成功: %A %A" update signals

let private 反応記録 operationID requestID tick actor priority hypothesis =
    let state = 状態 tick 敵
    let request = 要求 requestID tick actor priority hypothesis
    let _, record = 適用する state operationID None [] request
    record

let private 地盤振動操作 causalID signalID strength events : 地盤振動発生操作 =
    {
        因果 =
            {
                ID = 因果操作ID causalID
                Tick = 10L
                種別 = 伝播信号生成
                原因因果ID = None
                実行者ID = Some 発生者
                対象EntityID = None
                概要 = "正式な地盤振動を発生する"
                発行Event一覧 = events
            }
        信号ID = 伝播信号ID signalID
        発生位置 = 位置 1.0 2.0
        方向 = None
        強度 = strength
        方式 = 波動
        寿命Tick = 5L
    }

let private 発生成功を得る = function
    | 発生成功(update, signal, record) -> update, signal, record
    | 発生失敗(_, classification, reasons) -> failtestf "地盤振動発生失敗: %A %A" classification reasons

let private sensingSettings: 感知設定 =
    {
        種別 = 地盤感知
        対象量種別 = 地盤振動
        対象媒体 = 地盤
        最大距離 = 10.0
        感度倍率 = 1.0
        感知閾値 = 0.0
        確信飽和値 = 10.0
    }

let private 信号から候補 index tick (signal: 伝播信号) =
    let input: 地盤振動警戒入力 =
        {
            現在Tick = tick
            観測者ID = 主体A
            観測位置 = signal.発生位置
            感知設定 = sensingSettings
            信号 = signal
            観測概要 = $"Replay地盤振動{index}を観測"
            仮説 = $"Replay地盤振動{index}は危険"
            事前確率 = 0.5
            警戒設定 = { 発生閾値 = 0.0 }
        }

    let intent =
        match 地盤振動警戒経路.実行する input with
        | Ok(警戒成立(_, _, _, intent)) -> intent
        | result -> failtestf "認識経路失敗: %A" result

    match 行動候補生成.警戒意図から作る (行動候補ID $"loop.action.{index}") intent with
    | Ok candidate -> candidate
    | Error reasons -> failtestf "候補生成失敗: %A" reasons

let private 完全閉ループを作る () =
    let initial = 状態 10L 敵

    let _, weakSignal, weakRecord =
        地盤振動発生実行.適用する initial (地盤振動操作 "loop.vibration.weak" "loop.signal.weak" 5.0 [ Tick進行 10L ])
        |> 発生成功を得る

    let _, strongSignal, strongRecord =
        地盤振動発生実行.適用する initial (地盤振動操作 "loop.vibration.strong" "loop.signal.strong" 10.0 [ 衝突発生(発生者, 主体A) ])
        |> 発生成功を得る

    let vibrationLedger =
        統合因果台帳.一括追記する 統合因果台帳.空 [ 振動項目 weakRecord; 振動項目 strongRecord ]
        |> 台帳成功を得る

    let replayBefore, replaySignals =
        統合因果台帳再生.再生する initial vibrationLedger
        |> Replay成功を得る

    let candidates =
        replaySignals
        |> List.mapi (fun index signal -> 信号から候補 index replayBefore.状態.Tick signal)

    let selected = candidates |> 行動候補選択.選ぶ |> 選択済みを得る

    let request =
        selected
        |> エンティティ反応要求生成.選択済み行動候補から作る
            (エンティティ反応要求ID "loop.reaction.request")
            replayBefore.状態.Tick
        |> 要求を得る

    let selectedSignalID =
        (エンティティ反応要求.由来行動候補 request).由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head.信号ID

    let sourceRecord =
        match 統合因果台帳.伝播信号IDで探す selectedSignalID vibrationLedger with
        | Some record -> record
        | None -> failtest "選択候補由来の信号記録がありません。"

    let liveUpdate, reactionRecord =
        適用する
            initial
            "loop.reaction.causal"
            (Some sourceRecord.因果操作ID)
            [ ダメージ発生(主体A, 0) ]
            request

    let fullLedger =
        統合因果台帳.エンティティ反応状態変更記録一覧を追記する vibrationLedger [ reactionRecord ]
        |> 台帳成功を得る

    let replayAfter, finalSignals =
        統合因果台帳再生.再生する initial fullLedger
        |> Replay成功を得る

    initial, weakSignal, strongSignal, vibrationLedger, replayBefore, candidates, selected, request, sourceRecord, liveUpdate, reactionRecord, fullLedger, replayAfter, finalSignals

[<Tests>]
let 全テスト =
    testList
        "統合因果台帳の反応状態変更とReplay"
        [
            testCase "反応状態変更記録一件を追記する" <| fun _ ->
                let record = 反応記録 "ledger.reaction.1" "ledger.request.1" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.件数 ledger) 1 "一件"

            testCase "第3種の種別アクセサ" <| fun _ ->
                let record = 反応記録 "ledger.kind" "ledger.request.kind" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.記録一覧 ledger |> List.exactlyOne |> 統合因果記録.種別) 統合因果記録種別.エンティティ反応状態変更 "種別"

            testCase "第3種の共通因果操作IDアクセサ" <| fun _ ->
                let record = 反応記録 "ledger.common.id" "ledger.request.common.id" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.記録一覧 ledger |> List.exactlyOne |> 統合因果記録.因果操作ID) record.因果操作ID "ID"

            testCase "第3種の共通Tickアクセサ" <| fun _ ->
                let record = 反応記録 "ledger.common.tick" "ledger.request.common.tick" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.記録一覧 ledger |> List.exactlyOne |> 統合因果記録.Tick) 10L "Tick"

            testCase "第3種の共通Eventアクセサ" <| fun _ ->
                let request = 要求 "ledger.request.events" 10L 主体A 0.5 "危険"
                let _, record = 適用する (状態 10L 敵) "ledger.events" None [ Tick進行 10L; ダメージ発生(主体A, 1) ] request
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.記録一覧 ledger |> List.exactlyOne |> 統合因果記録.発行Event一覧) record.発行Event一覧 "Event"

            testCase "反応状態変更件数を返す" <| fun _ ->
                let record = 反応記録 "ledger.count" "ledger.request.count" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.エンティティ反応状態変更件数 ledger) 1 "件数"

            testCase "反応状態変更記録一覧を入力順で返す" <| fun _ ->
                let first = 反応記録 "ledger.list.1" "ledger.request.list.1" 10L 主体A 0.5 "危険A"
                let second = 反応記録 "ledger.list.2" "ledger.request.list.2" 10L 主体B 0.7 "危険B"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ first; second ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.エンティティ反応状態変更記録一覧 ledger) [ first; second ] "入力順"

            testCase "要求IDで記録を探す" <| fun _ ->
                let record = 反応記録 "ledger.find" "ledger.request.find" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.エンティティ反応要求IDで探す (エンティティ反応要求ID "ledger.request.find") ledger) (Some record) "検索"

            testCase "存在しない要求IDはNone" <| fun _ ->
                Expect.isNone (統合因果台帳.エンティティ反応要求IDで探す (エンティティ反応要求ID "missing") 統合因果台帳.空) "不存在"

            testCase "既存要求ID重複を拒否する" <| fun _ ->
                let request = 要求 "ledger.request.duplicate.existing" 10L 主体A 0.5 "危険"
                let _, first = 適用する (状態 10L 敵) "ledger.duplicate.existing.1" None [] request
                let _, second = 適用する (状態 10L 敵) "ledger.duplicate.existing.2" None [] request
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ first ] |> 台帳成功を得る
                let _, _, _, reasons = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する ledger [ second ] |> 台帳失敗を得る
                Expect.contains reasons "統合因果台帳に同じエンティティ反応要求IDが既に存在します。" "既存重複"

            testCase "今回要求ID重複を拒否する" <| fun _ ->
                let request = 要求 "ledger.request.duplicate.batch" 10L 主体A 0.5 "危険"
                let _, first = 適用する (状態 10L 敵) "ledger.duplicate.batch.1" None [] request
                let _, second = 適用する (状態 10L 敵) "ledger.duplicate.batch.2" None [] request
                let _, index, _, reasons = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ first; second ] |> 台帳失敗を得る
                Expect.equal index 0 "最小index"
                Expect.contains reasons "統合因果追記項目一覧内でエンティティ反応要求IDが重複しています。" "今回重複"

            testCase "種別横断因果ID重複を拒否する" <| fun _ ->
                let shared = "ledger.cross.id"
                let reaction = 反応記録 shared "ledger.request.cross" 10L 主体A 0.5 "危険"
                let position = IntegratedCausalLedgerTests.TestData.基本位置変更候補 shared 10L
                let _, index, _, reasons = 統合因果台帳.一括追記する 統合因果台帳.空 [ 位置項目 position; 反応項目 reaction ] |> 台帳失敗を得る
                Expect.equal index 0 "最小index"
                Expect.contains reasons "統合因果追記項目一覧内で因果操作IDが重複しています。" "横断重複"

            testCase "反応記録のTick逆行を拒否する" <| fun _ ->
                let first = 反応記録 "ledger.tick.10" "ledger.request.tick.10" 10L 主体A 0.5 "危険"
                let second = 反応記録 "ledger.tick.9" "ledger.request.tick.9" 9L 主体B 0.5 "危険"
                let _, index, _, reasons = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ first; second ] |> 台帳失敗を得る
                Expect.equal index 1 "逆行位置"
                Expect.contains reasons "統合因果台帳のTick順序が逆行しています。" "逆行"

            testCase "3種混在入力順を維持する" <| fun _ ->
                let position = IntegratedCausalLedgerTests.TestData.基本位置変更候補 "ledger.mixed.position" 10L
                let vibration = IntegratedCausalLedgerTests.TestData.基本地盤振動記録 "ledger.mixed.vibration" "ledger.mixed.signal" 10L
                let reaction = 反応記録 "ledger.mixed.reaction" "ledger.mixed.request" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.一括追記する 統合因果台帳.空 [ 位置項目 position; 振動項目 vibration; 反応項目 reaction ] |> 台帳成功を得る
                Expect.equal (統合因果台帳.記録一覧 ledger |> List.map 統合因果記録.種別) [ 統合因果記録種別.Entity位置変更; 統合因果記録種別.地盤振動発生; 統合因果記録種別.エンティティ反応状態変更 ] "3種順"

            testCase "後方不正でも台帳追記は原子的" <| fun _ ->
                let first = 反応記録 "ledger.atomic.1" "ledger.request.atomic.1" 10L 主体A 0.5 "危険"
                let second = { 反応記録 "ledger.atomic.2" "ledger.request.atomic.2" 10L 主体B 0.5 "危険" with 概要 = " " }
                let returned, index, _, _ = 統合因果台帳.一括追記する 統合因果台帳.空 [ 反応項目 first; 反応項目 second ] |> 台帳失敗を得る
                Expect.equal index 1 "後方"
                Expect.equal returned 統合因果台帳.空 "元台帳"

            testCase "反応専用wrapperと共通追記が等価" <| fun _ ->
                let record = 反応記録 "ledger.wrapper" "ledger.request.wrapper" 10L 主体A 0.5 "危険"
                let wrapped = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ]
                let common = 統合因果台帳.一括追記する 統合因果台帳.空 [ 反応項目 record ]
                Expect.equal wrapped common "委譲"

            testCase "反応状態変更一件をReplayする" <| fun _ ->
                let initial = 状態 10L 敵
                let request = 要求 "replay.request.one" 10L 主体A 0.5 "危険"
                let live, record = 適用する initial "replay.reaction.one" None [] request
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let replay, signals = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal replay live "live等価"
                Expect.isEmpty signals "信号なし"

            testCase "Replayは反応状態を末尾追加する" <| fun _ ->
                let initial = 状態 10L 敵
                let record = 反応記録 "replay.append" "replay.request.append" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let replay, _ = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal replay.状態.エンティティ反応状態一覧 [ record.変更後状態 ] "末尾"

            testCase "Replayは既存反応状態を置換する" <| fun _ ->
                let initial = 状態 10L 敵
                let firstRequest = 要求 "replay.request.replace.1" 10L 主体A 0.4 "危険A"
                let firstUpdate, first = 適用する initial "replay.replace.1" None [] firstRequest
                let secondRequest = 要求 "replay.request.replace.2" 10L 主体A 0.9 "危険B"
                let _, second = 適用する firstUpdate.状態 "replay.replace.2" None [] secondRequest
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ first; second ] |> 台帳成功を得る
                let replay, _ = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal replay.状態.エンティティ反応状態一覧 [ second.変更後状態 ] "置換"

            testCase "Replayは変更前状態不一致を拒否する" <| fun _ ->
                let initial = 状態 10L 敵
                let existingRequest = 要求 "replay.request.existing" 10L 主体A 0.4 "既存"
                let withExisting, _ = 適用する initial "replay.existing" None [] existingRequest
                let mismatched = 反応記録 "replay.mismatch" "replay.request.mismatch" 10L 主体A 0.7 "新規"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ mismatched ] |> 台帳成功を得る
                let returned, _, _, reasons = 統合因果台帳再生.再生する withExisting.状態 ledger |> Replay失敗を得る
                Expect.equal returned withExisting.状態 "初期復帰"
                Expect.contains reasons "対象Entityの現在反応状態が記録の変更前状態と一致しません。" "前提不一致"

            testCase "Replayは反応対象不存在を拒否する" <| fun _ ->
                let record = 反応記録 "replay.missing" "replay.request.missing" 10L 主体A 0.5 "危険"
                let initial = { 状態 10L 敵 with エンティティ一覧 = 状態 10L 敵 |> fun value -> value.エンティティ一覧 |> List.filter (fun entity -> entity.ID <> 主体A) }
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let _, _, _, reasons = 統合因果台帳再生.再生する initial ledger |> Replay失敗を得る
                Expect.contains reasons "反応対象Entityが存在しません。" "不存在"

            testCase "Replayは反応対象重複を拒否する" <| fun _ ->
                let record = 反応記録 "replay.entity.duplicate" "replay.request.entity.duplicate" 10L 主体A 0.5 "危険"
                let initial = 状態 10L 敵
                let duplicate = initial.エンティティ一覧.Head
                let initial = { initial with エンティティ一覧 = duplicate :: initial.エンティティ一覧 }
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let _, _, _, reasons = 統合因果台帳再生.再生する initial ledger |> Replay失敗を得る
                Expect.contains reasons "反応対象EntityIDがゲーム状態内で重複しています。" "重複"

            testCase "Replayは状態一覧重複を拒否する" <| fun _ ->
                let initial = 状態 10L 敵
                let _, _, _, first = 基本適用 10L
                let secondRequest = 要求 "replay.request.state.duplicate" 10L 主体A 0.6 "重複"
                let _, second = 適用する initial "replay.state.duplicate.other" None [] secondRequest
                let initial = { initial with エンティティ反応状態一覧 = [ first.変更後状態; second.変更後状態 ] }
                let record = 反応記録 "replay.state.duplicate" "replay.request.target" 10L 主体B 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let _, _, _, reasons = 統合因果台帳再生.再生する initial ledger |> Replay失敗を得る
                Expect.contains reasons "エンティティ反応状態一覧内で実行者IDが重複しています。" "状態重複"

            testCase "Replayは終了状態を拒否する" <| fun _ ->
                let record = 反応記録 "replay.finished" "replay.request.finished" 10L 主体A 0.5 "危険"
                let initial = { 状態 10L 敵 with 終了状態 = Some ゲームオーバー }
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let _, _, _, reasons = 統合因果台帳再生.再生する initial ledger |> Replay失敗を得る
                Expect.contains reasons "終了状態のためエンティティ反応状態変更記録を再生できません。" "終了"

            testCase "ReplayはTick逆行を拒否する" <| fun _ ->
                let record = 反応記録 "replay.tick" "replay.request.tick" 10L 主体A 0.5 "危険"
                let initial = 状態 11L 敵
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let _, _, _, reasons = 統合因果台帳再生.再生する initial ledger |> Replay失敗を得る
                Expect.contains reasons "エンティティ反応状態変更記録のTickが現在の再生状態より前です。" "逆行"

            testCase "位置振動反応の3種混在Replay" <| fun _ ->
                let initial = IntegratedCausalLedgerTests.TestData.状態 10L
                let position = IntegratedCausalLedgerTests.TestData.基本位置変更候補 "replay.mixed.position" 10L
                let vibration = IntegratedCausalLedgerTests.TestData.基本地盤振動記録 "replay.mixed.vibration" "replay.mixed.signal" 10L
                let request = 要求 "replay.mixed.request" 10L IntegratedCausalLedgerTests.TestData.観測者ID 0.8 "混在危険"
                let _, reaction = 適用する initial "replay.mixed.reaction" None [] request
                let ledger = 統合因果台帳.一括追記する 統合因果台帳.空 [ 位置項目 position; 振動項目 vibration; 反応項目 reaction ] |> 台帳成功を得る
                let replay, signals = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal replay.状態.エンティティ反応状態一覧 [ reaction.変更後状態 ] "反応"
                Expect.equal signals [ vibration.信号 ] "信号"

            testCase "3種ReplayはEvent順を維持する" <| fun _ ->
                let initial = IntegratedCausalLedgerTests.TestData.状態 10L
                let position = IntegratedCausalLedgerTests.TestData.位置変更候補 "replay.events.position" 10L IntegratedCausalLedgerTests.TestData.対象A (位置 1.0 2.0) (位置 3.0 4.0) [ Tick進行 1L ]
                let vibration = IntegratedCausalLedgerTests.TestData.地盤振動記録 "replay.events.vibration" "replay.events.signal" 10L (Some IntegratedCausalLedgerTests.TestData.発生者ID) [ Tick進行 2L ]
                let request = 要求 "replay.events.request" 10L IntegratedCausalLedgerTests.TestData.観測者ID 0.8 "危険"
                let _, reaction = 適用する initial "replay.events.reaction" None [ Tick進行 3L ] request
                let ledger = 統合因果台帳.一括追記する 統合因果台帳.空 [ 位置項目 position; 振動項目 vibration; 反応項目 reaction ] |> 台帳成功を得る
                let replay, _ = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal replay.イベント一覧 [ Tick進行 1L; Tick進行 2L; Tick進行 3L ] "Event順"

            testCase "3種Replayは信号順を維持する" <| fun _ ->
                let initial = 状態 10L 敵
                let first = IntegratedCausalLedgerTests.TestData.地盤振動記録 "replay.signal.1" "replay.signal.id.1" 10L (Some 発生者) []
                let reaction = 反応記録 "replay.signal.reaction" "replay.signal.request" 10L 主体A 0.5 "危険"
                let second = IntegratedCausalLedgerTests.TestData.地盤振動記録 "replay.signal.2" "replay.signal.id.2" 10L (Some 発生者) []
                let ledger = 統合因果台帳.一括追記する 統合因果台帳.空 [ 振動項目 first; 反応項目 reaction; 振動項目 second ] |> 台帳成功を得る
                let _, signals = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal signals [ first.信号; second.信号 ] "信号順"

            testCase "反応記録はReplay信号を増やさない" <| fun _ ->
                let record = 反応記録 "replay.no.signal" "replay.request.no.signal" 10L 主体A 0.5 "危険"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ record ] |> 台帳成功を得る
                let _, signals = 統合因果台帳再生.再生する (状態 10L 敵) ledger |> Replay成功を得る
                Expect.isEmpty signals "信号なし"

            testCase "Eventから反応状態を作らない" <| fun _ ->
                let vibration = IntegratedCausalLedgerTests.TestData.地盤振動記録 "replay.event.only" "replay.event.signal" 10L None [ ゲーム終了 クリア ]
                let ledger = 統合因果台帳.地盤振動発生記録一覧を追記する 統合因果台帳.空 [ vibration ] |> 台帳成功を得る
                let replay, _ = 統合因果台帳再生.再生する (状態 10L 敵) ledger |> Replay成功を得る
                Expect.isEmpty replay.状態.エンティティ反応状態一覧 "Event非権威"
                Expect.isNone replay.状態.終了状態 "終了Eventも状態へ反映しない"

            testCase "後方Replay失敗で初期状態復帰" <| fun _ ->
                let initial = 状態 10L 敵
                let first = 反応記録 "replay.atomic.1" "replay.request.atomic.1" 10L 主体A 0.5 "危険A"
                let second = 反応記録 "replay.atomic.2" "replay.request.atomic.2" 10L 主体A 0.8 "危険B"
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ first; second ] |> 台帳成功を得る
                let returned, index, _, _ = 統合因果台帳再生.再生する initial ledger |> Replay失敗を得る
                Expect.equal index 1 "後方失敗"
                Expect.equal returned initial "初期状態"

            testCase "後方Replay失敗で途中Eventを公開しない" <| fun _ ->
                let initial = 状態 10L 敵
                let request1 = 要求 "replay.event.atomic.1" 10L 主体A 0.5 "危険A"
                let _, first = 適用する initial "replay.event.atomic.causal.1" None [ Tick進行 1L ] request1
                let request2 = 要求 "replay.event.atomic.2" 10L 主体A 0.8 "危険B"
                let _, second = 適用する initial "replay.event.atomic.causal.2" None [ Tick進行 2L ] request2
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ first; second ] |> 台帳成功を得る
                match 統合因果台帳再生.再生する initial ledger with
                | 統合因果台帳再生結果.失敗 _ -> ()
                | result -> failtestf "失敗結果だけを期待: %A" result

            testCase "後方Replay失敗で途中信号を公開しない" <| fun _ ->
                let initial = 状態 10L 敵
                let vibration = IntegratedCausalLedgerTests.TestData.基本地盤振動記録 "replay.atomic.vibration" "replay.atomic.signal" 10L
                let first = 反応記録 "replay.atomic.reaction.1" "replay.atomic.request.1" 10L 主体A 0.5 "危険A"
                let second = 反応記録 "replay.atomic.reaction.2" "replay.atomic.request.2" 10L 主体A 0.8 "危険B"
                let ledger = 統合因果台帳.一括追記する 統合因果台帳.空 [ 振動項目 vibration; 反応項目 first; 反応項目 second ] |> 台帳成功を得る
                match 統合因果台帳再生.再生する initial ledger with
                | 統合因果台帳再生結果.失敗 _ -> ()
                | result -> failtestf "途中信号を含む成功を返しました: %A" result

            testCase "既存2種Replay互換" <| fun _ ->
                let initial = IntegratedCausalLedgerTests.TestData.状態 10L
                let position = IntegratedCausalLedgerTests.TestData.基本位置変更候補 "replay.compat.position" 10L
                let vibration = IntegratedCausalLedgerTests.TestData.基本地盤振動記録 "replay.compat.vibration" "replay.compat.signal" 10L
                let ledger = 統合因果台帳.一括追記する 統合因果台帳.空 [ 位置項目 position; 振動項目 vibration ] |> 台帳成功を得る
                let replay, signals = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.isEmpty replay.状態.エンティティ反応状態一覧 "旧履歴"
                Expect.equal signals [ vibration.信号 ] "旧信号"

            testCase "因果認識から反応状態まで完走する" <| fun _ ->
                let _, _, _, _, _, _, _, _, _, _, reactionRecord, _, replay, _ = 完全閉ループを作る ()
                Expect.equal replay.状態.エンティティ反応状態一覧 [ reactionRecord.変更後状態 ] "完走"

            testCase "地盤振動因果IDを原因因果IDへ接続する" <| fun _ ->
                let _, _, _, _, _, _, _, _, sourceRecord, _, reactionRecord, _, _, _ = 完全閉ループを作る ()
                Expect.equal reactionRecord.原因因果ID (Some sourceRecord.因果操作ID) "原因接続"

            testCase "信号IDから要求と状態のProvenanceを追跡する" <| fun _ ->
                let _, _, strongSignal, _, _, _, _, request, _, _, reactionRecord, _, _, _ = 完全閉ループを作る ()
                let sourceSignalID = (エンティティ反応要求.由来行動候補 request).由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head.信号ID
                Expect.equal sourceSignalID strongSignal.ID "強信号選択"
                Expect.equal (エンティティ反応状態.由来要求ID reactionRecord.変更後状態) (エンティティ反応要求.ID request) "要求由来"

            testCase "選択候補IDを反応状態へ保持する" <| fun _ ->
                let _, _, _, _, _, _, selected, _, _, _, reactionRecord, _, _, _ = 完全閉ループを作る ()
                Expect.equal (エンティティ反応状態.由来行動候補ID reactionRecord.変更後状態) (選択済み行動候補.選択候補 selected).ID "候補ID"

            testCase "要求IDを反応状態へ保持する" <| fun _ ->
                let _, _, _, _, _, _, _, request, _, _, reactionRecord, _, _, _ = 完全閉ループを作る ()
                Expect.equal (エンティティ反応状態.由来要求ID reactionRecord.変更後状態) (エンティティ反応要求.ID request) "要求ID"

            testCase "live実行とReplay状態が一致する" <| fun _ ->
                let _, _, _, _, _, _, _, _, _, live, _, _, replay, _ = 完全閉ループを作る ()
                Expect.equal replay.状態 live.状態 "状態一致"

            testCase "live EventとReplay末尾Eventが一致する" <| fun _ ->
                let _, _, _, _, replayBefore, _, _, _, _, live, _, _, replayAfter, _ = 完全閉ループを作る ()
                Expect.equal replayAfter.イベント一覧 (replayBefore.イベント一覧 @ live.イベント一覧) "Event一致"

            testCase "Replay信号が元の正式信号と一致する" <| fun _ ->
                let _, weak, strong, _, _, _, _, _, _, _, _, _, _, signals = 完全閉ループを作る ()
                Expect.equal signals [ weak; strong ] "信号一致"

            testCase "完全閉ループでも物理状態不変" <| fun _ ->
                let initial, _, _, _, _, _, _, _, _, live, _, _, replay, _ = 完全閉ループを作る ()
                Expect.equal live.状態.エンティティ一覧 initial.エンティティ一覧 "live物理"
                Expect.equal replay.状態.エンティティ一覧 initial.エンティティ一覧 "Replay物理"

            testCase "同一Entity二回更新の最終状態がReplay一致" <| fun _ ->
                let initial = 状態 10L 敵
                let requestA = 要求 "update.request.a" 10L 主体A 0.5 "仮説A"
                let updateA, recordA = 適用する initial "update.causal.a" None [] requestA
                let requestB = 要求 "update.request.b" 10L 主体A 0.9 "仮説B"
                let updateB, recordB = 適用する updateA.状態 "update.causal.b" None [] requestB
                let ledger = 統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 [ recordA; recordB ] |> 台帳成功を得る
                let replay, _ = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal replay.状態 updateB.状態 "二回更新"
                Expect.equal recordB.変更前状態 (Some recordA.変更後状態) "前状態"

            testCase "Tick増加をまたぐ同一Entity更新をReplayできる" <| fun _ ->
                let initial = 状態 10L 敵
                let requestA = 要求 "update.tick.request.a" 10L 主体A 0.5 "仮説A"
                let updateA, recordA = 適用する initial "update.tick.causal.a" None [] requestA
                let nextTickState = { updateA.状態 with Tick = 11L }
                let requestB = 要求 "update.tick.request.b" 11L 主体A 0.9 "仮説B"
                let updateB, recordB = 適用する nextTickState "update.tick.causal.b" None [] requestB

                let ledger =
                    統合因果台帳.エンティティ反応状態変更記録一覧を追記する
                        統合因果台帳.空
                        [ recordA; recordB ]
                    |> 台帳成功を得る

                let replay, _ = 統合因果台帳再生.再生する initial ledger |> Replay成功を得る
                Expect.equal replay.状態 updateB.状態 "Tick増加後の状態"
                Expect.equal replay.状態.Tick 11L "最終Tick"
                Expect.equal recordB.変更前状態 (Some recordA.変更後状態) "直前状態"

            testCase "2160反応状態変更Replay Matrixは決定論的" <| fun _ ->
                let initialModes = [ false; true ]
                let priorities = [ 0.0; 0.25; 0.5; 0.75; 1.0 ]
                let ticks = [ 0L; 10L; 100L ]
                let causes = [ None; Some(因果操作ID "matrix.source") ]
                let eventModes = [ 0; 1; 2 ]
                let entityKinds = [ プレイヤー; 敵; 弾; 障害物 ]
                let updateCounts = [ 1; 2; 3 ]

                let combinations =
                    [
                        for hasInitial in initialModes do
                            for priority in priorities do
                                for tick in ticks do
                                    for cause in causes do
                                        for eventMode in eventModes do
                                            for kind in entityKinds do
                                                for updateCount in updateCounts do
                                                    yield hasInitial, priority, tick, cause, eventMode, kind, updateCount
                    ]

                Expect.equal combinations.Length 2160 "Matrix件数"

                let runCase caseIndex (hasInitial, priority, tick, cause, eventMode, kind, updateCount) =
                    let baseState = 状態 tick kind

                    let initial =
                        if hasInitial then
                            let request = 要求 $"matrix.initial.request.{caseIndex}" tick 主体A 0.1 "初期仮説"
                            let update, _ = 適用する baseState $"matrix.initial.causal.{caseIndex}" None [] request
                            update.状態
                        else
                            baseState

                    let events ordinal =
                        match eventMode with
                        | 0 -> []
                        | 1 -> [ Tick進行 tick ]
                        | _ -> [ Tick進行 tick; ダメージ発生(主体A, ordinal) ]

                    let liveState, liveEvents, records =
                        [ 0 .. updateCount - 1 ]
                        |> List.fold
                            (fun (currentState, allEvents, allRecords) ordinal ->
                                let request = 要求 $"matrix.request.{caseIndex}.{ordinal}" tick 主体A priority $"Matrix仮説{ordinal}"
                                let update, record = 適用する currentState $"matrix.causal.{caseIndex}.{ordinal}" cause (events ordinal) request
                                update.状態, allEvents @ update.イベント一覧, allRecords @ [ record ])
                            (initial, [], [])

                    let ledger =
                        統合因果台帳.エンティティ反応状態変更記録一覧を追記する 統合因果台帳.空 records
                        |> 台帳成功を得る

                    let replay, signals =
                        統合因果台帳再生.再生する initial ledger
                        |> Replay成功を得る

                    Expect.equal replay.状態 liveState $"case {caseIndex} live/replay"
                    Expect.equal replay.イベント一覧 liveEvents $"case {caseIndex} Event"
                    Expect.isEmpty signals $"case {caseIndex} no signals"
                    Expect.equal liveState.エンティティ一覧 initial.エンティティ一覧 $"case {caseIndex} physical"
                    Expect.equal (liveState.エンティティ反応状態一覧 |> List.map エンティティ反応状態.実行者ID) [ 主体A ] $"case {caseIndex} order"
                    liveState, liveEvents, records, replay, signals

                combinations
                |> List.iteri (fun index combination ->
                    let first = runCase index combination
                    let second = runCase index combination
                    Expect.equal second first $"case {index} deterministic")
        ]
