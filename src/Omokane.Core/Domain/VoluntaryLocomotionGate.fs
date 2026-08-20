namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 自発移動意図制御結果 =
    private
    | 通過 of
        意図: 自発移動意図 *
        判断: 自発移動制御判断
    | 抑制 of
        意図: 自発移動意図 *
        判断: 自発移動制御判断

module 自発移動意図制御結果 =

    let 意図 = function
        | 自発移動意図制御結果.通過(意図, _)
        | 自発移動意図制御結果.抑制(意図, _) -> 意図

    let 制御判断 = function
        | 自発移動意図制御結果.通過(_, 判断)
        | 自発移動意図制御結果.抑制(_, 判断) -> 判断

    let 通過した = function
        | 自発移動意図制御結果.通過 _ -> true
        | 自発移動意図制御結果.抑制 _ -> false

    let 抑制された = function
        | 自発移動意図制御結果.通過 _ -> false
        | 自発移動意図制御結果.抑制 _ -> true

    let 通過意図 = function
        | 自発移動意図制御結果.通過(意図, _) -> Some 意図
        | 自発移動意図制御結果.抑制 _ -> None

    let 抑制意図 = function
        | 自発移動意図制御結果.通過 _ -> None
        | 自発移動意図制御結果.抑制(意図, _) -> Some 意図

    let 制御理由 結果 =
        結果
        |> 制御判断
        |> 自発移動制御判断.理由

    let 由来反応状態 結果 =
        結果
        |> 制御判断
        |> 自発移動制御判断.由来反応状態

    let 由来反応要求ID 結果 =
        結果
        |> 制御判断
        |> 自発移動制御判断.由来反応要求ID

    let 由来反応状態成立因果操作ID 結果 =
        結果
        |> 制御判断
        |> 自発移動制御判断.由来成立因果操作ID

module 自発移動意図制御 =

    let private 検証する 判断 意図 =
        [
            if 自発移動意図.実行者ID 意図 <> 自発移動制御判断.実行者ID 判断 then
                "自発移動意図の実行者IDが制御判断の実行者IDと一致しません。"

            if 自発移動意図.Tick 意図 <> 自発移動制御判断.評価Tick 判断 then
                "自発移動意図のTickが制御判断の評価Tickと一致しません。"
        ]

    let private 適用済み結果を作る 判断 意図 =
        match 自発移動制御判断.種別 判断 with
        | 自発移動制御種別.許可 ->
            自発移動意図制御結果.通過(意図, 判断)
        | 自発移動制御種別.抑制 ->
            自発移動意図制御結果.抑制(意図, 判断)

    let 適用する 判断 意図 =
        match 検証する 判断 意図 with
        | [] -> Ok(適用済み結果を作る 判断 意図)
        | エラー一覧 -> Error エラー一覧

    let 一括適用する 判断 意図一覧 =
        let 実行者IDエラー一覧 =
            意図一覧
            |> List.indexed
            |> List.choose (fun (index, 意図) ->
                if 自発移動意図.実行者ID 意図 <> 自発移動制御判断.実行者ID 判断 then
                    Some $"自発移動意図[{index}]: 自発移動意図の実行者IDが制御判断の実行者IDと一致しません。"
                else
                    None)

        let Tickエラー一覧 =
            意図一覧
            |> List.indexed
            |> List.choose (fun (index, 意図) ->
                if 自発移動意図.Tick 意図 <> 自発移動制御判断.評価Tick 判断 then
                    Some $"自発移動意図[{index}]: 自発移動意図のTickが制御判断の評価Tickと一致しません。"
                else
                    None)

        let ID重複エラー一覧 =
            let 重複あり =
                意図一覧
                |> List.countBy 自発移動意図.ID
                |> List.exists (fun (_, 件数) -> 件数 > 1)

            if 重複あり then
                [ "自発移動意図一覧内で自発移動意図IDが重複しています。" ]
            else
                []

        let エラー一覧 =
            実行者IDエラー一覧 @ Tickエラー一覧 @ ID重複エラー一覧

        if List.isEmpty エラー一覧 then
            意図一覧
            |> List.map (適用済み結果を作る 判断)
            |> Ok
        else
            Error エラー一覧
