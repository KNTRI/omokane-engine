namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 警戒意図設定 =
    {
        発生閾値: float
    }

[<RequireQualifiedAccess>]
type 警戒意図 =
    {
        観測者ID: エンティティID
        対象仮説: string
        警戒度: float
        由来信念候補: 信念候補
    }

module 警戒意図設定 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let 検証する (設定: 警戒意図設定) : Result<unit, string list> =
        let エラー一覧 =
            [
                not (有限である 設定.発生閾値), "警戒意図設定の発生閾値が有限値ではありません。"
                (設定.発生閾値 < 0.0 || 設定.発生閾値 > 1.0),
                "警戒意図設定の発生閾値は0.0以上1.0以下である必要があります。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

module 警戒意図生成 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let private 観測エラーを収集する 接頭辞 (観測一覧: 観測 list) =
        観測一覧
        |> List.mapi (fun index 対象観測 ->
            観測検証.観測を検証する 対象観測
            |> 結果エラーを得る
            |> List.map (fun エラー -> sprintf "%s[%d]: %s" 接頭辞 index エラー))
        |> List.concat

    let private 観測者エラーを収集する (全観測: 観測 list) =
        match 全観測 with
        | [] ->
            []
        | 基準 :: 残り ->
            let 空白IDを含む =
                全観測
                |> List.exists (fun 対象観測 ->
                    let (エンティティID ID) = 対象観測.観測者ID
                    System.String.IsNullOrWhiteSpace ID)

            [
                if 空白IDを含む then
                    "警戒意図生成に使用する観測者IDが空です。"

                if 残り |> List.exists (fun 対象観測 -> 対象観測.観測者ID <> 基準.観測者ID) then
                    "警戒意図生成に使用する観測の観測者IDが一致しません。"
            ]

    let 信念候補から作る
        (設定: 警戒意図設定)
        (候補: 信念候補)
        : Result<警戒意図 option, string list> =
        let 全観測 = 候補.根拠一覧 @ 候補.反証一覧

        let 設定エラー =
            警戒意図設定.検証する 設定
            |> 結果エラーを得る

        let 候補エラー =
            観測検証.信念候補を検証する 候補
            |> 結果エラーを得る
            |> List.map (fun エラー -> "信念候補: " + エラー)

        let 入力エラー =
            [
                yield! 設定エラー
                yield! 候補エラー

                if List.isEmpty 全観測 then
                    "警戒意図生成に使用する観測一覧が空です。"

                yield! 観測エラーを収集する "根拠観測" 候補.根拠一覧
                yield! 観測エラーを収集する "反証観測" 候補.反証一覧
                yield! 観測者エラーを収集する 全観測
            ]

        if not (List.isEmpty 入力エラー) then
            Error 入力エラー
        elif 候補.確率 < 設定.発生閾値 then
            Ok None
        else
            match 全観測 with
            | 基準 :: _ ->
                let 意図: 警戒意図 =
                    {
                        観測者ID = 基準.観測者ID
                        対象仮説 = 候補.仮説
                        警戒度 = 候補.確率
                        由来信念候補 = 候補
                    }

                Ok(Some 意図)
            | [] ->
                Error [ "警戒意図生成に使用する観測一覧が空です。" ]
