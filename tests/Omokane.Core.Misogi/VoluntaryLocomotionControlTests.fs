module VoluntaryLocomotionControlTests

open Expecto
open Omokane.Core.Domain
open EntityReactionStateCausalTests.TestData

module private TestData =

    let 判断を得る = function
        | Ok 判断 -> 判断
        | Error エラー一覧 -> failtestf "判断生成失敗: %A" エラー一覧

    let エラーを得る = function
        | Error エラー一覧 -> エラー一覧
        | Ok 判断 -> failtestf "判断生成成功: %A" 判断

    let 判断する 対象ID 状態 =
        状態
        |> 自発移動制御判断生成.ゲーム状態から作る 対象ID
        |> 判断を得る

    let 対象Entityを変更する 変更 状態 =
        {
            状態 with
                エンティティ一覧 =
                    状態.エンティティ一覧
                    |> List.map (fun entity -> if entity.ID = 主体A then 変更 entity else entity)
        }

    let 反応状態を持つ状態 tick 種別 actor priority hypothesis =
        let 初期 = 状態 tick 種別
        let request = 要求 $"locomotion.request.{hypothesis}" tick actor priority hypothesis
        let update, record = 適用する 初期 $"locomotion.causal.{hypothesis}" None [] request
        update.状態, record.変更後状態

    let 基本抑制状態 tick =
        反応状態を持つ状態 tick 敵 主体A 0.75 "地盤崩落の危険"

    let 他主体だけ警戒中 tick =
        反応状態を持つ状態 tick 敵 主体B 0.6 "別主体の危険"

    let 二主体警戒中 tick =
        let stateA, reactionA = 反応状態を持つ状態 tick 敵 主体A 0.75 "主体Aの危険"
        let requestB = 要求 "locomotion.request.actor.b" tick 主体B 0.5 "主体Bの危険"
        let updateB, recordB = 適用する stateA "locomotion.causal.actor.b" None [] requestB
        updateB.状態, reactionA, recordB.変更後状態

    let 地盤振動操作: 地盤振動発生操作 =
        {
            因果 =
                {
                    ID = 因果操作ID "locomotion.loop.vibration"
                    Tick = 10L
                    種別 = 伝播信号生成
                    原因因果ID = None
                    実行者ID = Some 発生者
                    対象EntityID = None
                    概要 = "自発移動制御判断へ至る地盤振動"
                    発行Event一覧 = [ 衝突発生(発生者, 主体A) ]
                }
            信号ID = 伝播信号ID "locomotion.loop.signal"
            発生位置 = 位置 1.0 2.0
            方向 = None
            強度 = 10.0
            方式 = 波動
            寿命Tick = 5L
        }

    let 感知設定: 感知設定 =
        {
            種別 = 地盤感知
            対象量種別 = 地盤振動
            対象媒体 = 地盤
            最大距離 = 10.0
            感度倍率 = 1.0
            感知閾値 = 0.0
            確信飽和値 = 10.0
        }

    type 完全経路結果 =
        {
            初期状態: ゲーム状態
            発生信号: 伝播信号
            感知: 感知値
            観測: 観測
            信念: 信念候補
            意図: 警戒意図
            候補: 行動候補
            選択済み: 選択済み行動候補
            要求: エンティティ反応要求
            発生記録: 地盤振動発生記録
            Live更新: 更新結果
            反応記録: エンティティ反応状態変更記録
            台帳: 統合因果台帳
            Replay更新: 更新結果
            Replay信号一覧: 伝播信号 list
            Live判断: 自発移動制御判断
            Replay判断: 自発移動制御判断
        }

    let 完全因果経路を作る () =
        let initial = 状態 10L 敵

        let _, signal, emissionRecord =
            match 地盤振動発生実行.適用する initial 地盤振動操作 with
            | 発生成功(update, signal, record) -> update, signal, record
            | 発生失敗(_, classification, reasons) ->
                failtestf "地盤振動発生失敗: %A %A" classification reasons

        let vibrationLedger =
            match 統合因果台帳.地盤振動発生記録一覧を追記する 統合因果台帳.空 [ emissionRecord ] with
            | 統合因果台帳追記結果.成功 ledger -> ledger
            | 統合因果台帳追記結果.失敗(_, index, id, reasons) ->
                failtestf "地盤振動台帳追記失敗: %d %A %A" index id reasons

        let replayBefore, replaySignals =
            match 統合因果台帳再生.再生する initial vibrationLedger with
            | 統合因果台帳再生結果.成功(update, signals) -> update, signals
            | 統合因果台帳再生結果.失敗(_, index, id, reasons) ->
                failtestf "地盤振動Replay失敗: %d %A %A" index id reasons

        let replaySignal = replaySignals |> List.exactlyOne

        let sensingInput: 地盤振動警戒入力 =
            {
                現在Tick = replayBefore.状態.Tick
                観測者ID = 主体A
                観測位置 = replaySignal.発生位置
                感知設定 = 感知設定
                信号 = replaySignal
                観測概要 = "地面の強い揺れを観測"
                仮説 = "地盤崩落が迫っている"
                事前確率 = 0.5
                警戒設定 = { 発生閾値 = 0.75 }
            }

        let sensing, observation, belief, intent =
            match 地盤振動警戒経路.実行する sensingInput with
            | Ok(警戒成立(sensing, observation, belief, intent)) ->
                sensing, observation, belief, intent
            | result -> failtestf "地盤振動警戒経路失敗: %A" result

        let candidate =
            match 行動候補生成.警戒意図から作る (行動候補ID "locomotion.loop.action") intent with
            | Ok candidate -> candidate
            | Error reasons -> failtestf "行動候補生成失敗: %A" reasons

        let selected =
            [ candidate ]
            |> 行動候補選択.選ぶ
            |> 選択済みを得る

        let request =
            selected
            |> エンティティ反応要求生成.選択済み行動候補から作る
                (エンティティ反応要求ID "locomotion.loop.request")
                replayBefore.状態.Tick
            |> 要求を得る

        let liveUpdate, reactionRecord =
            適用する
                initial
                "locomotion.loop.reaction"
                (Some emissionRecord.因果操作ID)
                [ ダメージ発生(主体A, 0) ]
                request

        let fullLedger =
            match 統合因果台帳.エンティティ反応状態変更記録一覧を追記する vibrationLedger [ reactionRecord ] with
            | 統合因果台帳追記結果.成功 ledger -> ledger
            | 統合因果台帳追記結果.失敗(_, index, id, reasons) ->
                failtestf "反応状態台帳追記失敗: %d %A %A" index id reasons

        let replayAfter, finalSignals =
            match 統合因果台帳再生.再生する initial fullLedger with
            | 統合因果台帳再生結果.成功(update, signals) -> update, signals
            | 統合因果台帳再生結果.失敗(_, index, id, reasons) ->
                failtestf "統合Replay失敗: %d %A %A" index id reasons

        {
            初期状態 = initial
            発生信号 = signal
            感知 = sensing
            観測 = observation
            信念 = belief
            意図 = intent
            候補 = candidate
            選択済み = selected
            要求 = request
            発生記録 = emissionRecord
            Live更新 = liveUpdate
            反応記録 = reactionRecord
            台帳 = fullLedger
            Replay更新 = replayAfter
            Replay信号一覧 = finalSignals
            Live判断 = 判断する 主体A liveUpdate.状態
            Replay判断 = 判断する 主体A replayAfter.状態
        }

