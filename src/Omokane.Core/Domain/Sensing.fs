namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 感知設定 =
    {
        種別: 感知種別
        対象量種別: 伝播量種別
        対象媒体: 媒体種別
        最大距離: float
        感度倍率: float
        感知閾値: float
        確信飽和値: float
    }

module 感知設定 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let 検証する (設定: 感知設定) : Result<unit, string list> =
        let 最大距離有限 = 有限である 設定.最大距離
        let 感度倍率有限 = 有限である 設定.感度倍率
        let 感知閾値有限 = 有限である 設定.感知閾値
        let 確信飽和値有限 = 有限である 設定.確信飽和値

        let エラー一覧 =
            [
                not 最大距離有限, "感知設定の最大距離が有限値ではありません。"
                最大距離有限 && 設定.最大距離 < 0.0, "感知設定の最大距離が負です。"
                not 感度倍率有限, "感知設定の感度倍率が有限値ではありません。"
                感度倍率有限 && 設定.感度倍率 < 0.0, "感知設定の感度倍率が負です。"
                not 感知閾値有限, "感知設定の感知閾値が有限値ではありません。"
                感知閾値有限 && 設定.感知閾値 < 0.0, "感知設定の感知閾値が負です。"
                not 確信飽和値有限, "感知設定の確信飽和値が有限値ではありません。"
                感知閾値有限
                && 確信飽和値有限
                && 設定.確信飽和値 <= 設定.感知閾値,
                "感知設定の確信飽和値は感知閾値より大きい必要があります。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

module 感知値生成 =

    let private 有限である 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 安定距離を計算する (第一: 位置) (第二: 位置) =
        let dx = 第一.X - 第二.X
        let dy = 第一.Y - 第二.Y

        if not (有限である dx) || not (有限である dy) then
            Error "観測位置と伝播信号の距離が有限値ではありません。"
        else
            let 最大成分 = max (abs dx) (abs dy)

            let 距離 =
                if 最大成分 = 0.0 then
                    0.0
                else
                    let 正規化X = dx / 最大成分
                    let 正規化Y = dy / 最大成分
                    最大成分 * sqrt (正規化X * 正規化X + 正規化Y * 正規化Y)

            if 有限である 距離 then
                Ok 距離
            else
                Error "観測位置と伝播信号の距離が有限値ではありません。"

    let private 結果を検証する (感知: 感知値) =
        match 観測検証.感知値を検証する 感知 with
        | Ok () -> Ok(Some 感知)
        | Error エラー一覧 -> Error エラー一覧

    let 伝播信号から作る
        (現在Tick: int64)
        (観測者ID: エンティティID)
        (観測位置: 位置)
        (設定: 感知設定)
        (信号: 伝播信号)
        : Result<感知値 option, string list> =
        let (エンティティID 観測者ID値) = 観測者ID

        let 設定エラー =
            match 感知設定.検証する 設定 with
            | Ok () -> []
            | Error エラー一覧 -> エラー一覧

        let 信号エラー =
            match 伝播信号.検証する 信号 with
            | Ok () -> []
            | Error エラー一覧 -> エラー一覧

        let 入力エラー =
            [
                if 現在Tick < 0L then
                    "感知値生成の現在Tickが負です。"

                if System.String.IsNullOrWhiteSpace 観測者ID値 then
                    "感知値生成の観測者IDが空です。"

                if not (有限である 観測位置.X) then
                    "感知値生成の観測位置.Xが有限値ではありません。"

                if not (有限である 観測位置.Y) then
                    "感知値生成の観測位置.Yが有限値ではありません。"

                yield! 設定エラー
                yield! 信号エラー
            ]

        if not (List.isEmpty 入力エラー) then
            Error 入力エラー
        elif 信号.量種別 <> 設定.対象量種別 || 信号.媒体 <> 設定.対象媒体 then
            Ok None
        else
            match 安定距離を計算する 観測位置 信号.発生位置 with
            | Error エラー ->
                Error [ エラー ]
            | Ok 距離 ->
                let 距離係数候補 =
                    if 設定.最大距離 = 0.0 then
                        if 距離 = 0.0 then Some 1.0 else None
                    elif 距離 >= 設定.最大距離 then
                        None
                    else
                        Some(max 0.0 (1.0 - 距離 / 設定.最大距離))

                match 距離係数候補 with
                | None ->
                    Ok None
                | Some 距離係数 ->
                    let 測定値 = 信号.強度 * 設定.感度倍率 * 距離係数

                    if not (有限である 測定値) then
                        Error [ "感知値の測定計算結果が有限値ではありません。" ]
                    elif 測定値 <= 設定.感知閾値 then
                        Ok None
                    else
                        let 確信度 =
                            if 測定値 >= 設定.確信飽和値 then
                                1.0
                            else
                                (測定値 - 設定.感知閾値)
                                / (設定.確信飽和値 - 設定.感知閾値)
                                |> max 0.0
                                |> min 1.0

                        let 生成値: 感知値 =
                            {
                                観測者ID = 観測者ID
                                信号ID = 信号.ID
                                種別 = 設定.種別
                                測定値 = 測定値
                                確信度 = 確信度
                                Tick = 現在Tick
                            }

                        結果を検証する 生成値
