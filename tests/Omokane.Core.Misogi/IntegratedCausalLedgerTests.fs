module IntegratedCausalLedgerTests

open System
open Expecto
open Omokane.Core.Domain

module TestData =

    let プレイヤーID = エンティティID "integrated.player"
    let 発生者ID = エンティティID "integrated.emitter"
    let 観測者ID = エンティティID "integrated.observer"
    let 対象A = エンティティID "integrated.target.a"
    let 対象B = エンティティID "integrated.target.b"
    let 不在ID = エンティティID "integrated.missing"

    let 位置 x y : 位置 = { X = x; Y = y }

    let entity id x y =
        {
            ID = id
            種別 = 敵
            所有者 = 中立
            位置 = 位置 x y
            速度 = { X = 0.25; Y = -0.5 }
            当たり判定 = None
            HP = Some(HP 9)
        }

    let 状態 tick =
        {
            Tick = tick
            プレイヤーID = プレイヤーID
            エンティティ一覧 =
                [
                    entity プレイヤーID 0.0 0.0
                    entity 発生者ID 5.0 5.0
                    entity 観測者ID 8.0 8.0
                    entity 対象A 1.0 2.0
                    entity 対象B 10.0 20.0
                ]
            エンティティ反応状態一覧 = []
            乱数Seed = 9876
            終了状態 = None
        }

    let 位置変更候補 id tick targetID beforePosition afterPosition event一覧 : 因果記録候補 =
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

    let 基本位置変更候補 id tick =
        位置変更候補 id tick 対象A (位置 1.0 2.0) (位置 3.0 4.0) []

    let 地盤振動記録 id signalID tick executor event一覧 : 地盤振動発生記録 =
        let 因果ID = 因果操作ID id

        {
            因果操作ID = 因果ID
            Tick = tick
            実行者ID = executor
            概要 = "地盤振動を発生した"
            信号 =
                {
                    ID = 伝播信号ID signalID
                    原因因果ID = Some 因果ID
                    発生源EntityID = executor
                    量種別 = 地盤振動
                    媒体 = 地盤
                    発生位置 = 位置 5.0 5.0
                    方向 = Some { X = 1.0; Y = 0.0 }
                    強度 = 10.0
                    方式 = 波動
                    寿命Tick = 5L
                }
            発行Event一覧 = event一覧
        }

    let 基本地盤振動記録 id signalID tick =
        地盤振動記録 id signalID tick (Some 発生者ID) []

    let 位置項目 候補 = 統合因果追記項目.位置変更候補 候補
    let 振動項目 記録 = 統合因果追記項目.地盤振動記録 記録

    let 統合追記成功を得る = function
        | 統合因果台帳追記結果.成功 台帳 -> 台帳
        | 統合因果台帳追記結果.失敗(_, index, id, 理由一覧) ->
            failtestf "統合追記が失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

    let 統合追記失敗を得る = function
        | 統合因果台帳追記結果.失敗(台帳, index, id, 理由一覧) ->
            台帳, index, id, 理由一覧
        | 統合因果台帳追記結果.成功 台帳 ->
            failtestf "統合追記が成功しました: records=%A" (統合因果台帳.記録一覧 台帳)

    let 統合台帳を作る 項目一覧 =
        統合因果台帳.一括追記する 統合因果台帳.空 項目一覧
        |> 統合追記成功を得る

    let 既存追記成功を得る = function
        | 追記成功 台帳 -> 台帳
        | 追記失敗(_, index, id, 理由一覧) ->
            failtestf "既存追記が失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

    let 既存台帳を作る 候補一覧 =
        因果台帳.追記する 因果台帳.空 候補一覧
        |> 既存追記成功を得る

open TestData

