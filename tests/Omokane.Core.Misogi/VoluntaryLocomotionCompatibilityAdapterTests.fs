module VoluntaryLocomotionCompatibilityAdapterTests

open Expecto
open Omokane.Core.Domain
open Omokane.Core.Systems
open EntityReactionStateCausalTests.TestData

module private TestData =

    let ok = function
        | Ok value -> value
        | Error reasons -> failtestf "契約エラー: %A" reasons

    let some = function
        | Some value -> value
        | None -> failtest "Someを期待"

    let directions = [ 自発移動方向.左; 自発移動方向.右; 自発移動方向.上; 自発移動方向.下 ]

    let input = function
        | 自発移動方向.左 -> 左へ移動
        | 自発移動方向.右 -> 右へ移動
        | 自発移動方向.上 -> 上へ移動
        | 自発移動方向.下 -> 下へ移動

    let state suppressed tick kind actor =
        let initial = { 状態 tick kind with プレイヤーID = 主体B }
        if suppressed then
            let request = 要求 "compat.request" tick actor 0.75 "地盤の危険"
            let update, _ = 適用する initial "compat.reaction" None [] request
            update.状態
        else
            initial

    let intent fromInput id tick actor direction =
        if fromInput then
            自発移動意図生成.入力から作る (自発移動意図ID id) tick actor (input direction) |> ok |> some
        else
            自発移動意図生成.方向から作る (自発移動意図ID id) tick actor direction |> ok

    let gateFrom state actor move =
        let decision = 自発移動制御判断生成.ゲーム状態から作る actor state |> ok
        自発移動意図制御.適用する decision move |> ok

    let gate suppressed fromInput tick actor direction =
        let s = state suppressed tick 敵 actor
        intent fromInput "compat.intent" tick actor direction |> gateFrom s actor

    let convert suppressed direction =
        gate suppressed true 10L 主体A direction |> 自発移動互換変換.変換する

    let command direction = convert false direction |> 互換移動変換結果.命令 |> some

    let mixed () =
        [ convert false 自発移動方向.右
          convert true 自発移動方向.左
          convert false 自発移動方向.上
          convert false 自発移動方向.左
          convert true 自発移動方向.下 ]

    let ledger = function
        | 統合因果台帳追記結果.成功 value -> value
        | 統合因果台帳追記結果.失敗(_, index, id, reasons) ->
            failtestf "台帳失敗: %d %A %A" index id reasons

    let replay = function
        | 統合因果台帳再生結果.成功(update, signals) -> update, signals
        | 統合因果台帳再生結果.失敗(_, index, id, reasons) ->
            failtestf "Replay失敗: %d %A %A" index id reasons

    type Path =
        { Initial: ゲーム状態
          Signal: 伝播信号
          Emission: 地盤振動発生記録
          Sensing: 感知値
          Belief: 信念候補
          Alert: 警戒意図
          Candidate: 行動候補
          Request: エンティティ反応要求
          Reaction: エンティティ反応状態変更記録
          Ledger: 統合因果台帳
          Live: 更新結果
          Replay: 更新結果
          Signals: 伝播信号 list
          Intent: 自発移動意図 }

    let path () =
        let initial = 状態 10L 敵
        let operation: 地盤振動発生操作 =
            { 因果 =
                { ID = 因果操作ID "compat.emission"
                  Tick = 10L
                  種別 = 伝播信号生成
                  原因因果ID = None
                  実行者ID = Some 発生者
                  対象EntityID = None
                  概要 = "互換境界へ至る地盤振動"
                  発行Event一覧 = [ 衝突発生(発生者, 主体A) ] }
              信号ID = 伝播信号ID "compat.signal"
              発生位置 = 位置 1.0 2.0
              方向 = None
              強度 = 10.0
              方式 = 波動
              寿命Tick = 5L }
        let signal, emission =
            match 地盤振動発生実行.適用する initial operation with
            | 発生成功(_, s, r) -> s, r
            | result -> failtestf "発生失敗: %A" result
        let firstLedger =
            統合因果台帳.地盤振動発生記録一覧を追記する 統合因果台帳.空 [ emission ] |> ledger
        let before, signals = 統合因果台帳再生.再生する initial firstLedger |> replay
        let replaySignal = List.exactlyOne signals
        let sensingInput: 地盤振動警戒入力 =
            { 現在Tick = before.状態.Tick
              観測者ID = 主体A
              観測位置 = 位置 1.0 2.0
              感知設定 =
                { 種別 = 地盤感知; 対象量種別 = 地盤振動; 対象媒体 = 地盤
                  最大距離 = 10.0; 感度倍率 = 1.0; 感知閾値 = 0.0; 確信飽和値 = 10.0 }
              信号 = replaySignal
              観測概要 = "強い揺れ"
              仮説 = "地盤崩落の危険"
              事前確率 = 0.5
              警戒設定 = { 発生閾値 = 0.75 } }
        let sensing, belief, alert =
            match 地盤振動警戒経路.実行する sensingInput with
            | Ok(警戒成立(s, _, b, a)) -> s, b, a
            | result -> failtestf "認識失敗: %A" result
        let candidate = 行動候補生成.警戒意図から作る (行動候補ID "compat.action") alert |> ok
        let selected = 行動候補選択.選ぶ [ candidate ] |> ok |> some
        let request =
            エンティティ反応要求生成.選択済み行動候補から作る
                (エンティティ反応要求ID "compat.path.request") 10L selected |> ok
        let live, reaction =
            適用する initial "compat.path.reaction" (Some emission.因果操作ID)
                [ ダメージ発生(主体A, 0) ] request
        let fullLedger =
            統合因果台帳.エンティティ反応状態変更記録一覧を追記する firstLedger [ reaction ] |> ledger
        let replayed, finalSignals = 統合因果台帳再生.再生する initial fullLedger |> replay
        { Initial = initial; Signal = signal; Emission = emission; Sensing = sensing
          Belief = belief; Alert = alert; Candidate = candidate; Request = request
          Reaction = reaction; Ledger = fullLedger; Live = live; Replay = replayed
          Signals = finalSignals; Intent = intent true "compat.path.intent" 10L 主体A 自発移動方向.右 }

    let fromPath state (p: Path) = gateFrom state 主体A p.Intent |> 自発移動互換変換.変換する

