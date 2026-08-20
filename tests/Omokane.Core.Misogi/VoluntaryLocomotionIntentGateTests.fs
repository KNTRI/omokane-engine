module VoluntaryLocomotionIntentGateTests

open Expecto
open Omokane.Core.Domain
open EntityReactionStateCausalTests.TestData

module private TestData =

    let 意図を得る = function
        | Ok 意図 -> 意図
        | Error エラー一覧 -> failtestf "意図生成失敗: %A" エラー一覧

    let 意図Optionを得る = function
        | Ok 意図 -> 意図
        | Error エラー一覧 -> failtestf "入力変換失敗: %A" エラー一覧

    let 意図エラーを得る = function
        | Error エラー一覧 -> エラー一覧
        | Ok 意図 -> failtestf "意図生成成功: %A" 意図

    let Gate結果を得る = function
        | Ok 結果 -> 結果
        | Error エラー一覧 -> failtestf "Gate失敗: %A" エラー一覧

    let Gateエラーを得る = function
        | Error エラー一覧 -> エラー一覧
        | Ok 結果 -> failtestf "Gate成功: %A" 結果

    let 一括結果を得る = function
        | Ok 結果一覧 -> 結果一覧
        | Error エラー一覧 -> failtestf "一括Gate失敗: %A" エラー一覧

    let 一括エラーを得る = function
        | Error エラー一覧 -> エラー一覧
        | Ok 結果一覧 -> failtestf "一括Gate成功: %A" 結果一覧

    let 明示意図 id tick actor direction =
        自発移動意図生成.方向から作る (自発移動意図ID id) tick actor direction
        |> 意図を得る

    let 入力意図 id tick actor input =
        自発移動意図生成.入力から作る (自発移動意図ID id) tick actor input
        |> 意図Optionを得る

    let 判断を得る actor state =
        match 自発移動制御判断生成.ゲーム状態から作る actor state with
        | Ok 判断 -> 判断
        | Error エラー一覧 -> failtestf "判断生成失敗: %A" エラー一覧

    let 警戒状態 tick kind =
        let initial = 状態 tick kind
        let request = 要求 $"intent.gate.request.{tick}.{kind}" tick 主体A 0.75 "地盤崩落の危険"
        let update, _ = 適用する initial $"intent.gate.causal.{tick}.{kind}" None [] request
        update.状態

    let 許可判断 tick kind =
        状態 tick kind |> 判断を得る 主体A

    let 抑制判断 tick kind =
        警戒状態 tick kind |> 判断を得る 主体A

    let 入力と方向 =
        [
            左へ移動, 自発移動方向.左
            右へ移動, 自発移動方向.右
            上へ移動, 自発移動方向.上
            下へ移動, 自発移動方向.下
        ]

    let 方向から入力 = function
        | 自発移動方向.左 -> 左へ移動
        | 自発移動方向.右 -> 右へ移動
        | 自発移動方向.上 -> 上へ移動
        | 自発移動方向.下 -> 下へ移動

    let 台帳成功を得る = function
        | 統合因果台帳追記結果.成功 ledger -> ledger
        | 統合因果台帳追記結果.失敗(_, index, id, reasons) ->
            failtestf "統合台帳追記失敗: %d %A %A" index id reasons

    let Replay成功を得る = function
        | 統合因果台帳再生結果.成功(update, signals) -> update, signals
        | 統合因果台帳再生結果.失敗(_, index, id, reasons) ->
            failtestf "統合Replay失敗: %d %A %A" index id reasons

    let 地盤振動操作: 地盤振動発生操作 =
        {
            因果 =
                {
                    ID = 因果操作ID "intent.gate.loop.vibration"
                    Tick = 10L
                    種別 = 伝播信号生成
                    原因因果ID = None
                    実行者ID = Some 発生者
                    対象EntityID = None
                    概要 = "自発移動意図Gateへ至る地盤振動"
                    発行Event一覧 = [ 衝突発生(発生者, 主体A) ]
                }
            信号ID = 伝播信号ID "intent.gate.loop.signal"
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
            信念: 信念候補
            警戒意図: 警戒意図
            候補: 行動候補
            要求: エンティティ反応要求
            発生記録: 地盤振動発生記録
            反応記録: エンティティ反応状態変更記録
            台帳: 統合因果台帳
            Live更新: 更新結果
            Replay更新: 更新結果
            Replay信号一覧: 伝播信号 list
            移動意図: 自発移動意図
            適用前Gate: 自発移動意図制御結果
            LiveGate: 自発移動意図制御結果
            ReplayGate: 自発移動意図制御結果
        }

    let 完全因果経路を作る () =
        let initial = 状態 10L 敵

        let signal, emissionRecord =
            match 地盤振動発生実行.適用する initial 地盤振動操作 with
            | 発生成功(_, signal, record) -> signal, record
            | 発生失敗(_, classification, reasons) ->
                failtestf "地盤振動発生失敗: %A %A" classification reasons

        let vibrationLedger =
            統合因果台帳.地盤振動発生記録一覧を追記する 統合因果台帳.空 [ emissionRecord ]
            |> 台帳成功を得る

        let replayBefore, replaySignals =
            統合因果台帳再生.再生する initial vibrationLedger
            |> Replay成功を得る

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

        let sensing, belief, alertIntent =
            match 地盤振動警戒経路.実行する sensingInput with
            | Ok(警戒成立(sensing, _, belief, alertIntent)) -> sensing, belief, alertIntent
            | result -> failtestf "認識経路失敗: %A" result

        let candidate =
            match 行動候補生成.警戒意図から作る (行動候補ID "intent.gate.loop.action") alertIntent with
            | Ok candidate -> candidate
            | Error reasons -> failtestf "行動候補生成失敗: %A" reasons

        let selected =
            [ candidate ]
            |> 行動候補選択.選ぶ
            |> 選択済みを得る

        let request =
            selected
            |> エンティティ反応要求生成.選択済み行動候補から作る
                (エンティティ反応要求ID "intent.gate.loop.request")
                replayBefore.状態.Tick
            |> 要求を得る

        let liveUpdate, reactionRecord =
            適用する
                initial
                "intent.gate.loop.reaction"
                (Some emissionRecord.因果操作ID)
                [ ダメージ発生(主体A, 0) ]
                request

        let fullLedger =
            統合因果台帳.エンティティ反応状態変更記録一覧を追記する vibrationLedger [ reactionRecord ]
            |> 台帳成功を得る

        let replayAfter, finalSignals =
            統合因果台帳再生.再生する initial fullLedger
            |> Replay成功を得る

        let moveIntent =
            入力意図 "intent.gate.loop.move" initial.Tick 主体A 右へ移動
            |> Option.defaultWith (fun () -> failtest "右移動意図を期待しました。")

        let beforeDecision = 判断を得る 主体A initial
        let liveDecision = 判断を得る 主体A liveUpdate.状態
        let replayDecision = 判断を得る 主体A replayAfter.状態

        {
            初期状態 = initial
            発生信号 = signal
            感知 = sensing
            信念 = belief
            警戒意図 = alertIntent
            候補 = candidate
            要求 = request
            発生記録 = emissionRecord
            反応記録 = reactionRecord
            台帳 = fullLedger
            Live更新 = liveUpdate
            Replay更新 = replayAfter
            Replay信号一覧 = finalSignals
            移動意図 = moveIntent
            適用前Gate = 自発移動意図制御.適用する beforeDecision moveIntent |> Gate結果を得る
            LiveGate = 自発移動意図制御.適用する liveDecision moveIntent |> Gate結果を得る
            ReplayGate = 自発移動意図制御.適用する replayDecision moveIntent |> Gate結果を得る
        }

