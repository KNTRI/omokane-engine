module CausalOperationExecutionTests

open System
open Expecto
open Omokane.Core.Domain

let private プレイヤーID = エンティティID "player"
let private 対象ID = エンティティID "target"
let private 別EntityID = エンティティID "other"
let private 存在しないID = エンティティID "missing"

let private entityを作る id x y =
    {
        ID = id
        種別 = 敵
        所有者 = 中立
        位置 = { X = x; Y = y }
        速度 = { X = 0.0; Y = 0.0 }
        当たり判定 = None
        HP = None
    }

let private 状態を作る entity一覧 =
    {
        Tick = 10L
        プレイヤーID = プレイヤーID
        エンティティ一覧 = entity一覧
        乱数Seed = 123
        終了状態 = None
    }

let private 基本状態 () =
    状態を作る
        [
            entityを作る プレイヤーID 0.0 0.0
            entityを作る 対象ID 1.0 2.0
            entityを作る 別EntityID 3.0 4.0
        ]

let private 操作を作る targetID =
    let 因果: 因果操作 =
        {
            ID = 因果操作ID "causal.move.target"
            Tick = 10L
            種別 = 状態変更
            原因因果ID = None
            実行者ID = Some プレイヤーID
            対象EntityID = targetID
            概要 = "対象Entityの位置を変更する"
            発行Event一覧 = []
        }

    {
        因果 = 因果
        変更後位置 = { X = 8.0; Y = 9.0 }
    }

let private 成功を得る = function
    | 適用成功(更新, 記録) -> 更新, 記録
    | 適用失敗(_, 分類, 理由一覧, _) ->
        failtestf "因果操作が失敗しました: %A %A" 分類 理由一覧

let private 失敗を得る = function
    | 適用失敗(状態, 分類, 理由一覧, 記録) -> 状態, 分類, 理由一覧, 記録
    | 適用成功(更新, _) ->
        failtestf "因果操作が成功しました: %A" 更新

let private entityを得る id (状態: ゲーム状態) =
    状態.エンティティ一覧
    |> List.find (fun entity -> entity.ID = id)

