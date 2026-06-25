namespace Omokane.Core.Domain

type 位置 =
    {
        X: float
        Y: float
    }

type 速度 =
    {
        X: float
        Y: float
    }

type 当たり判定 =
    | 矩形 of 幅: float * 高さ: float
