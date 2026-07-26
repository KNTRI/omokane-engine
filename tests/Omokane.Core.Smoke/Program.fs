module Program

open Omokane.Core.Domain
open Omokane.Core.Systems
open Omokane.Core.Validation

let サンプル入力 =
    [
        入力なし
        右へ移動
    ]

let 検証 = 正常

let 攻撃権能: 権能Asset =
    {
        ID = 権能ID "attack.basic"
        名前 = "基本攻撃"
        タグ =
            Set.ofList
                [
                    攻撃系
                    対象を更新する
                    生物対象
                    敵対行動
                    AI選択可能
                    プレイヤー選択可能
                    繰り返し可能
                ]
        発行Event一覧 = []
    }

let プレイヤーX (状態: ゲーム状態) =
    状態.エンティティ一覧
    |> List.tryFind (fun entity -> entity.ID = 状態.プレイヤーID)
    |> Option.map (fun entity -> entity.位置.X)

let 権能AssetSmoke成功 () =
    let 空ID権能 = { 攻撃権能 with ID = 権能ID "" }
    let 空名前権能 = { 攻撃権能 with 名前 = "   " }
    let 空タグ権能 = { 攻撃権能 with タグ = Set.empty }

    権能Asset.タグを持つか 攻撃系 攻撃権能
    && not (権能Asset.タグを持つか 食事系 攻撃権能)
    && 権能Asset.検証する 攻撃権能 = Ok ()
    && Result.isError (権能Asset.検証する 空ID権能)
    && Result.isError (権能Asset.検証する 空名前権能)
    && Result.isError (権能Asset.検証する 空タグ権能)

let 世界知能契約Smoke成功 () =
    let プレイヤーID = エンティティID "player"

    let 正常因果操作: 因果操作 =
        {
            ID = 因果操作ID "causal.move.player.1"
            Tick = 1L
            種別 = 状態変更
            原因因果ID = None
            実行者ID = Some プレイヤーID
            対象EntityID = Some プレイヤーID
            概要 = "プレイヤー移動要求"
            発行Event一覧 = [ Tick進行 1L ]
        }

    let 空ID因果操作 =
        { 正常因果操作 with ID = 因果操作ID "" }

    let 正常伝播信号: 伝播信号 =
        {
            ID = 伝播信号ID "signal.smoke.1"
            原因因果ID = Some 正常因果操作.ID
            発生源EntityID = Some プレイヤーID
            量種別 = 煙
            媒体 = 大気
            発生位置 = { X = 0.0; Y = 0.0 }
            方向 = Some { X = 1.0; Y = 0.0 }
            強度 = 0.5
            方式 = 移流
            寿命Tick = 10L
        }

    let 負強度伝播信号 =
        { 正常伝播信号 with 強度 = -0.1 }

    let 正常感知値: 感知値 =
        {
            観測者ID = プレイヤーID
            信号ID = 正常伝播信号.ID
            種別 = 視覚
            測定値 = 0.5
            確信度 = 0.8
            Tick = 1L
        }

    let 正常観測: 観測 =
        {
            観測者ID = プレイヤーID
            概要 = "煙が東へ流れている"
            確信度 = 0.8
            根拠一覧 = [ 正常感知値 ]
        }

    let 過信観測 =
        { 正常観測 with 確信度 = 1.1 }

    let 正常信念候補: 信念候補 =
        {
            仮説 = "東側に煙の発生源がある"
            確率 = 0.7
            根拠一覧 = [ 正常観測 ]
            反証一覧 = []
        }

    let 正常散逸状態: 散逸状態 =
        {
            種別 = 痕跡風化
            値 = 0.25
        }

    let 負値散逸状態 =
        { 正常散逸状態 with 値 = -0.1 }

    因果操作.検証する 正常因果操作 = Ok ()
    && Result.isError (因果操作.検証する 空ID因果操作)
    && 伝播信号.検証する 正常伝播信号 = Ok ()
    && Result.isError (伝播信号.検証する 負強度伝播信号)
    && 観測検証.感知値を検証する 正常感知値 = Ok ()
    && 観測検証.観測を検証する 正常観測 = Ok ()
    && 観測検証.信念候補を検証する 正常信念候補 = Ok ()
    && Result.isError (観測検証.観測を検証する 過信観測)
    && 散逸状態.検証する 正常散逸状態 = Ok ()
    && Result.isError (散逸状態.検証する 負値散逸状態)

let 観測生成Smoke成功 () =
    let 観測者ID = エンティティID "player"

    let 第一感知値: 感知値 =
        {
            観測者ID = 観測者ID
            信号ID = 伝播信号ID "signal.observation.1"
            種別 = 視覚
            測定値 = 0.5
            確信度 = 0.8
            Tick = 1L
        }

    let 第二感知値: 感知値 =
        {
            第一感知値 with
                信号ID = 伝播信号ID "signal.observation.2"
                測定値 = 0.3
                確信度 = 0.6
        }

    let 感知値一覧 = [ 第一感知値; 第二感知値 ]

    let 正常変換成功 =
        match 観測生成.感知値から観測を作る "2つの煙を観測した" 感知値一覧 with
        | Ok 生成観測 ->
            生成観測.観測者ID = 観測者ID
            && 生成観測.確信度 = 0.6
            && 生成観測.根拠一覧 = 感知値一覧
        | Error _ ->
            false

    正常変換成功
    && Result.isError (観測生成.感知値から観測を作る "空の観測" [])

