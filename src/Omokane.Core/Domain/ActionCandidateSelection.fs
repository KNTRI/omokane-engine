namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 行動候補選択理由 =
    | 単独最高優先度
    | 同率最高入力順

type 選択済み行動候補 =
    private
    | 選択済み行動候補 of
        実行者ID: エンティティID *
        候補一覧: 行動候補 list *
        選択位置: int *
        選択候補: 行動候補

module 選択済み行動候補 =

    let 実行者ID (選択済み行動候補(実行者ID, _, _, _)) =
        実行者ID

    let 候補一覧 (選択済み行動候補(_, 候補一覧, _, _)) =
        候補一覧

    let 候補数 選択済み =
        選択済み
        |> 候補一覧
        |> List.length

    let 選択位置 (選択済み行動候補(_, _, 選択位置, _)) =
        選択位置

    let 選択候補 (選択済み行動候補(_, _, _, 選択候補)) =
        選択候補

    let 却下候補一覧 選択済み =
        let 選択位置 = 選択位置 選択済み

        選択済み
        |> 候補一覧
        |> List.mapi (fun index 候補 -> index, 候補)
        |> List.choose (fun (index, 候補) ->
            if index = 選択位置 then None else Some 候補)

    let 同率最高候補一覧 選択済み =
        let 最高優先度 = (選択候補 選択済み).優先度

        選択済み
        |> 候補一覧
        |> List.filter (fun 候補 -> 候補.優先度 = 最高優先度)

    let 選択理由 選択済み =
        if 選択済み |> 同率最高候補一覧 |> List.length = 1 then
            行動候補選択理由.単独最高優先度
        else
            行動候補選択理由.同率最高入力順

module 行動候補選択 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let private 候補エラーを収集する (候補一覧: 行動候補 list) =
        候補一覧
        |> List.mapi (fun index 候補 ->
            候補
            |> 行動候補.検証する
            |> 結果エラーを得る
            |> List.map (fun エラー -> $"行動候補[{index}]: {エラー}"))
        |> List.concat

    let private ID出現回数を作る (候補一覧: 行動候補 list) =
        候補一覧
        |> List.fold
            (fun 出現回数 候補 ->
                出現回数
                |> Map.change 候補.ID (fun 現在回数 ->
                    現在回数
                    |> Option.defaultValue 0
                    |> fun 回数 -> Some(回数 + 1)))
            Map.empty

    let private 最高候補を選ぶ (先頭: 行動候補) (残り: 行動候補 list) =
        残り
        |> List.indexed
        |> List.fold
            (fun (現在位置, 現在候補: 行動候補) (残り位置, 候補: 行動候補) ->
                if 候補.優先度 > 現在候補.優先度 then
                    残り位置 + 1, 候補
                else
                    現在位置, 現在候補)
            (0, 先頭)

    let 選ぶ (候補一覧: 行動候補 list) : Result<選択済み行動候補 option, string list> =
        match 候補一覧 with
        | [] ->
            Ok None
        | 先頭 :: 残り ->
            let ID出現回数 = ID出現回数を作る 候補一覧

            let エラー一覧 =
                [
                    yield! 候補エラーを収集する 候補一覧

                    if 残り |> List.exists (fun 候補 -> 候補.実行者ID <> 先頭.実行者ID) then
                        "行動候補選択に使用する候補の実行者IDが一致しません。"

                    if ID出現回数 |> Map.exists (fun _ 回数 -> 回数 > 1) then
                        "行動候補選択に使用する行動候補IDが重複しています。"
                ]

            if not (List.isEmpty エラー一覧) then
                Error エラー一覧
            else
                let 選択位置, 選択候補 = 最高候補を選ぶ 先頭 残り

                Ok(
                    Some(
                        選択済み行動候補(
                            先頭.実行者ID,
                            候補一覧,
                            選択位置,
                            選択候補
                        )
                    )
                )
