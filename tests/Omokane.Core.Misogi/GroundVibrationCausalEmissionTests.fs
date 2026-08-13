module GroundVibrationCausalEmissionTests

open System
open Expecto
open Omokane.Core.Domain

let private 実行者ID = エンティティID "emitter"
let private 観測者ID = エンティティID "observer"
let private 不在ID = エンティティID "missing"

let private entityを作る ID 位置 =
    {
        ID = ID
        種別 = 敵
        所有者 = 中立
        位置 = 位置
        速度 = { X = 0.25; Y = -0.5 }
        当たり判定 = None
        HP = Some(HP 7)
    }

let private 状態を作る Entity一覧 =
    {
        Tick = 10L
        プレイヤーID = 実行者ID
        エンティティ一覧 = Entity一覧
        エンティティ反応状態一覧 = []
        乱数Seed = 2468
        終了状態 = None
    }

let private 基本状態 () =
    状態を作る
        [
            entityを作る 実行者ID { X = 1.0; Y = 2.0 }
            entityを作る 観測者ID { X = 4.0; Y = 5.0 }
        ]

let private 操作を作る 因果ID 信号ID 実行者 : 地盤振動発生操作 =
    let 因果: 因果操作 =
        {
            ID = 因果操作ID 因果ID
            Tick = 10L
            種別 = 伝播信号生成
            原因因果ID = None
            実行者ID = 実行者
            対象EntityID = None
            概要 = "地面を強く踏み鳴らした"
            発行Event一覧 = []
        }

    {
        因果 = 因果
        信号ID = 伝播信号ID 信号ID
        発生位置 = { X = 3.0; Y = 4.0 }
        方向 = Some { X = 1.0; Y = 0.0 }
        強度 = 10.0
        方式 = 波動
        寿命Tick = 5L
    }

let private 基本操作 () =
    操作を作る "causal.vibration" "signal.vibration" (Some 実行者ID)

let private 成功を得る = function
    | 発生成功(更新, 信号, 記録) -> 更新, 信号, 記録
    | 発生失敗(_, 分類, 理由一覧) ->
        failtestf "地盤振動発生が失敗しました: %A %A" 分類 理由一覧

let private 失敗を得る = function
    | 発生失敗(状態, 分類, 理由一覧) -> 状態, 分類, 理由一覧
    | 発生成功(更新, 信号, _) ->
        failtestf "地盤振動発生が成功しました: %A %A" 更新 信号

let private 一括成功を得る = function
    | 一括発生成功(更新, 信号一覧, 記録一覧) -> 更新, 信号一覧, 記録一覧
    | 一括発生失敗(_, index, id, 分類, 理由一覧) ->
        failtestf "地盤振動一括発生が失敗しました: %d %A %A %A" index id 分類 理由一覧

let private 一括失敗を得る = function
    | 一括発生失敗(状態, index, id, 分類, 理由一覧) ->
        状態, index, id, 分類, 理由一覧
    | 一括発生成功(更新, 信号一覧, _) ->
        failtestf "地盤振動一括発生が成功しました: %A %A" 更新 信号一覧

