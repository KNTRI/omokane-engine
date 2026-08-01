namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 地盤振動警戒入力 =
    {
        現在Tick: int64
        観測者ID: エンティティID
        観測位置: 位置
        感知設定: 感知設定
        信号: 伝播信号
        観測概要: string
        仮説: string
        事前確率: float
        警戒設定: 警戒意図設定
    }

type 地盤振動警戒結果 =
    | 非感知
    | 警戒不成立 of 感知: 感知値 * 観測: 観測 * 信念候補: 信念候補
    | 警戒成立 of 感知: 感知値 * 観測: 観測 * 信念候補: 信念候補 * 警戒意図: 警戒意図

module 地盤振動警戒入力 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let 検証する (入力: 地盤振動警戒入力) : Result<unit, string list> =
        let (エンティティID 観測者ID値) = 入力.観測者ID

        let 感知設定エラー =
            感知設定.検証する 入力.感知設定
            |> 結果エラーを得る

        let 伝播信号エラー =
            伝播信号.検証する 入力.信号
            |> 結果エラーを得る

        let 警戒設定エラー =
            警戒意図設定.検証する 入力.警戒設定
            |> 結果エラーを得る

        let エラー一覧 =
            [
                if 入力.現在Tick < 0L then
                    "地盤振動警戒入力の現在Tickが負です。"

                if System.String.IsNullOrWhiteSpace 観測者ID値 then
                    "地盤振動警戒入力の観測者IDが空です。"

                if not (有限である 入力.観測位置.X) then
                    "地盤振動警戒入力の観測位置.Xが有限値ではありません。"

                if not (有限である 入力.観測位置.Y) then
                    "地盤振動警戒入力の観測位置.Yが有限値ではありません。"

                yield! 感知設定エラー
                yield! 伝播信号エラー

                if 入力.感知設定.種別 <> 地盤感知 then
                    "地盤振動警戒入力の感知種別が地盤感知ではありません。"

                if 入力.感知設定.対象量種別 <> 地盤振動 then
                    "地盤振動警戒入力の対象量種別が地盤振動ではありません。"

                if 入力.感知設定.対象媒体 <> 地盤 then
                    "地盤振動警戒入力の対象媒体が地盤ではありません。"

                if 入力.信号.量種別 <> 地盤振動 then
                    "地盤振動警戒入力の信号量種別が地盤振動ではありません。"

                if 入力.信号.媒体 <> 地盤 then
                    "地盤振動警戒入力の信号媒体が地盤ではありません。"

                if System.String.IsNullOrWhiteSpace 入力.観測概要 then
                    "地盤振動警戒入力の観測概要が空です。"

                if System.String.IsNullOrWhiteSpace 入力.仮説 then
                    "地盤振動警戒入力の仮説が空です。"

                if not (有限である 入力.事前確率) then
                    "地盤振動警戒入力の事前確率が有限値ではありません。"

                if 入力.事前確率 < 0.0 || 入力.事前確率 > 1.0 then
                    "地盤振動警戒入力の事前確率は0.0以上1.0以下である必要があります。"

                yield! 警戒設定エラー
            ]

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

module 地盤振動警戒経路 =

    let private 段階エラーを付ける 段階名 エラー一覧 =
        エラー一覧
        |> List.map (fun エラー -> 段階名 + ": " + エラー)

    let 実行する (入力: 地盤振動警戒入力) : Result<地盤振動警戒結果, string list> =
        match 地盤振動警戒入力.検証する 入力 with
        | Error エラー一覧 ->
            Error エラー一覧
        | Ok () ->
            match
                感知値生成.伝播信号から作る
                    入力.現在Tick
                    入力.観測者ID
                    入力.観測位置
                    入力.感知設定
                    入力.信号
            with
            | Error エラー一覧 ->
                Error(段階エラーを付ける "感知段階" エラー一覧)
            | Ok None ->
                Ok 非感知
            | Ok(Some 感知) ->
                match 観測生成.感知値から観測を作る 入力.観測概要 [ 感知 ] with
                | Error エラー一覧 ->
                    Error(段階エラーを付ける "観測段階" エラー一覧)
                | Ok 観測 ->
                    match 信念候補生成.観測から作る 入力.仮説 入力.事前確率 [ 観測 ] [] with
                    | Error エラー一覧 ->
                        Error(段階エラーを付ける "信念候補段階" エラー一覧)
                    | Ok 信念候補 ->
                        match 警戒意図生成.信念候補から作る 入力.警戒設定 信念候補 with
                        | Error エラー一覧 ->
                            Error(段階エラーを付ける "警戒意図段階" エラー一覧)
                        | Ok None ->
                            Ok(警戒不成立(感知, 観測, 信念候補))
                        | Ok(Some 警戒意図) ->
                            Ok(警戒成立(感知, 観測, 信念候補, 警戒意図))
