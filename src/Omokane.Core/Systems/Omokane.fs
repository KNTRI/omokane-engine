namespace Omokane.Core.Systems

open Omokane.Core.Domain

module 思兼神 =

    let 初期状態を作る () : ゲーム状態 =
        let プレイヤーID = エンティティID "player"

        let プレイヤーエンティティ =
            {
                ID = プレイヤーID
                種別 = プレイヤー
                所有者 = プレイヤー側
                位置 = { X = 0.0; Y = 0.0 }
                速度 = { X = 0.0; Y = 0.0 }
                当たり判定 = Some (矩形 (16.0, 16.0))
                HP = Some (HP 10)
            }

        {
            Tick = 0L
            プレイヤーID = プレイヤーID
            エンティティ一覧 = [ プレイヤーエンティティ ]
            乱数Seed = 12345
            終了状態 = None
        }

    let Tickだけ進める (現在状態: ゲーム状態) : 更新結果 =
        match 現在状態.終了状態 with
        | Some _ ->
            {
                状態 = 現在状態
                イベント一覧 = []
            }

        | None ->
            let 次Tick = 現在状態.Tick + 1L

            let 次状態 =
                { 現在状態 with Tick = 次Tick }

            {
                状態 = 次状態
                イベント一覧 = [ Tick進行 次Tick ]
            }

    let 更新する (_入力一覧: 入力 list) (現在状態: ゲーム状態) : 更新結果 =
        Tickだけ進める 現在状態
