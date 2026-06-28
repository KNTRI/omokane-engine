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

let プレイヤーX (状態: ゲーム状態) =
    状態.エンティティ一覧
    |> List.tryFind (fun entity -> entity.ID = 状態.プレイヤーID)
    |> Option.map (fun entity -> entity.位置.X)

[<EntryPoint>]
let main _ =
    let 初期状態 = 思兼神.初期状態を作る ()
    let 入力なし結果 = 思兼神.更新する [ 入力なし ] 初期状態
    let 右移動結果 = 思兼神.更新する [ 右へ移動 ] 初期状態
    let 左移動結果 = 思兼神.更新する [ 左へ移動 ] 初期状態
    let 右二回結果 = 思兼神.更新する [ 右へ移動; 右へ移動 ] 初期状態
    let 終了済み状態 = { 初期状態 with 終了状態 = Some ゲームオーバー }
    let 終了済み更新結果 = 思兼神.更新する [ 右へ移動 ] 終了済み状態

    let 成功 =
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

    match 検証 with
    | 正常 when 成功 ->
        printfn "思兼神Core Movement Smoke OK"
        printfn "Validation: 正常"
        0
    | 正常 ->
        printfn "思兼神Core Movement Smoke FAILED"
        printfn "Input None Events: %A" 入力なし結果.イベント一覧
        printfn "Right Events: %A" 右移動結果.イベント一覧
        printfn "Left Events: %A" 左移動結果.イベント一覧
        printfn "Right Twice Events: %A" 右二回結果.イベント一覧
        printfn "Finished Events: %A" 終了済み更新結果.イベント一覧
        1
    | エラー errors ->
        printfn "Validation errors: %A" errors
        1
