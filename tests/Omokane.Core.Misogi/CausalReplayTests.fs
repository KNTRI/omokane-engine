module CausalReplayTests

open Expecto
open Omokane.Core.Domain

let private プレイヤーID = エンティティID "player"
let private 対象A = エンティティID "target.a"
let private 対象B = エンティティID "target.b"
let private 存在しないID = エンティティID "missing"

let private 位置を作る x y : 位置 =
    { X = x; Y = y }

let private entityを作る id x y speedX speedY hp : エンティティ =
    {
        ID = id
        種別 = 敵
        所有者 = 中立
        位置 = 位置を作る x y
        速度 = { X = speedX; Y = speedY }
        当たり判定 = Some(矩形(2.0, 3.0))
        HP = Some(HP hp)
    }

let private 状態を作る () =
    {
        Tick = 10L
        プレイヤーID = プレイヤーID
        エンティティ一覧 =
            [
                entityを作る プレイヤーID 0.0 0.0 0.0 0.0 100
                entityを作る 対象A 1.0 2.0 0.5 0.25 40
                entityを作る 対象B 5.0 6.0 -0.5 -0.25 60
            ]
        乱数Seed = 2468
        終了状態 = None
    }

let private 候補を作る id tick targetID beforePosition afterPosition event一覧 : 因果記録候補 =
    {
        因果操作ID = 因果操作ID id
        Tick = tick
        成功 = true
        実行者ID = Some プレイヤーID
        対象EntityID = Some targetID
        概要 = "Entity位置を変更した"
        変更前位置 = Some beforePosition
        変更後位置 = Some afterPosition
        発行Event一覧 = event一覧
        失敗理由一覧 = []
    }

let private 台帳を作る 候補一覧 =
    match 因果台帳.追記する 因果台帳.空 候補一覧 with
    | 追記成功 台帳 -> 台帳
    | 追記失敗(_, index, id, 理由一覧) ->
        failtestf "台帳構築が失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

let private 追記成功を得る = function
    | 追記成功 台帳 -> 台帳
    | 追記失敗(_, index, id, 理由一覧) ->
        failtestf "追記が失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

let private 追記失敗を得る = function
    | 追記失敗(台帳, index, id, 理由一覧) -> 台帳, index, id, 理由一覧
    | 追記成功 台帳 ->
        failtestf "追記が成功しました: records=%A" (因果台帳.記録一覧 台帳)

let private 再生成功を得る = function
    | 再生成功 更新 -> 更新
    | 再生失敗(_, index, id, 理由一覧) ->
        failtestf "再生が失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

let private 再生失敗を得る = function
    | 再生失敗(状態, index, id, 理由一覧) -> 状態, index, id, 理由一覧
    | 再生成功 更新 ->
        failtestf "再生が成功しました: update=%A" 更新

let private entityを得る id (状態: ゲーム状態) =
    状態.エンティティ一覧
    |> List.find (fun entity -> entity.ID = id)

let private 操作を作る id targetID x y event一覧 =
    let 因果: 因果操作 =
        {
            ID = 因果操作ID id
            Tick = 10L
            種別 = 状態変更
            原因因果ID = None
            実行者ID = Some プレイヤーID
            対象EntityID = Some targetID
            概要 = "Entity位置を変更する"
            発行Event一覧 = event一覧
        }

    {
        因果 = 因果
        変更後位置 = 位置を作る x y
    }

