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

[<EntryPoint>]
let main _ =
    let 初期状態 = 思兼神.初期状態を作る ()
    let 更新結果 = 思兼神.Tickだけ進める 初期状態
    let 終了済み状態 = { 初期状態 with 終了状態 = Some ゲームオーバー }
    let 終了済み更新結果 = 思兼神.Tickだけ進める 終了済み状態

    let 成功 =
        初期状態.Tick = 0L
        && 初期状態.エンティティ一覧.Length = 1
        && 更新結果.状態.Tick = 1L
        && 更新結果.イベント一覧 = [ Tick進行 1L ]
        && 終了済み更新結果.状態.Tick = 0L
        && List.isEmpty 終了済み更新結果.イベント一覧

    printfn "Inputs: %d" サンプル入力.Length
    printfn "Initial Tick: %d" 初期状態.Tick
    printfn "Next Tick: %d" 更新結果.状態.Tick
    printfn "Entities: %d" 初期状態.エンティティ一覧.Length
    printfn "Events: %d" 更新結果.イベント一覧.Length
    printfn "Finished Tick: %d" 終了済み更新結果.状態.Tick
    printfn "Finished Events: %d" 終了済み更新結果.イベント一覧.Length

    match 検証 with
    | 正常 when 成功 ->
        printfn "思兼神Core Tick Smoke OK"
        printfn "Validation: 正常"
        0
    | 正常 ->
        printfn "思兼神Core Tick Smoke FAILED"
        printfn "Events: %A" 更新結果.イベント一覧
        printfn "Finished Events: %A" 終了済み更新結果.イベント一覧
        1
    | エラー errors ->
        printfn "Validation errors: %A" errors
        1