[<Tests>]
let 全テスト =
    testList
        "統合因果台帳"
        [
            testCase "空台帳の件数は0" <| fun _ ->
                Expect.equal (統合因果台帳.件数 統合因果台帳.空) 0 "空台帳である"

            testCase "空追記は恒等成功" <| fun _ ->
                let 結果 = 統合因果台帳.一括追記する 統合因果台帳.空 []
                let 台帳 = 統合追記成功を得る 結果
                Expect.equal 台帳 統合因果台帳.空 "同じ空台帳を返す"

            testCase "位置変更候補一件を追記できる" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "causal.position" 10L |> 位置項目 ]
                Expect.equal (統合因果台帳.Entity位置変更件数 台帳) 1 "位置変更一件"

            testCase "地盤振動発生記録一件を追記できる" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "causal.vibration" "signal.1" 10L |> 振動項目 ]
                Expect.equal (統合因果台帳.地盤振動発生件数 台帳) 1 "地盤振動一件"

            testCase "位置変更と地盤振動を混在追記できる" <| fun _ ->
                let 台帳 =
                    統合台帳を作る
                        [
                            基本位置変更候補 "causal.position" 10L |> 位置項目
                            基本地盤振動記録 "causal.vibration" "signal.1" 10L |> 振動項目
                        ]
                Expect.equal (統合因果台帳.件数 台帳) 2 "異種二件"

            testCase "混在入力順を維持する" <| fun _ ->
                let 台帳 =
                    統合台帳を作る
                        [
                            基本地盤振動記録 "causal.vibration" "signal.1" 10L |> 振動項目
                            基本位置変更候補 "causal.position" 10L |> 位置項目
                        ]
                let 種別一覧 = 統合因果台帳.記録一覧 台帳 |> List.map 統合因果記録.種別
                Expect.equal 種別一覧 [ 統合因果記録種別.地盤振動発生; 統合因果記録種別.Entity位置変更 ] "入力順"

            testCase "既存記録の後ろへ追記する" <| fun _ ->
                let 最初 = 統合台帳を作る [ 基本位置変更候補 "causal.first" 10L |> 位置項目 ]
                let 台帳 =
                    統合因果台帳.一括追記する 最初 [ 基本地盤振動記録 "causal.second" "signal.2" 11L |> 振動項目 ]
                    |> 統合追記成功を得る
                let ids = 統合因果台帳.記録一覧 台帳 |> List.map 統合因果記録.因果操作ID
                Expect.equal ids [ 因果操作ID "causal.first"; 因果操作ID "causal.second" ] "後方追記"

            testCase "同一Tick混在記録を許可する" <| fun _ ->
                let 台帳 =
                    統合台帳を作る
                        [
                            基本位置変更候補 "causal.position" 10L |> 位置項目
                            基本地盤振動記録 "causal.vibration" "signal.1" 10L |> 振動項目
                        ]
                Expect.equal (統合因果台帳.記録一覧 台帳 |> List.map 統合因果記録.Tick) [ 10L; 10L ] "同一Tick"

            testCase "Tick増加を許可する" <| fun _ ->
                let 台帳 =
                    統合台帳を作る
                        [
                            基本位置変更候補 "causal.position" 10L |> 位置項目
                            基本地盤振動記録 "causal.vibration" "signal.1" 15L |> 振動項目
                        ]
                Expect.equal (統合因果台帳.件数 台帳) 2 "Tick増加"

            testCase "候補一覧内Tick逆行を拒否する" <| fun _ ->
                let 結果 =
                    統合因果台帳.一括追記する
                        統合因果台帳.空
                        [
                            基本位置変更候補 "causal.first" 12L |> 位置項目
                            基本地盤振動記録 "causal.second" "signal.2" 11L |> 振動項目
                        ]
                let _, _, _, 理由一覧 = 統合追記失敗を得る 結果
                Expect.contains 理由一覧 "統合因果台帳のTick順序が逆行しています。" "逆行拒否"

            testCase "既存末尾Tickより前を拒否する" <| fun _ ->
                let 現在 = 統合台帳を作る [ 基本位置変更候補 "causal.first" 12L |> 位置項目 ]
                let 結果 = 統合因果台帳.一括追記する 現在 [ 基本地盤振動記録 "causal.second" "signal.2" 11L |> 振動項目 ]
                let 台帳, _, _, _ = 統合追記失敗を得る 結果
                Expect.equal 台帳 現在 "元台帳"

            testCase "Tick逆行の最小indexを返す" <| fun _ ->
                let 結果 =
                    統合因果台帳.一括追記する
                        統合因果台帳.空
                        [
                            基本位置変更候補 "causal.0" 10L |> 位置項目
                            基本地盤振動記録 "causal.1" "signal.1" 9L |> 振動項目
                            基本地盤振動記録 "causal.2" "signal.2" 8L |> 振動項目
                        ]
                let _, index, _, _ = 統合追記失敗を得る 結果
                Expect.equal index 1 "最初の逆行"

            testCase "因果操作ID重複を種別横断で拒否する" <| fun _ ->
                let 結果 =
                    統合因果台帳.一括追記する
                        統合因果台帳.空
                        [
                            基本位置変更候補 "causal.same" 10L |> 位置項目
                            基本地盤振動記録 "causal.same" "signal.1" 10L |> 振動項目
                        ]
                let _, index, _, 理由一覧 = 統合追記失敗を得る 結果
                Expect.equal index 0 "最小index"
                Expect.contains 理由一覧 "統合因果追記項目一覧内で因果操作IDが重複しています。" "種別横断"

            testCase "既存位置変更IDと新規地盤振動IDの重複を拒否する" <| fun _ ->
                let 現在 = 統合台帳を作る [ 基本位置変更候補 "causal.same" 10L |> 位置項目 ]
                let 結果 = 統合因果台帳.一括追記する 現在 [ 基本地盤振動記録 "causal.same" "signal.2" 10L |> 振動項目 ]
                let _, _, _, 理由一覧 = 統合追記失敗を得る 結果
                Expect.contains 理由一覧 "統合因果台帳に同じ因果操作IDが既に存在します。" "既存ID"

            testCase "既存地盤振動IDと新規位置変更IDの重複を拒否する" <| fun _ ->
                let 現在 = 統合台帳を作る [ 基本地盤振動記録 "causal.same" "signal.1" 10L |> 振動項目 ]
                let 結果 = 統合因果台帳.一括追記する 現在 [ 基本位置変更候補 "causal.same" 10L |> 位置項目 ]
                let _, _, _, 理由一覧 = 統合追記失敗を得る 結果
                Expect.contains 理由一覧 "統合因果台帳に同じ因果操作IDが既に存在します。" "既存ID"

            testCase "今回一覧内の種別横断ID重複を拒否する" <| fun _ ->
                let 結果 =
                    統合因果台帳.一括追記する 統合因果台帳.空
                        [ 基本地盤振動記録 "causal.same" "signal.1" 10L |> 振動項目; 基本位置変更候補 "causal.same" 10L |> 位置項目 ]
                let _, index, _, _ = 統合追記失敗を得る 結果
                Expect.equal index 0 "一覧の最小index"

            testCase "既存伝播信号ID重複を拒否する" <| fun _ ->
                let 現在 = 統合台帳を作る [ 基本地盤振動記録 "causal.1" "signal.same" 10L |> 振動項目 ]
                let 結果 = 統合因果台帳.一括追記する 現在 [ 基本地盤振動記録 "causal.2" "signal.same" 10L |> 振動項目 ]
                let _, _, _, 理由一覧 = 統合追記失敗を得る 結果
                Expect.contains 理由一覧 "統合因果台帳に同じ伝播信号IDが既に存在します。" "既存信号"

            testCase "今回一覧内伝播信号ID重複を拒否する" <| fun _ ->
                let 結果 =
                    統合因果台帳.一括追記する 統合因果台帳.空
                        [ 基本地盤振動記録 "causal.1" "signal.same" 10L |> 振動項目; 基本地盤振動記録 "causal.2" "signal.same" 10L |> 振動項目 ]
                let _, index, _, 理由一覧 = 統合追記失敗を得る 結果
                Expect.equal index 0 "信号重複の最小index"
                Expect.contains 理由一覧 "統合因果追記項目一覧内で伝播信号IDが重複しています。" "一覧内信号"

            testCase "後方の不正位置変更候補でも一件も追記しない" <| fun _ ->
                let 不正 = { 基本位置変更候補 "causal.bad" 10L with 成功 = false }
                let 結果 = 統合因果台帳.一括追記する 統合因果台帳.空 [ 基本位置変更候補 "causal.good" 10L |> 位置項目; 位置項目 不正 ]
                let 台帳, index, _, _ = 統合追記失敗を得る 結果
                Expect.equal index 1 "後方失敗"
                Expect.equal (統合因果台帳.件数 台帳) 0 "部分追記なし"

            testCase "後方の不正地盤振動記録でも一件も追記しない" <| fun _ ->
                let 基本 = 基本地盤振動記録 "causal.bad" "signal.bad" 10L
                let 不正 = { 基本 with 信号 = { 基本.信号 with 強度 = Double.NaN } }
                let 結果 = 統合因果台帳.一括追記する 統合因果台帳.空 [ 基本位置変更候補 "causal.good" 10L |> 位置項目; 振動項目 不正 ]
                let 台帳, index, _, _ = 統合追記失敗を得る 結果
                Expect.equal index 1 "後方失敗"
                Expect.equal (統合因果台帳.件数 台帳) 0 "部分追記なし"

            testCase "位置変更候補の既存エラーを接頭辞付きで返す" <| fun _ ->
                let 不正 = { 基本位置変更候補 "causal.bad" 10L with 成功 = false }
                let _, _, _, 理由一覧 = 統合因果台帳.一括追記する 統合因果台帳.空 [ 位置項目 不正 ] |> 統合追記失敗を得る
                Expect.contains 理由一覧 "位置変更候補: 因果記録候補が成功記録ではありません。" "接頭辞"

            testCase "地盤振動記録の既存エラーを接頭辞付きで返す" <| fun _ ->
                let 基本 = 基本地盤振動記録 "causal.bad" "signal.bad" 10L
                let 不正 = { 基本 with 信号 = { 基本.信号 with 原因因果ID = None } }
                let _, _, _, 理由一覧 = 統合因果台帳.一括追記する 統合因果台帳.空 [ 振動項目 不正 ] |> 統合追記失敗を得る
                Expect.contains 理由一覧 "地盤振動発生記録: 地盤振動発生記録の信号原因因果IDが因果操作IDと一致しません。" "接頭辞"

            testCase "項目固有エラーを共通エラーより先に返す" <| fun _ ->
                let 現在 = 統合台帳を作る [ 基本位置変更候補 "causal.same" 20L |> 位置項目 ]
                let 不正 = { 基本位置変更候補 "causal.same" 10L with 成功 = false }
                let _, _, _, 理由一覧 = 統合因果台帳.一括追記する 現在 [ 位置項目 不正 ] |> 統合追記失敗を得る
                Expect.equal 理由一覧
                    [ "位置変更候補: 因果記録候補が成功記録ではありません。"; "統合因果台帳のTick順序が逆行しています。"; "統合因果台帳に同じ因果操作IDが既に存在します。" ]
                    "固有から共通の固定順"

            testCase "複数理由を固定順で返す" <| fun _ ->
                let 不正 =
                    { 基本位置変更候補 "" -1L with
                        成功 = false
                        失敗理由一覧 = [ "失敗" ]
                        概要 = ""
                        対象EntityID = None
                        変更前位置 = None
                        変更後位置 = None }
                let _, _, _, 理由一覧 = 統合因果台帳.一括追記する 統合因果台帳.空 [ 位置項目 不正 ] |> 統合追記失敗を得る
                Expect.equal 理由一覧
                    [
                        "位置変更候補: 因果記録候補が成功記録ではありません。"
                        "位置変更候補: 成功した因果記録候補に失敗理由があります。"
                        "位置変更候補: 因果操作IDが空です。"
                        "位置変更候補: 因果操作のTickが負です。"
                        "位置変更候補: 因果操作の概要が空です。"
                        "位置変更候補: 対象EntityIDが指定されていません。"
                        "位置変更候補: 変更前位置が指定されていません。"
                        "位置変更候補: 変更後位置が指定されていません。"
                    ] "固定順"

            testCase "Event順を維持する" <| fun _ ->
                let eventsA = [ Tick進行 10L; ダメージ発生(対象A, 1) ]
                let eventsB = [ エンティティ削除 対象B; ゲーム終了 クリア ]
                let 台帳 = 統合台帳を作る [ 位置変更候補 "causal.1" 10L 対象A (位置 1.0 2.0) (位置 3.0 4.0) eventsA |> 位置項目; 地盤振動記録 "causal.2" "signal.2" 10L (Some 発生者ID) eventsB |> 振動項目 ]
                let eventLists = 統合因果台帳.記録一覧 台帳 |> List.map 統合因果記録.発行Event一覧
                Expect.equal eventLists [ eventsA; eventsB ] "記録内と記録間の順序"

            testCase "IDによる再ソートを行わない" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "z.last" 10L |> 位置項目; 基本地盤振動記録 "a.first" "signal.a" 10L |> 振動項目 ]
                let ids = 統合因果台帳.記録一覧 台帳 |> List.map 統合因果記録.因果操作ID
                Expect.equal ids [ 因果操作ID "z.last"; 因果操作ID "a.first" ] "入力順"

            testCase "Tickによる再ソートを行わない" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "z" "signal.z" 10L |> 振動項目; 基本位置変更候補 "a" 10L |> 位置項目 ]
                let ids = 統合因果台帳.記録一覧 台帳 |> List.map 統合因果記録.因果操作ID
                Expect.equal ids [ 因果操作ID "z"; 因果操作ID "a" ] "同一Tick入力順"

            testCase "Entity位置変更件数を返す" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目; 基本位置変更候補 "p2" 10L |> 位置項目 ]
                Expect.equal (統合因果台帳.Entity位置変更件数 台帳) 2 "位置件数"

            testCase "地盤振動発生件数を返す" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 10L |> 振動項目; 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v2" "s2" 10L |> 振動項目 ]
                Expect.equal (統合因果台帳.地盤振動発生件数 台帳) 2 "振動件数"

            testCase "位置変更記録一覧だけを抽出できる" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目; 基本位置変更候補 "p2" 10L |> 位置項目 ]
                let ids = 統合因果台帳.Entity位置変更記録一覧 台帳 |> List.map (fun r -> r.因果操作ID)
                Expect.equal ids [ 因果操作ID "p1"; 因果操作ID "p2" ] "位置だけ"

            testCase "地盤振動発生記録一覧だけを抽出できる" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 10L |> 振動項目; 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v2" "s2" 10L |> 振動項目 ]
                let ids = 統合因果台帳.地盤振動発生記録一覧 台帳 |> List.map (fun r -> r.因果操作ID)
                Expect.equal ids [ 因果操作ID "v1"; 因果操作ID "v2" ] "振動だけ"

            testCase "因果操作ID検索が正しい" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ]
                let found = 統合因果台帳.因果操作IDで探す (因果操作ID "v1") 台帳 |> Option.map 統合因果記録.種別
                Expect.equal found (Some 統合因果記録種別.地盤振動発生) "該当記録"

            testCase "伝播信号ID検索が正しい" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ]
                let found = 統合因果台帳.伝播信号IDで探す (伝播信号ID "s1") 台帳
                Expect.equal (found |> Option.map (fun r -> r.因果操作ID)) (Some(因果操作ID "v1")) "信号検索"

            testCase "不存在ID検索はNone" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目 ]
                Expect.isNone (統合因果台帳.因果操作IDで探す (因果操作ID "missing") 台帳) "因果IDなし"
                Expect.isNone (統合因果台帳.伝播信号IDで探す (伝播信号ID "missing") 台帳) "信号IDなし"

            testCase "種別アクセサが正しい" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ]
                let kinds = 統合因果台帳.記録一覧 台帳 |> List.map 統合因果記録.種別
                Expect.equal kinds [ 統合因果記録種別.Entity位置変更; 統合因果記録種別.地盤振動発生 ] "種別"

            testCase "共通メタデータアクセサが正しい" <| fun _ ->
                let events = [ Tick進行 10L ]
                let 台帳 = 統合台帳を作る [ 地盤振動記録 "v1" "s1" 10L (Some 発生者ID) events |> 振動項目 ]
                let record = 統合因果台帳.記録一覧 台帳 |> List.exactlyOne
                Expect.equal (統合因果記録.因果操作ID record) (因果操作ID "v1") "因果ID"
                Expect.equal (統合因果記録.Tick record) 10L "Tick"
                Expect.equal (統合因果記録.実行者ID record) (Some 発生者ID) "実行者"
                Expect.equal (統合因果記録.概要 record) "地盤振動を発生した" "概要"
                Expect.equal (統合因果記録.発行Event一覧 record) events "Event"

            testCase "既存因果台帳の空移行" <| fun _ ->
                let 台帳 = 統合因果台帳.既存因果台帳から移行する 因果台帳.空
                Expect.equal 台帳 統合因果台帳.空 "空移行"

            testCase "既存因果台帳の記録順を維持して移行" <| fun _ ->
                let 既存 = 既存台帳を作る [ 基本位置変更候補 "p2" 10L; 基本位置変更候補 "p1" 10L ]
                let ids = 統合因果台帳.既存因果台帳から移行する 既存 |> 統合因果台帳.記録一覧 |> List.map 統合因果記録.因果操作ID
                Expect.equal ids [ 因果操作ID "p2"; 因果操作ID "p1" ] "移行順"

            testCase "既存因果台帳の件数を維持して移行" <| fun _ ->
                let 既存 = 既存台帳を作る [ 基本位置変更候補 "p1" 10L; 基本位置変更候補 "p2" 10L ]
                let 統合 = 統合因果台帳.既存因果台帳から移行する 既存
                Expect.equal (統合因果台帳.件数 統合) (因果台帳.件数 既存) "件数"

            testCase "移行後に地盤振動記録を追加できる" <| fun _ ->
                let 既存 = 既存台帳を作る [ 基本位置変更候補 "p1" 10L ]
                let 統合 = 統合因果台帳.既存因果台帳から移行する 既存
                let 更新後 = 統合因果台帳.地盤振動発生記録一覧を追記する 統合 [ 基本地盤振動記録 "v1" "s1" 10L ] |> 統合追記成功を得る
                Expect.equal (統合因果台帳.件数 更新後) 2 "異種追加"

            testCase "移行後に過去Tickを追加できない" <| fun _ ->
                let 既存 = 既存台帳を作る [ 基本位置変更候補 "p1" 12L ]
                let 統合 = 統合因果台帳.既存因果台帳から移行する 既存
                let _, _, _, reasons = 統合因果台帳.地盤振動発生記録一覧を追記する 統合 [ 基本地盤振動記録 "v1" "s1" 11L ] |> 統合追記失敗を得る
                Expect.contains reasons "統合因果台帳のTick順序が逆行しています。" "移行末尾"

            testCase "元の既存台帳を変更しない" <| fun _ ->
                let 既存 = 既存台帳を作る [ 基本位置変更候補 "p1" 10L ]
                let before = 因果台帳.記録一覧 既存
                let _ = 統合因果台帳.既存因果台帳から移行する 既存
                Expect.equal (因果台帳.記録一覧 既存) before "元値不変"

            testCase "入力項目一覧を変更しない" <| fun _ ->
                let items = [ 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ]
                let before = items
                let _ = 統合因果台帳.一括追記する 統合因果台帳.空 items
                Expect.equal items before "入力不変"

            testCase "同じ入力から同じ台帳結果を返す" <| fun _ ->
                let items = [ 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ]
                Expect.equal (統合因果台帳.一括追記する 統合因果台帳.空 items) (統合因果台帳.一括追記する 統合因果台帳.空 items) "決定論"

            testCase "位置変更専用wrapperが共通追記と等価" <| fun _ ->
                let candidates = [ 基本位置変更候補 "p1" 10L; 基本位置変更候補 "p2" 10L ]
                let wrapper = 統合因果台帳.位置変更候補一覧を追記する 統合因果台帳.空 candidates
                let common = 統合因果台帳.一括追記する 統合因果台帳.空 (candidates |> List.map 位置項目)
                Expect.equal wrapper common "wrapper"

            testCase "地盤振動専用wrapperが共通追記と等価" <| fun _ ->
                let records = [ 基本地盤振動記録 "v1" "s1" 10L; 基本地盤振動記録 "v2" "s2" 10L ]
                let wrapper = 統合因果台帳.地盤振動発生記録一覧を追記する 統合因果台帳.空 records
                let common = 統合因果台帳.一括追記する 統合因果台帳.空 (records |> List.map 振動項目)
                Expect.equal wrapper common "wrapper"
        ]
