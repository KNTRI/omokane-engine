namespace Omokane.Core.Domain

type エンティティID = エンティティID of string

type エンティティ種別 =
    | プレイヤー
    | 敵
    | 弾
    | 障害物

type 所有者 =
    | プレイヤー側
    | 敵側
    | 中立

type HP = HP of int

type エンティティ =
    {
        ID: エンティティID
        種別: エンティティ種別
        所有者: 所有者
        位置: 位置
        速度: 速度
        当たり判定: 当たり判定 option
        HP: HP option
    }
