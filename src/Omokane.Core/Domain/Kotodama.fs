namespace Omokane.Core.Domain

type ゲームイベント =
    | Tick進行 of Tick: int64
    | エンティティ生成 of ID: エンティティID
    | エンティティ削除 of ID: エンティティID
    | 衝突発生 of A: エンティティID * B: エンティティID
    | ダメージ発生 of 対象: エンティティID * 量: int
    | ゲーム終了 of 終了状態