open TestData

[<Tests>]
let 全テスト =
    testList
        "自発移動意図 v0.1と自発移動制御Gate"
        [
            testCase "左意図を生成できる" <| fun _ ->
                Expect.equal (明示意図 "intent.left" 10L 主体A 自発移動方向.左 |> 自発移動意図.方向) 自発移動方向.左 "左"

            testCase "右意図を生成できる" <| fun _ ->
                Expect.equal (明示意図 "intent.right" 10L 主体A 自発移動方向.右 |> 自発移動意図.方向) 自発移動方向.右 "右"

            testCase "上意図を生成できる" <| fun _ ->
                Expect.equal (明示意図 "intent.up" 10L 主体A 自発移動方向.上 |> 自発移動意図.方向) 自発移動方向.上 "上"

            testCase "下意図を生成できる" <| fun _ ->
                Expect.equal (明示意図 "intent.down" 10L 主体A 自発移動方向.下 |> 自発移動意図.方向) 自発移動方向.下 "下"

            testCase "明示IDを保存する" <| fun _ ->
                let actual = 明示意図 "intent.id" 10L 主体A 自発移動方向.右 |> 自発移動意図.ID
                Expect.equal actual (自発移動意図ID "intent.id") "ID"

            testCase "明示Tickを保存する" <| fun _ ->
                Expect.equal (明示意図 "intent.tick" 25L 主体A 自発移動方向.右 |> 自発移動意図.Tick) 25L "Tick"

            testCase "実行者IDを保存する" <| fun _ ->
                Expect.equal (明示意図 "intent.actor" 10L 主体B 自発移動方向.左 |> 自発移動意図.実行者ID) 主体B "実行者"

            testCase "方向を変更せず保存する" <| fun _ ->
                let directions = [ 自発移動方向.左; 自発移動方向.右; 自発移動方向.上; 自発移動方向.下 ]
                let actual = directions |> List.mapi (fun i direction -> 明示意図 $"intent.direction.{i}" 10L 主体A direction |> 自発移動意図.方向)
                Expect.equal actual directions "方向"

            testCase "明示指定由来を保存する" <| fun _ ->
                let actual = 明示意図 "intent.origin" 10L 主体A 自発移動方向.右 |> 自発移動意図.由来
                Expect.equal actual 自発移動意図由来.明示指定 "由来"

            testCase "明示指定boolを返す" <| fun _ ->
                let intent = 明示意図 "intent.explicit.bool" 10L 主体A 自発移動方向.右
                Expect.isTrue (自発移動意図.明示指定である intent) "明示"
                Expect.isFalse (自発移動意図.入力由来である intent) "非入力"

            testCase "明示指定の由来入力はNone" <| fun _ ->
                Expect.isNone (明示意図 "intent.explicit.none" 10L 主体A 自発移動方向.右 |> 自発移動意図.由来入力) "None"

            testCase "空意図IDを拒否する" <| fun _ ->
                let actual = 自発移動意図生成.方向から作る (自発移動意図ID "") 10L 主体A 自発移動方向.右 |> 意図エラーを得る
                Expect.equal actual [ "自発移動意図IDが空です。" ] "空ID"

            testCase "whitespace意図IDを拒否する" <| fun _ ->
                let actual = 自発移動意図生成.方向から作る (自発移動意図ID "  ") 10L 主体A 自発移動方向.右 |> 意図エラーを得る
                Expect.equal actual [ "自発移動意図IDが空です。" ] "空白ID"

            testCase "負Tickを拒否する" <| fun _ ->
                let actual = 自発移動意図生成.方向から作る (自発移動意図ID "intent.negative") -1L 主体A 自発移動方向.右 |> 意図エラーを得る
                Expect.equal actual [ "自発移動意図のTickが負です。" ] "負Tick"

            testCase "空実行者IDを拒否する" <| fun _ ->
                let actual = 自発移動意図生成.方向から作る (自発移動意図ID "intent.empty.actor") 10L (エンティティID " ") 自発移動方向.右 |> 意図エラーを得る
                Expect.equal actual [ "自発移動意図の実行者IDが空です。" ] "空主体"

            testCase "複数生成エラーを固定順で返す" <| fun _ ->
                let actual = 自発移動意図生成.方向から作る (自発移動意図ID "") -1L (エンティティID " ") 自発移動方向.右 |> 意図エラーを得る
                Expect.equal actual [ "自発移動意図IDが空です。"; "自発移動意図のTickが負です。"; "自発移動意図の実行者IDが空です。" ] "固定順"

            testCase "Tick 0を許可する" <| fun _ ->
                Expect.equal (明示意図 "intent.zero" 0L 主体A 自発移動方向.右 |> 自発移動意図.Tick) 0L "0"

            testCase "Tick Int64.MaxValueを許可する" <| fun _ ->
                Expect.equal (明示意図 "intent.max" System.Int64.MaxValue 主体A 自発移動方向.右 |> 自発移動意図.Tick) System.Int64.MaxValue "最大"

            testCase "Unicode意図IDを許可する" <| fun _ ->
                let id = 自発移動意図ID "自発移動.右.一"
                let actual = 自発移動意図生成.方向から作る id 10L 主体A 自発移動方向.右 |> 意図を得る |> 自発移動意図.ID
                Expect.equal actual id "Unicode"

            testCase "同じ明示入力から同じ意図" <| fun _ ->
                let first = 明示意図 "intent.same" 10L 主体A 自発移動方向.左
                let second = 明示意図 "intent.same" 10L 主体A 自発移動方向.左
                Expect.equal second first "決定論"

            testCase "左入力を左意図へ変換する" <| fun _ ->
                let actual = 入力意図 "input.left" 10L 主体A 左へ移動 |> Option.map 自発移動意図.方向
                Expect.equal actual (Some 自発移動方向.左) "左"

            testCase "右入力を右意図へ変換する" <| fun _ ->
                let actual = 入力意図 "input.right" 10L 主体A 右へ移動 |> Option.map 自発移動意図.方向
                Expect.equal actual (Some 自発移動方向.右) "右"

            testCase "上入力を上意図へ変換する" <| fun _ ->
                let actual = 入力意図 "input.up" 10L 主体A 上へ移動 |> Option.map 自発移動意図.方向
                Expect.equal actual (Some 自発移動方向.上) "上"

            testCase "下入力を下意図へ変換する" <| fun _ ->
                let actual = 入力意図 "input.down" 10L 主体A 下へ移動 |> Option.map 自発移動意図.方向
                Expect.equal actual (Some 自発移動方向.下) "下"

            testCase "ジャンプは意図なし" <| fun _ ->
                Expect.equal (入力意図 "input.jump" 10L 主体A ジャンプ) None "None"

            testCase "攻撃は意図なし" <| fun _ ->
                Expect.equal (入力意図 "input.attack" 10L 主体A 攻撃) None "None"

            testCase "入力なしは意図なし" <| fun _ ->
                Expect.equal (入力意図 "input.none" 10L 主体A 入力なし) None "None"

            testCase "入力由来caseを保存する" <| fun _ ->
                let intent = 入力意図 "input.origin" 10L 主体A 上へ移動 |> Option.get
                Expect.equal (自発移動意図.由来 intent) (自発移動意図由来.入力由来 上へ移動) "由来"

            testCase "由来入力を保存する" <| fun _ ->
                let intent = 入力意図 "input.source" 10L 主体A 下へ移動 |> Option.get
                Expect.equal (自発移動意図.由来入力 intent) (Some 下へ移動) "入力"

            testCase "移動入力の不正IDを拒否する" <| fun _ ->
                let actual = 自発移動意図生成.入力から作る (自発移動意図ID " ") 10L 主体A 右へ移動
                Expect.equal actual (Error [ "自発移動意図IDが空です。" ]) "ID"

            testCase "移動入力の負Tickを拒否する" <| fun _ ->
                let actual = 自発移動意図生成.入力から作る (自発移動意図ID "input.negative") -1L 主体A 左へ移動
                Expect.equal actual (Error [ "自発移動意図のTickが負です。" ]) "Tick"

            testCase "移動入力の空実行者を拒否する" <| fun _ ->
                let actual = 自発移動意図生成.入力から作る (自発移動意図ID "input.empty.actor") 10L (エンティティID "") 上へ移動
                Expect.equal actual (Error [ "自発移動意図の実行者IDが空です。" ]) "主体"

            testCase "非移動入力は不正IDでもNone" <| fun _ ->
                Expect.equal (自発移動意図生成.入力から作る (自発移動意図ID "") 10L 主体A 攻撃) (Ok None) "検証不要"

            testCase "非移動入力は負TickでもNone" <| fun _ ->
                Expect.equal (自発移動意図生成.入力から作る (自発移動意図ID "input.none.tick") -1L 主体A ジャンプ) (Ok None) "検証不要"

            testCase "非移動入力は空実行者でもNone" <| fun _ ->
                Expect.equal (自発移動意図生成.入力から作る (自発移動意図ID "input.none.actor") 10L (エンティティID "") 入力なし) (Ok None) "検証不要"

            testCase "入力値を変更しない" <| fun _ ->
                let input = 右へ移動
                let before = input
                入力意図 "input.immutable" 10L 主体A input |> ignore
                Expect.equal input before "不変"

            testCase "入力変換は決定論的" <| fun _ ->
                let first = 入力意図 "input.deterministic" 10L 主体A 左へ移動
                let second = 入力意図 "input.deterministic" 10L 主体A 左へ移動
                Expect.equal second first "決定論"

            testCase "許可判断で意図を通過する" <| fun _ ->
                let intent = 明示意図 "gate.allow" 10L 主体A 自発移動方向.右
                let result = 自発移動意図制御.適用する (許可判断 10L 敵) intent |> Gate結果を得る
                Expect.isTrue (自発移動意図制御結果.通過した result) "通過"

            testCase "抑制判断で意図を抑制する" <| fun _ ->
                let intent = 明示意図 "gate.suppress" 10L 主体A 自発移動方向.右
                let result = 自発移動意図制御.適用する (抑制判断 10L 敵) intent |> Gate結果を得る
                Expect.isTrue (自発移動意図制御結果.抑制された result) "抑制"

            testCase "通過boolは相補的" <| fun _ ->
                let result = 自発移動意図制御.適用する (許可判断 10L 敵) (明示意図 "gate.pass.bool" 10L 主体A 自発移動方向.左) |> Gate結果を得る
                Expect.isTrue (自発移動意図制御結果.通過した result) "通過"
                Expect.isFalse (自発移動意図制御結果.抑制された result) "非抑制"

            testCase "抑制boolは相補的" <| fun _ ->
                let result = 自発移動意図制御.適用する (抑制判断 10L 敵) (明示意図 "gate.stop.bool" 10L 主体A 自発移動方向.左) |> Gate結果を得る
                Expect.isFalse (自発移動意図制御結果.通過した result) "非通過"
                Expect.isTrue (自発移動意図制御結果.抑制された result) "抑制"

            testCase "通過意図はSome" <| fun _ ->
                let intent = 明示意図 "gate.pass.some" 10L 主体A 自発移動方向.上
                let result = 自発移動意図制御.適用する (許可判断 10L 敵) intent |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.通過意図 result) (Some intent) "Some"

            testCase "通過時の抑制意図はNone" <| fun _ ->
                let result = 自発移動意図制御.適用する (許可判断 10L 敵) (明示意図 "gate.pass.none" 10L 主体A 自発移動方向.上) |> Gate結果を得る
                Expect.isNone (自発移動意図制御結果.抑制意図 result) "None"

            testCase "抑制意図はSome" <| fun _ ->
                let intent = 明示意図 "gate.stop.some" 10L 主体A 自発移動方向.下
                let result = 自発移動意図制御.適用する (抑制判断 10L 敵) intent |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.抑制意図 result) (Some intent) "Some"

            testCase "抑制時の通過意図はNone" <| fun _ ->
                let result = 自発移動意図制御.適用する (抑制判断 10L 敵) (明示意図 "gate.stop.none" 10L 主体A 自発移動方向.下) |> Gate結果を得る
                Expect.isNone (自発移動意図制御結果.通過意図 result) "None"

            testCase "Gateは意図を構造的に保持する" <| fun _ ->
                let intent = 明示意図 "gate.intent" 10L 主体A 自発移動方向.右
                let result = 自発移動意図制御.適用する (抑制判断 10L 敵) intent |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.意図 result) intent "同一意図"

            testCase "Gateは判断を構造的に保持する" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let result = 自発移動意図制御.適用する decision (明示意図 "gate.decision" 10L 主体A 自発移動方向.右) |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.制御判断 result) decision "同一判断"

            testCase "制御理由を判断から保持する" <| fun _ ->
                let result = 自発移動意図制御.適用する (抑制判断 10L 敵) (明示意図 "gate.reason" 10L 主体A 自発移動方向.右) |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.制御理由 result) 自発移動制御理由.その場警戒中 "理由"

            testCase "由来反応状態を保持する" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let result = 自発移動意図制御.適用する decision (明示意図 "gate.reaction" 10L 主体A 自発移動方向.右) |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.由来反応状態 result) (自発移動制御判断.由来反応状態 decision) "状態"

            testCase "由来要求IDを保持する" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let result = 自発移動意図制御.適用する decision (明示意図 "gate.request" 10L 主体A 自発移動方向.右) |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.由来反応要求ID result) (自発移動制御判断.由来反応要求ID decision) "要求"

            testCase "由来成立因果IDを保持する" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let result = 自発移動意図制御.適用する decision (明示意図 "gate.causal" 10L 主体A 自発移動方向.右) |> Gate結果を得る
                Expect.equal (自発移動意図制御結果.由来反応状態成立因果操作ID result) (自発移動制御判断.由来成立因果操作ID decision) "因果"

            testCase "実行者不一致を拒否する" <| fun _ ->
                let errors = 自発移動意図制御.適用する (許可判断 10L 敵) (明示意図 "gate.actor.error" 10L 主体B 自発移動方向.右) |> Gateエラーを得る
                Expect.equal errors [ "自発移動意図の実行者IDが制御判断の実行者IDと一致しません。" ] "主体"

            testCase "Tick不一致を拒否する" <| fun _ ->
                let errors = 自発移動意図制御.適用する (許可判断 10L 敵) (明示意図 "gate.tick.error" 9L 主体A 自発移動方向.右) |> Gateエラーを得る
                Expect.equal errors [ "自発移動意図のTickが制御判断の評価Tickと一致しません。" ] "Tick"

            testCase "実行者とTick両不一致を固定順で返す" <| fun _ ->
                let errors = 自発移動意図制御.適用する (許可判断 10L 敵) (明示意図 "gate.both.error" 9L 主体B 自発移動方向.右) |> Gateエラーを得る
                Expect.equal errors [ "自発移動意図の実行者IDが制御判断の実行者IDと一致しません。"; "自発移動意図のTickが制御判断の評価Tickと一致しません。" ] "固定順"

            testCase "Gateは意図を変更しない" <| fun _ ->
                let intent = 明示意図 "gate.intent.immutable" 10L 主体A 自発移動方向.左
                let before = intent
                自発移動意図制御.適用する (許可判断 10L 敵) intent |> ignore
                Expect.equal intent before "不変"

            testCase "Gateは判断を変更しない" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let before = decision
                自発移動意図制御.適用する decision (明示意図 "gate.decision.immutable" 10L 主体A 自発移動方向.左) |> ignore
                Expect.equal decision before "不変"

            testCase "同じ入力から同じGate結果" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let intent = 明示意図 "gate.same" 10L 主体A 自発移動方向.左
                Expect.equal (自発移動意図制御.適用する decision intent) (自発移動意図制御.適用する decision intent) "決定論"

            testCase "空一覧の一括Gateは成功" <| fun _ ->
                Expect.equal (自発移動意図制御.一括適用する (許可判断 10L 敵) []) (Ok []) "空"

            testCase "一件一覧を通過する" <| fun _ ->
                let results = 自発移動意図制御.一括適用する (許可判断 10L 敵) [ 明示意図 "batch.one.pass" 10L 主体A 自発移動方向.右 ] |> 一括結果を得る
                Expect.isTrue (results.Head |> 自発移動意図制御結果.通過した) "通過"

            testCase "一件一覧を抑制する" <| fun _ ->
                let results = 自発移動意図制御.一括適用する (抑制判断 10L 敵) [ 明示意図 "batch.one.stop" 10L 主体A 自発移動方向.右 ] |> 一括結果を得る
                Expect.isTrue (results.Head |> 自発移動意図制御結果.抑制された) "抑制"

            testCase "複数意図を入力順で通過する" <| fun _ ->
                let intents = [ 明示意図 "batch.pass.1" 10L 主体A 自発移動方向.右; 明示意図 "batch.pass.2" 10L 主体A 自発移動方向.上; 明示意図 "batch.pass.3" 10L 主体A 自発移動方向.左 ]
                let actual = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る |> List.map 自発移動意図制御結果.意図
                Expect.equal actual intents "入力順"

            testCase "複数意図を入力順で抑制する" <| fun _ ->
                let intents = [ 明示意図 "batch.stop.1" 10L 主体A 自発移動方向.下; 明示意図 "batch.stop.2" 10L 主体A 自発移動方向.左 ]
                let results = 自発移動意図制御.一括適用する (抑制判断 10L 敵) intents |> 一括結果を得る
                Expect.equal (results |> List.map 自発移動意図制御結果.意図) intents "入力順"
                Expect.isTrue (results |> List.forall 自発移動意図制御結果.抑制された) "全抑制"

            testCase "同じ方向で異なるIDを許可する" <| fun _ ->
                let intents = [ 明示意図 "batch.same.direction.1" 10L 主体A 自発移動方向.右; 明示意図 "batch.same.direction.2" 10L 主体A 自発移動方向.右 ]
                Expect.equal (自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る |> List.length) 2 "別意図"

            testCase "異なる四方向を許可する" <| fun _ ->
                let intents = [ 自発移動方向.左; 自発移動方向.右; 自発移動方向.上; 自発移動方向.下 ] |> List.mapi (fun i d -> 明示意図 $"batch.direction.{i}" 10L 主体A d)
                Expect.equal (自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る |> List.length) 4 "四方向"

            testCase "重複意図IDを拒否する" <| fun _ ->
                let intents = [ 明示意図 "batch.duplicate" 10L 主体A 自発移動方向.右; 明示意図 "batch.duplicate" 10L 主体A 自発移動方向.左 ]
                let errors = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括エラーを得る
                Expect.equal errors [ "自発移動意図一覧内で自発移動意図IDが重複しています。" ] "重複"

            testCase "後方実行者不一致でも一括Gateは原子的失敗" <| fun _ ->
                let intents = [ 明示意図 "batch.actor.ok" 10L 主体A 自発移動方向.右; 明示意図 "batch.actor.bad" 10L 主体B 自発移動方向.上 ]
                let actual = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents
                Expect.equal actual (Error [ "自発移動意図[1]: 自発移動意図の実行者IDが制御判断の実行者IDと一致しません。" ]) "部分結果なし"

            testCase "後方Tick不一致でも一括Gateは原子的失敗" <| fun _ ->
                let intents = [ 明示意図 "batch.tick.ok" 10L 主体A 自発移動方向.右; 明示意図 "batch.tick.bad" 11L 主体A 自発移動方向.上 ]
                let actual = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents
                Expect.equal actual (Error [ "自発移動意図[1]: 自発移動意図のTickが制御判断の評価Tickと一致しません。" ]) "部分結果なし"

            testCase "複数indexエラーを固定順で返す" <| fun _ ->
                let intents = [ 明示意図 "batch.errors.0" 9L 主体B 自発移動方向.右; 明示意図 "batch.errors.1" 8L 発生者 自発移動方向.上 ]
                let errors = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括エラーを得る
                Expect.equal errors [ "自発移動意図[0]: 自発移動意図の実行者IDが制御判断の実行者IDと一致しません。"; "自発移動意図[1]: 自発移動意図の実行者IDが制御判断の実行者IDと一致しません。"; "自発移動意図[0]: 自発移動意図のTickが制御判断の評価Tickと一致しません。"; "自発移動意図[1]: 自発移動意図のTickが制御判断の評価Tickと一致しません。" ] "固定順"

            testCase "indexエラーをID重複より先に返す" <| fun _ ->
                let intents = [ 明示意図 "batch.order" 9L 主体B 自発移動方向.右; 明示意図 "batch.order" 10L 主体A 自発移動方向.上 ]
                let errors = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括エラーを得る
                Expect.equal errors [ "自発移動意図[0]: 自発移動意図の実行者IDが制御判断の実行者IDと一致しません。"; "自発移動意図[0]: 自発移動意図のTickが制御判断の評価Tickと一致しません。"; "自発移動意図一覧内で自発移動意図IDが重複しています。" ] "エラー順"

            testCase "一括結果件数は入力件数と一致" <| fun _ ->
                let intents = [ 0 .. 7 ] |> List.map (fun i -> 明示意図 $"batch.count.{i}" 10L 主体A 自発移動方向.右)
                Expect.equal (自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る |> List.length) intents.Length "件数"

            testCase "一括Gateは意図一覧をソートしない" <| fun _ ->
                let intents = [ 明示意図 "z.intent" 10L 主体A 自発移動方向.左; 明示意図 "a.intent" 10L 主体A 自発移動方向.右; 明示意図 "m.intent" 10L 主体A 自発移動方向.上 ]
                let actual = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る |> List.map (自発移動意図制御結果.意図 >> 自発移動意図.ID)
                Expect.equal actual [ 自発移動意図ID "z.intent"; 自発移動意図ID "a.intent"; 自発移動意図ID "m.intent" ] "非sort"

            testCase "一括Gateは内容重複を除去しない" <| fun _ ->
                let intents = [ 明示意図 "batch.content.1" 10L 主体A 自発移動方向.左; 明示意図 "batch.content.2" 10L 主体A 自発移動方向.左 ]
                let actual = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る |> List.map 自発移動意図制御結果.意図
                Expect.equal actual intents "重複保持"

            testCase "右と左を相殺しない" <| fun _ ->
                let intents = [ 明示意図 "batch.horizontal.right" 10L 主体A 自発移動方向.右; 明示意図 "batch.horizontal.left" 10L 主体A 自発移動方向.左 ]
                let actual = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る |> List.map (自発移動意図制御結果.意図 >> 自発移動意図.方向)
                Expect.equal actual [ 自発移動方向.右; 自発移動方向.左 ] "相殺なし"

            testCase "上と下を相殺しない" <| fun _ ->
                let intents = [ 明示意図 "batch.vertical.up" 10L 主体A 自発移動方向.上; 明示意図 "batch.vertical.down" 10L 主体A 自発移動方向.下 ]
                let actual = 自発移動意図制御.一括適用する (抑制判断 10L 敵) intents |> 一括結果を得る |> List.map (自発移動意図制御結果.意図 >> 自発移動意図.方向)
                Expect.equal actual [ 自発移動方向.上; 自発移動方向.下 ] "相殺なし"

            testCase "一括Gateは決定論的" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let intents = [ 0 .. 7 ] |> List.map (fun i -> 明示意図 $"batch.same.{i}" 10L 主体A ([ 自発移動方向.左; 自発移動方向.右; 自発移動方向.上; 自発移動方向.下 ].[i % 4]))
                Expect.equal (自発移動意図制御.一括適用する decision intents) (自発移動意図制御.一括適用する decision intents) "決定論"

            testCase "状態なしの右入力は通過する" <| fun _ ->
                let intent = 入力意図 "integration.right.allow" 10L 主体A 右へ移動 |> Option.get
                let result = 自発移動意図制御.適用する (許可判断 10L 敵) intent |> Gate結果を得る
                Expect.isTrue (自発移動意図制御結果.通過した result) "通過"

            testCase "警戒中の右入力は抑制される" <| fun _ ->
                let intent = 入力意図 "integration.right.suppress" 10L 主体A 右へ移動 |> Option.get
                let result = 自発移動意図制御.適用する (抑制判断 10L 敵) intent |> Gate結果を得る
                Expect.isTrue (自発移動意図制御結果.抑制された result) "抑制"

            testCase "状態なしの四方向はすべて通過" <| fun _ ->
                let decision = 許可判断 10L 敵
                let results = 入力と方向 |> List.mapi (fun i (input, _) -> 入力意図 $"integration.allow.{i}" 10L 主体A input |> Option.get |> 自発移動意図制御.適用する decision |> Gate結果を得る)
                Expect.isTrue (results |> List.forall 自発移動意図制御結果.通過した) "全通過"

            testCase "警戒中の四方向はすべて抑制" <| fun _ ->
                let decision = 抑制判断 10L 敵
                let results = 入力と方向 |> List.mapi (fun i (input, _) -> 入力意図 $"integration.suppress.{i}" 10L 主体A input |> Option.get |> 自発移動意図制御.適用する decision |> Gate結果を得る)
                Expect.isTrue (results |> List.forall 自発移動意図制御結果.抑制された) "全抑制"

            testCase "攻撃入力はGate対象を作らない" <| fun _ ->
                Expect.isNone (入力意図 "integration.attack" 10L 主体A 攻撃) "Gateを呼ばない"

            testCase "ジャンプ入力はGate対象を作らない" <| fun _ ->
                Expect.isNone (入力意図 "integration.jump" 10L 主体A ジャンプ) "Gateを呼ばない"

            testCase "入力なしはGate対象を作らない" <| fun _ ->
                Expect.isNone (入力意図 "integration.none" 10L 主体A 入力なし) "Gateを呼ばない"

            testCase "完全因果経路後は右意図を抑制" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.isTrue (自発移動意図制御結果.抑制された path.LiveGate) "完全経路"

            testCase "反応状態適用前は意図を通過" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.isTrue (自発移動意図制御結果.通過した path.適用前Gate) "適用前"

            testCase "反応状態適用後は意図を抑制" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.isTrue (自発移動意図制御結果.抑制された path.LiveGate) "適用後"

            testCase "Replay後も意図を抑制" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.isTrue (自発移動意図制御結果.抑制された path.ReplayGate) "Replay"

            testCase "live GateとReplay Gateが一致" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal path.ReplayGate path.LiveGate "一致"

            testCase "Gate後もGameStateは不変" <| fun _ ->
                let path = 完全因果経路を作る ()
                let before = path.Live更新.状態
                自発移動意図制御.適用する (判断を得る 主体A before) path.移動意図 |> ignore
                Expect.equal path.Live更新.状態 before "状態不変"

            testCase "Gate後もEvent列は不変" <| fun _ ->
                let path = 完全因果経路を作る ()
                let before = path.Replay更新.イベント一覧
                path.ReplayGate |> ignore
                Expect.equal path.Replay更新.イベント一覧 before "Event不変"

            testCase "Gate後も統合台帳は不変" <| fun _ ->
                let path = 完全因果経路を作る ()
                let before = 統合因果台帳.記録一覧 path.台帳
                path.LiveGate |> ignore
                Expect.equal (統合因果台帳.記録一覧 path.台帳) before "台帳不変"

            testCase "Gate後もEntity物理値は不変" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal path.Live更新.状態.エンティティ一覧 path.初期状態.エンティティ一覧 "位置速度HP不変"

            testCase "完全ProvenanceをGate結果まで保持" <| fun _ ->
                let path = 完全因果経路を作る ()
                Expect.equal path.感知.信号ID path.発生信号.ID "信号"
                Expect.equal path.候補.由来警戒意図 path.警戒意図 "警戒意図"
                Expect.equal path.警戒意図.由来信念候補 path.信念 "信念"
                Expect.equal (自発移動意図制御結果.由来反応要求ID path.LiveGate) (Some(エンティティ反応要求.ID path.要求)) "要求"
                Expect.equal (自発移動意図制御結果.由来反応状態成立因果操作ID path.LiveGate) (Some path.反応記録.因果操作ID) "反応因果"
                Expect.equal path.反応記録.原因因果ID (Some path.発生記録.因果操作ID) "発生因果"

            testCase "警戒状態で右上左の一括意図を全抑制" <| fun _ ->
                let intents = [ 右へ移動; 上へ移動; 左へ移動 ] |> List.mapi (fun i input -> 入力意図 $"integration.batch.stop.{i}" 10L 主体A input |> Option.get)
                let results = 自発移動意図制御.一括適用する (抑制判断 10L 敵) intents |> 一括結果を得る
                Expect.equal (results |> List.map 自発移動意図制御結果.意図) intents "順序"
                Expect.isTrue (results |> List.forall 自発移動意図制御結果.抑制された) "全抑制"

            testCase "状態なしで右上左の一括意図を全通過" <| fun _ ->
                let intents = [ 右へ移動; 上へ移動; 左へ移動 ] |> List.mapi (fun i input -> 入力意図 $"integration.batch.pass.{i}" 10L 主体A input |> Option.get)
                let results = 自発移動意図制御.一括適用する (許可判断 10L 敵) intents |> 一括結果を得る
                Expect.equal (results |> List.map 自発移動意図制御結果.意図) intents "順序"
                Expect.isTrue (results |> List.forall 自発移動意図制御結果.通過した) "全通過"

            testCase "Int64.MaxValueの同一TickをGateできる" <| fun _ ->
                let intent = 明示意図 "gate.max.tick" System.Int64.MaxValue 主体A 自発移動方向.右
                let result = 自発移動意図制御.適用する (許可判断 System.Int64.MaxValue 敵) intent |> Gate結果を得る
                Expect.isTrue (自発移動意図制御結果.通過した result) "最大Tick"

            testCase "Entity種別をGate適格性に使わない" <| fun _ ->
                let kinds = [ プレイヤー; 敵; 弾; 障害物 ]
                let results = kinds |> List.mapi (fun i kind -> 自発移動意図制御.適用する (許可判断 10L kind) (明示意図 $"gate.kind.{i}" 10L 主体A 自発移動方向.右) |> Gate結果を得る)
                Expect.isTrue (results |> List.forall 自発移動意図制御結果.通過した) "全種別"

            testCase "終了状態をGate規則に使わない" <| fun _ ->
                let states = [ None; Some ゲームオーバー; Some クリア ] |> List.map (fun ending -> { 状態 10L 敵 with 終了状態 = ending })
                let results = states |> List.mapi (fun i state -> 自発移動意図制御.適用する (判断を得る 主体A state) (明示意図 $"gate.ending.{i}" 10L 主体A 自発移動方向.右) |> Gate結果を得る)
                Expect.isTrue (results |> List.forall 自発移動意図制御結果.通過した) "終了非参照"

            testCase "3840正常ケースMatrixは決定論的" <| fun _ ->
                let origins = [ false; true ]
                let directions = [ 自発移動方向.左; 自発移動方向.右; 自発移動方向.上; 自発移動方向.下 ]
                let suppressedModes = [ false; true ]
                let ticks = [ 0L; 10L; 25L; 100L ]
                let kinds = [ プレイヤー; 敵; 弾; 障害物 ]
                let endings = [ None; Some ゲームオーバー; Some クリア ]
                let batchSizes = [ 0; 1; 2; 4; 8 ]

                let cases =
                    [
                        for inputOrigin in origins do
                            for direction in directions do
                                for suppressed in suppressedModes do
                                    for tick in ticks do
                                        for kind in kinds do
                                            for ending in endings do
                                                for batchSize in batchSizes do
                                                    yield inputOrigin, direction, suppressed, tick, kind, ending, batchSize
                    ]

                Expect.equal cases.Length 3840 "Matrix件数"

                cases
                |> List.iteri (fun caseIndex (inputOrigin, direction, suppressed, tick, kind, ending, batchSize) ->
                    let baseState = if suppressed then 警戒状態 tick kind else 状態 tick kind
                    let state = { baseState with 終了状態 = ending }
                    let before = state
                    let decision = 判断を得る 主体A state

                    let create index =
                        let id = $"matrix.intent.{caseIndex}.{index}"
                        if inputOrigin then
                            入力意図 id tick 主体A (方向から入力 direction) |> Option.get
                        else
                            明示意図 id tick 主体A direction

                    let firstIntents = [ 0 .. batchSize - 1 ] |> List.map create
                    let secondIntents = [ 0 .. batchSize - 1 ] |> List.map create
                    Expect.equal secondIntents firstIntents $"case {caseIndex} intent deterministic"

                    let first = 自発移動意図制御.一括適用する decision firstIntents
                    let second = 自発移動意図制御.一括適用する decision firstIntents
                    Expect.equal second first $"case {caseIndex} gate deterministic"

                    let results = first |> 一括結果を得る
                    Expect.equal (results |> List.map 自発移動意図制御結果.意図) firstIntents $"case {caseIndex} order"
                    Expect.equal state before $"case {caseIndex} state"

                    if suppressed then
                        Expect.isTrue (results |> List.forall 自発移動意図制御結果.抑制された) $"case {caseIndex} suppressed"
                    else
                        Expect.isTrue (results |> List.forall 自発移動意図制御結果.通過した) $"case {caseIndex} passed")

            testCase "88不正ケースMatrixは決定論的" <| fun _ ->
                let cases =
                    [
                        for direction in [ 自発移動方向.左; 自発移動方向.右; 自発移動方向.上; 自発移動方向.下 ] do
                            for tickOffset in [ -1L; 0L; 1L ] do
                                for actorMismatch in [ false; true ] do
                                    for duplicateID in [ false; true ] do
                                        for suppressed in [ false; true ] do
                                            if tickOffset <> 0L || actorMismatch || duplicateID then
                                                yield direction, tickOffset, actorMismatch, duplicateID, suppressed
                    ]

                Expect.equal cases.Length 88 "不正Matrix件数"

                cases
                |> List.iteri (fun index (direction, tickOffset, actorMismatch, duplicateID, suppressed) ->
                    let decision = if suppressed then 抑制判断 10L 敵 else 許可判断 10L 敵
                    let actor = if actorMismatch then 主体B else 主体A
                    let firstID = $"invalid.matrix.{index}"
                    let secondID = if duplicateID then firstID else $"{firstID}.second"
                    let intents = [ 明示意図 firstID (10L + tickOffset) actor direction; 明示意図 secondID 10L 主体A direction ]
                    let first = 自発移動意図制御.一括適用する decision intents
                    let second = 自発移動意図制御.一括適用する decision intents
                    Expect.equal second first $"case {index} deterministic")
        ]
