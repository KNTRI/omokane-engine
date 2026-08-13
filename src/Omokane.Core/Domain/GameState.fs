namespace Omokane.Core.Domain

type ゲーム状態 =
    {
        Tick: int64
        プレイヤーID: エンティティID
        エンティティ一覧: エンティティ list
        エンティティ反応状態一覧: エンティティ反応状態 list
        乱数Seed: int
        終了状態: 終了状態 option
    }

module ゲーム状態 =

    let エンティティ反応状態を探す 実行者ID (状態: ゲーム状態) =
        状態.エンティティ反応状態一覧
        |> エンティティ反応状態一覧.実行者IDで探す 実行者ID