[<EntryPoint>]
let main _ =
    let 初期状態 = 思兼神.初期状態を作る ()
    let 入力なし結果 = 思兼神.更新する [ 入力なし ] 初期状態
    let 右移動結果 = 思兼神.更新する [ 右へ移動 ] 初期状態
    let 左移動結果 = 思兼神.更新する [ 左へ移動 ] 初期状態
    let 右二回結果 = 思兼神.更新する [ 右へ移動; 右へ移動 ] 初期状態
    let 終了済み状態 = { 初期状態 with 終了状態 = Some ゲームオーバー }
    let 終了済み更新結果 = 思兼神.更新する [ 右へ移動 ] 終了済み状態
    let 権能Asset成功 = 権能AssetSmoke成功 ()
    let 世界知能契約成功 = 世界知能契約Smoke成功 ()
    let 観測生成成功 = 観測生成Smoke成功 ()

    let Movement成功 =
        初期状態.Tick = 0L
        && 初期状態.エンティティ一覧.Length = 1
        && プレイヤーX 初期状態 = Some 0.0
        && 入力なし結果.状態.Tick = 1L
        && 入力なし結果.イベント一覧 = [ Tick進行 1L ]
        && プレイヤーX 入力なし結果.状態 = Some 0.0
        && 右移動結果.状態.Tick = 1L
        && 右移動結果.イベント一覧 = [ Tick進行 1L ]
        && プレイヤーX 右移動結果.状態 = Some 1.0
        && 左移動結果.状態.Tick = 1L
        && 左移動結果.イベント一覧 = [ Tick進行 1L ]
        && プレイヤーX 左移動結果.状態 = Some -1.0
        && 右二回結果.状態.Tick = 1L
        && 右二回結果.イベント一覧 = [ Tick進行 1L ]
        && プレイヤーX 右二回結果.状態 = Some 2.0
        && 終了済み更新結果.状態.Tick = 0L
        && プレイヤーX 終了済み更新結果.状態 = Some 0.0
        && List.isEmpty 終了済み更新結果.イベント一覧

    let 成功 =
        Movement成功
        && 権能Asset成功
        && 世界知能契約成功
        && 観測生成成功

    printfn "Inputs: %d" サンプル入力.Length
    printfn "Initial Tick: %d" 初期状態.Tick
    printfn "Input None Tick: %d" 入力なし結果.状態.Tick
    printfn "Entities: %d" 初期状態.エンティティ一覧.Length
    printfn "Input None X: %A" (プレイヤーX 入力なし結果.状態)
    printfn "Right X: %A" (プレイヤーX 右移動結果.状態)
    printfn "Left X: %A" (プレイヤーX 左移動結果.状態)
    printfn "Right Twice X: %A" (プレイヤーX 右二回結果.状態)
    printfn "Events: %d" 入力なし結果.イベント一覧.Length
    printfn "Finished Tick: %d" 終了済み更新結果.状態.Tick
    printfn "Finished X: %A" (プレイヤーX 終了済み更新結果.状態)
    printfn "Finished Events: %d" 終了済み更新結果.イベント一覧.Length
    printfn "Kengou Has Attack Tag: %b" (権能Asset.タグを持つか 攻撃系 攻撃権能)
    printfn "Kengou Has Food Tag: %b" (権能Asset.タグを持つか 食事系 攻撃権能)

    match 検証 with
    | 正常 when 成功 ->
        printfn "思兼神Core Movement Smoke OK"
        printfn "思兼神Core KengouAsset Smoke OK"
        printfn "思兼神Core WorldIntelligence Contracts Smoke OK"
        printfn "Validation: 正常"
        0
    | 正常 ->
        printfn "思兼神Core Movement Smoke FAILED"
        printfn "Movement OK: %b" Movement成功
        printfn "KengouAsset OK: %b" 権能Asset成功
        printfn "WorldIntelligence Contracts OK: %b" 世界知能契約成功
        printfn "Observation Generation OK: %b" 観測生成成功
        printfn "Input None Events: %A" 入力なし結果.イベント一覧
        printfn "Right Events: %A" 右移動結果.イベント一覧
        printfn "Left Events: %A" 左移動結果.イベント一覧
        printfn "Right Twice Events: %A" 右二回結果.イベント一覧
        printfn "Finished Events: %A" 終了済み更新結果.イベント一覧
        1
    | エラー errors ->
        printfn "Validation errors: %A" errors
        1
