module VoluntaryLocomotionApplicationPreflightTests

open Expecto
open Omokane.Core.Domain
open Omokane.Core.Systems
open EntityReactionStateCausalTests.TestData
open VoluntaryLocomotionPreflightTestData.TestData

[<Tests>]
let 全テスト =
    testList "互換移動適用前検証" [
        testCase "単一右準備が成立" <| fun _ ->
            Expect.equal (互換移動適用前検証.単一を準備する (initial ()) (cmd "r" 自発移動方向.右) |> ok |> 互換移動適用準備.単一命令) (Some(cmd "r" 自発移動方向.右)) "単一右準備が成立"

        testCase "単一左準備が成立" <| fun _ ->
            Expect.equal (互換移動適用前検証.単一を準備する (initial ()) (cmd "l" 自発移動方向.左) |> ok |> 互換移動適用準備.既存入力一覧) ([ 左へ移動 ]) "単一左準備が成立"

        testCase "対象GameState" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.対象ゲーム状態) (initial ()) "対象GameState"

        testCase "対象Entity" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.対象Entity) ((initial ()).エンティティ一覧.Head) "対象Entity"

        testCase "現在制御判断" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.現在制御判断) (自発移動制御判断生成.ゲーム状態から作る 主体A (initial ()) |> ok) "現在制御判断"

        testCase "命令入力順" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.命令一覧) (commands ()) "命令入力順"

        testCase "非空命令数" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.命令数) (3) "非空命令数"

        testCase "複数なら単一命令なし" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.単一命令) (None) "複数なら単一命令なし"

        testCase "既存入力順" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.既存入力一覧) ([ 右へ移動; 右へ移動; 左へ移動 ]) "既存入力順"

        testCase "明示Tick" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.Tick) (10L) "明示Tick"

        testCase "プレイヤーID" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.プレイヤーID) (主体A) "プレイヤーID"

        testCase "実行者ID" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.実行者ID) (主体A) "実行者ID"

        testCase "意図ID逆辞書順" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.自発移動意図ID一覧) ([ 自発移動意図ID "z"; 自発移動意図ID "a"; 自発移動意図ID "m" ]) "意図ID逆辞書順"

        testCase "種別一覧" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.命令種別一覧) ([ 互換移動命令種別.右へ移動; 互換移動命令種別.右へ移動; 互換移動命令種別.左へ移動 ]) "種別一覧"

        testCase "方向一覧" <| fun _ ->
            Expect.equal (prepared () |> 互換移動適用準備.自発移動方向一覧) ([ 自発移動方向.右; 自発移動方向.右; 自発移動方向.左 ]) "方向一覧"

        testCase "対象状態を参照のまま保持" <| fun _ ->
            let s = initial ()
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.isTrue (obj.ReferenceEquals(s, 互換移動適用準備.対象ゲーム状態 p)) "snapshot"

        testCase "対象Entityを再構築しない" <| fun _ ->
            let s = initial ()
            let p = 互換移動適用前検証.単一を準備する s (cmd "r" 自発移動方向.右) |> ok
            Expect.isTrue (obj.ReferenceEquals(s.エンティティ一覧.Head, 互換移動適用準備.対象Entity p)) "entity"

        testCase "空一覧正常" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) []) (Ok None) "空一覧正常"

        testCase "空一覧負Tick" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with Tick = -1L }) []) (Ok None) "空一覧負Tick"

        testCase "空一覧空主体" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ( { initial () with プレイヤーID = エンティティID "" }) []) (Ok None) "空一覧空主体"

        testCase "空一覧不存在" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with エンティティ一覧 = [] }) []) (Ok None) "空一覧不存在"

        testCase "空一覧ゲームオーバー" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with 終了状態 = Some ゲームオーバー }) []) (Ok None) "空一覧ゲームオーバー"

        testCase "空一覧クリア" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with 終了状態 = Some クリア }) []) (Ok None) "空一覧クリア"

        testCase "空一覧反応重複" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (let s = alertState 10L 主体A in { s with エンティティ反応状態一覧 = s.エンティティ反応状態一覧 @ s.エンティティ反応状態一覧 }) []) (Ok None) "空一覧反応重複"

        testCase "空プレイヤーID" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ( { initial () with プレイヤーID = エンティティID "" }) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ prefix "自発移動制御対象EntityIDが空です。"; prefix "自発移動制御対象Entityが存在しません。"; actorError 0 ]) "空プレイヤーID"

        testCase "空白プレイヤーID" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ( { initial () with プレイヤーID = エンティティID " \t" }) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ prefix "自発移動制御対象EntityIDが空です。"; prefix "自発移動制御対象Entityが存在しません。"; actorError 0 ]) "空白プレイヤーID"

        testCase "負Tick既存接頭辞" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with Tick = -1L }) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ prefix "自発移動制御判断のゲーム状態Tickが負です。"; tickError 0 ]) "負Tick既存接頭辞"

        testCase "対象不存在" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with エンティティ一覧 = [] }) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ prefix "自発移動制御対象Entityが存在しません。" ]) "対象不存在"

        testCase "対象ID重複" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (let s = initial () in { s with エンティティ一覧 = s.エンティティ一覧.Head :: s.エンティティ一覧 }) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ prefix "自発移動制御対象EntityIDがゲーム状態内で重複しています。" ]) "対象ID重複"

        testCase "反応重複" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (let s = alertState 10L 主体A in { s with エンティティ反応状態一覧 = s.エンティティ反応状態一覧 @ s.エンティティ反応状態一覧 }) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ prefix "エンティティ反応状態一覧内で実行者IDが重複しています。" ]) "反応重複"

        testCase "未来反応" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ alertState 25L 主体A with Tick = 10L }) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ prefix "エンティティ反応状態の成立Tickがゲーム状態Tickより後です。" ]) "未来反応"

        testCase "対象種別敵を拒否" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 10L 敵) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ kindError ]) "対象種別敵を拒否"

        testCase "対象種別弾を拒否" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 10L 弾) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ kindError ]) "対象種別弾を拒否"

        testCase "対象種別障害物を拒否" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 10L 障害物) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ kindError ]) "対象種別障害物を拒否"

        testCase "所有者中立は適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with 所有者 = 中立 })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "所有者敵側は適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with 所有者 = 敵側 })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "物理位置は適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with 位置 = 位置 -100.0 83.0 })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "物理速度は適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with 速度 = { X = 3.0; Y = -6.0 } })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "HPなしは適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with HP = None })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "HPゼロは適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with HP = Some(HP 0) })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "当たり判定なしは適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with 当たり判定 = None })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "当たり判定差分は適格性へ使わない" <| fun _ ->
            let s = initial () |> changeTarget (fun e -> { e with 当たり判定 = Some(矩形(2.0, 3.0)) })
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "current snapshot"

        testCase "別IDのプレイヤー種別も存在可能" <| fun _ ->
            let s = initial ()
            let s = { s with エンティティ一覧 = entity プレイヤー (エンティティID "another") 8.0 9.0 :: s.エンティティ一覧 }
            Expect.equal (互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some |> 互換移動適用準備.実行者ID) 主体A "target only"

        testCase "ゲームオーバーを拒否" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with 終了状態 = Some ゲームオーバー }) (commands ()) |> errors) ([ endError ]) "ゲームオーバーを拒否"

        testCase "クリアを拒否" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with 終了状態 = Some クリア }) (commands ()) |> errors) ([ endError ]) "クリアを拒否"

        testCase "終了と種別とTickの固定順" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ 状態 25L 敵 with 終了状態 = Some クリア }) (commands ()) |> errors) ([ endError; kindError; tickError 0; tickError 1; tickError 2 ]) "終了と種別とTickの固定順"

        testCase "終了と抑制を両収集" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ alertState 10L 主体A with 終了状態 = Some ゲームオーバー }) (commands ()) |> errors) ([ endError; suppressionError ]) "終了と抑制を両収集"

        testCase "命令Tick10対現在0" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 0L プレイヤー) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ tickError 0 ]) "命令Tick10対現在0"

        testCase "命令Tick10対現在9" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 9L プレイヤー) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ tickError 0 ]) "命令Tick10対現在9"

        testCase "命令Tick10対現在11" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 11L プレイヤー) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ tickError 0 ]) "命令Tick10対現在11"

        testCase "命令Tick10対現在25" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 25L プレイヤー) ([ cmd "r" 自発移動方向.右 ]) |> errors) ([ tickError 0 ]) "命令Tick10対現在25"

        testCase "Tick境界0L一致を許可" <| fun _ ->
            let s = 状態 0L プレイヤー
            let c = commandFrom s 主体A "boundary" 自発移動方向.右 false
            Expect.equal (互換移動適用前検証.単一を準備する s c |> ok |> 互換移動適用準備.Tick) 0L "exact"

        testCase "Tick境界System.Int64.MaxValue一致を許可" <| fun _ ->
            let s = 状態 System.Int64.MaxValue プレイヤー
            let c = commandFrom s 主体A "boundary" 自発移動方向.右 false
            Expect.equal (互換移動適用前検証.単一を準備する s c |> ok |> 互換移動適用準備.Tick) System.Int64.MaxValue "exact"

        testCase "敵主体不一致" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) ([ commandFrom (initial ()) 主体B "other" 自発移動方向.右 true ]) |> errors) ([ actorError 0 ]) "敵主体不一致"

        testCase "障害物主体不一致" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) ([ commandFrom (initial ()) 発生者 "other" 自発移動方向.右 true ]) |> errors) ([ actorError 0 ]) "障害物主体不一致"

        testCase "全Tickの後に全主体エラー" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (状態 25L プレイヤー) ([ commandFrom (initial ()) 主体B "b" 自発移動方向.右 true; commandFrom (initial ()) 発生者 "e" 自発移動方向.左 true ]) |> errors) ([ tickError 0; tickError 1; actorError 0; actorError 1 ]) "全Tickの後に全主体エラー"

        testCase "反応成立同一Tickの古い命令" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (alertState 10L 主体A) (commands ()) |> errors) ([ suppressionError ]) "反応成立同一Tickの古い命令"

        testCase "他Entityの反応は現在許可を妨げない" <| fun _ ->
            let s = alertState 10L 主体B
            Expect.equal (互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some |> 互換移動適用準備.現在制御判断 |> 自発移動制御判断.種別) 自発移動制御種別.許可 "own reaction only"

        testCase "別の状態でも同じ許可判断なら受理" <| fun _ ->
            let s = { initial () with 乱数Seed = 999 }
            let p = 互換移動適用前検証.一括を準備する s (commands ()) |> ok |> some
            Expect.equal (互換移動適用準備.現在制御判断 p) (互換移動命令.由来制御判断 (commands ()).Head) "structural decision"
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "new snapshot"

        testCase "準備後の別状態は保存snapshotへ逆流しない" <| fun _ ->
            let p = prepared ()
            let changed = alertState 10L 主体A
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) (initial ()) "immutable"
            Expect.notEqual changed (互換移動適用準備.対象ゲーム状態 p) "different authority"

        testCase "同一命令重複" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) ([ cmd "x" 自発移動方向.右; cmd "x" 自発移動方向.右 ]) |> errors) ([ duplicateError ]) "同一命令重複"

        testCase "同一ID異方向" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) ([ cmd "x" 自発移動方向.右; cmd "x" 自発移動方向.左 ]) |> errors) ([ duplicateError ]) "同一ID異方向"

        testCase "末尾ID重複" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) (commands () @ [ cmd "z" 自発移動方向.左 ]) |> errors) ([ duplicateError ]) "末尾ID重複"

        testCase "複数ID重複エラー一件" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) (commands () @ commands ()) |> errors) ([ duplicateError ]) "複数ID重複エラー一件"

        testCase "抑制の後にID重複" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (alertState 10L 主体A) (commands () @ commands ()) |> errors) ([ suppressionError; duplicateError ]) "抑制の後にID重複"

        testCase "後方Tick不正は部分公開しない" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) (commands () @ [ commandFrom (状態 25L プレイヤー) 主体A "late" 自発移動方向.右 true ]) |> errors) ([ tickError 3 ]) "後方Tick不正は部分公開しない"

        testCase "後方主体不正は部分公開しない" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) (commands () @ [ commandFrom (initial ()) 主体B "late" 自発移動方向.右 true ]) |> errors) ([ actorError 3 ]) "後方主体不正は部分公開しない"

        testCase "現在判断失敗でも独立エラーを全収集" <| fun _ ->
            Expect.equal (互換移動適用前検証.一括を準備する ({ initial () with Tick = -1L; 終了状態 = Some クリア; エンティティ一覧 = [] }) (let c = commandFrom (initial ()) 主体B "x" 自発移動方向.右 true in [ c; c ]) |> errors) ([ prefix "自発移動制御判断のゲーム状態Tickが負です。"; prefix "自発移動制御対象Entityが存在しません。"; endError; tickError 0; tickError 1; actorError 0; actorError 1; duplicateError ]) "現在判断失敗でも独立エラーを全収集"

        testCase "右左を相殺しない" <| fun _ ->
            let cs = [ cmd "r" 自発移動方向.右; cmd "l" 自発移動方向.左 ]
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) cs |> ok |> some |> 互換移動適用準備.既存入力一覧) [ 右へ移動; 左へ移動 ] "no cancel"

        testCase "同方向別IDを保持" <| fun _ ->
            let cs = [ cmd "z" 自発移動方向.右; cmd "a" 自発移動方向.右 ]
            Expect.equal (互換移動適用前検証.一括を準備する (initial ()) cs |> ok |> some |> 互換移動適用準備.命令一覧) cs "no collapse"

        testCase "単一と一件一括は同値" <| fun _ ->
            let s = initial ()
            let c = cmd "x" 自発移動方向.右
            Expect.equal (互換移動適用前検証.一括を準備する s [ c ]) (互換移動適用前検証.単一を準備する s c |> Result.map Some) "shared validation"

        testCase "単一と一件一括の失敗も同値" <| fun _ ->
            let s = alertState 10L 主体A
            let c = cmd "x" 自発移動方向.右
            Expect.equal (互換移動適用前検証.一括を準備する s [ c ]) (互換移動適用前検証.単一を準備する s c |> Result.map Some) "same errors"

        testCase "命令の参照を保持" <| fun _ ->
            let cs = commands ()
            let p = 互換移動適用前検証.一括を準備する (initial ()) cs |> ok |> some
            List.iter2 (fun a b -> Expect.isTrue (obj.ReferenceEquals(a,b)) "same command") cs (互換移動適用準備.命令一覧 p)

        testCase "成功後も入力状態と命令不変" <| fun _ ->
            let s, cs = initial (), commands ()
            let before = s, cs
            互換移動適用前検証.一括を準備する s cs |> ok |> ignore
            Expect.equal (s, cs) before "no mutation"

        testCase "失敗後も入力状態と命令不変" <| fun _ ->
            let s, cs = alertState 10L 主体A, commands ()
            let before = s, cs
            互換移動適用前検証.一括を準備する s cs |> errors |> ignore
            Expect.equal (s, cs) before "no mutation"

        testCase "単一決定論" <| fun _ ->
            Expect.equal (互換移動適用前検証.単一を準備する (initial ()) (cmd "x" 自発移動方向.左)) (互換移動適用前検証.単一を準備する (initial ()) (cmd "x" 自発移動方向.左)) "単一決定論"

        testCase "一括決定論" <| fun _ ->
            Expect.equal (prepared ()) (prepared ()) "一括決定論"

        testCase "Test限定Movement左等価" <| fun _ ->
            let s = initial ()
            let cs = [ cmd "0" 自発移動方向.左 ]
            let p = 互換移動適用前検証.一括を準備する s cs |> ok |> some
            let actual = 思兼神.更新する (互換移動適用準備.既存入力一覧 p) (互換移動適用準備.対象ゲーム状態 p)
            Expect.equal actual (思兼神.更新する [ 左へ移動 ] s) "legacy equivalent"

        testCase "Test限定Movement右等価" <| fun _ ->
            let s = initial ()
            let cs = [ cmd "0" 自発移動方向.右 ]
            let p = 互換移動適用前検証.一括を準備する s cs |> ok |> some
            let actual = 思兼神.更新する (互換移動適用準備.既存入力一覧 p) (互換移動適用準備.対象ゲーム状態 p)
            Expect.equal actual (思兼神.更新する [ 右へ移動 ] s) "legacy equivalent"

        testCase "Test限定Movement右右左等価" <| fun _ ->
            let s = initial ()
            let cs = [ cmd "0" 自発移動方向.右; cmd "1" 自発移動方向.右; cmd "2" 自発移動方向.左 ]
            let p = 互換移動適用前検証.一括を準備する s cs |> ok |> some
            let actual = 思兼神.更新する (互換移動適用準備.既存入力一覧 p) (互換移動適用準備.対象ゲーム状態 p)
            Expect.equal actual (思兼神.更新する [ 右へ移動; 右へ移動; 左へ移動 ] s) "legacy equivalent"

        testCase "準備だけではTickも位置も変わらない" <| fun _ ->
            let p = prepared ()
            Expect.equal (互換移動適用準備.対象ゲーム状態 p) (initial ()) "no Movement call"

        testCase "警戒中左は命令なし準備なし" <| fun _ ->
            let s = alertState 10L 主体A
            let move = intent true "empty" 10L 主体A 自発移動方向.左
            let converted = gateFrom s 主体A move |> 自発移動互換変換.変換する
            let cs = 互換移動変換結果一覧.命令一覧 [ converted ]
            Expect.equal cs [] "suppressed"
            Expect.equal (互換移動適用前検証.一括を準備する s cs) (Ok None) "no target"

        testCase "警戒中右は命令なし準備なし" <| fun _ ->
            let s = alertState 10L 主体A
            let move = intent true "empty" 10L 主体A 自発移動方向.右
            let converted = gateFrom s 主体A move |> 自発移動互換変換.変換する
            let cs = 互換移動変換結果一覧.命令一覧 [ converted ]
            Expect.equal cs [] "suppressed"
            Expect.equal (互換移動適用前検証.一括を準備する s cs) (Ok None) "no target"

        testCase "警戒中上は命令なし準備なし" <| fun _ ->
            let s = alertState 10L 主体A
            let move = intent true "empty" 10L 主体A 自発移動方向.上
            let converted = gateFrom s 主体A move |> 自発移動互換変換.変換する
            let cs = 互換移動変換結果一覧.命令一覧 [ converted ]
            Expect.equal cs [] "suppressed"
            Expect.equal (互換移動適用前検証.一括を準備する s cs) (Ok None) "no target"

        testCase "警戒中下は命令なし準備なし" <| fun _ ->
            let s = alertState 10L 主体A
            let move = intent true "empty" 10L 主体A 自発移動方向.下
            let converted = gateFrom s 主体A move |> 自発移動互換変換.変換する
            let cs = 互換移動変換結果一覧.命令一覧 [ converted ]
            Expect.equal cs [] "suppressed"
            Expect.equal (互換移動適用前検証.一括を準備する s cs) (Ok None) "no target"

        testCase "通過上未対応は準備なし" <| fun _ ->
            let s = initial ()
            let move = intent true "unsupported" 10L 主体A 自発移動方向.上
            let converted = gateFrom s 主体A move |> 自発移動互換変換.変換する
            Expect.isTrue (互換移動変換結果.方向未対応である converted) "unsupported"
            Expect.equal (互換移動適用前検証.一括を準備する s (互換移動変換結果一覧.命令一覧 [ converted ])) (Ok None) "empty"

        testCase "通過下未対応は準備なし" <| fun _ ->
            let s = initial ()
            let move = intent true "unsupported" 10L 主体A 自発移動方向.下
            let converted = gateFrom s 主体A move |> 自発移動互換変換.変換する
            Expect.isTrue (互換移動変換結果.方向未対応である converted) "unsupported"
            Expect.equal (互換移動適用前検証.一括を準備する s (互換移動変換結果一覧.命令一覧 [ converted ])) (Ok None) "empty"

        testCase "敵互換命令は適用時昇格しない" <| fun _ ->
            let s = 状態 10L 敵
            let c = commandFrom s 主体A "nonplayer" 自発移動方向.右 true
            Expect.equal (互換移動命令.実行者ID c) 主体A "preserved"
            Expect.equal (互換移動適用前検証.単一を準備する s c |> errors) [ kindError ] "not upgraded"

        testCase "弾互換命令は適用時昇格しない" <| fun _ ->
            let s = 状態 10L 弾
            let c = commandFrom s 主体A "nonplayer" 自発移動方向.右 true
            Expect.equal (互換移動命令.実行者ID c) 主体A "preserved"
            Expect.equal (互換移動適用前検証.単一を準備する s c |> errors) [ kindError ] "not upgraded"

        testCase "障害物互換命令は適用時昇格しない" <| fun _ ->
            let s = 状態 10L 障害物
            let c = commandFrom s 主体A "nonplayer" 自発移動方向.右 true
            Expect.equal (互換移動命令.実行者ID c) 主体A "preserved"
            Expect.equal (互換移動適用前検証.単一を準備する s c |> errors) [ kindError ] "not upgraded"

        testCase "完全因果経路の成立前は準備成功" <| fun _ ->
            let p = path ()
            let c = fromPath p.Initial p |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動適用前検証.単一を準備する p.Initial c |> ok |> 互換移動適用準備.対象ゲーム状態) p.Initial "before"

        testCase "完全因果経路の成立後は新命令なし" <| fun _ ->
            let p = path ()
            Expect.equal (fromPath p.Live.状態 p |> 互換移動変換結果.非生成理由) (Some 互換移動非生成理由.自発移動抑制) "after"

        testCase "完全因果経路の古い命令を拒否" <| fun _ ->
            let p = path ()
            let c = fromPath p.Initial p |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動適用前検証.単一を準備する p.Live.状態 c |> errors) [ suppressionError ] "stale"

        testCase "Replayでも古い命令を拒否" <| fun _ ->
            let p = path ()
            let c = fromPath p.Initial p |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動適用前検証.単一を準備する p.Replay.状態 c |> errors) [ suppressionError ] "replay stale"

        testCase "liveとReplayで準備結果一致" <| fun _ ->
            let p = path ()
            let cs = fromPath p.Initial p |> 互換移動変換結果.命令 |> some |> List.singleton
            Expect.equal (互換移動適用前検証.一括を準備する p.Live.状態 cs) (互換移動適用前検証.一括を準備する p.Replay.状態 cs) "same rejection"

        testCase "Replay信号とEventと台帳は検証で不変" <| fun _ ->
            let p = path ()
            let before = p.Replay, p.Signals, p.Ledger, p.Live
            let c = fromPath p.Initial p |> 互換移動変換結果.命令 |> some
            互換移動適用前検証.単一を準備する p.Replay.状態 c |> errors |> ignore
            Expect.equal (p.Replay, p.Signals, p.Ledger, p.Live) before "no side effects"
            let replayed, signals = 統合因果台帳再生.再生する p.Initial p.Ledger |> replay
            Expect.equal (replayed, signals) (p.Replay, p.Signals) "history preserved"

        testCase "完全Provenanceは現在抑制判断から辿れる" <| fun _ ->
            let p = path ()
            let d = 自発移動制御判断生成.ゲーム状態から作る 主体A p.Live.状態 |> ok
            let r = 自発移動制御判断.由来反応状態 d |> some
            Expect.equal (エンティティ反応状態.由来要求 r) p.Request "request"
            Expect.equal (エンティティ反応状態.由来行動候補 r) p.Candidate "candidate"
            Expect.equal p.Candidate.由来警戒意図 p.Alert "alert"
            Expect.equal p.Alert.由来信念候補 p.Belief "belief"
            Expect.equal p.Sensing.信号ID p.Signal.ID "signal"
            Expect.equal p.Signal.原因因果ID (Some p.Emission.因果操作ID) "cause"
            Expect.equal (自発移動制御判断.由来成立因果操作ID d) (Some(因果操作ID "preflight.path.reaction")) "reaction cause"

        testCase "正常Matrix3072件はsnapshotと順序を保持" <| fun _ ->
            let mutable count = 0
            for direction in [ 自発移動方向.左; 自発移動方向.右 ] do
              for fromInput in [ false; true ] do
               for tick in [ 0L; 10L; 25L; 100L ] do
                for owner in [ プレイヤー側; 敵側; 中立 ] do
                 for x in [ -20.0; 40.0 ] do
                  for velocity in [ -3.0; 9.0 ] do
                   for hp in [ None; Some(HP 0) ] do
                    for hitbox in [ None; Some(矩形(3.0, 4.0)) ] do
                     for size in [ 1; 2; 4; 8 ] do
                        let s =
                            状態 tick プレイヤー
                            |> changeTarget (fun e ->
                                { e with 所有者 = owner; 位置 = 位置 x 7.0
                                         速度 = { X = velocity; Y = -2.0 }; HP = hp; 当たり判定 = hitbox })
                        let cs = [ for i in 0 .. size - 1 -> commandFrom s 主体A (string (size-i)) direction fromInput ]
                        let before = s, cs
                        let first = 互換移動適用前検証.一括を準備する s cs
                        let second = 互換移動適用前検証.一括を準備する s cs
                        Expect.equal first second "deterministic"
                        let p = first |> ok |> some
                        Expect.equal (互換移動適用準備.対象ゲーム状態 p) s "snapshot"
                        Expect.equal (互換移動適用準備.命令一覧 p) cs "ordered"
                        Expect.equal (互換移動適用準備.既存入力一覧 p) (List.map 互換移動命令.既存入力 cs) "projection"
                        Expect.equal (s,cs) before "unchanged"
                        count <- count + 1
            Expect.equal count 3072 "matrix size"

        testCase "不正Matrix80件はErrorを決定論的に収集" <| fun _ ->
            let mutable count = 0
            for tick in [ 0L; 10L; 25L; 100L ] do
             for direction in [ 自発移動方向.左; 自発移動方向.右 ] do
                let s = 状態 tick プレイヤー
                let c = commandFrom s 主体A "invalid" direction true
                let alert = alertState tick 主体A
                let cases =
                    [ { s with 終了状態 = Some クリア }, [ c ], [ endError ]
                      s, [ commandFrom (状態 (tick+1L) プレイヤー) 主体A "future" direction true ], [ tickError 0 ]
                      s, [ commandFrom s 主体B "actor" direction true ], [ actorError 0 ]
                      { s with エンティティ一覧 = [] }, [ c ], [ prefix "自発移動制御対象Entityが存在しません。" ]
                      { s with エンティティ一覧 = s.エンティティ一覧.Head :: s.エンティティ一覧 }, [ c ], [ prefix "自発移動制御対象EntityIDがゲーム状態内で重複しています。" ]
                      changeTarget (fun e -> { e with 種別 = 敵 }) s, [ c ], [ kindError ]
                      alert, [ c ], [ suppressionError ]
                      { alert with エンティティ反応状態一覧 = alert.エンティティ反応状態一覧 @ alert.エンティティ反応状態一覧 }, [ c ], [ prefix "エンティティ反応状態一覧内で実行者IDが重複しています。" ]
                      { alertState (tick+1L) 主体A with Tick = tick }, [ c ], [ prefix "エンティティ反応状態の成立Tickがゲーム状態Tickより後です。" ]
                      s, [ c; c ], [ duplicateError ] ]
                for current, cs, expected in cases do
                    let first = 互換移動適用前検証.一括を準備する current cs
                    Expect.equal (first |> errors) expected "fixed errors"
                    Expect.equal first (互換移動適用前検証.一括を準備する current cs) "deterministic errors"
                    count <- count + 1
            Expect.equal count 80 "matrix size"

        testCase "準備後Tickが進んでも準備のTickは元snapshot" <| fun _ ->
            let p = prepared ()
            let later = { 互換移動適用準備.対象ゲーム状態 p with Tick = 11L }
            Expect.equal (互換移動適用準備.Tick p) 10L "original"
            Expect.equal (互換移動適用前検証.一括を準備する later (互換移動適用準備.命令一覧 p) |> errors)
                [ tickError 0; tickError 1; tickError 2 ] "cannot retarget"

        testCase "判断不能ではEntity種別と抑制の二次エラーを出さない" <| fun _ ->
            let s = { alertState 10L 主体A with Tick = -1L } |> changeTarget (fun e -> { e with 種別 = 敵 })
            Expect.equal (互換移動適用前検証.単一を準備する s (cmd "r" 自発移動方向.右) |> errors)
                [ prefix "自発移動制御判断のゲーム状態Tickが負です。"
                  prefix "エンティティ反応状態の成立Tickがゲーム状態Tickより後です。"
                  tickError 0 ] "existing failure first"

        testCase "完全因果経路の観測根拠に正式感知値を保持" <| fun _ ->
            let p = path ()
            let sensing = p.Belief.根拠一覧 |> List.collect (fun o -> o.根拠一覧)
            Expect.contains sensing p.Sensing "exact sensing"
            Expect.equal p.Signals [ p.Signal ] "replayed authoritative signal"
            Expect.equal p.Initial.エンティティ一覧 p.Live.状態.エンティティ一覧 "physical entities"
    ]
