module IntegratedCausalReplayTests

open Expecto
open Omokane.Core.Domain
open IntegratedCausalLedgerTests.TestData

let private 再生成功を得る = function
    | 統合因果台帳再生結果.成功(更新, 信号一覧) -> 更新, 信号一覧
    | 統合因果台帳再生結果.失敗(_, index, id, 理由一覧) ->
        failtestf "統合Replayが失敗しました: index=%d id=%A reasons=%A" index id 理由一覧

let private 再生失敗を得る = function
    | 統合因果台帳再生結果.失敗(状態, index, id, 理由一覧) -> 状態, index, id, 理由一覧
    | 統合因果台帳再生結果.成功(更新, 信号一覧) ->
        failtestf "統合Replayが成功しました: update=%A signals=%A" 更新 信号一覧

let private entityを得る id (状態: ゲーム状態) =
    状態.エンティティ一覧 |> List.find (fun entity -> entity.ID = id)

[<Tests>]
let 全テスト =
    testList
        "統合因果台帳Replay"
        [
            testCase "空統合台帳は状態不変かつEvent空かつ信号空" <| fun _ ->
                let 初期 = 状態 10L
                let 更新, 信号一覧 = 統合因果台帳再生.再生する 初期 統合因果台帳.空 |> 再生成功を得る
                Expect.equal 更新.状態 初期 "状態不変"
                Expect.isEmpty 更新.イベント一覧 "Event空"
                Expect.isEmpty 信号一覧 "信号空"

            testCase "位置変更一件をReplayできる" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "position.1" 10L |> 位置項目 ]
                let 更新, _ = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal (entityを得る 対象A 更新.状態).位置 (位置 3.0 4.0) "位置更新"

            testCase "地盤振動一件をReplayできる" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "vibration.1" "signal.1" 10L |> 振動項目 ]
                let _, signals = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal (signals |> List.map (fun signal -> signal.ID)) [ 伝播信号ID "signal.1" ] "信号再出力"

            testCase "地盤振動Replayは状態を変更しない" <| fun _ ->
                let 初期 = 状態 10L
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "vibration.1" "signal.1" 10L |> 振動項目 ]
                let 更新, _ = 統合因果台帳再生.再生する 初期 台帳 |> 再生成功を得る
                Expect.equal 更新.状態 初期 "同一Tickでは完全不変"

            testCase "地盤振動Replayは記録済み信号をそのまま返す" <| fun _ ->
                let record = 基本地盤振動記録 "vibration.1" "signal.1" 10L
                let 台帳 = 統合台帳を作る [ 振動項目 record ]
                let _, signals = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal signals [ record.信号 ] "構造的に同一"

            testCase "位置変更と地盤振動の混在Replay" <| fun _ ->
                let 台帳 =
                    統合台帳を作る
                        [
                            基本位置変更候補 "position.1" 10L |> 位置項目
                            基本地盤振動記録 "vibration.1" "signal.1" 11L |> 振動項目
                        ]
                let 更新, signals = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal (entityを得る 対象A 更新.状態).位置 (位置 3.0 4.0) "位置"
                Expect.equal 更新.状態.Tick 11L "Tick"
                Expect.equal signals.Length 1 "信号"

            testCase "同一Tickの混在Replay" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 10L |> 振動項目; 基本位置変更候補 "p1" 10L |> 位置項目 ]
                let 更新, signals = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal 更新.状態.Tick 10L "同一Tick"
                Expect.equal signals.Length 1 "両記録成功"

            testCase "Tickの飛びを許可する" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 12L |> 振動項目; 基本位置変更候補 "p1" 20L |> 位置項目 ]
                let 更新, _ = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal 更新.状態.Tick 20L "飛びを許可"

            testCase "飛ばしたTick進行Eventを生成しない" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 20L |> 振動項目 ]
                let 更新, _ = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.isEmpty 更新.イベント一覧 "合成Eventなし"

            testCase "最終Tickが最後の記録Tickになる" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "p1" 12L |> 位置項目; 基本地盤振動記録 "v1" "s1" 15L |> 振動項目 ]
                let 更新, _ = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal 更新.状態.Tick 15L "末尾Tick"

            testCase "Eventを台帳順で維持する" <| fun _ ->
                let first = 地盤振動記録 "v1" "s1" 10L (Some 発生者ID) [ Tick進行 1L ]
                let second = 位置変更候補 "p1" 10L 対象A (位置 1.0 2.0) (位置 3.0 4.0) [ Tick進行 2L ]
                let 更新, _ = 統合台帳を作る [ 振動項目 first; 位置項目 second ] |> 統合因果台帳再生.再生する (状態 10L) |> 再生成功を得る
                Expect.equal 更新.イベント一覧 [ Tick進行 1L; Tick進行 2L ] "台帳順"

            testCase "記録内Event順を維持する" <| fun _ ->
                let events = [ Tick進行 2L; ダメージ発生(対象A, 3); ゲーム終了 クリア ]
                let record = 地盤振動記録 "v1" "s1" 10L (Some 発生者ID) events
                let 更新, _ = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する (状態 10L) |> 再生成功を得る
                Expect.equal 更新.イベント一覧 events "記録内順"

            testCase "信号を台帳順で維持する" <| fun _ ->
                let 台帳 = 統合台帳を作る [ 基本地盤振動記録 "v2" "s2" 10L |> 振動項目; 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ]
                let _, signals = 統合因果台帳再生.再生する (状態 10L) 台帳 |> 再生成功を得る
                Expect.equal (signals |> List.map (fun signal -> signal.ID)) [ 伝播信号ID "s2"; 伝播信号ID "s1" ] "信号順"

            testCase "Entity生成EventからEntityを生成しない" <| fun _ ->
                let 初期 = 状態 10L
                let record = 地盤振動記録 "v1" "s1" 10L None [ エンティティ生成 不在ID ]
                let 更新, _ = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                Expect.equal 更新.状態.エンティティ一覧 初期.エンティティ一覧 "Eventは通知"

            testCase "Entity削除EventからEntityを削除しない" <| fun _ ->
                let 初期 = 状態 10L
                let record = 地盤振動記録 "v1" "s1" 10L None [ エンティティ削除 対象A ]
                let 更新, _ = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                Expect.equal 更新.状態.エンティティ一覧 初期.エンティティ一覧 "削除しない"

            testCase "ダメージEventからHPを変更しない" <| fun _ ->
                let 初期 = 状態 10L
                let record = 地盤振動記録 "v1" "s1" 10L None [ ダメージ発生(対象A, 99) ]
                let 更新, _ = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                Expect.equal (entityを得る 対象A 更新.状態).HP (entityを得る 対象A 初期).HP "HP不変"

            testCase "ゲーム終了Eventから終了状態を変更しない" <| fun _ ->
                let 初期 = 状態 10L
                let record = 地盤振動記録 "v1" "s1" 10L None [ ゲーム終了 ゲームオーバー ]
                let 更新, _ = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                Expect.equal 更新.状態.終了状態 None "終了状態不変"

            testCase "位置変更対象不存在を拒否する" <| fun _ ->
                let candidate = 位置変更候補 "p1" 10L 不在ID (位置 1.0 2.0) (位置 3.0 4.0) []
                let _, _, _, reasons = 統合台帳を作る [ 位置項目 candidate ] |> 統合因果台帳再生.再生する (状態 10L) |> 再生失敗を得る
                Expect.contains reasons "対象Entityが存在しません。" "不存在"

            testCase "位置変更対象重複を拒否する" <| fun _ ->
                let 初期 = 状態 10L
                let duplicate = entity 対象A 1.0 2.0
                let 重複状態 = { 初期 with エンティティ一覧 = duplicate :: 初期.エンティティ一覧 }
                let _, _, _, reasons = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目 ] |> 統合因果台帳再生.再生する 重複状態 |> 再生失敗を得る
                Expect.contains reasons "対象EntityIDがゲーム状態内で重複しています。" "重複"

            testCase "変更前位置不一致を拒否する" <| fun _ ->
                let candidate = 位置変更候補 "p1" 10L 対象A (位置 99.0 99.0) (位置 3.0 4.0) []
                let _, _, _, reasons = 統合台帳を作る [ 位置項目 candidate ] |> 統合因果台帳再生.再生する (状態 10L) |> 再生失敗を得る
                Expect.contains reasons "対象Entityの現在位置が因果台帳記録の変更前位置と一致しません。" "分岐検出"

            testCase "初期Tickが位置変更記録より後なら拒否する" <| fun _ ->
                let _, _, _, reasons = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目 ] |> 統合因果台帳再生.再生する (状態 11L) |> 再生失敗を得る
                Expect.contains reasons "因果台帳記録のTickが現在の再生状態より前です。" "Tick逆行"

            testCase "地盤振動発生元不存在を拒否する" <| fun _ ->
                let record = 地盤振動記録 "v1" "s1" 10L (Some 不在ID) []
                let _, _, _, reasons = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する (状態 10L) |> 再生失敗を得る
                Expect.contains reasons "地盤振動発生元Entityが存在しません。" "不存在"

            testCase "地盤振動発生元重複を拒否する" <| fun _ ->
                let 初期 = 状態 10L
                let duplicate = entity 発生者ID 5.0 5.0
                let 重複状態 = { 初期 with エンティティ一覧 = duplicate :: 初期.エンティティ一覧 }
                let _, _, _, reasons = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ] |> 統合因果台帳再生.再生する 重複状態 |> 再生失敗を得る
                Expect.contains reasons "地盤振動発生元EntityIDがゲーム状態内で重複しています。" "重複"

            testCase "地盤振動発生元Noneを許可する" <| fun _ ->
                let record = 地盤振動記録 "v1" "s1" 10L None []
                let _, signals = 統合台帳を作る [ 振動項目 record ] |> 統合因果台帳再生.再生する (状態 10L) |> 再生成功を得る
                Expect.equal signals [ record.信号 ] "非Entity発生源"

            testCase "初期Tickが地盤振動記録より後なら拒否する" <| fun _ ->
                let _, _, _, reasons = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ] |> 統合因果台帳再生.再生する (状態 11L) |> 再生失敗を得る
                Expect.contains reasons "地盤振動発生記録のTickが現在の再生状態より前です。" "Tick逆行"

            testCase "終了状態から非空台帳Replayを拒否する" <| fun _ ->
                let 初期 = { 状態 10L with 終了状態 = Some クリア }
                let _, _, _, reasons = 統合台帳を作る [ 地盤振動記録 "v1" "s1" 10L None [] |> 振動項目 ] |> 統合因果台帳再生.再生する 初期 |> 再生失敗を得る
                Expect.contains reasons "終了状態のため地盤振動発生記録を再生できません。" "終了拒否"

            testCase "後方位置変更失敗でも初期状態を返す" <| fun _ ->
                let 初期 = 状態 10L
                let first = 基本地盤振動記録 "v1" "s1" 10L
                let bad = 位置変更候補 "p1" 10L 対象A (位置 99.0 99.0) (位置 3.0 4.0) []
                let returned, index, _, _ = 統合台帳を作る [ 振動項目 first; 位置項目 bad ] |> 統合因果台帳再生.再生する 初期 |> 再生失敗を得る
                Expect.equal index 1 "後方失敗"
                Expect.equal returned 初期 "原子的復帰"

            testCase "後方地盤振動失敗でも初期状態を返す" <| fun _ ->
                let 初期 = 状態 10L
                let first = 基本位置変更候補 "p1" 10L
                let bad = 地盤振動記録 "v1" "s1" 10L (Some 不在ID) []
                let returned, index, _, _ = 統合台帳を作る [ 位置項目 first; 振動項目 bad ] |> 統合因果台帳再生.再生する 初期 |> 再生失敗を得る
                Expect.equal index 1 "後方失敗"
                Expect.equal returned 初期 "原子的復帰"

            testCase "失敗時に途中Eventを公開しない" <| fun _ ->
                let first = 地盤振動記録 "v1" "s1" 10L None [ Tick進行 99L ]
                let bad = 位置変更候補 "p1" 10L 対象A (位置 99.0 99.0) (位置 3.0 4.0) []
                match 統合台帳を作る [ 振動項目 first; 位置項目 bad ] |> 統合因果台帳再生.再生する (状態 10L) with
                | 統合因果台帳再生結果.失敗(returned, 1, 因果操作ID "p1", _) -> Expect.equal returned (状態 10L) "Eventを含む更新を公開しない"
                | other -> failtestf "期待した失敗ではありません: %A" other

            testCase "失敗時に途中信号を公開しない" <| fun _ ->
                let first = 基本地盤振動記録 "v1" "s1" 10L
                let bad = 地盤振動記録 "v2" "s2" 10L (Some 不在ID) []
                match 統合台帳を作る [ 振動項目 first; 振動項目 bad ] |> 統合因果台帳再生.再生する (状態 10L) with
                | 統合因果台帳再生結果.失敗 _ -> ()
                | 統合因果台帳再生結果.成功(_, signals) -> failtestf "途中信号が公開されました: %A" signals

            testCase "失敗位置と因果操作IDが正しい" <| fun _ ->
                let bad = 地盤振動記録 "v.bad" "s.bad" 10L (Some 不在ID) []
                let _, index, id, _ = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目; 振動項目 bad ] |> 統合因果台帳再生.再生する (状態 10L) |> 再生失敗を得る
                Expect.equal index 1 "index"
                Expect.equal id (因果操作ID "v.bad") "ID"

            testCase "入力初期状態を変更しない" <| fun _ ->
                let 初期 = 状態 10L
                let before = 初期
                let _ = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目 ] |> 統合因果台帳再生.再生する 初期
                Expect.equal 初期 before "入力不変"

            testCase "同じ入力から同じReplay結果を返す" <| fun _ ->
                let 初期 = 状態 10L
                let 台帳 = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目; 基本地盤振動記録 "v1" "s1" 10L |> 振動項目 ]
                Expect.equal (統合因果台帳再生.再生する 初期 台帳) (統合因果台帳再生.再生する 初期 台帳) "決定論"

            testCase "Entity一覧順を維持する" <| fun _ ->
                let 初期 = 状態 10L
                let idsBefore = 初期.エンティティ一覧 |> List.map (fun entity -> entity.ID)
                let 更新, _ = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目 ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                Expect.equal (更新.状態.エンティティ一覧 |> List.map (fun entity -> entity.ID)) idsBefore "順序"

            testCase "非位置Entityフィールドを維持する" <| fun _ ->
                let 初期 = 状態 10L
                let before = entityを得る 対象A 初期
                let 更新, _ = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目 ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                let after = entityを得る 対象A 更新.状態
                Expect.equal { after with 位置 = before.位置 } before "位置以外不変"

            testCase "乱数Seedを維持する" <| fun _ ->
                let 初期 = 状態 10L
                let 更新, _ = 統合台帳を作る [ 基本地盤振動記録 "v1" "s1" 12L |> 振動項目 ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                Expect.equal 更新.状態.乱数Seed 初期.乱数Seed "Seed"

            testCase "プレイヤーIDを維持する" <| fun _ ->
                let 初期 = 状態 10L
                let 更新, _ = 統合台帳を作る [ 基本位置変更候補 "p1" 10L |> 位置項目 ] |> 統合因果台帳再生.再生する 初期 |> 再生成功を得る
                Expect.equal 更新.状態.プレイヤーID 初期.プレイヤーID "プレイヤーID"
        ]
