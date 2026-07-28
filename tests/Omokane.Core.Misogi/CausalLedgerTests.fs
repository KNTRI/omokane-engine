module CausalLedgerTests

open System
open Expecto
open Omokane.Core.Domain

let private プレイヤーID = エンティティID "player"
let private 対象A = エンティティID "target.a"
let private 対象B = エンティティID "target.b"

let private 候補を作る id targetID beforePosition afterPosition event一覧 : 因果記録候補 =
    {
        因果操作ID = 因果操作ID id
        Tick = 30L
        成功 = true
        実行者ID = Some プレイヤーID
        対象EntityID = targetID
        概要 = "Entity位置を変更した"
        変更前位置 = beforePosition
        変更後位置 = afterPosition
        発行Event一覧 = event一覧
        失敗理由一覧 = []
    }

let private 正常候補 id =
    候補を作る
        id
        (Some 対象A)
        (Some { X = 1.0; Y = 2.0 })
        (Some { X = 3.0; Y = 4.0 })
        []

let private 追記成功を得る = function
    | 追記成功 台帳 -> 台帳
    | 追記失敗(_, index, id, 理由一覧) ->
        failtestf "追記が失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

let private 追記失敗を得る = function
    | 追記失敗(台帳, index, id, 理由一覧) -> 台帳, index, id, 理由一覧
    | 追記成功 台帳 ->
        failtestf "追記が成功しました: records=%A" (因果台帳.記録一覧 台帳)

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
        Tick = 30L
        プレイヤーID = プレイヤーID
        エンティティ一覧 =
            [
                entityを作る プレイヤーID 0.0 0.0
                entityを作る 対象A 1.0 2.0
                entityを作る 対象B 5.0 6.0
            ]
        乱数Seed = 789
        終了状態 = None
    }

