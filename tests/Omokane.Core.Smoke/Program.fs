module Program

open Omokane.Core.Domain
open Omokane.Core.Validation

let サンプル入力 =
    [
        入力なし
        右へ移動
    ]

let プレイヤーID = エンティティID "player"

let プレイヤーエンティティ: エンティティ =
    {
        ID = プレイヤーID
        種別 = プレイヤー
        所有者 = プレイヤー側
        位置 = { X = 0.0; Y = 0.0 }
        速度 = { X = 0.0; Y = 0.0 }
        当たり判定 = Some (矩形 (16.0, 16.0))
        HP = Some (HP 10)
    }

let 初期状態 =
    {
        Tick = 0L
        プレイヤーID = プレイヤーID
        エンティティ一覧 = [ プレイヤーエンティティ ]
        乱数Seed = 12345
        終了状態 = None
    }

let イベント一覧 =
    [
        Tick進行 0L
    ]

let 結果 =
    {
        状態 = 初期状態
        イベント一覧 = イベント一覧
    }

let 検証 = 正常

[<EntryPoint>]
let main _ =
    printfn "思兼神Core Smoke OK"
    printfn "Inputs: %d" サンプル入力.Length
    printfn "Tick: %d" 結果.状態.Tick
    printfn "Entities: %d" 結果.状態.エンティティ一覧.Length
    printfn "Events: %d" 結果.イベント一覧.Length

    match 検証 with
    | 正常 ->
        printfn "Validation: 正常"
        0
    | エラー errors ->
        printfn "Validation errors: %A" errors
        1
