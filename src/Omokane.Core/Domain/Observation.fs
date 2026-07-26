namespace Omokane.Core.Domain

type 感知種別 =
    | 視覚
    | 聴覚
    | 嗅覚
    | 地盤感知
    | 権能感知

[<RequireQualifiedAccess>]
type 感知値 =
    {
        観測者ID: エンティティID
        信号ID: 伝播信号ID
        種別: 感知種別
        測定値: float
        確信度: float
        Tick: int64
    }

[<RequireQualifiedAccess>]
type 観測 =
    {
        観測者ID: エンティティID
        概要: string
        確信度: float
        根拠一覧: 感知値 list
    }

[<RequireQualifiedAccess>]
type 信念候補 =
    {
        仮説: string
        確率: float
        根拠一覧: 観測 list
        反証一覧: 観測 list
    }

module 観測検証 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 単位区間外 値 =
        値 < 0.0 || 値 > 1.0

    let 感知値を検証する (感知: 感知値) : Result<unit, string list> =
        let エラー一覧 =
            [
                not (有限である 感知.測定値), "感知値の測定値が有限値ではありません。"
                not (有限である 感知.確信度), "感知値の確信度が有限値ではありません。"
                単位区間外 感知.確信度, "感知値の確信度は0.0以上1.0以下である必要があります。"
                感知.Tick < 0L, "感知値のTickが負です。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

    let 観測を検証する (対象観測: 観測) : Result<unit, string list> =
        let エラー一覧 =
            [
                System.String.IsNullOrWhiteSpace 対象観測.概要, "観測の概要が空です。"
                not (有限である 対象観測.確信度), "観測の確信度が有限値ではありません。"
                単位区間外 対象観測.確信度, "観測の確信度は0.0以上1.0以下である必要があります。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

    let 信念候補を検証する (候補: 信念候補) : Result<unit, string list> =
        let エラー一覧 =
            [
                System.String.IsNullOrWhiteSpace 候補.仮説, "信念候補の仮説が空です。"
                not (有限である 候補.確率), "信念候補の確率が有限値ではありません。"
                単位区間外 候補.確率, "信念候補の確率は0.0以上1.0以下である必要があります。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

module 観測生成 =

    let private 感知値のエラーを収集する (感知値一覧: 感知値 list) =
        感知値一覧
        |> List.mapi (fun index 感知 ->
            match 観測検証.感知値を検証する 感知 with
            | Ok () ->
                []
            | Error エラー一覧 ->
                エラー一覧
                |> List.map (fun エラー -> sprintf "感知値[%d]: %s" index エラー))
        |> List.concat

    let private 一致エラーを収集する (感知値一覧: 感知値 list) =
        match 感知値一覧 with
        | [] ->
            []
        | 基準 :: 残り ->
            [
                残り
                |> List.exists (fun 感知 -> 感知.観測者ID <> 基準.観測者ID),
                "感知値一覧の観測者IDが一致しません。"

                残り
                |> List.exists (fun 感知 -> 感知.Tick <> 基準.Tick),
                "感知値一覧のTickが一致しません。"

                残り
                |> List.exists (fun 感知 -> 感知.種別 <> 基準.種別),
                "感知値一覧の感知種別が一致しません。"
            ]
            |> List.choose (fun (不一致, メッセージ) ->
                if 不一致 then Some メッセージ else None)

    let 感知値から観測を作る
        (概要: string)
        (感知値一覧: 感知値 list)
        : Result<観測, string list> =
        let 概要エラー =
            if System.String.IsNullOrWhiteSpace 概要 then
                [ "観測の概要が空です。" ]
            else
                []

        let 空一覧エラー =
            if List.isEmpty 感知値一覧 then
                [ "感知値一覧が空です。" ]
            else
                []

        let エラー一覧 =
            [
                概要エラー
                空一覧エラー
                感知値のエラーを収集する 感知値一覧
                一致エラーを収集する 感知値一覧
            ]
            |> List.concat

        match エラー一覧, 感知値一覧 with
        | _ :: _, _ ->
            Error エラー一覧
        | [], 基準 :: _ ->
            let 対象観測: 観測 =
                {
                    観測者ID = 基準.観測者ID
                    概要 = 概要
                    確信度 =
                        感知値一覧
                        |> List.map (fun 感知 -> 感知.確信度)
                        |> List.min
                    根拠一覧 = 感知値一覧
                }

            match 観測検証.観測を検証する 対象観測 with
            | Ok () -> Ok 対象観測
            | Error 構築後エラー一覧 -> Error 構築後エラー一覧
        | [], [] ->
            Error [ "感知値一覧が空です。" ]