open TestData

[<Tests>]
let 全テスト =
    testList
        "反応状態から自発移動制御判断"
        [
            testCase "反応状態なしで判断を生成できる" <| fun _ ->
                状態 10L 敵 |> 判断する 主体A |> ignore

            testCase "その場警戒状態で判断を生成できる" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                state |> 判断する 主体A |> ignore

            testCase "評価Tickをゲーム状態から保存する" <| fun _ ->
                let actual = 状態 25L 敵 |> 判断する 主体A |> 自発移動制御判断.評価Tick
                Expect.equal actual 25L "評価Tick"

            testCase "実行者IDを保存する" <| fun _ ->
                let actual = 状態 10L 敵 |> 判断する 主体A |> 自発移動制御判断.実行者ID
                Expect.equal actual 主体A "実行者"

            testCase "反応状態なしは許可種別" <| fun _ ->
                let actual = 状態 10L 敵 |> 判断する 主体A |> 自発移動制御判断.種別
                Expect.equal actual 自発移動制御種別.許可 "許可"

            testCase "その場警戒中は抑制種別" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.種別
                Expect.equal actual 自発移動制御種別.抑制 "抑制"

            testCase "反応状態なし理由を返す" <| fun _ ->
                let actual = 状態 10L 敵 |> 判断する 主体A |> 自発移動制御判断.理由
                Expect.equal actual 自発移動制御理由.反応状態なし "理由"

            testCase "その場警戒中理由を返す" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.理由
                Expect.equal actual 自発移動制御理由.その場警戒中 "理由"

            testCase "許可判断のboolアクセサ" <| fun _ ->
                let decision = 状態 10L 敵 |> 判断する 主体A
                Expect.isTrue (自発移動制御判断.自発移動を許可する decision) "許可"
                Expect.isFalse (自発移動制御判断.自発移動を抑制する decision) "非抑制"

            testCase "抑制判断のboolアクセサ" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let decision = state |> 判断する 主体A
                Expect.isFalse (自発移動制御判断.自発移動を許可する decision) "非許可"
                Expect.isTrue (自発移動制御判断.自発移動を抑制する decision) "抑制"

            testCase "許可判断の由来状態はNone" <| fun _ ->
                let actual = 状態 10L 敵 |> 判断する 主体A |> 自発移動制御判断.由来反応状態
                Expect.isNone actual "由来なし"

            testCase "抑制判断の由来状態はSome" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.由来反応状態
                Expect.equal actual (Some reaction) "由来状態"

            testCase "成立因果操作IDを由来状態から返す" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.由来成立因果操作ID
                Expect.equal actual (Some(エンティティ反応状態.成立因果操作ID reaction)) "因果ID"

            testCase "成立Tickを由来状態から返す" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.由来成立Tick
                Expect.equal actual (Some 10L) "成立Tick"

            testCase "反応要求IDを由来状態から返す" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.由来反応要求ID
                Expect.equal actual (Some(エンティティ反応状態.由来要求ID reaction)) "要求ID"

            testCase "行動候補IDを由来状態から返す" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.由来行動候補ID
                Expect.equal actual (Some(エンティティ反応状態.由来行動候補ID reaction)) "候補ID"

            testCase "対象仮説を由来状態から返す" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.由来対象仮説
                Expect.equal actual (Some "地盤崩落の危険") "仮説"

            testCase "優先度を由来状態から返す" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let actual = state |> 判断する 主体A |> 自発移動制御判断.由来優先度
                Expect.equal actual (Some 0.75) "優先度"

            testCase "空白対象EntityIDを拒否する" <| fun _ ->
                let reasons = 自発移動制御判断生成.ゲーム状態から作る (エンティティID "") (状態 10L 敵) |> エラーを得る
                Expect.equal reasons [ "自発移動制御対象EntityIDが空です。"; "自発移動制御対象Entityが存在しません。" ] "空ID"

            testCase "whitespace対象EntityIDを拒否する" <| fun _ ->
                let reasons = 自発移動制御判断生成.ゲーム状態から作る (エンティティID "  ") (状態 10L 敵) |> エラーを得る
                Expect.equal reasons [ "自発移動制御対象EntityIDが空です。"; "自発移動制御対象Entityが存在しません。" ] "空白ID"

            testCase "負ゲーム状態Tickを拒否する" <| fun _ ->
                let reasons = 自発移動制御判断生成.ゲーム状態から作る 主体A (状態 -1L 敵) |> エラーを得る
                Expect.equal reasons [ "自発移動制御判断のゲーム状態Tickが負です。" ] "負Tick"

            testCase "対象Entity不存在を拒否する" <| fun _ ->
                let reasons = 自発移動制御判断生成.ゲーム状態から作る 不在 (状態 10L 敵) |> エラーを得る
                Expect.equal reasons [ "自発移動制御対象Entityが存在しません。" ] "不存在"

            testCase "対象Entity重複を拒否する" <| fun _ ->
                let state = 状態 10L 敵
                let duplicate = { state with エンティティ一覧 = state.エンティティ一覧.Head :: state.エンティティ一覧 }
                let reasons = 自発移動制御判断生成.ゲーム状態から作る 主体A duplicate |> エラーを得る
                Expect.equal reasons [ "自発移動制御対象EntityIDがゲーム状態内で重複しています。" ] "Entity重複"

            testCase "反応状態一覧重複を拒否する" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let duplicate = { state with エンティティ反応状態一覧 = [ reaction; reaction ] }
                let reasons = 自発移動制御判断生成.ゲーム状態から作る 主体A duplicate |> エラーを得る
                Expect.equal reasons [ "エンティティ反応状態一覧内で実行者IDが重複しています。" ] "状態重複"

            testCase "未来成立Tickを拒否する" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let pastState = { state with Tick = 9L }
                let reasons = 自発移動制御判断生成.ゲーム状態から作る 主体A pastState |> エラーを得る
                Expect.equal reasons [ "エンティティ反応状態の成立Tickがゲーム状態Tickより後です。" ] "未来状態"

            testCase "入力エラーを固定順で返す" <| fun _ ->
                let reasons = 自発移動制御判断生成.ゲーム状態から作る (エンティティID " ") (状態 -1L 敵) |> エラーを得る
                Expect.equal reasons [ "自発移動制御対象EntityIDが空です。"; "自発移動制御判断のゲーム状態Tickが負です。"; "自発移動制御対象Entityが存在しません。" ] "固定順"

            testCase "複数の状態エラーを収集する" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let invalid =
                    {
                        state with
                            Tick = -1L
                            エンティティ一覧 = state.エンティティ一覧.Head :: state.エンティティ一覧
                            エンティティ反応状態一覧 = [ reaction; reaction ]
                    }
                let reasons = 自発移動制御判断生成.ゲーム状態から作る 主体A invalid |> エラーを得る
                Expect.equal reasons [ "自発移動制御判断のゲーム状態Tickが負です。"; "自発移動制御対象EntityIDがゲーム状態内で重複しています。"; "エンティティ反応状態一覧内で実行者IDが重複しています。" ] "全収集"

            testCase "状態なしは自発移動を許可する" <| fun _ ->
                let decision = 状態 0L 敵 |> 判断する 主体A
                Expect.isTrue (自発移動制御判断.自発移動を許可する decision) "許可"

            testCase "状態ありは自発移動を抑制する" <| fun _ ->
                let state, _ = 基本抑制状態 0L
                let decision = state |> 判断する 主体A
                Expect.isTrue (自発移動制御判断.自発移動を抑制する decision) "抑制"

            testCase "他Entityの警戒状態は対象へ影響しない" <| fun _ ->
                let state, _ = 他主体だけ警戒中 10L
                let decision = state |> 判断する 主体A
                Expect.equal (自発移動制御判断.種別 decision) 自発移動制御種別.許可 "Aは許可"

            testCase "二Entityを別々に判断できる" <| fun _ ->
                let state, _, _ = 二主体警戒中 10L
                Expect.equal (state |> 判断する 主体A |> 自発移動制御判断.種別) 自発移動制御種別.抑制 "A"
                Expect.equal (state |> 判断する 主体B |> 自発移動制御判断.種別) 自発移動制御種別.抑制 "B"

            testCase "異なる状態一覧順でも対象判断は同じ" <| fun _ ->
                let state, reactionA, reactionB = 二主体警戒中 10L
                let first = state |> 判断する 主体A
                let reordered = { state with エンティティ反応状態一覧 = [ reactionB; reactionA ] }
                let second = reordered |> 判断する 主体A
                Expect.equal second first "順序非依存"

            testCase "ゲームオーバーでも状態なしなら許可" <| fun _ ->
                let ended = { 状態 10L 敵 with 終了状態 = Some ゲームオーバー }
                Expect.equal (ended |> 判断する 主体A |> 自発移動制御判断.種別) 自発移動制御種別.許可 "終了状態は責務外"

            testCase "クリア後でも警戒中なら抑制" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let ended = { state with 終了状態 = Some クリア }
                Expect.equal (ended |> 判断する 主体A |> 自発移動制御判断.種別) 自発移動制御種別.抑制 "終了状態は責務外"

            testCase "プレイヤー種別を許可する" <| fun _ ->
                状態 10L プレイヤー |> 判断する 主体A |> ignore

            testCase "敵種別を許可する" <| fun _ ->
                状態 10L 敵 |> 判断する 主体A |> ignore

            testCase "弾種別を許可する" <| fun _ ->
                状態 10L 弾 |> 判断する 主体A |> ignore

            testCase "障害物種別を許可する" <| fun _ ->
                状態 10L 障害物 |> 判断する 主体A |> ignore

            testCase "所有者の違いで判断は変わらない" <| fun _ ->
                let baseState = 状態 10L 敵
                let decisions = [ プレイヤー側; 敵側; 中立 ] |> List.map (fun owner -> baseState |> 対象Entityを変更する (fun e -> { e with 所有者 = owner }) |> 判断する 主体A |> 自発移動制御判断.種別)
                Expect.equal decisions [ 自発移動制御種別.許可; 自発移動制御種別.許可; 自発移動制御種別.許可 ] "所有者非参照"

            testCase "位置の違いで判断は変わらない" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let moved = state |> 対象Entityを変更する (fun e -> { e with 位置 = 位置 999.0 -999.0 })
                Expect.equal (判断する 主体A moved |> 自発移動制御判断.種別) 自発移動制御種別.抑制 "位置非参照"

            testCase "速度の違いで判断は変わらない" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let moving = state |> 対象Entityを変更する (fun e -> { e with 速度 = { X = 123.0; Y = -456.0 } })
                Expect.equal (判断する 主体A moving |> 自発移動制御判断.種別) 自発移動制御種別.抑制 "速度非参照"

            testCase "HPの違いで判断は変わらない" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let variants = [ None; Some(HP 0); Some(HP 999) ] |> List.map (fun hp -> state |> 対象Entityを変更する (fun e -> { e with HP = hp }) |> 判断する 主体A |> 自発移動制御判断.種別)
                Expect.equal variants [ 自発移動制御種別.抑制; 自発移動制御種別.抑制; 自発移動制御種別.抑制 ] "HP非参照"

            testCase "当たり判定の違いで判断は変わらない" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let variants = [ None; Some(矩形(1.0, 1.0)); Some(矩形(100.0, 50.0)) ] |> List.map (fun collider -> state |> 対象Entityを変更する (fun e -> { e with 当たり判定 = collider }) |> 判断する 主体A |> 自発移動制御判断.種別)
                Expect.equal variants [ 自発移動制御種別.抑制; 自発移動制御種別.抑制; 自発移動制御種別.抑制 ] "当たり判定非参照"

            testCase "成立Tickと評価Tickを区別する" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let evaluated = { state with Tick = 25L } |> 判断する 主体A
                Expect.equal (自発移動制御判断.評価Tick evaluated) 25L "評価Tick"
                Expect.equal (自発移動制御判断.由来成立Tick evaluated) (Some 10L) "成立Tick"

            testCase "成立Tickと評価Tickが同一でも許可する" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                Expect.equal (state |> 判断する 主体A |> 自発移動制御判断.評価Tick) 10L "同一Tick"

            testCase "古い反応状態を後のTickで読める" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let later = { state with Tick = 100L }
                Expect.equal (later |> 判断する 主体A |> 自発移動制御判断.種別) 自発移動制御種別.抑制 "継続状態"

            testCase "後の評価でも要求IDを保持する" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let decision = { state with Tick = 25L } |> 判断する 主体A
                Expect.equal (自発移動制御判断.由来反応要求ID decision) (Some(エンティティ反応状態.由来要求ID reaction)) "要求ID"

            testCase "後の評価でも候補IDを保持する" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let decision = { state with Tick = 25L } |> 判断する 主体A
                Expect.equal (自発移動制御判断.由来行動候補ID decision) (Some(エンティティ反応状態.由来行動候補ID reaction)) "候補ID"

            testCase "後の評価でも仮説を保持する" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let decision = { state with Tick = 25L } |> 判断する 主体A
                Expect.equal (自発移動制御判断.由来対象仮説 decision) (Some "地盤崩落の危険") "仮説"

            testCase "後の評価でも優先度を保持する" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let decision = { state with Tick = 25L } |> 判断する 主体A
                Expect.equal (自発移動制御判断.由来優先度 decision) (Some 0.75) "優先度"

            testCase "入力ゲーム状態を変更しない" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let before = state
                state |> 判断する 主体A |> ignore
                Expect.equal state before "不変"

            testCase "同じ入力から同じ判断を返す" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let first = 判断する 主体A state
                let second = 判断する 主体A state
                Expect.equal second first "決定論"

            testCase "完全因果経路後は自発移動を抑制する" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal (自発移動制御判断.種別 path.Live判断) 自発移動制御種別.抑制 "完全経路"

            testCase "live適用前は自発移動を許可する" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal (path.初期状態 |> 判断する 主体A |> 自発移動制御判断.種別) 自発移動制御種別.許可 "適用前"

            testCase "live適用後は自発移動を抑制する" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.isTrue (自発移動制御判断.自発移動を抑制する path.Live判断) "live"

            testCase "Replay後は自発移動を抑制する" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.isTrue (自発移動制御判断.自発移動を抑制する path.Replay判断) "Replay"

            testCase "live判断とReplay判断が一致する" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal path.Replay判断 path.Live判断 "判断一致"

            testCase "判断生成後もEvent列は不変" <| fun _ ->
                let path = 完全因果経路を作る ()
                let before = path.Replay更新.イベント一覧
                path.Replay更新.状態 |> 判断する 主体A |> ignore
                Expect.equal path.Replay更新.イベント一覧 before "Event不変"

            testCase "判断生成後も統合台帳は不変" <| fun _ ->
                let path = 完全因果経路を作る ()
                let before = 統合因果台帳.記録一覧 path.台帳
                path.Replay更新.状態 |> 判断する 主体A |> ignore
                Expect.equal (統合因果台帳.記録一覧 path.台帳) before "台帳不変"

            testCase "判断生成後もEntity物理値は不変" <| fun _ ->
                let path = 完全因果経路を作る ()
                path.Live更新.状態 |> 判断する 主体A |> ignore
                Expect.equal path.Live更新.状態.エンティティ一覧 path.初期状態.エンティティ一覧 "物理不変"

            testCase "状態一覧の先頭が他主体でも対象だけを見る" <| fun _ ->
                let state, reactionA, reactionB = 二主体警戒中 10L
                let reordered = { state with エンティティ反応状態一覧 = [ reactionB; reactionA ] }
                Expect.equal (reordered |> 判断する 主体A |> 自発移動制御判断.由来反応状態) (Some reactionA) "対象検索"

            testCase "複数の他主体状態を対象へ集約しない" <| fun _ ->
                let stateB, reactionB = 他主体だけ警戒中 10L
                let requestC = 要求 "locomotion.request.actor.c" 10L 発生者 0.9 "発生者の危険"
                let updateC, recordC = 適用する stateB "locomotion.causal.actor.c" None [] requestC
                let decision = updateC.状態 |> 判断する 主体A
                Expect.equal (自発移動制御判断.種別 decision) 自発移動制御種別.許可 "全体集約なし"
                Expect.equal updateC.状態.エンティティ反応状態一覧 [ reactionB; recordC.変更後状態 ] "順序維持"

            testCase "他主体の未来成立状態は対象判断へ影響しない" <| fun _ ->
                let state, _ = 他主体だけ警戒中 10L
                let earlier = { state with Tick = 9L }
                Expect.equal (earlier |> 判断する 主体A |> 自発移動制御判断.種別) 自発移動制御種別.許可 "対象だけのTick検証"

            testCase "乱数Seedの違いで判断は変わらない" <| fun _ ->
                let state, _ = 基本抑制状態 10L
                let decisions = [ -1; 0; 12345; System.Int32.MaxValue ] |> List.map (fun seed -> { state with 乱数Seed = seed } |> 判断する 主体A)
                decisions |> List.tail |> List.iter (fun decision -> Expect.equal decision decisions.Head "Seed非参照")

            testCase "許可判断の全ProvenanceアクセサはNone" <| fun _ ->
                let decision = 状態 10L 敵 |> 判断する 主体A
                Expect.isNone (自発移動制御判断.由来成立因果操作ID decision) "因果"
                Expect.isNone (自発移動制御判断.由来成立Tick decision) "Tick"
                Expect.isNone (自発移動制御判断.由来反応要求ID decision) "要求"
                Expect.isNone (自発移動制御判断.由来行動候補ID decision) "候補"
                Expect.isNone (自発移動制御判断.由来対象仮説 decision) "仮説"
                Expect.isNone (自発移動制御判断.由来優先度 decision) "優先度"

            testCase "終了状態でも抑制判断のProvenanceを維持する" <| fun _ ->
                let state, reaction = 基本抑制状態 10L
                let decision = { state with 終了状態 = Some ゲームオーバー } |> 判断する 主体A
                Expect.equal (自発移動制御判断.由来成立因果操作ID decision) (Some(エンティティ反応状態.成立因果操作ID reaction)) "終了非干渉"

            testCase "完全因果経路のProvenanceを判断まで辿れる" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal path.感知.信号ID path.発生信号.ID "信号"
                Expect.equal path.候補.由来警戒意図 path.意図 "意図"
                Expect.equal (自発移動制御判断.由来反応要求ID path.Live判断) (Some(エンティティ反応要求.ID path.要求)) "要求"
                Expect.equal (自発移動制御判断.由来成立因果操作ID path.Live判断) (Some path.反応記録.因果操作ID) "反応因果"
                Expect.equal path.反応記録.原因因果ID (Some path.発生記録.因果操作ID) "原因因果"

            testCase "Replay信号とEvent列を判断生成が変えない" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal path.Replay信号一覧 [ path.発生信号 ] "信号"
                Expect.equal path.Replay更新.イベント一覧 (path.発生記録.発行Event一覧 @ path.反応記録.発行Event一覧) "Event"

            testCase "1152正常ケースMatrixは決定論的" <| fun _ ->
                let reactionModes = [ false; true ]
                let ticks = [ 0L; 10L; 25L; 100L ]
                let kinds = [ プレイヤー; 敵; 弾; 障害物 ]
                let owners = [ プレイヤー側; 敵側; 中立 ]
                let positions = [ 位置 0.0 0.0; 位置 100.0 -100.0 ]
                let velocities: 速度 list = [ { X = 0.0; Y = 0.0 }; { X = 9.0; Y = -3.0 } ]
                let endings = [ None; Some ゲームオーバー; Some クリア ]

                let combinations =
                    [
                        for hasReaction in reactionModes do
                            for tick in ticks do
                                for kind in kinds do
                                    for owner in owners do
                                        for position in positions do
                                            for velocity in velocities do
                                                for ending in endings do
                                                    yield hasReaction, tick, kind, owner, position, velocity, ending
                    ]

                Expect.equal combinations.Length 1152 "Matrix件数"

                combinations
                |> List.iteri (fun index (hasReaction, tick, kind, owner, position, velocity, ending) ->
                    let baseState = 状態 0L kind

                    let state =
                        if hasReaction then
                            let request = 要求 $"matrix.locomotion.request.{index}" 0L 主体A 0.5 $"Matrix仮説{index}"
                            let update, _ = 適用する baseState $"matrix.locomotion.causal.{index}" None [] request
                            update.状態
                        else
                            baseState

                    let input =
                        {
                            state with
                                Tick = tick
                                終了状態 = ending
                        }
                        |> 対象Entityを変更する (fun entity ->
                            {
                                entity with
                                    所有者 = owner
                                    位置 = position
                                    速度 = velocity
                            })

                    let before = input
                    let first = 判断する 主体A input
                    let second = 判断する 主体A input
                    Expect.equal second first $"case {index} deterministic"
                    Expect.equal input before $"case {index} input"

                    if hasReaction then
                        Expect.equal (自発移動制御判断.種別 first) 自発移動制御種別.抑制 $"case {index} suppressed"
                    else
                        Expect.equal (自発移動制御判断.種別 first) 自発移動制御種別.許可 $"case {index} allowed")

            testCase "未来成立Tick不正Matrixは決定論的" <| fun _ ->
                let cases =
                    [
                        for stateTick in [ 0L; 5L; 9L ] do
                            for reactionTick in [ 10L; 25L; 100L ] do
                                for kind in [ プレイヤー; 敵; 弾; 障害物 ] do
                                    yield stateTick, reactionTick, kind
                    ]

                Expect.equal cases.Length 36 "不正Matrix件数"

                cases
                |> List.iteri (fun index (stateTick, reactionTick, kind) ->
                    let futureState, _ = 反応状態を持つ状態 reactionTick kind 主体A 0.5 $"未来Matrix{index}"
                    let input = { futureState with Tick = stateTick }
                    let first = 自発移動制御判断生成.ゲーム状態から作る 主体A input
                    let second = 自発移動制御判断生成.ゲーム状態から作る 主体A input
                    Expect.equal second first $"case {index} deterministic"
                    Expect.equal first (Error [ "エンティティ反応状態の成立Tickがゲーム状態Tickより後です。" ]) $"case {index} future")
        ]
