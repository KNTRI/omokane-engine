namespace Omokane.Core.Domain

module 信念候補生成 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 観測エラーを収集する 接頭辞 (観測一覧: 観測 list) =
        観測一覧
        |> List.mapi (fun index 対象観測 ->
            match 観測検証.観測を検証する 対象観測 with
            | Ok () ->
                []
            | Error エラー一覧 ->
                エラー一覧
                |> List.map (fun エラー -> sprintf "%s[%d]: %s" 接頭辞 index エラー))
        |> List.concat

    let private 観測者不一致エラー (全観測: 観測 list) =
        match 全観測 with
        | [] ->
            []
        | 基準 :: 残り ->
            if 残り |> List.exists (fun 対象観測 -> 対象観測.観測者ID <> 基準.観測者ID) then
                [ "信念候補生成に使用する観測の観測者IDが一致しません。" ]
            else
                []

    let 観測から作る
        (仮説: string)
        (事前確率: float)
        (根拠一覧: 観測 list)
        (反証一覧: 観測 list)
        : Result<信念候補, string list> =
        let 全観測 = 根拠一覧 @ 反証一覧

        let 入力エラー =
            [
                if System.String.IsNullOrWhiteSpace 仮説 then
                    "信念候補生成の仮説が空です。"

                if not (有限である 事前確率) then
                    "信念候補生成の事前確率が有限値ではありません。"

                if 事前確率 < 0.0 || 事前確率 > 1.0 then
                    "信念候補生成の事前確率は0.0以上1.0以下である必要があります。"

                if List.isEmpty 全観測 then
                    "信念候補生成に使用する観測一覧が空です。"

                yield! 観測エラーを収集する "根拠観測" 根拠一覧
                yield! 観測エラーを収集する "反証観測" 反証一覧
                yield! 観測者不一致エラー 全観測
            ]

        if not (List.isEmpty 入力エラー) then
            Error 入力エラー
        else
            let 根拠総量 = 根拠一覧 |> List.sumBy (fun 対象観測 -> 対象観測.確信度)
            let 反証総量 = 反証一覧 |> List.sumBy (fun 対象観測 -> 対象観測.確信度)

            let 計算確率 =
                (事前確率 + 根拠総量)
                / (1.0 + 根拠総量 + 反証総量)

            if not (有限である 計算確率) then
                Error [ "信念候補の確率計算結果が有限値ではありません。" ]
            else
                let 対象候補: 信念候補 =
                    {
                        仮説 = 仮説
                        確率 = 計算確率 |> max 0.0 |> min 1.0
                        根拠一覧 = 根拠一覧
                        反証一覧 = 反証一覧
                    }

                match 観測検証.信念候補を検証する 対象候補 with
                | Ok () -> Ok 対象候補
                | Error エラー一覧 -> Error エラー一覧
