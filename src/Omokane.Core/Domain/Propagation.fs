namespace Omokane.Core.Domain

type 伝播信号ID =
    | 伝播信号ID of string

type 伝播量種別 =
    | 熱
    | 煙
    | 匂い
    | 地盤振動
    | 権能残滓

type 媒体種別 =
    | 大気
    | 水流
    | 地盤

type 伝播方式 =
    | 移流
    | 拡散
    | 伝導
    | 波動
    | 流動

[<RequireQualifiedAccess>]
type 伝播信号 =
    {
        ID: 伝播信号ID
        原因因果ID: 因果操作ID option
        発生源EntityID: エンティティID option
        量種別: 伝播量種別
        媒体: 媒体種別
        発生位置: 位置
        方向: 速度 option
        強度: float
        方式: 伝播方式
        寿命Tick: int64
    }

module 伝播信号 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let 検証する (信号: 伝播信号) : Result<unit, string list> =
        let (伝播信号ID id) = 信号.ID

        let エラー一覧 =
            [
                System.String.IsNullOrWhiteSpace id, "伝播信号IDが空です。"
                not (有限である 信号.強度), "伝播信号の強度が有限値ではありません。"
                信号.強度 < 0.0, "伝播信号の強度が負です。"
                信号.寿命Tick < 0L, "伝播信号の寿命Tickが負です。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧
