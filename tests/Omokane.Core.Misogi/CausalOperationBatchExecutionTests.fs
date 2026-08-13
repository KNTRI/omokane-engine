module CausalOperationBatchExecutionTests

open System
open Expecto
open Omokane.Core.Domain

let private プレイヤーID = エンティティID "player"
let private 対象A = エンティティID "target.a"
let private 対象B = エンティティID "target.b"
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

let private 状態を作る () =
    {
        Tick = 20L
        プレイヤーID = プレイヤーID
        エンティティ一覧 =
            [
                entityを作る プレイヤーID 0.0 0.0
                entityを作る 対象A 1.0 2.0
                entityを作る 対象B 3.0 4.0
            ]
        エンティティ反応状態一覧 = []
        乱数Seed = 456
        終了状態 = None
    }

let private 操作を作る id targetID x y event一覧 =
    let 因果: 因果操作 =
        {
            ID = 因果操作ID id
            Tick = 20L
            種別 = 状態変更
            原因因果ID = None
            実行者ID = Some プレイヤーID
            対象EntityID = targetID
            概要 = "Entity位置を変更する"
            発行Event一覧 = event一覧
        }

    {
        因果 = 因果
        変更後位置 = { X = x; Y = y }
    }

let private entityを得る id (状態: ゲーム状態) =
    状態.エンティティ一覧
    |> List.find (fun entity -> entity.ID = id)

let private 成功を得る = function
    | 一括適用成功(更新, 記録一覧) -> 更新, 記録一覧
    | 一括適用失敗(_, index, id, 分類, 理由一覧, _) ->
        failtestf "一括適用が失敗しました: index=%d id=%A class=%A reasons=%A" index id 分類 理由一覧

let private 失敗を得る = function
    | 一括適用失敗(状態, index, id, 分類, 理由一覧, 記録) ->
        状態, index, id, 分類, 理由一覧, 記録
    | 一括適用成功(更新, 記録一覧) ->
        failtestf "一括適用が成功しました: update=%A records=%A" 更新 記録一覧

