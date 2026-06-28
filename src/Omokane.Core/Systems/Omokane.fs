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

    let private プレイヤーを移動する (入力: 入力) (entity: エンティティ) =
        match entity.種別, 入力 with
        | プレイヤー, 右へ移動 ->
            { entity with 位置 = { entity.位置 with X = entity.位置.X + 1.0 } }

        | プレイヤー, 左へ移動 ->
            { entity with 位置 = { entity.位置 with X = entity.位置.X - 1.0 } }

        | _ ->
            entity

    let private 入力をEntityに適用する プレイヤーID 入力 entity =
        if entity.ID = プレイヤーID then
            プレイヤーを移動する 入力 entity
        else
            entity

    let private 入力を適用する (入力一覧: 入力 list) (状態: ゲーム状態) =
        let 入力を単体適用する 状態 入力 =
            let 更新Entity一覧 =
                状態.エンティティ一覧
                |> List.map (入力をEntityに適用する 状態.プレイヤーID 入力)

            { 状態 with エンティティ一覧 = 更新Entity一覧 }

        入力一覧
        |> List.fold 入力を単体適用する 状態

    let 更新する (入力一覧: 入力 list) (現在状態: ゲーム状態) : 更新結果 =
        match 現在状態.終了状態 with
        | Some _ ->
            Tickだけ進める 現在状態

        | None ->
            現在状態
            |> 入力を適用する 入力一覧
            |> Tickだけ進める