[<Tests>]
let 全テスト =
    testList
        "因果操作最小実行器"
        [
            testCase "正常な位置変更が成功する" <| fun _ ->
                let 状態 = 基本状態 ()
                let 更新, _ = 因果操作実行.適用する 状態 (操作を作る (Some 対象ID)) |> 成功を得る

                Expect.equal (entityを得る 対象ID 更新.状態).位置 { X = 8.0; Y = 9.0 } "絶対座標へ変更する"
                Expect.equal 更新.状態.Tick 状態.Tick "操作適用ではTickを進めない"

            testCase "対象Entityだけが更新される" <| fun _ ->
                let 状態 = 基本状態 ()
                let 更新, _ = 因果操作実行.適用する 状態 (操作を作る (Some 対象ID)) |> 成功を得る

                Expect.equal (entityを得る 対象ID 更新.状態).位置 { X = 8.0; Y = 9.0 } "対象を更新する"
                Expect.equal (entityを得る プレイヤーID 更新.状態) (entityを得る プレイヤーID 状態) "プレイヤーは変更しない"
                Expect.equal (entityを得る 別EntityID 更新.状態) (entityを得る 別EntityID 状態) "別Entityは変更しない"

            testCase "Entity一覧の順序が維持される" <| fun _ ->
                let 状態 = 基本状態 ()
                let 更新, _ = 因果操作実行.適用する 状態 (操作を作る (Some 対象ID)) |> 成功を得る
                let 変更前順序 = 状態.エンティティ一覧 |> List.map (fun entity -> entity.ID)
                let 変更後順序 = 更新.状態.エンティティ一覧 |> List.map (fun entity -> entity.ID)

                Expect.equal 変更後順序 変更前順序 "Entity一覧を並べ替えない"

            testCase "元状態が変更されない" <| fun _ ->
                let 状態 = 基本状態 ()
                let 変更前 = 状態
                因果操作実行.適用する 状態 (操作を作る (Some 対象ID)) |> ignore

                Expect.equal 状態 変更前 "入力状態を変更しない"
                Expect.equal (entityを得る 対象ID 状態).位置 { X = 1.0; Y = 2.0 } "元位置を保持する"

            testCase "存在しない対象はゲーム内失敗" <| fun _ ->
                let 状態, 分類, 理由一覧, _ =
                    因果操作実行.適用する (基本状態 ()) (操作を作る (Some 存在しないID))
                    |> 失敗を得る

                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗へ分類する"
                Expect.equal 理由一覧 [ "対象Entityが存在しません。" ] "存在しない理由を返す"
                Expect.equal 状態 (基本状態 ()) "入力状態を返す"

            testCase "対象ID重複は契約不正" <| fun _ ->
                let 重複状態 =
                    状態を作る
                        [
                            entityを作る 対象ID 1.0 2.0
                            entityを作る 対象ID 3.0 4.0
                        ]

                let _, 分類, 理由一覧, _ =
                    因果操作実行.適用する 重複状態 (操作を作る (Some 対象ID))
                    |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"
                Expect.equal 理由一覧 [ "対象EntityIDがゲーム状態内で重複しています。" ] "重複を拒否する"

            testCase "過去Tickはゲーム内失敗" <| fun _ ->
                let 要求 = { 操作を作る (Some 対象ID) with 因果.Tick = 9L }
                let _, 分類, 理由一覧, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗へ分類する"
                Expect.equal 理由一覧 [ "因果操作のTickが現在状態のTickと一致しません。" ] "過去Tickを拒否する"

            testCase "未来Tickはゲーム内失敗" <| fun _ ->
                let 要求 = { 操作を作る (Some 対象ID) with 因果.Tick = 11L }
                let _, 分類, 理由一覧, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗へ分類する"
                Expect.equal 理由一覧 [ "因果操作のTickが現在状態のTickと一致しません。" ] "未来Tickを拒否する"

            testCase "終了状態ではゲーム内失敗" <| fun _ ->
                let 終了済み = { 基本状態 () with 終了状態 = Some ゲームオーバー }
                let _, 分類, 理由一覧, _ =
                    因果操作実行.適用する 終了済み (操作を作る (Some 対象ID))
                    |> 失敗を得る

                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗へ分類する"
                Expect.equal 理由一覧 [ "終了状態のため因果操作を適用できません。" ] "終了状態を拒否する"

            testCase "失敗時は状態が変化しない" <| fun _ ->
                let 状態 = 基本状態 ()
                let 返却状態, _, _, _ =
                    因果操作実行.適用する 状態 (操作を作る (Some 存在しないID))
                    |> 失敗を得る

                Expect.equal 返却状態 状態 "全失敗時は入力状態を返す"

            testCase "状態変更以外の種別は契約不正" <| fun _ ->
                let 要求 = { 操作を作る (Some 対象ID) with 因果.種別 = 伝播信号生成 }
                let _, 分類, 理由一覧, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"
                Expect.equal 理由一覧 [ "因果操作種別が状態変更ではありません。" ] "種別を検証する"

            testCase "対象IDなしは契約不正" <| fun _ ->
                let _, 分類, 理由一覧, _ =
                    因果操作実行.適用する (基本状態 ()) (操作を作る None)
                    |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"
                Expect.equal 理由一覧 [ "対象EntityIDが指定されていません。" ] "対象指定を要求する"

            testCase "XがNaNなら契約不正" <| fun _ ->
                let 要求 = { 操作を作る (Some 対象ID) with 変更後位置.X = Double.NaN }
                let _, 分類, 理由一覧, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"
                Expect.equal 理由一覧 [ "変更後位置.Xが有限値ではありません。" ] "NaNを拒否する"

            testCase "XがInfinityなら契約不正" <| fun _ ->
                let 要求 = { 操作を作る (Some 対象ID) with 変更後位置.X = Double.PositiveInfinity }
                let _, 分類, 理由一覧, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"
                Expect.equal 理由一覧 [ "変更後位置.Xが有限値ではありません。" ] "Infinityを拒否する"

            testCase "YがNaNなら契約不正" <| fun _ ->
                let 要求 = { 操作を作る (Some 対象ID) with 変更後位置.Y = Double.NaN }
                let _, 分類, 理由一覧, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"
                Expect.equal 理由一覧 [ "変更後位置.Yが有限値ではありません。" ] "NaNを拒否する"

            testCase "YがInfinityなら契約不正" <| fun _ ->
                let 要求 = { 操作を作る (Some 対象ID) with 変更後位置.Y = Double.NegativeInfinity }
                let _, 分類, 理由一覧, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"
                Expect.equal 理由一覧 [ "変更後位置.Yが有限値ではありません。" ] "Infinityを拒否する"

            testCase "同じ入力から同じ結果を返す" <| fun _ ->
                let 状態 = 基本状態 ()
                let 要求 = 操作を作る (Some 対象ID)

                Expect.equal
                    (因果操作実行.適用する 状態 要求)
                    (因果操作実行.適用する 状態 要求)
                    "構造的に同じ結果を返す"

            testCase "成功時のEvent順序を維持する" <| fun _ ->
                let event一覧 = [ エンティティ生成 別EntityID; エンティティ削除 対象ID ]
                let 要求 = { 操作を作る (Some 対象ID) with 因果.発行Event一覧 = event一覧 }
                let 更新, 記録 = 因果操作実行.適用する (基本状態 ()) 要求 |> 成功を得る

                Expect.equal 更新.イベント一覧 event一覧 "更新結果のEvent順を維持する"
                Expect.equal 記録.発行Event一覧 event一覧 "記録候補のEvent順を維持する"

            testCase "失敗時のEventは空" <| fun _ ->
                let event一覧 = [ エンティティ削除 対象ID ]

                let 要求 =
                    {
                        操作を作る (Some 存在しないID) with
                            因果.発行Event一覧 = event一覧
                    }

                let _, _, _, 記録 = 因果操作実行.適用する (基本状態 ()) 要求 |> 失敗を得る

                Expect.isEmpty 記録.発行Event一覧 "失敗時はEventを返さない"

            testCase "複数Entityでも対象だけを更新する" <| fun _ ->
                let 状態 = 基本状態 ()
                let 更新, _ = 因果操作実行.適用する 状態 (操作を作る (Some 別EntityID)) |> 成功を得る

                Expect.equal (entityを得る 別EntityID 更新.状態).位置 { X = 8.0; Y = 9.0 } "指定対象を更新する"
                Expect.equal (entityを得る 対象ID 更新.状態) (entityを得る 対象ID 状態) "非対象を保持する"

            testCase "同じ位置への再適用が成功する" <| fun _ ->
                let 要求 = 操作を作る (Some 対象ID)
                let 第一更新, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 成功を得る
                let 第二結果 = 因果操作実行.適用する 第一更新.状態 要求

                match 第二結果 with
                | 適用成功 _ -> ()
                | 適用失敗(_, 分類, 理由一覧, _) ->
                    failtestf "再適用が失敗しました: %A %A" 分類 理由一覧

            testCase "同じ位置への再適用後も状態が構造的に同じ" <| fun _ ->
                let 要求 = 操作を作る (Some 対象ID)
                let 第一更新, _ = 因果操作実行.適用する (基本状態 ()) 要求 |> 成功を得る
                let 第二更新, _ = 因果操作実行.適用する 第一更新.状態 要求 |> 成功を得る

                Expect.equal 第二更新.状態 第一更新.状態 "同値な位置変更は状態を変えない"

            testCase "契約エラーの理由順が固定される" <| fun _ ->
                let 重複状態 =
                    状態を作る
                        [
                            entityを作る 対象ID 1.0 2.0
                            entityを作る 対象ID 3.0 4.0
                        ]

                let 不正因果: 因果操作 =
                    {
                        ID = 因果操作ID ""
                        Tick = -1L
                        種別 = 伝播信号生成
                        原因因果ID = None
                        実行者ID = None
                        対象EntityID = Some 対象ID
                        概要 = " "
                        発行Event一覧 = []
                    }

                let 要求 =
                    {
                        因果 = 不正因果
                        変更後位置 = { X = Double.NaN; Y = Double.PositiveInfinity }
                    }

                let _, 分類, 理由一覧, _ = 因果操作実行.適用する 重複状態 要求 |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正へ分類する"

                Expect.equal
                    理由一覧
                    [
                        "因果操作IDが空です。"
                        "因果操作のTickが負です。"
                        "因果操作の概要が空です。"
                        "因果操作種別が状態変更ではありません。"
                        "変更後位置.Xが有限値ではありません。"
                        "変更後位置.Yが有限値ではありません。"
                        "対象EntityIDがゲーム状態内で重複しています。"
                    ]
                    "既存検証の後に固定順で契約エラーを返す"

            testCase "ゲーム内失敗の理由順が固定される" <| fun _ ->
                let 終了済み =
                    {
                        基本状態 () with
                            終了状態 = Some クリア
                    }

                let 要求 =
                    {
                        操作を作る (Some 存在しないID) with
                            因果.Tick = 11L
                    }

                let _, 分類, 理由一覧, _ = 因果操作実行.適用する 終了済み 要求 |> 失敗を得る

                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗へ分類する"

                Expect.equal
                    理由一覧
                    [
                        "因果操作のTickが現在状態のTickと一致しません。"
                        "終了状態のため因果操作を適用できません。"
                        "対象Entityが存在しません。"
                    ]
                    "ゲーム内前提条件の固定順で返す"

            testCase "成功記録が更新結果と一致する" <| fun _ ->
                let event一覧 = [ エンティティ生成 対象ID ]
                let 要求 = { 操作を作る (Some 対象ID) with 因果.発行Event一覧 = event一覧 }
                let 更新, 記録 = 因果操作実行.適用する (基本状態 ()) 要求 |> 成功を得る

                Expect.isTrue 記録.成功 "成功を記録する"
                Expect.equal 記録.変更前位置 (Some { X = 1.0; Y = 2.0 }) "変更前位置を記録する"
                Expect.equal 記録.変更後位置 (Some (entityを得る 対象ID 更新.状態).位置) "更新後位置と一致する"
                Expect.equal 記録.発行Event一覧 更新.イベント一覧 "Event一覧と一致する"
                Expect.isEmpty 記録.失敗理由一覧 "成功時は失敗理由がない"

            testCase "失敗記録が失敗結果と一致する" <| fun _ ->
                let 状態 = 基本状態 ()
                let 返却状態, 分類, 理由一覧, 記録 =
                    因果操作実行.適用する 状態 (操作を作る (Some 存在しないID))
                    |> 失敗を得る

                Expect.equal 返却状態 状態 "失敗結果は入力状態を返す"
                Expect.equal 分類 ゲーム内失敗 "分類が一致する"
                Expect.isFalse 記録.成功 "失敗を記録する"
                Expect.equal 記録.失敗理由一覧 理由一覧 "失敗理由が一致する"
                Expect.isNone 記録.変更前位置 "変更前位置を記録しない"
                Expect.isNone 記録.変更後位置 "変更後位置を記録しない"
                Expect.isEmpty 記録.発行Event一覧 "Eventを記録しない"
        ]