[<Tests>]
let 全テスト =
    testList
        "因果操作列の原子的な一括適用"
        [
            testCase "空一覧が状態不変で成功する" <| fun _ ->
                let 状態 = 状態を作る ()
                let 更新, 記録一覧 = 因果操作列実行.一括適用する 状態 [] |> 成功を得る

                Expect.equal 更新.状態 状態 "入力状態を返す"
                Expect.isEmpty 更新.イベント一覧 "Eventは空である"
                Expect.isEmpty 記録一覧 "記録は空である"

            testCase "1件を適用できる" <| fun _ ->
                let 操作 = 操作を作る "batch.1" (Some 対象A) 5.0 6.0 []
                let 更新, 記録一覧 = 因果操作列実行.一括適用する (状態を作る ()) [ 操作 ] |> 成功を得る

                Expect.equal (entityを得る 対象A 更新.状態).位置 { X = 5.0; Y = 6.0 } "絶対位置へ変更する"
                Expect.equal 更新.状態.Tick 20L "Tickを進めない"
                Expect.equal 記録一覧.Length 1 "成功記録を1件返す"

            testCase "複数件を入力順に適用できる" <| fun _ ->
                let 第一 = 操作を作る "batch.1" (Some 対象A) 5.0 6.0 []
                let 第二 = 操作を作る "batch.2" (Some 対象A) 7.0 8.0 []
                let 開始状態 = 状態を作る ()
                let 一括更新, _ = 因果操作列実行.一括適用する 開始状態 [ 第一; 第二 ] |> 成功を得る
                let 第一更新, _ = 因果操作実行.適用する 開始状態 第一 |> function
                    | 適用成功(更新, 記録) -> 更新, 記録
                    | 結果 -> failtestf "単一適用が失敗しました: %A" 結果
                let 第二更新, _ = 因果操作実行.適用する 第一更新.状態 第二 |> function
                    | 適用成功(更新, 記録) -> 更新, 記録
                    | 結果 -> failtestf "単一適用が失敗しました: %A" 結果

                Expect.equal 一括更新.状態 第二更新.状態 "単一実行器の入力順適用と一致する"

            testCase "複数対象を更新できる" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.a" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.b" (Some 対象B) 7.0 8.0 []
                    ]

                let 更新, _ = 因果操作列実行.一括適用する (状態を作る ()) 操作一覧 |> 成功を得る

                Expect.equal (entityを得る 対象A 更新.状態).位置 { X = 5.0; Y = 6.0 } "対象Aを更新する"
                Expect.equal (entityを得る 対象B 更新.状態).位置 { X = 7.0; Y = 8.0 } "対象Bを更新する"

            testCase "同一対象では最後の絶対位置が残る" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.1" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.2" (Some 対象A) 9.0 10.0 []
                    ]

                let 更新, _ = 因果操作列実行.一括適用する (状態を作る ()) 操作一覧 |> 成功を得る

                Expect.equal (entityを得る 対象A 更新.状態).位置 { X = 9.0; Y = 10.0 } "最後の入力で上書きする"

            testCase "同一対象の記録が中間位置を反映する" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.1" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.2" (Some 対象A) 9.0 10.0 []
                    ]

                let _, 記録一覧 = 因果操作列実行.一括適用する (状態を作る ()) 操作一覧 |> 成功を得る

                Expect.equal 記録一覧[0].変更前位置 (Some { X = 1.0; Y = 2.0 }) "最初の変更前位置を記録する"
                Expect.equal 記録一覧[0].変更後位置 (Some { X = 5.0; Y = 6.0 }) "最初の変更後位置を記録する"
                Expect.equal 記録一覧[1].変更前位置 (Some { X = 5.0; Y = 6.0 }) "仮状態の位置を次の変更前位置にする"
                Expect.equal 記録一覧[1].変更後位置 (Some { X = 9.0; Y = 10.0 }) "最後の変更後位置を記録する"

            testCase "Eventが操作順と操作内順で連結される" <| fun _ ->
                let 第一Event = [ エンティティ生成 対象A; エンティティ削除 対象B ]
                let 第二Event = [ エンティティ生成 対象B; エンティティ削除 対象A ]

                let 操作一覧 =
                    [
                        操作を作る "batch.1" (Some 対象A) 5.0 6.0 第一Event
                        操作を作る "batch.2" (Some 対象B) 7.0 8.0 第二Event
                    ]

                let 更新, _ = 因果操作列実行.一括適用する (状態を作る ()) 操作一覧 |> 成功を得る

                Expect.equal 更新.イベント一覧 (第一Event @ 第二Event) "Event順を維持する"

            testCase "成功記録が入力順になる" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.first" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.second" (Some 対象B) 7.0 8.0 []
                    ]

                let _, 記録一覧 = 因果操作列実行.一括適用する (状態を作る ()) 操作一覧 |> 成功を得る

                Expect.equal
                    (記録一覧 |> List.map (fun 記録 -> 記録.因果操作ID))
                    [ 因果操作ID "batch.first"; 因果操作ID "batch.second" ]
                    "記録を入力順で返す"

            testCase "途中失敗で元状態を返す" <| fun _ ->
                let 開始状態 = 状態を作る ()

                let 操作一覧 =
                    [
                        操作を作る "batch.ok" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.fail" (Some 存在しないID) 7.0 8.0 []
                    ]

                let 返却状態, _, _, _, _, _ = 因果操作列実行.一括適用する 開始状態 操作一覧 |> 失敗を得る

                Expect.equal 返却状態 開始状態 "仮更新を破棄する"

            testCase "途中失敗でEventが空になる" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.ok" (Some 対象A) 5.0 6.0 [ エンティティ生成 対象A ]
                        操作を作る "batch.fail" (Some 存在しないID) 7.0 8.0 [ エンティティ削除 対象A ]
                    ]

                let _, _, _, _, _, 失敗記録 =
                    因果操作列実行.一括適用する (状態を作る ()) 操作一覧
                    |> 失敗を得る

                Expect.isEmpty 失敗記録.発行Event一覧 "失敗結果へEventを公開しない"

            testCase "途中失敗で仮成功記録を返さない" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.ok" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.fail" (Some 存在しないID) 7.0 8.0 []
                    ]

                let _, _, _, _, _, 失敗記録 =
                    因果操作列実行.一括適用する (状態を作る ()) 操作一覧
                    |> 失敗を得る

                Expect.equal 失敗記録.因果操作ID (因果操作ID "batch.fail") "失敗操作の記録だけを返す"

            testCase "最初のゲーム内失敗で後続操作を処理しない" <| fun _ ->
                let 開始状態 = 状態を作る ()

                let 操作一覧 =
                    [
                        操作を作る "batch.ok" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.first-fail" (Some 存在しないID) 7.0 8.0 []
                        { 操作を作る "batch.later-fail" (Some 対象B) 9.0 10.0 [] with 因果.Tick = 21L }
                    ]

                let 返却状態, index, id, _, _, _ =
                    因果操作列実行.一括適用する 開始状態 操作一覧
                    |> 失敗を得る

                Expect.equal index 1 "最初の失敗位置で停止する"
                Expect.equal id (因果操作ID "batch.first-fail") "最初の失敗操作を返す"
                Expect.equal 返却状態 開始状態 "後続を含む仮更新を公開しない"

            testCase "静的契約不正があれば仮適用を開始しない" <| fun _ ->
                let 開始状態 = 状態を作る ()

                let 不正操作 =
                    {
                        操作を作る "batch.invalid" (Some 対象B) 7.0 8.0 [] with
                            因果.種別 = 伝播信号生成
                    }

                let 操作一覧 =
                    [
                        操作を作る "batch.valid" (Some 対象A) 5.0 6.0 []
                        不正操作
                    ]

                let 返却状態, index, _, 分類, _, _ =
                    因果操作列実行.一括適用する 開始状態 操作一覧
                    |> 失敗を得る

                Expect.equal index 1 "不正操作位置を返す"
                Expect.equal 分類 契約不正 "契約不正として返す"
                Expect.equal 返却状態 開始状態 "先行操作を仮適用しない"

            testCase "因果操作ID重複を契約不正として拒否する" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.duplicate" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.duplicate" (Some 対象B) 7.0 8.0 []
                    ]

                let _, _, _, 分類, 理由一覧, _ =
                    因果操作列実行.一括適用する (状態を作る ()) 操作一覧
                    |> 失敗を得る

                Expect.equal 分類 契約不正 "契約不正として返す"
                Expect.equal 理由一覧 [ "因果操作一覧内で因果操作IDが重複しています。" ] "重複理由を返す"

            testCase "重複IDの最小indexとIDを返す" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.unique" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.duplicate" (Some 対象A) 7.0 8.0 []
                        操作を作る "batch.duplicate" (Some 対象B) 9.0 10.0 []
                    ]

                let _, index, id, _, _, _ =
                    因果操作列実行.一括適用する (状態を作る ()) 操作一覧
                    |> 失敗を得る

                Expect.equal index 1 "重複IDを持つ最小indexを返す"
                Expect.equal id (因果操作ID "batch.duplicate") "重複したIDを返す"

            testCase "Tick不一致をゲーム内失敗にする" <| fun _ ->
                let 不一致 = { 操作を作る "batch.tick" (Some 対象A) 5.0 6.0 [] with 因果.Tick = 21L }

                let _, index, _, 分類, 理由一覧, _ =
                    因果操作列実行.一括適用する (状態を作る ()) [ 不一致 ]
                    |> 失敗を得る

                Expect.equal index 0 "不一致位置を返す"
                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗として返す"
                Expect.equal 理由一覧 [ "因果操作のTickが現在状態のTickと一致しません。" ] "Tick不一致理由を返す"

            testCase "終了状態を拒否する" <| fun _ ->
                let 終了済み = { 状態を作る () with 終了状態 = Some ゲームオーバー }
                let 操作 = 操作を作る "batch.finished" (Some 対象A) 5.0 6.0 []

                let 返却状態, index, _, 分類, 理由一覧, _ =
                    因果操作列実行.一括適用する 終了済み [ 操作 ]
                    |> 失敗を得る

                Expect.equal index 0 "終了状態で最初の操作が失敗する"
                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗として返す"
                Expect.equal 理由一覧 [ "終了状態のため因果操作を適用できません。" ] "終了状態理由を返す"
                Expect.equal 返却状態 終了済み "終了状態を変更しない"

            testCase "存在しない対象を拒否する" <| fun _ ->
                let 操作 = 操作を作る "batch.missing" (Some 存在しないID) 5.0 6.0 []

                let _, index, _, 分類, 理由一覧, _ =
                    因果操作列実行.一括適用する (状態を作る ()) [ 操作 ]
                    |> 失敗を得る

                Expect.equal index 0 "存在しない対象の位置を返す"
                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗として返す"
                Expect.equal 理由一覧 [ "対象Entityが存在しません。" ] "対象不存在理由を返す"

            testCase "最初のゲーム内失敗位置を返す" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.ok" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.first-fail" (Some 存在しないID) 7.0 8.0 []
                        { 操作を作る "batch.second-fail" (Some 対象B) 9.0 10.0 [] with 因果.Tick = 21L }
                    ]

                let _, index, id, _, _, _ =
                    因果操作列実行.一括適用する (状態を作る ()) 操作一覧
                    |> 失敗を得る

                Expect.equal index 1 "入力順で最初の失敗を返す"
                Expect.equal id (因果操作ID "batch.first-fail") "失敗操作IDを返す"

            testCase "入力状態を変更しない" <| fun _ ->
                let 開始状態 = 状態を作る ()
                let 変更前 = 開始状態

                let 操作一覧 =
                    [
                        操作を作る "batch.a" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.b" (Some 対象B) 7.0 8.0 []
                    ]

                因果操作列実行.一括適用する 開始状態 操作一覧 |> ignore

                Expect.equal 開始状態 変更前 "入力状態は不変である"

            testCase "同じ入力から同じ結果を返す" <| fun _ ->
                let 開始状態 = 状態を作る ()

                let 操作一覧 =
                    [
                        操作を作る "batch.a" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.b" (Some 対象B) 7.0 8.0 []
                    ]

                Expect.equal
                    (因果操作列実行.一括適用する 開始状態 操作一覧)
                    (因果操作列実行.一括適用する 開始状態 操作一覧)
                    "構造的に同じ結果を返す"

            testCase "静的エラーとゲーム内失敗の併存時は静的エラーを返す" <| fun _ ->
                let ゲーム内失敗操作 = 操作を作る "batch.game-fail" (Some 存在しないID) 5.0 6.0 []

                let 静的不正操作 =
                    {
                        操作を作る "batch.contract-fail" (Some 対象A) 7.0 8.0 [] with
                            因果.種別 = 伝播信号生成
                    }

                let 返却状態, index, id, 分類, _, _ =
                    因果操作列実行.一括適用する (状態を作る ()) [ ゲーム内失敗操作; 静的不正操作 ]
                    |> 失敗を得る

                Expect.equal index 1 "静的不正の位置を返す"
                Expect.equal id (因果操作ID "batch.contract-fail") "静的不正操作IDを返す"
                Expect.equal 分類 契約不正 "静的契約不正を優先する"
                Expect.equal 返却状態 (状態を作る ()) "仮適用を開始しない"

            testCase "静的理由順が固定される" <| fun _ ->
                let 不正因果: 因果操作 =
                    {
                        ID = 因果操作ID ""
                        Tick = -1L
                        種別 = 伝播信号生成
                        原因因果ID = None
                        実行者ID = None
                        対象EntityID = None
                        概要 = " "
                        発行Event一覧 = []
                    }

                let 不正操作 =
                    {
                        因果 = 不正因果
                        変更後位置 = { X = Double.NaN; Y = Double.PositiveInfinity }
                    }

                let _, index, _, 分類, 理由一覧, _ =
                    因果操作列実行.一括適用する (状態を作る ()) [ 不正操作; 不正操作 ]
                    |> 失敗を得る

                Expect.equal index 0 "最小indexを返す"
                Expect.equal 分類 契約不正 "契約不正として返す"

                Expect.equal
                    理由一覧
                    [
                        "因果操作IDが空です。"
                        "因果操作のTickが負です。"
                        "因果操作の概要が空です。"
                        "因果操作種別が状態変更ではありません。"
                        "対象EntityIDが指定されていません。"
                        "変更後位置.Xが有限値ではありません。"
                        "変更後位置.Yが有限値ではありません。"
                        "因果操作一覧内で因果操作IDが重複しています。"
                    ]
                    "既存静的エラーの後に一覧内重複を返す"

            testCase "ゲーム内失敗理由順が固定される" <| fun _ ->
                let 終了済み = { 状態を作る () with 終了状態 = Some クリア }

                let 不正操作 =
                    {
                        操作を作る "batch.game-errors" (Some 存在しないID) 5.0 6.0 [] with
                            因果.Tick = 21L
                    }

                let _, index, _, 分類, 理由一覧, _ =
                    因果操作列実行.一括適用する 終了済み [ 不正操作 ]
                    |> 失敗を得る

                Expect.equal index 0 "最初の操作が失敗する"
                Expect.equal 分類 ゲーム内失敗 "ゲーム内失敗として返す"

                Expect.equal
                    理由一覧
                    [
                        "因果操作のTickが現在状態のTickと一致しません。"
                        "終了状態のため因果操作を適用できません。"
                        "対象Entityが存在しません。"
                    ]
                    "単一実行器のゲーム内理由順を維持する"

            testCase "失敗記録が失敗結果と一致する" <| fun _ ->
                let 操作 = 操作を作る "batch.failure-record" (Some 存在しないID) 5.0 6.0 []

                let _, index, id, 分類, 理由一覧, 記録 =
                    因果操作列実行.一括適用する (状態を作る ()) [ 操作 ]
                    |> 失敗を得る

                Expect.equal index 0 "失敗位置と一致する"
                Expect.equal id 記録.因果操作ID "失敗操作IDと一致する"
                Expect.equal 分類 ゲーム内失敗 "失敗分類と一致する"
                Expect.isFalse 記録.成功 "失敗を記録する"
                Expect.equal 記録.失敗理由一覧 理由一覧 "理由一覧と一致する"
                Expect.isNone 記録.変更前位置 "変更前位置を公開しない"
                Expect.isNone 記録.変更後位置 "変更後位置を公開しない"
                Expect.isEmpty 記録.発行Event一覧 "Eventを公開しない"

            testCase "全成功記録が仮状態推移と一致する" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "batch.transition.1" (Some 対象A) 5.0 6.0 []
                        操作を作る "batch.transition.2" (Some 対象A) 7.0 8.0 []
                        操作を作る "batch.transition.3" (Some 対象B) 9.0 10.0 []
                    ]

                let 更新, 記録一覧 = 因果操作列実行.一括適用する (状態を作る ()) 操作一覧 |> 成功を得る

                Expect.equal 記録一覧[0].変更前位置 (Some { X = 1.0; Y = 2.0 }) "最初の開始位置と一致する"
                Expect.equal 記録一覧[1].変更前位置 記録一覧[0].変更後位置 "同一対象の仮状態推移と一致する"
                Expect.equal 記録一覧[2].変更前位置 (Some { X = 3.0; Y = 4.0 }) "別対象の開始位置と一致する"
                Expect.equal 記録一覧[1].変更後位置 (Some (entityを得る 対象A 更新.状態).位置) "対象Aの最終状態と一致する"
                Expect.equal 記録一覧[2].変更後位置 (Some (entityを得る 対象B 更新.状態).位置) "対象Bの最終状態と一致する"
        ]