[<Tests>]
let 全テスト =
    testList
        "因果台帳の決定論的な最小再生"
        [
            testCase "空台帳は状態不変かつEvent空で成功する" <| fun _ ->
                let 初期状態 = 状態を作る ()
                let 更新 = 因果台帳再生.再生する 初期状態 因果台帳.空 |> 再生成功を得る

                Expect.equal 更新.状態 初期状態 "状態を変更しない"
                Expect.equal 更新.状態.Tick 初期状態.Tick "Tickを変更しない"
                Expect.isEmpty 更新.イベント一覧 "Eventを生成しない"

            testCase "1件の位置変更を再生できる" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る
                            "replay.one"
                            12L
                            対象A
                            (位置を作る 1.0 2.0)
                            (位置を作る 3.0 4.0)
                            []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal (entityを得る 対象A 更新.状態).位置 (位置を作る 3.0 4.0) "変更後位置を再適用する"

            testCase "複数記録を入力順で再生できる" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.first" 11L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                        候補を作る "replay.second" 12L 対象B (位置を作る 5.0 6.0) (位置を作る 7.0 8.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal (entityを得る 対象A 更新.状態).位置 (位置を作る 3.0 4.0) "第一記録を適用する"
                Expect.equal (entityを得る 対象B 更新.状態).位置 (位置を作る 7.0 8.0) "第二記録を適用する"

            testCase "同一対象の連続変更を再生できる" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.chain.1" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                        候補を作る "replay.chain.2" 12L 対象A (位置を作る 3.0 4.0) (位置を作る 7.0 8.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal (entityを得る 対象A 更新.状態).位置 (位置を作る 7.0 8.0) "中間位置を経て最後の位置へ進める"

            testCase "複数対象を再生できる" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.a" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 9.0 10.0) []
                        候補を作る "replay.b" 12L 対象B (位置を作る 5.0 6.0) (位置を作る 11.0 12.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal (entityを得る 対象A 更新.状態).位置 (位置を作る 9.0 10.0) "対象Aを再生する"
                Expect.equal (entityを得る 対象B 更新.状態).位置 (位置を作る 11.0 12.0) "対象Bを再生する"

            testCase "最終Tickが最後の記録Tickになる" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.tick.1" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                        候補を作る "replay.tick.2" 15L 対象B (位置を作る 5.0 6.0) (位置を作る 7.0 8.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal 更新.状態.Tick 15L "最後の正式記録Tickへ進める"

            testCase "同一Tickの複数記録を再生できる" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.same-tick.1" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                        候補を作る "replay.same-tick.2" 12L 対象B (位置を作る 5.0 6.0) (位置を作る 7.0 8.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal 更新.状態.Tick 12L "同一Tickを許可する"
                Expect.equal (entityを得る 対象A 更新.状態).位置 (位置を作る 3.0 4.0) "第一記録を適用する"
                Expect.equal (entityを得る 対象B 更新.状態).位置 (位置を作る 7.0 8.0) "第二記録を適用する"

            testCase "Tickの飛びを許容する" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.jump.1" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                        候補を作る "replay.jump.2" 20L 対象B (位置を作る 5.0 6.0) (位置を作る 7.0 8.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal 更新.状態.Tick 20L "飛び先のTickへ進める"

            testCase "飛ばしたTick進行Eventを生成しない" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.no-tick-events" 20L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.isEmpty 更新.イベント一覧 "未記録のTick進行Eventを合成しない"

            testCase "記録内および記録間のEvent順を維持する" <| fun _ ->
                let 第一Event =
                    [
                        エンティティ生成 対象A
                        ゲーム終了 クリア
                    ]

                let 第二Event =
                    [
                        エンティティ削除 対象B
                        エンティティ生成 対象B
                    ]

                let 台帳 =
                    [
                        候補を作る "replay.events.1" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) 第一Event
                        候補を作る "replay.events.2" 13L 対象B (位置を作る 5.0 6.0) (位置を作る 7.0 8.0) 第二Event
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する (状態を作る ()) 台帳 |> 再生成功を得る

                Expect.equal 更新.イベント一覧 (第一Event @ 第二Event) "台帳順と操作内順を維持する"
                Expect.isNone 更新.状態.終了状態 "ゲーム終了EventをWorld Truthへ反映しない"

            testCase "対象外Entityと非位置フィールドを維持する" <| fun _ ->
                let 初期状態 = 状態を作る ()
                let 元対象 = entityを得る 対象A 初期状態
                let 元対象外 = entityを得る 対象B 初期状態

                let 台帳 =
                    [
                        候補を作る "replay.preserve" 12L 対象A 元対象.位置 (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 更新 = 因果台帳再生.再生する 初期状態 台帳 |> 再生成功を得る
                let 更新対象 = entityを得る 対象A 更新.状態

                Expect.equal 更新対象 { 元対象 with 位置 = 位置を作る 3.0 4.0 } "位置以外を維持する"
                Expect.equal (entityを得る 対象B 更新.状態) 元対象外 "対象外Entityを維持する"
                Expect.equal (更新.状態.エンティティ一覧 |> List.map (fun entity -> entity.ID)) (初期状態.エンティティ一覧 |> List.map (fun entity -> entity.ID)) "Entity順を維持する"
                Expect.equal 更新.状態.プレイヤーID 初期状態.プレイヤーID "プレイヤーIDを維持する"
                Expect.equal 更新.状態.乱数Seed 初期状態.乱数Seed "乱数Seedを維持する"
                Expect.equal 更新.状態.終了状態 初期状態.終了状態 "終了状態を維持する"

            testCase "入力状態を変更しない" <| fun _ ->
                let 初期状態 = 状態を作る ()
                let 変更前 = 初期状態

                let 台帳 =
                    [
                        候補を作る "replay.immutable" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                因果台帳再生.再生する 初期状態 台帳 |> ignore

                Expect.equal 初期状態 変更前 "入力値は不変である"

            testCase "同じ入力から同じ結果を返す" <| fun _ ->
                let 初期状態 = 状態を作る ()

                let 台帳 =
                    [
                        候補を作る "replay.deterministic" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                Expect.equal
                    (因果台帳再生.再生する 初期状態 台帳)
                    (因果台帳再生.再生する 初期状態 台帳)
                    "構造的に同じ結果を返す"

            testCase "対象Entity不存在を拒否する" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.missing" 12L 存在しないID (位置を作る 0.0 0.0) (位置を作る 1.0 1.0) []
                    ]
                    |> 台帳を作る

                let 返却状態, index, id, 理由一覧 =
                    因果台帳再生.再生する (状態を作る ()) 台帳
                    |> 再生失敗を得る

                Expect.equal 返却状態 (状態を作る ()) "初期状態を返す"
                Expect.equal index 0 "失敗位置を返す"
                Expect.equal id (因果操作ID "replay.missing") "失敗操作IDを返す"
                Expect.equal 理由一覧 [ "対象Entityが存在しません。" ] "不存在理由だけを返す"

            testCase "対象EntityID重複を拒否する" <| fun _ ->
                let 初期状態 =
                    {
                        状態を作る () with
                            エンティティ一覧 =
                                [
                                    entityを作る プレイヤーID 0.0 0.0 0.0 0.0 100
                                    entityを作る 対象A 1.0 2.0 0.5 0.25 40
                                    entityを作る 対象A 1.0 2.0 0.5 0.25 40
                                ]
                    }

                let 台帳 =
                    [
                        候補を作る "replay.duplicate-target" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 返却状態, _, _, 理由一覧 =
                    因果台帳再生.再生する 初期状態 台帳
                    |> 再生失敗を得る

                Expect.equal 返却状態 初期状態 "初期状態を返す"
                Expect.equal 理由一覧 [ "対象EntityIDがゲーム状態内で重複しています。" ] "重複時は位置比較しない"

            testCase "変更前位置不一致を拒否する" <| fun _ ->
                let 台帳 =
                    [
                        候補を作る "replay.position-mismatch" 12L 対象A (位置を作る 99.0 99.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 返却状態, _, _, 理由一覧 =
                    因果台帳再生.再生する (状態を作る ()) 台帳
                    |> 再生失敗を得る

                Expect.equal 返却状態 (状態を作る ()) "初期状態を返す"
                Expect.equal 理由一覧 [ "対象Entityの現在位置が因果台帳記録の変更前位置と一致しません。" ] "分岐を検出する"

            testCase "初期状態Tickが最初の記録Tickより後なら拒否する" <| fun _ ->
                let 初期状態 = { 状態を作る () with Tick = 13L }

                let 台帳 =
                    [
                        候補を作る "replay.past-tick" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let _, _, _, 理由一覧 =
                    因果台帳再生.再生する 初期状態 台帳
                    |> 再生失敗を得る

                Expect.equal 理由一覧 [ "因果台帳記録のTickが現在の再生状態より前です。" ] "過去Tickを拒否する"

            testCase "終了状態からの非空台帳再生を拒否する" <| fun _ ->
                let 初期状態 = { 状態を作る () with 終了状態 = Some ゲームオーバー }

                let 台帳 =
                    [
                        候補を作る "replay.finished" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 返却状態, _, _, 理由一覧 =
                    因果台帳再生.再生する 初期状態 台帳
                    |> 再生失敗を得る

                Expect.equal 返却状態 初期状態 "終了状態を変更しない"
                Expect.equal 理由一覧 [ "終了状態のため因果台帳を再生できません。" ] "終了状態を拒否する"

            testCase "後方の記録が失敗しても初期状態を返す" <| fun _ ->
                let 初期状態 = 状態を作る ()

                let 台帳 =
                    [
                        候補を作る "replay.atomic.1" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                        候補を作る "replay.atomic.2" 13L 対象B (位置を作る 99.0 99.0) (位置を作る 7.0 8.0) []
                    ]
                    |> 台帳を作る

                let 返却状態, index, id, _ =
                    因果台帳再生.再生する 初期状態 台帳
                    |> 再生失敗を得る

                Expect.equal 返却状態 初期状態 "途中状態を公開しない"
                Expect.equal index 1 "後方の失敗位置を返す"
                Expect.equal id (因果操作ID "replay.atomic.2") "後方の失敗IDを返す"

            testCase "失敗時に途中Eventを公開しない" <| fun _ ->
                let 初期状態 = 状態を作る ()

                let 台帳 =
                    [
                        候補を作る "replay.event.atomic.1" 12L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) [ エンティティ生成 対象A ]
                        候補を作る "replay.event.atomic.2" 13L 対象B (位置を作る 99.0 99.0) (位置を作る 7.0 8.0) [ エンティティ削除 対象B ]
                    ]
                    |> 台帳を作る

                let 結果 = 因果台帳再生.再生する 初期状態 台帳

                Expect.equal
                    結果
                    (再生失敗(
                        初期状態,
                        1,
                        因果操作ID "replay.event.atomic.2",
                        [ "対象Entityの現在位置が因果台帳記録の変更前位置と一致しません。" ]
                    ))
                    "失敗結果へ途中Eventを含めない"

            testCase "複数理由を固定順で返す" <| fun _ ->
                let 初期状態 =
                    {
                        状態を作る () with
                            Tick = 20L
                            終了状態 = Some クリア
                    }

                let 台帳 =
                    [
                        候補を作る "replay.errors" 10L 存在しないID (位置を作る 0.0 0.0) (位置を作る 1.0 1.0) []
                    ]
                    |> 台帳を作る

                let _, _, _, 理由一覧 =
                    因果台帳再生.再生する 初期状態 台帳
                    |> 再生失敗を得る

                Expect.equal
                    理由一覧
                    [
                        "因果台帳記録のTickが現在の再生状態より前です。"
                        "終了状態のため因果台帳を再生できません。"
                        "対象Entityが存在しません。"
                    ]
                    "固定された検証順で返す"

            testCase "一括因果操作の成功結果と再生結果が一致する" <| fun _ ->
                let 初期状態 = 状態を作る ()

                let 操作一覧 =
                    [
                        操作を作る "replay.integration.a" 対象A 3.0 4.0 [ エンティティ生成 対象A ]
                        操作を作る "replay.integration.b" 対象B 7.0 8.0 [ エンティティ削除 対象B ]
                    ]

                let 一括更新, 記録候補一覧 =
                    match 因果操作列実行.一括適用する 初期状態 操作一覧 with
                    | 一括適用成功(更新, 記録一覧) -> 更新, 記録一覧
                    | 結果 -> failtestf "一括適用が失敗しました: %A" 結果

                let 台帳 =
                    因果台帳.追記する 因果台帳.空 記録候補一覧
                    |> 追記成功を得る

                let 再生更新 =
                    因果台帳再生.再生する 初期状態 台帳
                    |> 再生成功を得る

                Expect.equal 再生更新.状態 一括更新.状態 "最終状態が一致する"
                Expect.equal 再生更新.イベント一覧 一括更新.イベント一覧 "Event列が一致する"

            testCase "既存台帳末尾より小さいTickの追記を拒否する" <| fun _ ->
                let 既存台帳 =
                    [
                        候補を作る "ledger.tick.existing" 10L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 返却台帳, index, id, 理由一覧 =
                    因果台帳.追記する
                        既存台帳
                        [ 候補を作る "ledger.tick.past" 9L 対象A (位置を作る 3.0 4.0) (位置を作る 5.0 6.0) [] ]
                    |> 追記失敗を得る

                Expect.equal 返却台帳 既存台帳 "元台帳を返す"
                Expect.equal index 0 "最初の候補を返す"
                Expect.equal id (因果操作ID "ledger.tick.past") "逆行IDを返す"
                Expect.equal 理由一覧 [ "因果台帳のTick順序が逆行しています。" ] "既存末尾からの逆行を拒否する"

            testCase "候補一覧内のTick逆行を最小indexで拒否する" <| fun _ ->
                let 候補一覧 =
                    [
                        候補を作る "ledger.sequence.10" 10L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                        候補を作る "ledger.sequence.12" 12L 対象A (位置を作る 3.0 4.0) (位置を作る 5.0 6.0) []
                        候補を作る "ledger.sequence.11" 11L 対象A (位置を作る 5.0 6.0) (位置を作る 7.0 8.0) []
                    ]

                let 返却台帳, index, id, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 候補一覧
                    |> 追記失敗を得る

                Expect.equal 返却台帳 因果台帳.空 "一件も追記しない"
                Expect.equal index 2 "最初の逆行位置を返す"
                Expect.equal id (因果操作ID "ledger.sequence.11") "逆行したIDを返す"
                Expect.equal 理由一覧 [ "因果台帳のTick順序が逆行しています。" ] "候補入力順の逆行を拒否する"

            testCase "同一Tick追記を許可する" <| fun _ ->
                let 既存台帳 =
                    [
                        候補を作る "ledger.same.existing" 10L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 更新後 =
                    因果台帳.追記する
                        既存台帳
                        [ 候補を作る "ledger.same.next" 10L 対象A (位置を作る 3.0 4.0) (位置を作る 5.0 6.0) [] ]
                    |> 追記成功を得る

                Expect.equal (因果台帳.記録一覧 更新後 |> List.map (fun 記録 -> 記録.Tick)) [ 10L; 10L ] "非減少として許可する"

            testCase "Tick増加追記を許可する" <| fun _ ->
                let 既存台帳 =
                    [
                        候補を作る "ledger.increase.existing" 10L 対象A (位置を作る 1.0 2.0) (位置を作る 3.0 4.0) []
                    ]
                    |> 台帳を作る

                let 更新後 =
                    因果台帳.追記する
                        既存台帳
                        [
                            候補を作る "ledger.increase.11" 11L 対象A (位置を作る 3.0 4.0) (位置を作る 5.0 6.0) []
                            候補を作る "ledger.increase.15" 15L 対象A (位置を作る 5.0 6.0) (位置を作る 7.0 8.0) []
                        ]
                    |> 追記成功を得る

                Expect.equal (因果台帳.記録一覧 更新後 |> List.map (fun 記録 -> 記録.Tick)) [ 10L; 11L; 15L ] "Tickの飛びを許可する"
        ]
