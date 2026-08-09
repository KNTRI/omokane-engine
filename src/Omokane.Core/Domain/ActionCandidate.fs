namespace Omokane.Core.Domain

type 行動候補ID =
    | 行動候補ID of string

[<RequireQualifiedAccess>]
type 行動候補種別 =
    | その場警戒

[<RequireQualifiedAccess>]
type 行動候補 =
    {
        ID: 行動候補ID
        実行者ID: エンティティID
        種別: 行動候補種別
        優先度: float
        対象仮説: string
        由来警戒意図: 警戒意図
    }

module 行動候補 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let 検証する (候補: 行動候補) : Result<unit, string list> =
        let (行動候補ID 候補ID値) = 候補.ID
        let (エンティティID 実行者ID値) = 候補.実行者ID

        let 由来意図エラー =
            警戒意図.検証する 候補.由来警戒意図
            |> 結果エラーを得る
            |> List.map (fun エラー -> "由来警戒意図: " + エラー)

        let エラー一覧 =
            [
                if System.String.IsNullOrWhiteSpace 候補ID値 then
                    "行動候補IDが空です。"

                if System.String.IsNullOrWhiteSpace 実行者ID値 then
                    "行動候補の実行者IDが空です。"

                if not (有限である 候補.優先度) then
                    "行動候補の優先度が有限値ではありません。"

                if 候補.優先度 < 0.0 || 候補.優先度 > 1.0 then
                    "行動候補の優先度は0.0以上1.0以下である必要があります。"

                if System.String.IsNullOrWhiteSpace 候補.対象仮説 then
                    "行動候補の対象仮説が空です。"

                yield! 由来意図エラー

                if 候補.実行者ID <> 候補.由来警戒意図.観測者ID then
                    "行動候補の実行者IDが由来警戒意図の観測者IDと一致しません。"

                if 候補.対象仮説 <> 候補.由来警戒意図.対象仮説 then
                    "行動候補の対象仮説が由来警戒意図の対象仮説と一致しません。"

                if 候補.優先度 <> 候補.由来警戒意図.警戒度 then
                    "行動候補の優先度が由来警戒意図の警戒度と一致しません。"
            ]

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

module 行動候補生成 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let 警戒意図から作る
        (候補ID: 行動候補ID)
        (意図: 警戒意図)
        : Result<行動候補, string list> =
        let (行動候補ID 候補ID値) = 候補ID

        let 意図エラー =
            警戒意図.検証する 意図
            |> 結果エラーを得る
            |> List.map (fun エラー -> "警戒意図: " + エラー)

        let 入力エラー =
            [
                if System.String.IsNullOrWhiteSpace 候補ID値 then
                    "行動候補IDが空です。"

                yield! 意図エラー
            ]

        if not (List.isEmpty 入力エラー) then
            Error 入力エラー
        else
            let 候補: 行動候補 =
                {
                    ID = 候補ID
                    実行者ID = 意図.観測者ID
                    種別 = 行動候補種別.その場警戒
                    優先度 = 意図.警戒度
                    対象仮説 = 意図.対象仮説
                    由来警戒意図 = 意図
                }

            match 行動候補.検証する 候補 with
            | Ok () -> Ok 候補
            | Error エラー一覧 -> Error エラー一覧