let private 操作を作る id targetID x y event一覧 =
    let 因果: 因果操作 =
        {
            ID = 因果操作ID id
            Tick = 30L
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

[<Tests>]
let 全テスト =
    testList
        "因果台帳の最小追記"
        [
            testCase "空一覧を追記すると内容不変で成功する" <| fun _ ->
                let 台帳 = 因果台帳.空
                let 更新後 = 因果台帳.追記する 台帳 [] |> 追記成功を得る

                Expect.equal 更新後 台帳 "台帳値を維持する"
                Expect.equal (因果台帳.件数 更新後) 0 "件数を維持する"
                Expect.isEmpty (因果台帳.記録一覧 更新後) "記録順を維持する"

            testCase "空台帳へ1件追記できる" <| fun _ ->
                let 更新後 =
                    因果台帳.追記する 因果台帳.空 [ 正常候補 "ledger.one" ]
                    |> 追記成功を得る

                Expect.equal (因果台帳.件数 更新後) 1 "1件を保存する"
                Expect.equal
                    (因果台帳.記録一覧 更新後 |> List.head |> fun 記録 -> 記録.因果操作ID)
                    (因果操作ID "ledger.one")
                    "因果操作IDを保存する"

            testCase "複数候補が入力順で記録される" <| fun _ ->
                let 更新後 =
                    因果台帳.追記する
                        因果台帳.空
                        [ 正常候補 "ledger.first"; 正常候補 "ledger.second" ]
                    |> 追記成功を得る

                Expect.equal
                    (因果台帳.記録一覧 更新後 |> List.map (fun 記録 -> 記録.因果操作ID))
                    [ 因果操作ID "ledger.first"; 因果操作ID "ledger.second" ]
                    "候補の入力順を正式な因果順とする"

            testCase "既存記録の後ろへ新規記録が追加される" <| fun _ ->
                let 既存台帳 =
                    因果台帳.追記する 因果台帳.空 [ 正常候補 "ledger.existing" ]
                    |> 追記成功を得る

                let 更新後 =
                    因果台帳.追記する
                        既存台帳
                        [ 正常候補 "ledger.next"; 正常候補 "ledger.last" ]
                    |> 追記成功を得る

                Expect.equal
                    (因果台帳.記録一覧 更新後 |> List.map (fun 記録 -> 記録.因果操作ID))
                    [
                        因果操作ID "ledger.existing"
                        因果操作ID "ledger.next"
                        因果操作ID "ledger.last"
                    ]
                    "既存順の後ろへ入力順で追記する"

            testCase "Event一覧の順序を維持する" <| fun _ ->
                let event一覧 =
                    [
                        エンティティ生成 対象A
                        エンティティ削除 対象B
                        エンティティ生成 対象B
                    ]

                let 候補 =
                    候補を作る
                        "ledger.events"
                        (Some 対象A)
                        (Some { X = 1.0; Y = 2.0 })
                        (Some { X = 3.0; Y = 4.0 })
                        event一覧

                let 更新後 = 因果台帳.追記する 因果台帳.空 [ 候補 ] |> 追記成功を得る
                let 記録 = 因果台帳.記録一覧 更新後 |> List.head

                Expect.equal 記録.発行Event一覧 event一覧 "操作内のEvent順を変更しない"

            testCase "対象IDと変更前後位置を正式記録へ保存する" <| fun _ ->
                let 変更前: 位置 = { X = 7.0; Y = 8.0 }
                let 変更後: 位置 = { X = 9.0; Y = 10.0 }

                let 候補 =
                    候補を作る
                        "ledger.positions"
                        (Some 対象B)
                        (Some 変更前)
                        (Some 変更後)
                        []

                let 更新後 = 因果台帳.追記する 因果台帳.空 [ 候補 ] |> 追記成功を得る
                let 記録 = 因果台帳.記録一覧 更新後 |> List.head

                Expect.equal 記録.対象EntityID 対象B "対象IDを非Optionで保存する"
                Expect.equal 記録.変更前位置 変更前 "変更前位置を非Optionで保存する"
                Expect.equal 記録.変更後位置 変更後 "変更後位置を非Optionで保存する"

            testCase "入力台帳を変更しない" <| fun _ ->
                let 入力台帳 =
                    因果台帳.追記する 因果台帳.空 [ 正常候補 "ledger.before" ]
                    |> 追記成功を得る

                let 入力記録一覧 = 因果台帳.記録一覧 入力台帳

                因果台帳.追記する 入力台帳 [ 正常候補 "ledger.after" ]
                |> ignore

                Expect.equal (因果台帳.記録一覧 入力台帳) 入力記録一覧 "入力値は不変である"

            testCase "同じ入力から同じ結果を返す" <| fun _ ->
                let 候補一覧 = [ 正常候補 "ledger.a"; 正常候補 "ledger.b" ]

                Expect.equal
                    (因果台帳.追記する 因果台帳.空 候補一覧)
                    (因果台帳.追記する 因果台帳.空 候補一覧)
                    "構造的に同じ結果を返す"

            testCase "成功falseの候補を拒否する" <| fun _ ->
                let 不正候補 = { 正常候補 "ledger.failed" with 成功 = false }
                let 台帳, index, id, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal 台帳 因果台帳.空 "元台帳を返す"
                Expect.equal index 0 "失敗位置を返す"
                Expect.equal id (因果操作ID "ledger.failed") "失敗操作IDを返す"
                Expect.equal 理由一覧 [ "因果記録候補が成功記録ではありません。" ] "失敗記録を拒否する"

            testCase "成功記録に失敗理由があれば拒否する" <| fun _ ->
                let 不正候補 =
                    {
                        正常候補 "ledger.failure-reason" with
                            失敗理由一覧 = [ "失敗している" ]
                    }

                let _, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal
                    理由一覧
                    [ "成功した因果記録候補に失敗理由があります。" ]
                    "成功フラグと失敗理由の矛盾を拒否する"

            testCase "対象EntityIDなしを拒否する" <| fun _ ->
                let 不正候補 = { 正常候補 "ledger.no-target" with 対象EntityID = None }
                let _, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal 理由一覧 [ "対象EntityIDが指定されていません。" ] "対象なしを拒否する"

            testCase "変更前位置なしを拒否する" <| fun _ ->
                let 不正候補 = { 正常候補 "ledger.no-before" with 変更前位置 = None }
                let _, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal 理由一覧 [ "変更前位置が指定されていません。" ] "変更前位置なしを拒否する"

            testCase "変更後位置なしを拒否する" <| fun _ ->
                let 不正候補 = { 正常候補 "ledger.no-after" with 変更後位置 = None }
                let _, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal 理由一覧 [ "変更後位置が指定されていません。" ] "変更後位置なしを拒否する"

            testCase "位置のNaNとInfinityを拒否する" <| fun _ ->
                let 不正一覧 =
                    [
                        { 正常候補 "ledger.before-x" with 変更前位置 = Some { X = Double.NaN; Y = 0.0 } },
                        "変更前位置.Xが有限値ではありません。"
                        { 正常候補 "ledger.before-y" with 変更前位置 = Some { X = 0.0; Y = Double.PositiveInfinity } },
                        "変更前位置.Yが有限値ではありません。"
                        { 正常候補 "ledger.after-x" with 変更後位置 = Some { X = Double.NegativeInfinity; Y = 0.0 } },
                        "変更後位置.Xが有限値ではありません。"
                        { 正常候補 "ledger.after-y" with 変更後位置 = Some { X = 0.0; Y = Double.NaN } },
                        "変更後位置.Yが有限値ではありません。"
                    ]

                不正一覧
                |> List.iter (fun (不正候補, 期待理由) ->
                    let _, _, _, 理由一覧 =
                        因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                        |> 追記失敗を得る

                    Expect.equal 理由一覧 [ 期待理由 ] "有限値でない座標を拒否する")

            testCase "候補一覧内の重複IDを最小indexで拒否する" <| fun _ ->
                let 候補一覧 =
                    [
                        正常候補 "ledger.unique"
                        正常候補 "ledger.duplicate"
                        正常候補 "ledger.duplicate"
                    ]

                let 台帳, index, id, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 候補一覧
                    |> 追記失敗を得る

                Expect.equal 台帳 因果台帳.空 "一件も追記しない"
                Expect.equal index 1 "重複IDを持つ最小indexを返す"
                Expect.equal id (因果操作ID "ledger.duplicate") "重複IDを返す"
                Expect.equal
                    理由一覧
                    [ "因果記録候補一覧内で因果操作IDが重複しています。" ]
                    "候補一覧内重複として返す"

            testCase "既存台帳に存在するIDを拒否する" <| fun _ ->
                let 既存台帳 =
                    因果台帳.追記する 因果台帳.空 [ 正常候補 "ledger.existing" ]
                    |> 追記成功を得る

                let 返却台帳, index, id, 理由一覧 =
                    因果台帳.追記する 既存台帳 [ 正常候補 "ledger.existing" ]
                    |> 追記失敗を得る

                Expect.equal 返却台帳 既存台帳 "元台帳を返す"
                Expect.equal index 0 "重複候補の位置を返す"
                Expect.equal id (因果操作ID "ledger.existing") "既存IDを返す"
                Expect.equal
                    理由一覧
                    [ "因果台帳に同じ因果操作IDが既に存在します。" ]
                    "既存台帳との重複として返す"

            testCase "後方の候補が不正でも一件も追記しない" <| fun _ ->
                let 既存台帳 =
                    因果台帳.追記する 因果台帳.空 [ 正常候補 "ledger.base" ]
                    |> 追記成功を得る

                let 候補一覧 =
                    [
                        正常候補 "ledger.valid"
                        { 正常候補 "ledger.invalid" with 対象EntityID = None }
                    ]

                let 返却台帳, index, _, _ =
                    因果台帳.追記する 既存台帳 候補一覧
                    |> 追記失敗を得る

                Expect.equal index 1 "後方の不正位置を返す"
                Expect.equal 返却台帳 既存台帳 "部分追記を公開しない"
                Expect.equal (因果台帳.件数 返却台帳) 1 "既存件数を維持する"

            testCase "複数の不正理由を固定順で返す" <| fun _ ->
                let 既存台帳 =
                    因果台帳.追記する 因果台帳.空 [ 正常候補 "ledger.invalid" ]
                    |> 追記成功を得る

                let 不正候補 =
                    {
                        正常候補 "ledger.invalid" with
                            Tick = -1L
                            成功 = false
                            概要 = " "
                            対象EntityID = None
                            変更前位置 = Some { X = Double.NaN; Y = Double.PositiveInfinity }
                            変更後位置 = Some { X = Double.NegativeInfinity; Y = Double.NaN }
                            失敗理由一覧 = [ "失敗" ]
                    }

                let _, index, _, 理由一覧 =
                    因果台帳.追記する 既存台帳 [ 不正候補; 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal index 0 "最小indexを返す"

                Expect.equal
                    理由一覧
                    [
                        "因果記録候補が成功記録ではありません。"
                        "成功した因果記録候補に失敗理由があります。"
                        "因果操作のTickが負です。"
                        "因果台帳のTick順序が逆行しています。"
                        "因果操作の概要が空です。"
                        "対象EntityIDが指定されていません。"
                        "変更前位置.Xが有限値ではありません。"
                        "変更前位置.Yが有限値ではありません。"
                        "変更後位置.Xが有限値ではありません。"
                        "変更後位置.Yが有限値ではありません。"
                        "因果台帳に同じ因果操作IDが既に存在します。"
                        "因果記録候補一覧内で因果操作IDが重複しています。"
                    ]
                    "固定された検証順で返す"

            testCase "空白の因果操作IDを拒否する" <| fun _ ->
                let 不正候補 = { 正常候補 " " with 因果操作ID = 因果操作ID " " }
                let _, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal 理由一覧 [ "因果操作IDが空です。" ] "空白IDを拒否する"

            testCase "負のTickを拒否する" <| fun _ ->
                let 不正候補 = { 正常候補 "ledger.negative-tick" with Tick = -1L }
                let _, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal 理由一覧 [ "因果操作のTickが負です。" ] "負のTickを拒否する"

            testCase "空白の概要を拒否する" <| fun _ ->
                let 不正候補 = { 正常候補 "ledger.blank-summary" with 概要 = " " }
                let _, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 不正候補 ]
                    |> 追記失敗を得る

                Expect.equal 理由一覧 [ "因果操作の概要が空です。" ] "空白概要を拒否する"

            testCase "一括適用成功の記録候補一覧を追記できる" <| fun _ ->
                let 操作一覧 =
                    [
                        操作を作る "ledger.batch.a" (Some 対象A) 3.0 4.0 [ エンティティ生成 対象A ]
                        操作を作る "ledger.batch.b" (Some 対象B) 7.0 8.0 [ エンティティ削除 対象B ]
                    ]

                let 記録候補一覧 =
                    match 因果操作列実行.一括適用する (状態を作る ()) 操作一覧 with
                    | 一括適用成功(_, 記録一覧) -> 記録一覧
                    | 結果 -> failtestf "一括適用が失敗しました: %A" 結果

                let 台帳 =
                    因果台帳.追記する 因果台帳.空 記録候補一覧
                    |> 追記成功を得る

                Expect.equal (因果台帳.件数 台帳) 2 "全成功記録を追記する"
                Expect.equal
                    (因果台帳.記録一覧 台帳 |> List.map (fun 記録 -> 記録.因果操作ID))
                    [ 因果操作ID "ledger.batch.a"; 因果操作ID "ledger.batch.b" ]
                    "一括適用の因果順を維持する"

            testCase "一括適用失敗の失敗記録を追記できない" <| fun _ ->
                let 存在しないID = エンティティID "missing"
                let 操作 = 操作を作る "ledger.batch.failure" (Some 存在しないID) 3.0 4.0 []

                let 失敗記録 =
                    match 因果操作列実行.一括適用する (状態を作る ()) [ 操作 ] with
                    | 一括適用失敗(_, _, _, _, _, 記録) -> 記録
                    | 結果 -> failtestf "一括適用が成功しました: %A" 結果

                let 返却台帳, _, _, 理由一覧 =
                    因果台帳.追記する 因果台帳.空 [ 失敗記録 ]
                    |> 追記失敗を得る

                Expect.equal 返却台帳 因果台帳.空 "正式台帳を変更しない"
                Expect.contains 理由一覧 "因果記録候補が成功記録ではありません。" "失敗記録を拒否する"
        ]
