namespace Omokane.Core.Domain

type エンティティ反応状態 =
    private
    | エンティティ反応状態 of
        成立因果操作ID: 因果操作ID *
        由来要求: エンティティ反応要求

module エンティティ反応状態 =

    let 成立因果操作ID (エンティティ反応状態(成立因果操作ID, _)) =
        成立因果操作ID

    let 由来要求 (エンティティ反応状態(_, 由来要求)) =
        由来要求

    let 成立Tick 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.Tick

    let 実行者ID 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.実行者ID

    let 種別 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.種別

    let 優先度 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.優先度

    let 対象仮説 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.対象仮説

    let 由来要求ID 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.ID

    let 由来行動候補 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.由来行動候補

    let 由来行動候補ID 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.由来行動候補ID

    let 由来選択位置 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.由来選択位置

    let 由来選択理由 状態 =
        状態
        |> 由来要求
        |> エンティティ反応要求.由来選択理由

    let internal 作る 成立因果操作ID 由来要求 =
        エンティティ反応状態(成立因果操作ID, 由来要求)

    let internal 置換または追加する 新状態 状態一覧 =
        let 対象ID = 実行者ID 新状態

        if 状態一覧 |> List.exists (fun 状態 -> 実行者ID 状態 = 対象ID) then
            状態一覧
            |> List.map (fun 状態 ->
                if 実行者ID 状態 = 対象ID then 新状態 else 状態)
        else
            状態一覧 @ [ 新状態 ]

module エンティティ反応状態一覧 =

    let 検証する (状態一覧: エンティティ反応状態 list) : Result<unit, string list> =
        let 重複あり =
            状態一覧
            |> List.countBy エンティティ反応状態.実行者ID
            |> List.exists (fun (_, 件数) -> 件数 > 1)

        if 重複あり then
            Error [ "エンティティ反応状態一覧内で実行者IDが重複しています。" ]
        else
            Ok ()

    let 実行者IDで探す
        (実行者ID: エンティティID)
        (状態一覧: エンティティ反応状態 list)
        : Result<エンティティ反応状態 option, string list> =
        match 状態一覧 |> List.filter (fun 状態 -> エンティティ反応状態.実行者ID 状態 = 実行者ID) with
        | [] -> Ok None
        | [ 状態 ] -> Ok(Some 状態)
        | _ -> Error [ "エンティティ反応状態一覧内で実行者IDが重複しています。" ]
