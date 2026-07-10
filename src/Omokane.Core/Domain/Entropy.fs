namespace Omokane.Core.Domain

type 散逸種別 =
    | 熱散逸
    | 物質拡散
    | 構造劣化
    | 痕跡風化
    | 情報不確実性
    | 常態維持負荷

[<RequireQualifiedAccess>]
type 散逸状態 =
    {
        種別: 散逸種別
        値: float
    }

module 散逸状態 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let 検証する (状態: 散逸状態) : Result<unit, string list> =
        let エラー一覧 =
            [
                not (有限である 状態.値), "散逸状態の値が有限値ではありません。"
                状態.値 < 0.0, "散逸状態の値が負です。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧
