module EntityReactionStateCausalTests

open System
open Expecto
open Omokane.Core.Domain
open Omokane.Core.Systems

module TestData =

    let 主体A = エンティティID "reaction.state.actor.a"
    let 主体B = エンティティID "reaction.state.actor.b"
    let 発生者 = エンティティID "reaction.state.emitter"
    let 不在 = エンティティID "reaction.state.missing"

    let 位置 x y : 位置 = { X = x; Y = y }

    let entity 種別 ID x y : エンティティ =
        {
            ID = ID
            種別 = 種別
            所有者 = 中立
            位置 = 位置 x y
            速度 = { X = 0.75; Y = -0.25 }
            当たり判定 = Some(矩形(3.0, 4.0))
            HP = Some(HP 17)
        }

    let 状態 tick 主体種別 : ゲーム状態 =
        {
            Tick = tick
            プレイヤーID = 主体A
            エンティティ一覧 =
                [
                    entity 主体種別 主体A 1.0 2.0
                    entity 敵 主体B 3.0 4.0
                    entity 障害物 発生者 5.0 6.0
                ]
            エンティティ反応状態一覧 = []
            乱数Seed = 8642
            終了状態 = None
        }

    let 候補 id actor priority hypothesis tick senseKind : 行動候補 =
        let sensing: 感知値 =
            {
                観測者ID = actor
                信号ID = 伝播信号ID($"reaction.state.signal.{id}")
                種別 = senseKind
                測定値 = 1.0
                確信度 = 1.0
                Tick = tick
            }

        let observation: 観測 =
            {
                観測者ID = actor
                概要 = $"{hypothesis}の観測"
                確信度 = 1.0
                根拠一覧 = [ sensing ]
            }

        let belief: 信念候補 =
            {
                仮説 = hypothesis
                確率 = priority
                根拠一覧 = [ observation ]
                反証一覧 = []
            }

        let intent: 警戒意図 =
            {
                観測者ID = actor
                対象仮説 = hypothesis
                警戒度 = priority
                由来信念候補 = belief
            }

        {
            ID = 行動候補ID id
            実行者ID = actor
            種別 = 行動候補種別.その場警戒
            優先度 = priority
            対象仮説 = hypothesis
            由来警戒意図 = intent
        }

    let 選択済みを得る = function
        | Ok(Some value) -> value
        | Ok None -> failtest "選択済み候補を期待しました。"
        | Error reasons -> failtestf "候補選択失敗: %A" reasons

    let 要求を得る = function
        | Ok value -> value
        | Error reasons -> failtestf "要求生成失敗: %A" reasons

    let 操作を得る = function
        | Ok value -> value
        | Error reasons -> failtestf "操作生成失敗: %A" reasons

    let 適用成功を得る = function
        | エンティティ反応状態変更結果.成功(update, record) -> update, record
        | エンティティ反応状態変更結果.失敗(_, classification, reasons) ->
            failtestf "反応状態変更失敗: %A %A" classification reasons

    let 適用失敗を得る = function
        | エンティティ反応状態変更結果.失敗(state, classification, reasons) ->
            state, classification, reasons
        | エンティティ反応状態変更結果.成功(update, record) ->
            failtestf "反応状態変更成功: %A %A" update record

    let 要求 requestID tick actor priority hypothesis =
        [ 候補 ($"candidate.{requestID}") actor priority hypothesis tick 地盤感知 ]
        |> 行動候補選択.選ぶ
        |> 選択済みを得る
        |> エンティティ反応要求生成.選択済み行動候補から作る
            (エンティティ反応要求ID requestID)
            tick
        |> 要求を得る

    let 操作 operationID cause events request =
        request
        |> エンティティ反応状態変更操作生成.反応要求から作る
            (因果操作ID operationID)
            cause
            events
        |> 操作を得る

    let 適用する state operationID cause events request =
        操作 operationID cause events request
        |> エンティティ反応状態変更実行.適用する state
        |> 適用成功を得る

    let 反応状態 state actor =
        state
        |> ゲーム状態.エンティティ反応状態を探す actor
        |> function
            | Ok(Some value) -> value
            | Ok None -> failtest "反応状態を期待しました。"
            | Error reasons -> failtestf "反応状態検索失敗: %A" reasons

    let 基本要求 tick = 要求 "request.base" tick 主体A 0.75 "地盤が崩れる危険"

    let 基本適用 tick =
        let state = 状態 tick 敵
        let request = 基本要求 tick
        let update, record = 適用する state "causal.reaction.base" (Some(因果操作ID "causal.vibration.base")) [ Tick進行 tick ] request
        state, request, update, record

    let 別主体状態 tick =
        let state = 状態 tick 敵
        let request = 要求 "request.actor.b" tick 主体B 0.5 "別の危険"
        let update, record = 適用する state "causal.reaction.actor.b" None [] request
        update.状態, record.変更後状態

