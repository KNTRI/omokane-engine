namespace Omokane.Core.Domain

type 入力 =
    | 左へ移動
    | 右へ移動
    | 上へ移動
    | 下へ移動
    | ジャンプ
    | 攻撃
    | 入力なし