open TestData

[<Tests>]
let 全テスト =
    testList "主体付き互換移動Adapter" [

        testCase "左通過は対応命令を生成" <| fun _ ->
            Expect.equal (command 自発移動方向.左 |> 互換移動命令.種別) (互換移動命令種別.左へ移動) "左通過は対応命令を生成"

        testCase "左命令は既存Inputへ投影" <| fun _ ->
            Expect.equal (command 自発移動方向.左 |> 互換移動命令.既存入力) (左へ移動) "左命令は既存Inputへ投影"

        testCase "右通過は対応命令を生成" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.種別) (互換移動命令種別.右へ移動) "右通過は対応命令を生成"

        testCase "右命令は既存Inputへ投影" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.既存入力) (右へ移動) "右命令は既存Inputへ投影"

        testCase "命令の由来制御結果を保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来制御結果) (gate false true 10L 主体A 自発移動方向.右) "命令の由来制御結果を保存"

        testCase "命令の由来自発移動意図を保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来自発移動意図) (gate false true 10L 主体A 自発移動方向.右 |> 自発移動意図制御結果.意図) "命令の由来自発移動意図を保存"

        testCase "命令の由来自発移動意図IDを保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来自発移動意図ID) (自発移動意図ID "compat.intent") "命令の由来自発移動意図IDを保存"

        testCase "命令のTickを保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.Tick) (10L) "命令のTickを保存"

        testCase "命令の実行者IDを保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.実行者ID) (主体A) "命令の実行者IDを保存"

        testCase "命令の由来自発移動方向を保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来自発移動方向) (自発移動方向.右) "命令の由来自発移動方向を保存"

        testCase "命令の由来入力を保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来入力) (Some 右へ移動) "命令の由来入力を保存"

        testCase "命令の由来制御判断を保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来制御判断) (gate false true 10L 主体A 自発移動方向.右 |> 自発移動意図制御結果.制御判断) "命令の由来制御判断を保存"

        testCase "命令の由来制御理由を保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来制御理由) (自発移動制御理由.反応状態なし) "命令の由来制御理由を保存"

        testCase "命令の由来反応要求IDを保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来反応要求ID) (None) "命令の由来反応要求IDを保存"

        testCase "命令の由来反応状態成立因果操作IDを保存" <| fun _ ->
            Expect.equal (command 自発移動方向.右 |> 互換移動命令.由来反応状態成立因果操作ID) (None) "命令の由来反応状態成立因果操作IDを保存"

        testCase "明示方向命令に由来Inputはない" <| fun _ ->
            Expect.equal (gate false false 10L 主体A 自発移動方向.左 |> 自発移動互換変換.変換する |> 互換移動変換結果.命令 |> some |> 互換移動命令.由来入力) (None) "明示方向命令に由来Inputはない"

        testCase "通過上は正常な方向未対応" <| fun _ ->
            Expect.equal (convert false 自発移動方向.上 |> 互換移動変換結果.非生成理由) (Some 互換移動非生成理由.上方向未対応) "通過上は正常な方向未対応"

        testCase "上未対応は既存Inputなし" <| fun _ ->
            Expect.equal (convert false 自発移動方向.上 |> 互換移動変換結果.既存入力) (None) "上未対応は既存Inputなし"

        testCase "上未対応方向アクセサ" <| fun _ ->
            Expect.equal (convert false 自発移動方向.上 |> 互換移動変換結果.未対応方向) (Some 自発移動方向.上) "上未対応方向アクセサ"

        testCase "通過下は正常な方向未対応" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.非生成理由) (Some 互換移動非生成理由.下方向未対応) "通過下は正常な方向未対応"

        testCase "下未対応は既存Inputなし" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.既存入力) (None) "下未対応は既存Inputなし"

        testCase "下未対応方向アクセサ" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.未対応方向) (Some 自発移動方向.下) "下未対応方向アクセサ"

        testCase "抑制左は方向未対応より優先" <| fun _ ->
            let r = convert true 自発移動方向.左
            Expect.equal (互換移動変換結果.非生成理由 r) (Some 互換移動非生成理由.自発移動抑制) "抑制優先"
            Expect.isFalse (互換移動変換結果.方向未対応である r) "未対応ではない"
            Expect.isNone (互換移動変換結果.未対応方向 r) "未対応方向なし"
            Expect.isNone (互換移動変換結果.命令 r) "命令なし"

        testCase "抑制右は方向未対応より優先" <| fun _ ->
            let r = convert true 自発移動方向.右
            Expect.equal (互換移動変換結果.非生成理由 r) (Some 互換移動非生成理由.自発移動抑制) "抑制優先"
            Expect.isFalse (互換移動変換結果.方向未対応である r) "未対応ではない"
            Expect.isNone (互換移動変換結果.未対応方向 r) "未対応方向なし"
            Expect.isNone (互換移動変換結果.命令 r) "命令なし"

        testCase "抑制上は方向未対応より優先" <| fun _ ->
            let r = convert true 自発移動方向.上
            Expect.equal (互換移動変換結果.非生成理由 r) (Some 互換移動非生成理由.自発移動抑制) "抑制優先"
            Expect.isFalse (互換移動変換結果.方向未対応である r) "未対応ではない"
            Expect.isNone (互換移動変換結果.未対応方向 r) "未対応方向なし"
            Expect.isNone (互換移動変換結果.命令 r) "命令なし"

        testCase "抑制下は方向未対応より優先" <| fun _ ->
            let r = convert true 自発移動方向.下
            Expect.equal (互換移動変換結果.非生成理由 r) (Some 互換移動非生成理由.自発移動抑制) "抑制優先"
            Expect.isFalse (互換移動変換結果.方向未対応である r) "未対応ではない"
            Expect.isNone (互換移動変換結果.未対応方向 r) "未対応方向なし"
            Expect.isNone (互換移動変換結果.命令 r) "命令なし"

        testCase "命令結果の命令を生成した" <| fun _ ->
            Expect.equal (convert false 自発移動方向.右 |> 互換移動変換結果.命令を生成した) (true) "命令結果の命令を生成した"

        testCase "命令結果の命令を生成しなかった" <| fun _ ->
            Expect.equal (convert false 自発移動方向.右 |> 互換移動変換結果.命令を生成しなかった) (false) "命令結果の命令を生成しなかった"

        testCase "命令結果の自発移動抑制である" <| fun _ ->
            Expect.equal (convert false 自発移動方向.右 |> 互換移動変換結果.自発移動抑制である) (false) "命令結果の自発移動抑制である"

        testCase "命令結果の方向未対応である" <| fun _ ->
            Expect.equal (convert false 自発移動方向.右 |> 互換移動変換結果.方向未対応である) (false) "命令結果の方向未対応である"

        testCase "命令結果の非生成理由" <| fun _ ->
            Expect.equal (convert false 自発移動方向.右 |> 互換移動変換結果.非生成理由) (None) "命令結果の非生成理由"

        testCase "命令結果の未対応方向" <| fun _ ->
            Expect.equal (convert false 自発移動方向.右 |> 互換移動変換結果.未対応方向) (None) "命令結果の未対応方向"

        testCase "抑制結果の命令を生成した" <| fun _ ->
            Expect.equal (convert true 自発移動方向.上 |> 互換移動変換結果.命令を生成した) (false) "抑制結果の命令を生成した"

        testCase "抑制結果の命令を生成しなかった" <| fun _ ->
            Expect.equal (convert true 自発移動方向.上 |> 互換移動変換結果.命令を生成しなかった) (true) "抑制結果の命令を生成しなかった"

        testCase "抑制結果の自発移動抑制である" <| fun _ ->
            Expect.equal (convert true 自発移動方向.上 |> 互換移動変換結果.自発移動抑制である) (true) "抑制結果の自発移動抑制である"

        testCase "抑制結果の既存入力" <| fun _ ->
            Expect.equal (convert true 自発移動方向.上 |> 互換移動変換結果.既存入力) (None) "抑制結果の既存入力"

        testCase "未対応結果の命令を生成した" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.命令を生成した) (false) "未対応結果の命令を生成した"

        testCase "未対応結果の命令を生成しなかった" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.命令を生成しなかった) (true) "未対応結果の命令を生成しなかった"

        testCase "未対応結果の自発移動抑制である" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.自発移動抑制である) (false) "未対応結果の自発移動抑制である"

        testCase "未対応結果の方向未対応である" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.方向未対応である) (true) "未対応結果の方向未対応である"

        testCase "未対応結果の命令" <| fun _ ->
            Expect.equal (convert false 自発移動方向.下 |> 互換移動変換結果.命令) (None) "未対応結果の命令"

        testCase "全結果の自発移動意図ID" <| fun _ ->
            for suppressed in [ false; true ] do
                for direction in directions do
                    Expect.equal (convert suppressed direction |> 互換移動変換結果.自発移動意図ID) (自発移動意図ID "compat.intent") "自発移動意図ID"

        testCase "全結果のTick" <| fun _ ->
            for suppressed in [ false; true ] do
                for direction in directions do
                    Expect.equal (convert suppressed direction |> 互換移動変換結果.Tick) (10L) "Tick"

        testCase "全結果の実行者ID" <| fun _ ->
            for suppressed in [ false; true ] do
                for direction in directions do
                    Expect.equal (convert suppressed direction |> 互換移動変換結果.実行者ID) (主体A) "実行者ID"

        testCase "全結果の方向は元方向" <| fun _ ->
            for suppressed in [ false; true ] do
                for direction in directions do
                    Expect.equal (convert suppressed direction |> 互換移動変換結果.自発移動方向) direction "方向"

        testCase "命令はGateと意図を構造的に保持" <| fun _ ->
            let g = gate false true 10L 主体A 自発移動方向.右
            let r = 自発移動互換変換.変換する g
            Expect.equal (互換移動変換結果.由来制御結果 r) g "Gate"
            Expect.equal (互換移動変換結果.由来自発移動意図 r) (自発移動意図制御結果.意図 g) "意図"

        testCase "未対応はGateと意図を構造的に保持" <| fun _ ->
            let g = gate false true 10L 主体A 自発移動方向.上
            let r = 自発移動互換変換.変換する g
            Expect.equal (互換移動変換結果.由来制御結果 r) g "Gate"
            Expect.equal (互換移動変換結果.由来自発移動意図 r) (自発移動意図制御結果.意図 g) "意図"

        testCase "抑制はGateと意図を構造的に保持" <| fun _ ->
            let g = gate true true 10L 主体A 自発移動方向.下
            let r = 自発移動互換変換.変換する g
            Expect.equal (互換移動変換結果.由来制御結果 r) g "Gate"
            Expect.equal (互換移動変換結果.由来自発移動意図 r) (自発移動意図制御結果.意図 g) "意図"

        testCase "抑制の要求IDを保持" <| fun _ ->
            Expect.equal (convert true 自発移動方向.右 |> 互換移動変換結果.由来反応要求ID) (Some(エンティティ反応要求ID "compat.request")) "抑制の要求IDを保持"

        testCase "抑制の成立因果IDを保持" <| fun _ ->
            Expect.equal (convert true 自発移動方向.右 |> 互換移動変換結果.由来反応状態成立因果操作ID) (Some(因果操作ID "compat.reaction")) "抑制の成立因果IDを保持"

        testCase "抑制の制御理由を保持" <| fun _ ->
            Expect.equal (convert true 自発移動方向.右 |> 互換移動変換結果.由来制御結果 |> 自発移動意図制御結果.制御理由) (自発移動制御理由.その場警戒中) "抑制の制御理由を保持"

        testCase "空一括変換" <| fun _ ->
            Expect.equal (自発移動互換変換.一括変換する []) ([]) "空一括変換"

        testCase "一件左の一括変換" <| fun _ ->
            Expect.equal ([ gate false true 10L 主体A 自発移動方向.左 ] |> 自発移動互換変換.一括変換する) ([ convert false 自発移動方向.左 ]) "一件左の一括変換"

        testCase "一件右の一括変換" <| fun _ ->
            Expect.equal ([ gate false true 10L 主体A 自発移動方向.右 ] |> 自発移動互換変換.一括変換する) ([ convert false 自発移動方向.右 ]) "一件右の一括変換"

        testCase "一件抑制の一括変換" <| fun _ ->
            Expect.equal ([ gate true true 10L 主体A 自発移動方向.右 ] |> 自発移動互換変換.一括変換する) ([ convert true 自発移動方向.右 ]) "一件抑制の一括変換"

        testCase "一件未対応の一括変換" <| fun _ ->
            Expect.equal ([ gate false true 10L 主体A 自発移動方向.上 ] |> 自発移動互換変換.一括変換する) ([ convert false 自発移動方向.上 ]) "一件未対応の一括変換"

        testCase "混在一括変換は一入力一結果で順序維持" <| fun _ ->
            let expected = mixed ()
            let gates = expected |> List.map 互換移動変換結果.由来制御結果
            let actual = 自発移動互換変換.一括変換する gates
            Expect.equal actual expected "順序"
            Expect.equal actual.Length gates.Length "件数"

        testCase "命令Queryの入力順" <| fun _ ->
            Expect.equal (mixed () |> 互換移動変換結果一覧.命令一覧 |> List.map 互換移動命令.種別) ([ 互換移動命令種別.右へ移動; 互換移動命令種別.左へ移動 ]) "命令Queryの入力順"

        testCase "未生成Queryの入力順" <| fun _ ->
            Expect.equal (mixed () |> 互換移動変換結果一覧.未生成結果一覧) ([ convert true 自発移動方向.左; convert false 自発移動方向.上; convert true 自発移動方向.下 ]) "未生成Queryの入力順"

        testCase "抑制Queryの入力順" <| fun _ ->
            Expect.equal (mixed () |> 互換移動変換結果一覧.抑制結果一覧) ([ convert true 自発移動方向.左; convert true 自発移動方向.下 ]) "抑制Queryの入力順"

        testCase "方向未対応Queryの入力順" <| fun _ ->
            Expect.equal (mixed () |> 互換移動変換結果一覧.方向未対応結果一覧) ([ convert false 自発移動方向.上 ]) "方向未対応Queryの入力順"

        testCase "同じGateの重複は命令Queryまで保持" <| fun _ ->
            let g = gate false true 10L 主体A 自発移動方向.右
            let results = 自発移動互換変換.一括変換する [ g; g ]
            Expect.equal results [ 自発移動互換変換.変換する g; 自発移動互換変換.変換する g ] "重複"
            Expect.equal (互換移動変換結果一覧.命令一覧 results).Length 2 "命令重複"

        testCase "未生成Queryも重複を保持" <| fun _ ->
            let r = convert true 自発移動方向.上
            Expect.equal (互換移動変換結果一覧.未生成結果一覧 [ r; r ]) [ r; r ] "重複"
            Expect.equal (互換移動変換結果一覧.抑制結果一覧 [ r; r ]) [ r; r ] "抑制重複"

        testCase "複数主体は元の順序を保持" <| fun _ ->
            let actors = [ 主体B; 主体A; 主体B ]
            let results = actors |> List.map (fun a -> gate false true 10L a 自発移動方向.右) |> 自発移動互換変換.一括変換する
            Expect.equal (results |> List.map 互換移動変換結果.実行者ID) actors "主体順"

        testCase "複数Tickは元の順序を保持" <| fun _ ->
            let ticks = [ 100L; 0L; 25L; 10L ]
            let results = ticks |> List.map (fun t -> gate false true t 主体A 自発移動方向.右) |> 自発移動互換変換.一括変換する
            Expect.equal (results |> List.map 互換移動変換結果.Tick) ticks "Tick順"

        testCase "左右命令を相殺しない" <| fun _ ->
            let results = [ 自発移動方向.右; 自発移動方向.左 ] |> List.map (convert false)
            Expect.equal (互換移動変換結果一覧.命令一覧 results |> List.map 互換移動命令.既存入力) [ 右へ移動; 左へ移動 ] "非相殺"

        testCase "プレイヤー左命令と旧Movement入力は等価" <| fun _ ->
            let initial = 思兼神.初期状態を作る ()
            let move = intent true "compat.legacy" initial.Tick initial.プレイヤーID 自発移動方向.左
            let c = gateFrom initial initial.プレイヤーID move |> 自発移動互換変換.変換する |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動命令.実行者ID c) initial.プレイヤーID "適用主体"
            Expect.equal (思兼神.更新する [ 互換移動命令.既存入力 c ] initial) (思兼神.更新する [ 左へ移動 ] initial) "既存互換"

        testCase "プレイヤー右命令と旧Movement入力は等価" <| fun _ ->
            let initial = 思兼神.初期状態を作る ()
            let move = intent true "compat.legacy" initial.Tick initial.プレイヤーID 自発移動方向.右
            let c = gateFrom initial initial.プレイヤーID move |> 自発移動互換変換.変換する |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動命令.実行者ID c) initial.プレイヤーID "適用主体"
            Expect.equal (思兼神.更新する [ 互換移動命令.既存入力 c ] initial) (思兼神.更新する [ 右へ移動 ] initial) "既存互換"

        testCase "敵命令はプレイヤーへ主体を書き換えない" <| fun _ ->
            let s = state false 10L 敵 主体A
            Expect.notEqual s.プレイヤーID 主体A "非プレイヤーID"
            let c = intent true "non.player" 10L 主体A 自発移動方向.右 |> gateFrom s 主体A |> 自発移動互換変換.変換する |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動命令.実行者ID c) 主体A "元主体"

        testCase "弾命令はプレイヤーへ主体を書き換えない" <| fun _ ->
            let s = state false 10L 弾 主体A
            Expect.notEqual s.プレイヤーID 主体A "非プレイヤーID"
            let c = intent true "non.player" 10L 主体A 自発移動方向.右 |> gateFrom s 主体A |> 自発移動互換変換.変換する |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動命令.実行者ID c) 主体A "元主体"

        testCase "障害物命令はプレイヤーへ主体を書き換えない" <| fun _ ->
            let s = state false 10L 障害物 主体A
            Expect.notEqual s.プレイヤーID 主体A "非プレイヤーID"
            let c = intent true "non.player" 10L 主体A 自発移動方向.右 |> gateFrom s 主体A |> 自発移動互換変換.変換する |> 互換移動変換結果.命令 |> some
            Expect.equal (互換移動命令.実行者ID c) 主体A "元主体"

        testCase "攻撃はAdapter対象を生成しない" <| fun _ ->
            Expect.equal (自発移動意図生成.入力から作る (自発移動意図ID "") -1L (エンティティID "") 攻撃) (Ok None) "攻撃はAdapter対象を生成しない"

        testCase "ジャンプはAdapter対象を生成しない" <| fun _ ->
            Expect.equal (自発移動意図生成.入力から作る (自発移動意図ID "") -1L (エンティティID "") ジャンプ) (Ok None) "ジャンプはAdapter対象を生成しない"

        testCase "入力なしはAdapter対象を生成しない" <| fun _ ->
            Expect.equal (自発移動意図生成.入力から作る (自発移動意図ID "") -1L (エンティティID "") 入力なし) (Ok None) "入力なしはAdapter対象を生成しない"

        testCase "正式反応適用前の右意図は命令生成" <| fun _ ->
            let p = path ()
            Expect.equal (fromPath p.Initial p |> 互換移動変換結果.既存入力) (Some 右へ移動) "適用前"

        testCase "正式反応適用後の右意図は抑制" <| fun _ ->
            let p = path ()
            Expect.equal (fromPath p.Live.状態 p |> 互換移動変換結果.非生成理由) (Some 互換移動非生成理由.自発移動抑制) "適用後"

        testCase "統合Replay後の右意図は抑制" <| fun _ ->
            let p = path ()
            Expect.isTrue (fromPath p.Replay.状態 p |> 互換移動変換結果.自発移動抑制である) "Replay抑制"

        testCase "live変換結果とReplay変換結果は構造的一致" <| fun _ ->
            let p = path ()
            Expect.equal (fromPath p.Live.状態 p) (fromPath p.Replay.状態 p) "live/Replay"

        testCase "完全Provenanceを変換結果から正式因果まで辿れる" <| fun _ ->
            let p = path ()
            let r = fromPath p.Live.状態 p
            let reaction = r |> 互換移動変換結果.由来制御結果 |> 自発移動意図制御結果.由来反応状態 |> some
            let request = エンティティ反応状態.由来要求 reaction
            let candidate = エンティティ反応要求.由来行動候補 request
            let alert = candidate.由来警戒意図
            let sensing = alert.由来信念候補.根拠一覧.Head.根拠一覧.Head
            Expect.equal (互換移動変換結果.由来反応要求ID r) (Some(エンティティ反応要求.ID p.Request)) "要求"
            Expect.equal (互換移動変換結果.由来反応状態成立因果操作ID r) (Some p.Reaction.因果操作ID) "反応因果"
            Expect.equal candidate p.Candidate "候補"
            Expect.equal alert p.Alert "警戒"
            Expect.equal alert.由来信念候補 p.Belief "信念"
            Expect.equal sensing p.Sensing "感知"
            Expect.equal sensing.信号ID p.Signal.ID "信号"
            Expect.equal p.Signal.原因因果ID (Some p.Emission.因果操作ID) "発生因果"
            Expect.equal p.Reaction.原因因果ID p.Signal.原因因果ID "因果接続"
            Expect.equal (互換移動変換結果.由来自発移動意図 r) p.Intent "意図"

        testCase "変換後もGameStateは因果適用の結果だけを保持" <| fun _ ->
            let p = path ()
            fromPath p.Live.状態 p |> ignore
            Expect.equal p.Live.状態 { p.Initial with エンティティ反応状態一覧 = [ p.Reaction.変更後状態 ] } "World Truth"

        testCase "変換後も記録済みEvent列だけを再出力" <| fun _ ->
            let p = path ()
            fromPath p.Replay.状態 p |> ignore
            Expect.equal p.Replay.イベント一覧 (p.Emission.発行Event一覧 @ p.Reaction.発行Event一覧) "Event"

        testCase "変換後の統合台帳再生は同一結果" <| fun _ ->
            let p = path ()
            let before = 統合因果台帳再生.再生する p.Initial p.Ledger
            fromPath p.Replay.状態 p |> ignore
            Expect.equal (統合因果台帳再生.再生する p.Initial p.Ledger) before "台帳とReplay"

        testCase "変換後も正式Replay信号を保持" <| fun _ ->
            let p = path ()
            fromPath p.Replay.状態 p |> ignore
            Expect.equal p.Signals [ p.Signal ] "信号"
            Expect.equal p.Signal p.Emission.信号 "正式生成信号"

        testCase "完全経路でEntity物理値が不変" <| fun _ ->
            let p = path ()
            fromPath p.Live.状態 p |> ignore
            Expect.equal p.Live.状態.エンティティ一覧 p.Initial.エンティティ一覧 "live Entity"
            Expect.equal p.Replay.状態.エンティティ一覧 p.Initial.エンティティ一覧 "Replay Entity"

        testCase "終了状態差は互換変換へ影響しない" <| fun _ ->
            for suppressed in [ false; true ] do
                let s = state suppressed 10L 敵 主体A
                let changed = { s with 終了状態 = Some ゲームオーバー }
                let move = intent true "same.intent" 10L 主体A 自発移動方向.右
                let first = gateFrom s 主体A move |> 自発移動互換変換.変換する
                let second = gateFrom changed 主体A move |> 自発移動互換変換.変換する
                Expect.equal second first "終了状態非干渉"

        testCase "位置差は互換変換へ影響しない" <| fun _ ->
            for suppressed in [ false; true ] do
                let s = state suppressed 10L 敵 主体A
                let changed = { s with エンティティ一覧 = s.エンティティ一覧 |> List.map (fun e -> { e with 位置 = 位置 -100.0 42.0 }) }
                let move = intent true "same.intent" 10L 主体A 自発移動方向.右
                let first = gateFrom s 主体A move |> 自発移動互換変換.変換する
                let second = gateFrom changed 主体A move |> 自発移動互換変換.変換する
                Expect.equal second first "位置非干渉"

        testCase "速度差は互換変換へ影響しない" <| fun _ ->
            for suppressed in [ false; true ] do
                let s = state suppressed 10L 敵 主体A
                let changed = { s with エンティティ一覧 = s.エンティティ一覧 |> List.map (fun e -> { e with 速度 = { X = -30.0; Y = 50.0 } }) }
                let move = intent true "same.intent" 10L 主体A 自発移動方向.右
                let first = gateFrom s 主体A move |> 自発移動互換変換.変換する
                let second = gateFrom changed 主体A move |> 自発移動互換変換.変換する
                Expect.equal second first "速度非干渉"

        testCase "HP差は互換変換へ影響しない" <| fun _ ->
            for suppressed in [ false; true ] do
                let s = state suppressed 10L 敵 主体A
                let changed = { s with エンティティ一覧 = s.エンティティ一覧 |> List.map (fun e -> { e with HP = None }) }
                let move = intent true "same.intent" 10L 主体A 自発移動方向.右
                let first = gateFrom s 主体A move |> 自発移動互換変換.変換する
                let second = gateFrom changed 主体A move |> 自発移動互換変換.変換する
                Expect.equal second first "HP非干渉"

        testCase "乱数Seed差は互換変換へ影響しない" <| fun _ ->
            for suppressed in [ false; true ] do
                let s = state suppressed 10L 敵 主体A
                let changed = { s with 乱数Seed = -99 }
                let move = intent true "same.intent" 10L 主体A 自発移動方向.右
                let first = gateFrom s 主体A move |> 自発移動互換変換.変換する
                let second = gateFrom changed 主体A move |> 自発移動互換変換.変換する
                Expect.equal second first "乱数Seed非干渉"

        testCase "単一変換の決定論と元Gate保持" <| fun _ ->
            let g = gate true true 10L 主体A 自発移動方向.上
            let first = 自発移動互換変換.変換する g
            Expect.equal (自発移動互換変換.変換する g) first "決定論"
            Expect.equal (互換移動変換結果.由来制御結果 first) g "元Gate"

        testCase "一括変換は重複を含め決定論的" <| fun _ ->
            let gates = mixed () |> List.map 互換移動変換結果.由来制御結果
            let input = gates @ gates
            Expect.equal (自発移動互換変換.一括変換する input) (自発移動互換変換.一括変換する input) "決定論"

        testCase "Int64最大TickとUnicode意図IDを保持" <| fun _ ->
            let s = state false System.Int64.MaxValue 敵 主体A
            let r = intent false "意図.互換" s.Tick 主体A 自発移動方向.左 |> gateFrom s 主体A |> 自発移動互換変換.変換する
            Expect.equal (互換移動変換結果.Tick r) System.Int64.MaxValue "Tick"
            Expect.equal (互換移動変換結果.自発移動意図ID r) (自発移動意図ID "意図.互換") "ID"

        testCase "2304ケース単一変換Matrix" <| fun _ ->
            let cases =
                [ for suppressed in [ false; true ] do
                    for direction in directions do
                      for fromInput in [ false; true ] do
                        for tick in [ 0L; 10L; 25L; 100L ] do
                          for kind in [ プレイヤー; 敵; 弾; 障害物 ] do
                            for owner in [ プレイヤー側; 敵側; 中立 ] do
                              for ending in [ None; Some ゲームオーバー; Some クリア ] do
                                yield suppressed, direction, fromInput, tick, kind, owner, ending ]
            Expect.equal cases.Length 2304 "Matrix件数"
            for suppressed, direction, fromInput, tick, kind, owner, ending in cases do
                let s = state suppressed tick kind 主体A
                let original =
                    { s with
                        終了状態 = ending
                        エンティティ一覧 = s.エンティティ一覧 |> List.map (fun e -> { e with 所有者 = owner }) }
                let move = intent fromInput "matrix.intent" tick 主体A direction
                let g = gateFrom original 主体A move
                let first = 自発移動互換変換.変換する g
                Expect.equal (自発移動互換変換.変換する g) first "決定論"
                Expect.equal (互換移動変換結果.由来制御結果 first) g "Gate保存"
                Expect.equal (互換移動変換結果.由来自発移動意図 first) move "意図保存"
                Expect.equal original.エンティティ反応状態一覧 s.エンティティ反応状態一覧 "状態不変"
                if suppressed then
                    Expect.equal (互換移動変換結果.非生成理由 first) (Some 互換移動非生成理由.自発移動抑制) "抑制"
                else
                    Expect.equal (互換移動変換結果.命令を生成した first)
                        (List.contains direction [ 自発移動方向.左; 自発移動方向.右 ]) "対応集合"
                    Expect.equal (互換移動変換結果.方向未対応である first)
                        (List.contains direction [ 自発移動方向.上; 自発移動方向.下 ]) "未対応集合"
                Expect.equal (gateFrom original 主体A move) g "GameState非干渉"

        testCase "42ケース一括変換Matrix" <| fun _ ->
            let patterns =
                [ [ false, 自発移動方向.右, 主体A, 10L; false, 自発移動方向.左, 主体A, 10L ]
                  [ false, 自発移動方向.上, 主体A, 10L; false, 自発移動方向.下, 主体A, 10L ]
                  [ true, 自発移動方向.上, 主体A, 10L; true, 自発移動方向.右, 主体A, 10L ]
                  [ false, 自発移動方向.右, 主体A, 10L; true, 自発移動方向.左, 主体A, 10L ]
                  [ false, 自発移動方向.右, 主体A, 10L; false, 自発移動方向.上, 主体A, 10L ]
                  [ false, 自発移動方向.右, 主体B, 10L; false, 自発移動方向.左, 主体A, 10L ]
                  [ false, 自発移動方向.右, 主体A, 100L; false, 自発移動方向.左, 主体A, 0L ] ]
            let cases = [ for count in [ 0; 1; 2; 4; 8; 16 ] do for pattern in patterns do yield count, pattern ]
            Expect.equal cases.Length 42 "一括Matrix件数"
            for count, pattern in cases do
                let gates = List.init count (fun i ->
                    let suppressed, direction, actor, tick = pattern[i % pattern.Length]
                    gate suppressed true tick actor direction)
                let first = 自発移動互換変換.一括変換する gates
                Expect.equal (自発移動互換変換.一括変換する gates) first "決定論"
                Expect.equal first.Length count "件数"
                Expect.equal (first |> List.map 互換移動変換結果.由来制御結果) gates "順序と重複"
                Expect.equal (first |> List.map 互換移動変換結果.実行者ID)
                    (gates |> List.map (自発移動意図制御結果.意図 >> 自発移動意図.実行者ID)) "主体"
                Expect.equal (first |> List.map 互換移動変換結果.Tick)
                    (gates |> List.map (自発移動意図制御結果.意図 >> 自発移動意図.Tick)) "Tick"

        testCase "空結果一覧の全Queryは空" <| fun _ ->
            Expect.equal (互換移動変換結果一覧.命令一覧 []) [] "命令"
            Expect.equal (互換移動変換結果一覧.未生成結果一覧 []) [] "未生成"
            Expect.equal (互換移動変換結果一覧.抑制結果一覧 []) [] "抑制"
            Expect.equal (互換移動変換結果一覧.方向未対応結果一覧 []) [] "未対応"

        testCase "方向未対応Queryは上下順と重複を保持" <| fun _ ->
            let upper = convert false 自発移動方向.上
            let lower = convert false 自発移動方向.下
            let results = [ lower; convert true 自発移動方向.上; upper; lower ]
            Expect.equal (互換移動変換結果一覧.方向未対応結果一覧 results) [ lower; upper; lower ] "未対応順"

    ]