[<Tests>]
let 全テスト =
    testList
        "地盤振動発生因果操作"
        [
            testCase "正常な地盤振動を発生できる" <| fun _ ->
                地盤振動発生実行.適用する (基本状態 ()) (基本操作 ())
                |> 成功を得る
                |> ignore

            testCase "成功時にゲーム状態が不変である" <| fun _ ->
                let 状態 = 基本状態 ()
                let 更新, _, _ = 地盤振動発生実行.適用する 状態 (基本操作 ()) |> 成功を得る
                Expect.equal 更新.状態 状態 "権威信号生成だけでは状態を変更しない"

            testCase "発生操作はTickを進めない" <| fun _ ->
                let 状態 = 基本状態 ()
                let 更新, _, _ = 地盤振動発生実行.適用する 状態 (基本操作 ()) |> 成功を得る
                Expect.equal 更新.状態.Tick 状態.Tick "Tick進行は別責務"

            testCase "因果操作のEventを入力順で返す" <| fun _ ->
                let Event一覧 = [ エンティティ生成 観測者ID; Tick進行 10L ]
                let 操作 = { 基本操作 () with 因果.発行Event一覧 = Event一覧 }
                let 更新, _, 記録 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 更新.イベント一覧 Event一覧 "更新結果のEvent順"
                Expect.equal 記録.発行Event一覧 Event一覧 "記録のEvent順"

            testCase "信号IDを保存する" <| fun _ ->
                let 操作 = 基本操作 ()
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.ID 操作.信号ID "明示IDを保持する"

            testCase "原因因果IDを自動設定する" <| fun _ ->
                let 操作 = 基本操作 ()
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.原因因果ID (Some 操作.因果.ID) "呼び出し側に由来を委ねない"

            testCase "実行者IDを発生源EntityIDへ自動設定する" <| fun _ ->
                let 操作 = 基本操作 ()
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.発生源EntityID 操作.因果.実行者ID "因果実行者を由来にする"

            testCase "量種別を地盤振動へ固定する" <| fun _ ->
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) (基本操作 ()) |> 成功を得る
                Expect.equal 信号.量種別 地盤振動 "専用発生操作の量種別"

            testCase "媒体を地盤へ固定する" <| fun _ ->
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) (基本操作 ()) |> 成功を得る
                Expect.equal 信号.媒体 地盤 "専用発生操作の媒体"

            testCase "発生位置を保存する" <| fun _ ->
                let 操作 = 基本操作 ()
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.発生位置 操作.発生位置 "発生位置を変更しない"

            testCase "強度を保存する" <| fun _ ->
                let 操作 = { 基本操作 () with 強度 = 12.5 }
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.強度 12.5 "強度を再計算しない"

            testCase "方向を保存する" <| fun _ ->
                let 方向 = Some { X = -1.0; Y = 2.0 }
                let 操作 = { 基本操作 () with 方向 = 方向 }
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.方向 方向 "方向を保持する"

            testCase "伝播方式を保存する" <| fun _ ->
                let 操作 = { 基本操作 () with 方式 = 伝導 }
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.方式 伝導 "方式を保持する"

            testCase "寿命Tickを保存する" <| fun _ ->
                let 操作 = { 基本操作 () with 寿命Tick = 19L }
                let _, 信号, _ = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.equal 信号.寿命Tick 19L "寿命を保持する"

            testCase "発生記録が正式信号を保持する" <| fun _ ->
                let _, 信号, 記録 = 地盤振動発生実行.適用する (基本状態 ()) (基本操作 ()) |> 成功を得る
                Expect.equal 記録.信号 信号 "再構築せず同じ信号を記録する"

            testCase "発生記録の公開検証を通る" <| fun _ ->
                let _, _, 記録 = 地盤振動発生実行.適用する (基本状態 ()) (基本操作 ()) |> 成功を得る
                Expect.equal (地盤振動発生記録.検証する 記録) (Ok()) "正式記録契約を満たす"

            testCase "因果操作種別不一致を拒否する" <| fun _ ->
                let 操作 = { 基本操作 () with 因果.種別 = 状態変更 }
                let _, 分類, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal 分類 契約不正 "静的契約不正"
                Expect.equal 理由一覧 [ "因果操作種別が伝播信号生成ではありません。" ] "専用種別を要求する"

            testCase "対象EntityID指定を拒否する" <| fun _ ->
                let 操作 = { 基本操作 () with 因果.対象EntityID = Some 観測者ID }
                let _, 分類, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal 分類 契約不正 "静的契約不正"
                Expect.equal 理由一覧 [ "地盤振動発生操作には対象EntityIDを指定できません。" ] "対象を捏造しない"

            testCase "空白信号IDを拒否する" <| fun _ ->
                let 操作 = { 基本操作 () with 信号ID = 伝播信号ID " " }
                let _, _, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal 理由一覧 [ "伝播信号IDが空です。" ] "既存信号検証を再利用する"

            testCase "強度のNaNとInfinityと負値を拒否する" <| fun _ ->
                [ Double.NaN; Double.PositiveInfinity; Double.NegativeInfinity; -0.1 ]
                |> List.iter (fun 強度 ->
                    let 操作 = { 基本操作 () with 強度 = 強度 }
                    let _, 分類, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                    Expect.equal 分類 契約不正 "不正強度は契約不正"
                    Expect.isNonEmpty 理由一覧 "不正強度を拒否する")

            testCase "発生位置のNaNとInfinityを拒否する" <| fun _ ->
                let 操作 = { 基本操作 () with 発生位置 = { X = Double.NaN; Y = Double.PositiveInfinity } }
                let _, _, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal
                    理由一覧
                    [
                        "伝播信号の発生位置.Xが有限値ではありません。"
                        "伝播信号の発生位置.Yが有限値ではありません。"
                    ]
                    "既存信号検証順を維持する"

            testCase "方向のNaNとInfinityを拒否する" <| fun _ ->
                let 操作 = { 基本操作 () with 方向 = Some { X = Double.NaN; Y = Double.NegativeInfinity } }
                let _, _, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal
                    理由一覧
                    [
                        "伝播信号の方向.Xが有限値ではありません。"
                        "伝播信号の方向.Yが有限値ではありません。"
                    ]
                    "方向契約を再利用する"

            testCase "負の寿命Tickを拒否する" <| fun _ ->
                let 操作 = { 基本操作 () with 寿命Tick = -1L }
                let _, _, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal 理由一覧 [ "伝播信号の寿命Tickが負です。" ] "寿命契約を再利用する"

            testCase "Tick不一致をゲーム内失敗にする" <| fun _ ->
                let 操作 = { 基本操作 () with 因果.Tick = 11L }
                let _, 分類, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal 分類 ゲーム内失敗 "状態依存前提条件"
                Expect.equal 理由一覧 [ "地盤振動発生操作のTickが現在状態のTickと一致しません。" ] "Tickを一致させる"

            testCase "終了状態を拒否する" <| fun _ ->
                let 状態 = { 基本状態 () with 終了状態 = Some クリア }
                let _, 分類, 理由一覧 = 地盤振動発生実行.適用する 状態 (基本操作 ()) |> 失敗を得る
                Expect.equal 分類 ゲーム内失敗 "終了はゲーム内失敗"
                Expect.equal 理由一覧 [ "終了状態のため地盤振動を発生できません。" ] "終了状態を拒否する"

            testCase "実行者不存在を拒否する" <| fun _ ->
                let 操作 = { 基本操作 () with 因果.実行者ID = Some 不在ID }
                let _, 分類, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal 分類 ゲーム内失敗 "存在はゲーム内前提"
                Expect.equal 理由一覧 [ "実行者Entityが存在しません。" ] "不在実行者を拒否する"

            testCase "実行者ID重複を拒否する" <| fun _ ->
                let 状態 = 状態を作る [ entityを作る 実行者ID { X = 0.0; Y = 0.0 }; entityを作る 実行者ID { X = 1.0; Y = 1.0 } ]
                let _, 分類, 理由一覧 = 地盤振動発生実行.適用する 状態 (基本操作 ()) |> 失敗を得る
                Expect.equal 分類 ゲーム内失敗 "重複はゲーム内前提"
                Expect.equal 理由一覧 [ "実行者EntityIDがゲーム状態内で重複しています。" ] "列挙順で選ばない"

            testCase "実行者Noneを許可する" <| fun _ ->
                let 操作 = { 基本操作 () with 因果.実行者ID = None }
                let _, 信号, 記録 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 成功を得る
                Expect.isNone 信号.発生源EntityID "Entity以外の発生源を許可する"
                Expect.isNone 記録.実行者ID "記録もNoneを保持する"

            testCase "入力値を変更しない" <| fun _ ->
                let 状態 = 基本状態 ()
                let 操作 = 基本操作 ()
                let 状態前 = 状態
                let 操作前 = 操作
                地盤振動発生実行.適用する 状態 操作 |> ignore
                Expect.equal 状態 状態前 "状態入力を変更しない"
                Expect.equal 操作 操作前 "操作入力を変更しない"

            testCase "同じ入力から同じ発生結果を返す" <| fun _ ->
                let 状態 = 基本状態 ()
                let 操作 = 基本操作 ()
                Expect.equal
                    (地盤振動発生実行.適用する 状態 操作)
                    (地盤振動発生実行.適用する 状態 操作)
                    "構造的に同じ結果"

            testCase "契約エラーを固定順で全収集する" <| fun _ ->
                let 操作 =
                    {
                        基本操作 () with
                            因果.ID = 因果操作ID ""
                            因果.Tick = -1L
                            因果.種別 = 状態変更
                            因果.対象EntityID = Some 観測者ID
                            因果.概要 = " "
                            信号ID = 伝播信号ID ""
                            発生位置 = { X = Double.NaN; Y = Double.PositiveInfinity }
                            方向 = Some { X = Double.NaN; Y = Double.NegativeInfinity }
                            強度 = -1.0
                            寿命Tick = -1L
                    }

                let _, 分類, 理由一覧 = 地盤振動発生実行.適用する (基本状態 ()) 操作 |> 失敗を得る
                Expect.equal 分類 契約不正 "ゲーム内検証へ進まない"
                Expect.equal
                    理由一覧
                    [
                        "因果操作IDが空です。"
                        "因果操作のTickが負です。"
                        "因果操作の概要が空です。"
                        "因果操作種別が伝播信号生成ではありません。"
                        "地盤振動発生操作には対象EntityIDを指定できません。"
                        "伝播信号IDが空です。"
                        "伝播信号の強度が負です。"
                        "伝播信号の寿命Tickが負です。"
                        "伝播信号の発生位置.Xが有限値ではありません。"
                        "伝播信号の発生位置.Yが有限値ではありません。"
                        "伝播信号の方向.Xが有限値ではありません。"
                        "伝播信号の方向.Yが有限値ではありません。"
                    ]
                    "既存検証の後へ固定順で追加する"

            testCase "空バッチは状態不変で成功する" <| fun _ ->
                let 状態 = 基本状態 ()
                let 更新, 信号一覧, 記録一覧 = 地盤振動発生列実行.一括適用する 状態 [] |> 一括成功を得る
                Expect.equal 更新.状態 状態 "恒等操作"
                Expect.isEmpty 更新.イベント一覧 "Event空"
                Expect.isEmpty 信号一覧 "信号空"
                Expect.isEmpty 記録一覧 "記録空"

            testCase "複数信号を入力順で生成する" <| fun _ ->
                let 操作一覧 = [ 操作を作る "causal.1" "signal.1" None; 操作を作る "causal.2" "signal.2" None ]
                let _, 信号一覧, _ = 地盤振動発生列実行.一括適用する (基本状態 ()) 操作一覧 |> 一括成功を得る
                Expect.equal (信号一覧 |> List.map (fun 信号 -> 信号.ID)) (操作一覧 |> List.map (fun 操作 -> 操作.信号ID)) "入力順"

            testCase "複数記録を入力順で生成する" <| fun _ ->
                let 操作一覧 = [ 操作を作る "causal.1" "signal.1" None; 操作を作る "causal.2" "signal.2" None ]
                let _, _, 記録一覧 = 地盤振動発生列実行.一括適用する (基本状態 ()) 操作一覧 |> 一括成功を得る
                Expect.equal (記録一覧 |> List.map (fun 記録 -> 記録.因果操作ID)) (操作一覧 |> List.map (fun 操作 -> 操作.因果.ID)) "入力順"

            testCase "Eventを操作順と操作内順で維持する" <| fun _ ->
                let 第一Event = [ エンティティ生成 観測者ID; Tick進行 10L ]
                let 第二Event = [ エンティティ削除 観測者ID; ゲーム終了 クリア ]
                let 第一 = { 操作を作る "causal.1" "signal.1" None with 因果.発行Event一覧 = 第一Event }
                let 第二 = { 操作を作る "causal.2" "signal.2" None with 因果.発行Event一覧 = 第二Event }
                let 更新, _, _ = 地盤振動発生列実行.一括適用する (基本状態 ()) [ 第一; 第二 ] |> 一括成功を得る
                Expect.equal 更新.イベント一覧 (第一Event @ 第二Event) "二重の入力順を維持する"

            testCase "同一Tickの複数発生を許可する" <| fun _ ->
                let 操作一覧 = [ 操作を作る "causal.1" "signal.1" None; 操作を作る "causal.2" "signal.2" None ]
                let _, 信号一覧, _ = 地盤振動発生列実行.一括適用する (基本状態 ()) 操作一覧 |> 一括成功を得る
                Expect.equal 信号一覧.Length 2 "Tickを進めず複数発生する"

            testCase "因果操作ID重複を最小indexで拒否する" <| fun _ ->
                let 操作一覧 = [ 操作を作る "duplicate" "signal.1" None; 操作を作る "duplicate" "signal.2" None ]
                let _, index, ID, 分類, 理由一覧 = 地盤振動発生列実行.一括適用する (基本状態 ()) 操作一覧 |> 一括失敗を得る
                Expect.equal index 0 "最小index"
                Expect.equal ID (因果操作ID "duplicate") "失敗ID"
                Expect.equal 分類 契約不正 "静的契約不正"
                Expect.equal 理由一覧 [ "地盤振動発生操作一覧内で因果操作IDが重複しています。" ] "重複理由"

            testCase "伝播信号ID重複を最小indexで拒否する" <| fun _ ->
                let 操作一覧 = [ 操作を作る "causal.1" "duplicate" None; 操作を作る "causal.2" "duplicate" None ]
                let _, index, _, 分類, 理由一覧 = 地盤振動発生列実行.一括適用する (基本状態 ()) 操作一覧 |> 一括失敗を得る
                Expect.equal index 0 "最小index"
                Expect.equal 分類 契約不正 "静的契約不正"
                Expect.equal 理由一覧 [ "地盤振動発生操作一覧内で伝播信号IDが重複しています。" ] "重複理由"

            testCase "後方静的契約失敗では成果物を公開しない" <| fun _ ->
                let 正常 = 操作を作る "causal.1" "signal.1" None
                let 不正 = { 操作を作る "causal.2" "" None with 強度 = -1.0 }
                let 状態, index, _, 分類, _ = 地盤振動発生列実行.一括適用する (基本状態 ()) [ 正常; 不正 ] |> 一括失敗を得る
                Expect.equal index 1 "後方の最初の静的不正"
                Expect.equal 分類 契約不正 "仮適用前に拒否"
                Expect.equal 状態 (基本状態 ()) "元状態だけを返す"

            testCase "後方ゲーム内失敗では成果物を公開しない" <| fun _ ->
                let 正常 = 操作を作る "causal.1" "signal.1" None
                let 不正 = 操作を作る "causal.2" "signal.2" (Some 不在ID)
                let 状態, index, _, 分類, 理由一覧 = 地盤振動発生列実行.一括適用する (基本状態 ()) [ 正常; 不正 ] |> 一括失敗を得る
                Expect.equal index 1 "最初のゲーム内失敗"
                Expect.equal 分類 ゲーム内失敗 "静的検証後の失敗"
                Expect.equal 理由一覧 [ "実行者Entityが存在しません。" ] "後続を実行しない"
                Expect.equal 状態 (基本状態 ()) "部分成功を公開しない"

            testCase "バッチ失敗時は初期状態を返す" <| fun _ ->
                let 状態 = 基本状態 ()
                let 操作 = { 基本操作 () with 因果.Tick = 99L }
                let 返却状態, _, _, _, _ = 地盤振動発生列実行.一括適用する 状態 [ 操作 ] |> 一括失敗を得る
                Expect.equal 返却状態 状態 "原子的に元状態へ戻す"

            testCase "バッチ入力を変更しない" <| fun _ ->
                let 状態 = 基本状態 ()
                let 操作一覧 = [ 操作を作る "causal.1" "signal.1" None; 操作を作る "causal.2" "signal.2" None ]
                let 状態前 = 状態
                let 操作一覧前 = 操作一覧
                地盤振動発生列実行.一括適用する 状態 操作一覧 |> ignore
                Expect.equal 状態 状態前 "状態を変更しない"
                Expect.equal 操作一覧 操作一覧前 "操作列を変更しない"

            testCase "同じバッチ入力から同じ結果を返す" <| fun _ ->
                let 状態 = 基本状態 ()
                let 操作一覧 = [ 操作を作る "causal.1" "signal.1" None; 操作を作る "causal.2" "signal.2" None ]
                Expect.equal
                    (地盤振動発生列実行.一括適用する 状態 操作一覧)
                    (地盤振動発生列実行.一括適用する 状態 操作一覧)
                    "構造的に同じ一括結果"
        ]