open TestData

[<Tests>]
let 全テスト =
    testList
        "エンティティ反応状態と因果適用"
        [
            testCase "状態が成立因果操作IDを保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.成立因果操作ID record.変更後状態) record.因果操作ID "成立因果ID"

            testCase "状態が成立Tickを保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.成立Tick record.変更後状態) 10L "成立Tick"

            testCase "状態が実行者IDを保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.実行者ID record.変更後状態) 主体A "実行者"

            testCase "状態が種別を保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.種別 record.変更後状態) エンティティ反応種別.その場警戒 "種別"

            testCase "状態が優先度を保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.優先度 record.変更後状態) 0.75 "優先度"

            testCase "状態が対象仮説を保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.対象仮説 record.変更後状態) "地盤が崩れる危険" "仮説"

            testCase "状態が由来要求を保持する" <| fun _ ->
                let _, request, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.由来要求 record.変更後状態) request "要求"

            testCase "状態が由来要求IDを保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.由来要求ID record.変更後状態) (エンティティ反応要求ID "request.base") "要求ID"

            testCase "状態が由来候補を保持する" <| fun _ ->
                let _, request, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.由来行動候補 record.変更後状態) (エンティティ反応要求.由来行動候補 request) "候補"

            testCase "状態が由来候補IDを保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.由来行動候補ID record.変更後状態) (行動候補ID "candidate.request.base") "候補ID"

            testCase "状態が選択位置と選択理由を保持する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.由来選択位置 record.変更後状態) 0 "位置"
                Expect.equal (エンティティ反応状態.由来選択理由 record.変更後状態) 行動候補選択理由.単独最高優先度 "理由"

            testCase "空状態一覧は妥当" <| fun _ ->
                Expect.equal (エンティティ反応状態一覧.検証する []) (Ok ()) "空一覧"

            testCase "一Entity一状態は妥当" <| fun _ ->
                let _, _, update, _ = 基本適用 10L
                Expect.equal (エンティティ反応状態一覧.検証する update.状態.エンティティ反応状態一覧) (Ok ()) "一件"

            testCase "実行者ID重複を拒否する" <| fun _ ->
                let _, _, _, first = 基本適用 10L
                let secondRequest = 要求 "request.duplicate" 10L 主体A 0.5 "別仮説"
                let _, second = 適用する (状態 10L 敵) "causal.duplicate" None [] secondRequest
                let actual = エンティティ反応状態一覧.検証する [ first.変更後状態; second.変更後状態 ]
                Expect.equal actual (Error [ "エンティティ反応状態一覧内で実行者IDが重複しています。" ]) "重複"

            testCase "ID検索0件はNone" <| fun _ ->
                Expect.equal (エンティティ反応状態一覧.実行者IDで探す 主体A []) (Ok None) "0件"

            testCase "ID検索1件はSome" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態一覧.実行者IDで探す 主体A [ record.変更後状態 ]) (Ok(Some record.変更後状態)) "1件"

            testCase "ID検索重複はError" <| fun _ ->
                let _, _, _, first = 基本適用 10L
                let actual = エンティティ反応状態一覧.実行者IDで探す 主体A [ first.変更後状態; first.変更後状態 ]
                Expect.equal actual (Error [ "エンティティ反応状態一覧内で実行者IDが重複しています。" ]) "2件"

            testCase "思兼神初期状態は空一覧" <| fun _ ->
                Expect.isEmpty (思兼神.初期状態を作る ()).エンティティ反応状態一覧 "初期値"

            testCase "Tickだけ進めるは反応状態を維持する" <| fun _ ->
                let _, _, update, _ = 基本適用 10L
                let next = 思兼神.Tickだけ進める update.状態
                Expect.equal next.状態.エンティティ反応状態一覧 update.状態.エンティティ反応状態一覧 "維持"

            testCase "Movement更新は反応状態を維持する" <| fun _ ->
                let baseState = 思兼神.初期状態を作る ()
                let request = 要求 "request.movement" 0L baseState.プレイヤーID 0.8 "足元の危険"
                let withReaction, _ = 適用する baseState "causal.reaction.movement" None [] request
                let moved = 思兼神.更新する [ 右へ移動 ] withReaction.状態
                Expect.equal moved.状態.エンティティ反応状態一覧 withReaction.状態.エンティティ反応状態一覧 "Movement非接続"
                Expect.equal moved.状態.エンティティ一覧.Head.位置.X 1.0 "既存Movementを抑制しない"

            testCase "既存位置変更実行は反応状態を維持する" <| fun _ ->
                let _, _, withReaction, _ = 基本適用 10L
                let operation: エンティティ位置変更操作 =
                    {
                        因果 =
                            {
                                ID = 因果操作ID "causal.position.compat"
                                Tick = 10L
                                種別 = 状態変更
                                原因因果ID = None
                                実行者ID = Some 主体A
                                対象EntityID = Some 主体A
                                概要 = "位置変更"
                                発行Event一覧 = []
                            }
                        変更後位置 = 位置 9.0 9.0
                    }
                match 因果操作実行.適用する withReaction.状態 operation with
                | 適用成功(update, _) -> Expect.equal update.状態.エンティティ反応状態一覧 withReaction.状態.エンティティ反応状態一覧 "維持"
                | result -> failtestf "位置変更失敗: %A" result

            testCase "地盤振動発生実行は反応状態を維持する" <| fun _ ->
                let _, _, withReaction, _ = 基本適用 10L
                let vibration: 地盤振動発生操作 =
                    {
                        因果 =
                            {
                                ID = 因果操作ID "causal.vibration.compat"
                                Tick = 10L
                                種別 = 伝播信号生成
                                原因因果ID = None
                                実行者ID = Some 発生者
                                対象EntityID = None
                                概要 = "地盤振動"
                                発行Event一覧 = []
                            }
                        信号ID = 伝播信号ID "signal.compat"
                        発生位置 = 位置 5.0 6.0
                        方向 = None
                        強度 = 1.0
                        方式 = 波動
                        寿命Tick = 1L
                    }
                match 地盤振動発生実行.適用する withReaction.状態 vibration with
                | 発生成功(update, _, _) -> Expect.equal update.状態.エンティティ反応状態一覧 withReaction.状態.エンティティ反応状態一覧 "維持"
                | result -> failtestf "振動発生失敗: %A" result

            testCase "既存位置Replayは反応状態を維持する" <| fun _ ->
                let _, _, withReaction, _ = 基本適用 10L
                let candidate: 因果記録候補 =
                    {
                        因果操作ID = 因果操作ID "causal.replay.compat"
                        Tick = 10L
                        成功 = true
                        実行者ID = Some 主体A
                        対象EntityID = Some 主体A
                        概要 = "位置変更"
                        変更前位置 = Some(位置 1.0 2.0)
                        変更後位置 = Some(位置 2.0 3.0)
                        発行Event一覧 = []
                        失敗理由一覧 = []
                    }
                let ledger =
                    match 因果台帳.追記する 因果台帳.空 [ candidate ] with
                    | 追記成功 value -> value
                    | result -> failtestf "台帳追記失敗: %A" result
                match 因果台帳再生.再生する withReaction.状態 ledger with
                | 再生成功 update -> Expect.equal update.状態.エンティティ反応状態一覧 withReaction.状態.エンティティ反応状態一覧 "維持"
                | result -> failtestf "Replay失敗: %A" result

            testCase "正常な要求から操作生成できる" <| fun _ ->
                基本要求 10L |> 操作 "causal.operation.normal" None [] |> ignore

            testCase "明示因果操作IDを保存する" <| fun _ ->
                let operation = 基本要求 10L |> 操作 "causal.operation.explicit" None []
                Expect.equal (エンティティ反応状態変更操作.因果 operation).ID (因果操作ID "causal.operation.explicit") "ID"

            testCase "要求Tickを因果Tickへ保存する" <| fun _ ->
                let operation = 基本要求 25L |> 操作 "causal.operation.tick" None []
                Expect.equal (エンティティ反応状態変更操作.因果 operation).Tick 25L "Tick"

            testCase "操作種別が状態変更" <| fun _ ->
                let operation = 基本要求 10L |> 操作 "causal.operation.kind" None []
                Expect.equal (エンティティ反応状態変更操作.因果 operation).種別 状態変更 "種別"

            testCase "操作実行者IDが要求実行者" <| fun _ ->
                let operation = 基本要求 10L |> 操作 "causal.operation.actor" None []
                Expect.equal (エンティティ反応状態変更操作.因果 operation).実行者ID (Some 主体A) "実行者"

            testCase "操作対象EntityIDが要求実行者" <| fun _ ->
                let operation = 基本要求 10L |> 操作 "causal.operation.target" None []
                Expect.equal (エンティティ反応状態変更操作.因果 operation).対象EntityID (Some 主体A) "対象"

            testCase "原因因果IDを保存する" <| fun _ ->
                let cause = Some(因果操作ID "causal.source")
                let operation = 基本要求 10L |> 操作 "causal.operation.cause" cause []
                Expect.equal (エンティティ反応状態変更操作.因果 operation).原因因果ID cause "原因"

            testCase "Event順を保存する" <| fun _ ->
                let events = [ Tick進行 10L; ダメージ発生(主体A, 2) ]
                let operation = 基本要求 10L |> 操作 "causal.operation.events" None events
                Expect.equal (エンティティ反応状態変更操作.因果 operation).発行Event一覧 events "順序"

            testCase "固定概要を保存する" <| fun _ ->
                let operation = 基本要求 10L |> 操作 "causal.operation.summary" None []
                Expect.equal (エンティティ反応状態変更操作.因果 operation).概要 "エンティティ反応状態をその場警戒へ変更する" "概要"

            testCase "操作が要求を再構築しない" <| fun _ ->
                let request = 基本要求 10L
                let operation = 操作 "causal.operation.provenance" None [] request
                Expect.equal (エンティティ反応状態変更操作.要求 operation) request "要求"
                Expect.isTrue (Object.ReferenceEquals(エンティティ反応状態変更操作.要求 operation, request)) "同じ値"

            testCase "空因果操作IDを拒否する" <| fun _ ->
                let actual = エンティティ反応状態変更操作生成.反応要求から作る (因果操作ID "") None [] (基本要求 10L)
                Expect.equal actual (Error [ "因果操作IDが空です。" ]) "空ID"

            testCase "空白原因因果IDを拒否する" <| fun _ ->
                let actual = エンティティ反応状態変更操作生成.反応要求から作る (因果操作ID "causal.valid") (Some(因果操作ID " ")) [] (基本要求 10L)
                Expect.equal actual (Error [ "原因因果IDが空です。" ]) "空原因"

            testCase "自己原因を拒否する" <| fun _ ->
                let id = 因果操作ID "causal.self"
                let actual = エンティティ反応状態変更操作生成.反応要求から作る id (Some id) [] (基本要求 10L)
                Expect.equal actual (Error [ "因果操作IDと原因因果IDを同じにできません。" ]) "自己参照"

            testCase "操作生成IDエラー順を固定する" <| fun _ ->
                let id = 因果操作ID " "
                let actual = エンティティ反応状態変更操作生成.反応要求から作る id (Some id) [] (基本要求 10L)
                Expect.equal actual (Error [ "因果操作IDが空です。"; "原因因果IDが空です。"; "因果操作IDと原因因果IDを同じにできません。" ]) "順序"

            testCase "正常に反応状態を追加する" <| fun _ ->
                let _, _, update, _ = 基本適用 10L
                Expect.equal update.状態.エンティティ反応状態一覧.Length 1 "一件追加"

            testCase "live適用はゲーム状態Tick不変" <| fun _ ->
                let state, _, update, _ = 基本適用 10L
                Expect.equal update.状態.Tick state.Tick "Tick"

            testCase "live適用はEntity一覧不変" <| fun _ ->
                let state, _, update, _ = 基本適用 10L
                Expect.equal update.状態.エンティティ一覧 state.エンティティ一覧 "Entity"

            testCase "live適用は位置不変" <| fun _ ->
                let state, _, update, _ = 基本適用 10L
                Expect.equal (update.状態.エンティティ一覧 |> List.map (fun e -> e.位置)) (state.エンティティ一覧 |> List.map (fun e -> e.位置)) "位置"

            testCase "live適用は速度不変" <| fun _ ->
                let state, _, update, _ = 基本適用 10L
                Expect.equal (update.状態.エンティティ一覧 |> List.map (fun e -> e.速度)) (state.エンティティ一覧 |> List.map (fun e -> e.速度)) "速度"

            testCase "live適用はHP不変" <| fun _ ->
                let state, _, update, _ = 基本適用 10L
                Expect.equal (update.状態.エンティティ一覧 |> List.map (fun e -> e.HP)) (state.エンティティ一覧 |> List.map (fun e -> e.HP)) "HP"

            testCase "live適用は乱数Seed不変" <| fun _ ->
                let state, _, update, _ = 基本適用 10L
                Expect.equal update.状態.乱数Seed state.乱数Seed "Seed"

            testCase "live適用は終了状態不変" <| fun _ ->
                let state, _, update, _ = 基本適用 10L
                Expect.equal update.状態.終了状態 state.終了状態 "終了"

            testCase "live適用はEventをそのまま返す" <| fun _ ->
                let _, _, update, record = 基本適用 10L
                Expect.equal update.イベント一覧 [ Tick進行 10L ] "更新Event"
                Expect.equal record.発行Event一覧 update.イベント一覧 "記録Event"

            testCase "live適用は成功記録を返す" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal record.因果操作ID (因果操作ID "causal.reaction.base") "記録"

            testCase "初回変更前状態はNone" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.isNone record.変更前状態 "初回"

            testCase "変更後状態は成立因果IDを保持" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.成立因果操作ID record.変更後状態) record.因果操作ID "一致"

            testCase "変更後状態は由来要求を保持" <| fun _ ->
                let _, request, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態.由来要求 record.変更後状態) request "一致"

            testCase "Tick不一致を拒否する" <| fun _ ->
                let state = 状態 11L 敵
                let operation = 基本要求 10L |> 操作 "causal.tick.failure" None []
                let returned, classification, reasons = エンティティ反応状態変更実行.適用する state operation |> 適用失敗を得る
                Expect.equal classification ゲーム内失敗 "分類"
                Expect.contains reasons "エンティティ反応状態変更操作のTickが現在状態のTickと一致しません。" "理由"
                Expect.equal returned state "原子性"

            testCase "終了状態を拒否する" <| fun _ ->
                let state = { 状態 10L 敵 with 終了状態 = Some クリア }
                let operation = 基本要求 10L |> 操作 "causal.finished.failure" None []
                let _, classification, reasons = エンティティ反応状態変更実行.適用する state operation |> 適用失敗を得る
                Expect.equal classification ゲーム内失敗 "分類"
                Expect.contains reasons "終了状態のためエンティティ反応状態を変更できません。" "理由"

            testCase "対象Entity不存在を拒否する" <| fun _ ->
                let request = 要求 "request.missing" 10L 不在 0.5 "不在主体"
                let operation = 操作 "causal.missing.failure" None [] request
                let _, _, reasons = エンティティ反応状態変更実行.適用する (状態 10L 敵) operation |> 適用失敗を得る
                Expect.contains reasons "反応対象Entityが存在しません。" "不存在"

            testCase "対象Entity重複を拒否する" <| fun _ ->
                let state = 状態 10L 敵
                let duplicate = state.エンティティ一覧.Head
                let state = { state with エンティティ一覧 = duplicate :: state.エンティティ一覧 }
                let operation = 基本要求 10L |> 操作 "causal.entity.duplicate" None []
                let _, _, reasons = エンティティ反応状態変更実行.適用する state operation |> 適用失敗を得る
                Expect.contains reasons "反応対象EntityIDがゲーム状態内で重複しています。" "Entity重複"

            testCase "反応状態重複を拒否する" <| fun _ ->
                let _, _, _, first = 基本適用 10L
                let secondRequest = 要求 "request.state.duplicate" 10L 主体A 0.6 "重複"
                let _, second = 適用する (状態 10L 敵) "causal.state.duplicate.other" None [] secondRequest
                let state = { 状態 10L 敵 with エンティティ反応状態一覧 = [ first.変更後状態; second.変更後状態 ] }
                let operation = 基本要求 10L |> 操作 "causal.state.duplicate" None []
                let _, _, reasons = エンティティ反応状態変更実行.適用する state operation |> 適用失敗を得る
                Expect.contains reasons "エンティティ反応状態一覧内で実行者IDが重複しています。" "状態重複"

            yield!
                [ プレイヤー; 敵; 弾; 障害物 ]
                |> List.mapi (fun index kind ->
                    testCase $"Entity種別{kind}を型上許可する" <| fun _ ->
                        let state = 状態 10L kind
                        let request = 要求 $"request.kind.{index}" 10L 主体A 0.5 "種別非依存"
                        let update, _ = 適用する state $"causal.kind.{index}" None [] request
                        Expect.equal update.状態.エンティティ反応状態一覧.Length 1 "種別を検査しない")

            testCase "同じ状態再適用を許可する" <| fun _ ->
                let _, request, firstUpdate, firstRecord = 基本適用 10L
                let secondUpdate, secondRecord = 適用する firstUpdate.状態 "causal.reaction.second" None [] request
                Expect.equal secondUpdate.状態.エンティティ反応状態一覧.Length 1 "一件"
                Expect.equal secondRecord.変更前状態 (Some firstRecord.変更後状態) "前状態"

            testCase "既存状態を同じlist位置で置換する" <| fun _ ->
                let stateWithB, stateB = 別主体状態 10L
                let firstRequest = 基本要求 10L
                let firstUpdate, _ = 適用する stateWithB "causal.order.a.first" None [] firstRequest
                let beforeIDs = firstUpdate.状態.エンティティ反応状態一覧 |> List.map エンティティ反応状態.実行者ID
                let secondRequest = 要求 "request.order.a.second" 10L 主体A 0.9 "新仮説"
                let secondUpdate, _ = 適用する firstUpdate.状態 "causal.order.a.second" None [] secondRequest
                let afterIDs = secondUpdate.状態.エンティティ反応状態一覧 |> List.map エンティティ反応状態.実行者ID
                Expect.equal afterIDs beforeIDs "位置維持"
                Expect.equal secondUpdate.状態.エンティティ反応状態一覧.Head stateB "他主体先頭維持"

            testCase "状態なしなら一覧末尾へ追加する" <| fun _ ->
                let stateWithB, stateB = 別主体状態 10L
                let update, _ = 適用する stateWithB "causal.append.a" None [] (基本要求 10L)
                Expect.equal update.状態.エンティティ反応状態一覧 [ stateB; update.状態.エンティティ反応状態一覧.[1] ] "末尾"

            testCase "入力状態を変更しない" <| fun _ ->
                let state = 状態 10L 敵
                let before = state
                let _ = 適用する state "causal.input.immutable" None [] (基本要求 10L)
                Expect.equal state before "入力不変"

            testCase "同じ入力から同じlive結果" <| fun _ ->
                let state = 状態 10L 敵
                let operation = 基本要求 10L |> 操作 "causal.deterministic" None [ Tick進行 10L ]
                Expect.equal (エンティティ反応状態変更実行.適用する state operation) (エンティティ反応状態変更実行.適用する state operation) "決定論"

            testCase "正常記録が公開検証を通る" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                Expect.equal (エンティティ反応状態変更記録.検証する record) (Ok ()) "正常"

            testCase "記録の因果ID不一致を拒否する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                let invalid = { record with 因果操作ID = 因果操作ID "causal.other" }
                match エンティティ反応状態変更記録.検証する invalid with
                | Error reasons -> Expect.contains reasons "エンティティ反応状態変更記録の変更後状態.成立因果操作IDが因果操作IDと一致しません。" "不一致"
                | Ok () -> failtest "不正記録が通過しました。"

            testCase "記録のTick不一致を拒否する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                let invalid = { record with Tick = 11L }
                match エンティティ反応状態変更記録.検証する invalid with
                | Error reasons -> Expect.contains reasons "エンティティ反応状態変更記録の変更後状態.成立TickがTickと一致しません。" "不一致"
                | Ok () -> failtest "不正記録が通過しました。"

            testCase "記録の実行者不一致を拒否する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                let invalid = { record with 実行者ID = Some 主体B }
                match エンティティ反応状態変更記録.検証する invalid with
                | Error reasons -> Expect.contains reasons "エンティティ反応状態変更記録の実行者IDが対象EntityIDと一致しません。" "不一致"
                | Ok () -> failtest "不正記録が通過しました。"

            testCase "記録の対象不一致を拒否する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                let invalid = { record with 対象EntityID = 主体B }
                match エンティティ反応状態変更記録.検証する invalid with
                | Error reasons -> Expect.contains reasons "エンティティ反応状態変更記録の変更後状態.実行者IDが対象EntityIDと一致しません。" "不一致"
                | Ok () -> failtest "不正記録が通過しました。"

            testCase "記録の変更後状態要求不一致を拒否する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                let otherRequest = 要求 "request.record.other" 10L 主体A 0.5 "他要求"
                let _, otherRecord = 適用する (状態 10L 敵) "causal.record.other" None [] otherRequest
                let invalid = { record with 変更後状態 = otherRecord.変更後状態 }
                match エンティティ反応状態変更記録.検証する invalid with
                | Error reasons -> Expect.contains reasons "エンティティ反応状態変更記録の変更後状態.由来要求が由来要求と一致しません。" "不一致"
                | Ok () -> failtest "不正記録が通過しました。"

            testCase "記録の変更前状態実行者不一致を拒否する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                let _, stateB = 別主体状態 10L
                let invalid = { record with 変更前状態 = Some stateB }
                match エンティティ反応状態変更記録.検証する invalid with
                | Error reasons -> Expect.contains reasons "エンティティ反応状態変更記録の変更前状態.実行者IDが対象EntityIDと一致しません。" "不一致"
                | Ok () -> failtest "不正記録が通過しました。"

            testCase "記録の複数エラー順を固定する" <| fun _ ->
                let _, _, _, record = 基本適用 10L
                let invalid =
                    {
                        record with
                            因果操作ID = 因果操作ID " "
                            原因因果ID = Some(因果操作ID " ")
                            Tick = -1L
                            実行者ID = None
                            対象EntityID = エンティティID " "
                            概要 = " "
                    }
                match エンティティ反応状態変更記録.検証する invalid with
                | Error reasons ->
                    Expect.equal reasons.[0] "エンティティ反応状態変更記録の因果操作IDが空です。" "因果ID先頭"
                    Expect.equal reasons.[1] "エンティティ反応状態変更記録の原因因果IDが空です。" "原因次"
                    Expect.equal reasons.[2] "エンティティ反応状態変更記録の因果操作IDと原因因果IDを同じにできません。" "自己参照次"
                    Expect.equal reasons.[3] "エンティティ反応状態変更記録のTickが負です。" "Tick次"
                | Ok () -> failtest "不正記録が通過しました。"
        ]
