namespace Omokane.Core.Domain

type 因果台帳記録 =
    {
        因果操作ID: 因果操作ID
        Tick: int64
        実行者ID: エンティティID option
        対象EntityID: エンティティID
        概要: string
        変更前位置: 位置
        変更後位置: 位置
        発行Event一覧: ゲームイベント list
    }

type 因果台帳 =
    private
    | 因果台帳 of 因果台帳記録 list

type 因果台帳追記結果 =
    | 追記成功 of
        台帳: 因果台帳
    | 追記失敗 of
        台帳: 因果台帳 *
        失敗位置: int *
        失敗操作ID: 因果操作ID *
        理由一覧: string list

module 因果台帳 =

    let 空 = 因果台帳 []

    let 記録一覧 (因果台帳 記録一覧) =
        記録一覧

    let 件数 台帳 =
        台帳
        |> 記録一覧
        |> List.length

    let private 有限値か 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 因果操作ID出現回数を作る (候補一覧: 因果記録候補 list) =
        候補一覧
        |> List.fold
            (fun 出現回数 候補 ->
                出現回数
                |> Map.change 候補.因果操作ID (fun 現在回数 ->
                    現在回数
                    |> Option.defaultValue 0
                    |> fun 回数 -> Some(回数 + 1)))
            Map.empty

    let private エラーを集める
        (既存ID一覧: Set<因果操作ID>)
        (候補ID出現回数: Map<因果操作ID, int>)
        (候補: 因果記録候補)
        =
        let (因果操作ID id) = 候補.因果操作ID

        [
            not 候補.成功, "因果記録候補が成功記録ではありません。"
            not (List.isEmpty 候補.失敗理由一覧), "成功した因果記録候補に失敗理由があります。"
            System.String.IsNullOrWhiteSpace id, "因果操作IDが空です。"
            候補.Tick < 0L, "因果操作のTickが負です。"
            System.String.IsNullOrWhiteSpace 候補.概要, "因果操作の概要が空です。"
            Option.isNone 候補.対象EntityID, "対象EntityIDが指定されていません。"
            Option.isNone 候補.変更前位置, "変更前位置が指定されていません。"
            Option.isNone 候補.変更後位置, "変更後位置が指定されていません。"
            (match 候補.変更前位置 with
             | Some 位置 -> not (有限値か 位置.X)
             | None -> false),
            "変更前位置.Xが有限値ではありません。"
            (match 候補.変更前位置 with
             | Some 位置 -> not (有限値か 位置.Y)
             | None -> false),
            "変更前位置.Yが有限値ではありません。"
            (match 候補.変更後位置 with
             | Some 位置 -> not (有限値か 位置.X)
             | None -> false),
            "変更後位置.Xが有限値ではありません。"
            (match 候補.変更後位置 with
             | Some 位置 -> not (有限値か 位置.Y)
             | None -> false),
            "変更後位置.Yが有限値ではありません。"
            Set.contains 候補.因果操作ID 既存ID一覧,
            "因果台帳に同じ因果操作IDが既に存在します。"
            (候補ID出現回数 |> Map.find 候補.因果操作ID) > 1,
            "因果記録候補一覧内で因果操作IDが重複しています。"
        ]
        |> List.choose (fun (不正, メッセージ) ->
            if 不正 then Some メッセージ else None)

    let private 正式記録へ変換する (候補: 因果記録候補) =
        {
            因果操作ID = 候補.因果操作ID
            Tick = 候補.Tick
            実行者ID = 候補.実行者ID
            対象EntityID = Option.get 候補.対象EntityID
            概要 = 候補.概要
            変更前位置 = Option.get 候補.変更前位置
            変更後位置 = Option.get 候補.変更後位置
            発行Event一覧 = 候補.発行Event一覧
        }

    let 追記する
        (現在台帳: 因果台帳)
        (候補一覧: 因果記録候補 list)
        : 因果台帳追記結果 =
        let 既存記録一覧 = 記録一覧 現在台帳

        let 既存ID一覧 =
            既存記録一覧
            |> List.map (fun 記録 -> 記録.因果操作ID)
            |> Set.ofList

        let 候補ID出現回数 = 因果操作ID出現回数を作る 候補一覧

        let 検証結果 =
            候補一覧
            |> List.mapi (fun index 候補 ->
                index,
                候補,
                エラーを集める 既存ID一覧 候補ID出現回数 候補)

        match 検証結果 |> List.tryFind (fun (_, _, 理由一覧) -> not (List.isEmpty 理由一覧)) with
        | Some(index, 候補, 理由一覧) ->
            追記失敗(
                現在台帳,
                index,
                候補.因果操作ID,
                理由一覧
            )
        | None ->
            let 追記記録一覧 =
                候補一覧
                |> List.map 正式記録へ変換する

            追記成功(因果台帳(既存記録一覧 @ 追記記録一覧))
