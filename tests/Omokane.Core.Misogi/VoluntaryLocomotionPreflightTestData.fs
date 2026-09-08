module VoluntaryLocomotionPreflightTestData

open Expecto
open Omokane.Core.Domain
open Omokane.Core.Systems
open EntityReactionStateCausalTests.TestData

module TestData =

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

    let intent fromInput id tick actor direction =
        if fromInput then
            自発移動意図生成.入力から作る (自発移動意図ID id) tick actor (input direction) |> ok |> some
        else
            自発移動意図生成.方向から作る (自発移動意図ID id) tick actor direction |> ok

    let gateFrom state actor move =
        let decision = 自発移動制御判断生成.ゲーム状態から作る actor state |> ok
        自発移動意図制御.適用する decision move |> ok

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
        let initial = 状態 10L プレイヤー
        let operation: 地盤振動発生操作 =
            { 因果 =
                { ID = 因果操作ID "preflight.emission"
                  Tick = 10L
                  種別 = 伝播信号生成
                  原因因果ID = None
                  実行者ID = Some 発生者
                  対象EntityID = None
                  概要 = "互換境界へ至る地盤振動"
                  発行Event一覧 = [ 衝突発生(発生者, 主体A) ] }
              信号ID = 伝播信号ID "preflight.signal"
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
        let candidate = 行動候補生成.警戒意図から作る (行動候補ID "preflight.action") alert |> ok
        let selected = 行動候補選択.選ぶ [ candidate ] |> ok |> some
        let request =
            エンティティ反応要求生成.選択済み行動候補から作る
                (エンティティ反応要求ID "preflight.path.request") 10L selected |> ok
        let live, reaction =
            適用する initial "preflight.path.reaction" (Some emission.因果操作ID)
                [ ダメージ発生(主体A, 0) ] request
        let fullLedger =
            統合因果台帳.エンティティ反応状態変更記録一覧を追記する firstLedger [ reaction ] |> ledger
        let replayed, finalSignals = 統合因果台帳再生.再生する initial fullLedger |> replay
        { Initial = initial; Signal = signal; Emission = emission; Sensing = sensing
          Belief = belief; Alert = alert; Candidate = candidate; Request = request
          Reaction = reaction; Ledger = fullLedger; Live = live; Replay = replayed
          Signals = finalSignals; Intent = intent true "preflight.path.intent" 10L 主体A 自発移動方向.右 }

    let fromPath state (p: Path) = gateFrom state 主体A p.Intent |> 自発移動互換変換.変換する

    let initial () = 状態 10L プレイヤー

    let commandFrom s actor id direction fromInput =
        intent fromInput id s.Tick actor direction
        |> gateFrom s actor
        |> 自発移動互換変換.変換する
        |> 互換移動変換結果.命令
        |> some

    let cmd id direction = commandFrom (initial ()) 主体A id direction true

    let commands () =
        [ cmd "z" 自発移動方向.右; cmd "a" 自発移動方向.右; cmd "m" 自発移動方向.左 ]

    let prepared () = 互換移動適用前検証.一括を準備する (initial ()) (commands ()) |> ok |> some

    let alertState tick actor =
        let s = 状態 tick プレイヤー
        let update, _ = 適用する s "preflight.alert" None [] (要求 "preflight.req" tick actor 0.8 "危険")
        update.状態

    let changeTarget f (s: ゲーム状態) =
        { s with エンティティ一覧 = s.エンティティ一覧 |> List.map (fun e -> if e.ID = s.プレイヤーID then f e else e) }

    let errors = function
        | Error reasons -> reasons
        | Ok _ -> failtest "Errorを期待"

    let tickError i = $"互換移動命令[{i}]: Tickがゲーム状態Tickと一致しません。"
    let actorError i = $"互換移動命令[{i}]: 実行者IDがゲーム状態のプレイヤーIDと一致しません。"
    let duplicateError = "互換移動命令一覧内で自発移動意図IDが重複しています。"
    let suppressionError = "現在の自発移動制御判断が自発移動を許可していません。"
    let endError = "終了状態のため互換移動命令を準備できません。"
    let kindError = "互換移動対象Entityの種別がプレイヤーではありません。"
    let prefix message = "現在の自発移動制御判断: " + message
