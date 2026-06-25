namespace Omokane.Core.Domain

type 終了状態 =
    | ゲームオーバー
    | クリア

type ゲーム状態 =
    {
        Tick: int64
        プレイヤーID: エンティティID
        エンティティ一覧: エンティティ list
        乱数Seed: int
        終了状態: 終了状態 option
    }
