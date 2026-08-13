namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 統合因果記録種別 =
    | Entity位置変更
    | 地盤振動発生
    | エンティティ反応状態変更

[<RequireQualifiedAccess>]
type 統合因果記録 =
    private
    | Entity位置変更記録 of 因果台帳記録
    | 地盤振動記録 of 地盤振動発生記録
    | エンティティ反応状態変更記録 of エンティティ反応状態変更記録

module 統合因果記録 =

    let 種別 = function
        | 統合因果記録.Entity位置変更記録 _ -> 統合因果記録種別.Entity位置変更
        | 統合因果記録.地盤振動記録 _ -> 統合因果記録種別.地盤振動発生
        | 統合因果記録.エンティティ反応状態変更記録 _ ->
            統合因果記録種別.エンティティ反応状態変更

    let 因果操作ID = function
        | 統合因果記録.Entity位置変更記録 記録 -> 記録.因果操作ID
        | 統合因果記録.地盤振動記録 記録 -> 記録.因果操作ID
        | 統合因果記録.エンティティ反応状態変更記録 記録 -> 記録.因果操作ID

    let Tick = function
        | 統合因果記録.Entity位置変更記録 記録 -> 記録.Tick
        | 統合因果記録.地盤振動記録 記録 -> 記録.Tick
        | 統合因果記録.エンティティ反応状態変更記録 記録 -> 記録.Tick

    let 実行者ID = function
        | 統合因果記録.Entity位置変更記録 記録 -> 記録.実行者ID
        | 統合因果記録.地盤振動記録 記録 -> 記録.実行者ID
        | 統合因果記録.エンティティ反応状態変更記録 記録 -> 記録.実行者ID

    let 概要 = function
        | 統合因果記録.Entity位置変更記録 記録 -> 記録.概要
        | 統合因果記録.地盤振動記録 記録 -> 記録.概要
        | 統合因果記録.エンティティ反応状態変更記録 記録 -> 記録.概要

    let 発行Event一覧 = function
        | 統合因果記録.Entity位置変更記録 記録 -> 記録.発行Event一覧
        | 統合因果記録.地盤振動記録 記録 -> 記録.発行Event一覧
        | 統合因果記録.エンティティ反応状態変更記録 記録 -> 記録.発行Event一覧

    let Entity位置変更記録を得る = function
        | 統合因果記録.Entity位置変更記録 記録 -> Some 記録
        | 統合因果記録.地盤振動記録 _ -> None
        | 統合因果記録.エンティティ反応状態変更記録 _ -> None

    let 地盤振動発生記録を得る = function
        | 統合因果記録.地盤振動記録 記録 -> Some 記録
        | 統合因果記録.Entity位置変更記録 _ -> None
        | 統合因果記録.エンティティ反応状態変更記録 _ -> None

    let エンティティ反応状態変更記録を得る = function
        | 統合因果記録.エンティティ反応状態変更記録 記録 -> Some 記録
        | 統合因果記録.Entity位置変更記録 _ -> None
        | 統合因果記録.地盤振動記録 _ -> None

    let internal 分岐する 位置変更を処理する 地盤振動を処理する 反応状態変更を処理する = function
        | 統合因果記録.Entity位置変更記録 記録 -> 位置変更を処理する 記録
        | 統合因果記録.地盤振動記録 記録 -> 地盤振動を処理する 記録
        | 統合因果記録.エンティティ反応状態変更記録 記録 -> 反応状態変更を処理する 記録

[<RequireQualifiedAccess>]
type 統合因果追記項目 =
    | 位置変更候補 of 因果記録候補
    | 地盤振動記録 of 地盤振動発生記録
    | エンティティ反応状態変更記録 of エンティティ反応状態変更記録

type 統合因果台帳 =
    private
    | 統合因果台帳 of 統合因果記録 list

[<RequireQualifiedAccess>]
type 統合因果台帳追記結果 =
    | 成功 of 台帳: 統合因果台帳
    | 失敗 of
        台帳: 統合因果台帳 *
        失敗位置: int *
        失敗操作ID: 因果操作ID *
        理由一覧: string list

module 統合因果台帳 =

    let 空 = 統合因果台帳 []

    let 記録一覧 (統合因果台帳 記録一覧) =
        記録一覧

    let 件数 台帳 =
        台帳
        |> 記録一覧
        |> List.length

    let Entity位置変更記録一覧 台帳 =
        台帳
        |> 記録一覧
        |> List.choose 統合因果記録.Entity位置変更記録を得る

    let 地盤振動発生記録一覧 台帳 =
        台帳
        |> 記録一覧
        |> List.choose 統合因果記録.地盤振動発生記録を得る

    let エンティティ反応状態変更記録一覧 台帳 =
        台帳
        |> 記録一覧
        |> List.choose 統合因果記録.エンティティ反応状態変更記録を得る

    let Entity位置変更件数 台帳 =
        台帳
        |> Entity位置変更記録一覧
        |> List.length

    let 地盤振動発生件数 台帳 =
        台帳
        |> 地盤振動発生記録一覧
        |> List.length

    let エンティティ反応状態変更件数 台帳 =
        台帳
        |> エンティティ反応状態変更記録一覧
        |> List.length

    let 因果操作IDで探す ID 台帳 =
        台帳
        |> 記録一覧
        |> List.tryFind (fun 記録 -> 統合因果記録.因果操作ID 記録 = ID)

    let 伝播信号IDで探す ID 台帳 =
        台帳
        |> 地盤振動発生記録一覧
        |> List.tryFind (fun 記録 -> 記録.信号.ID = ID)

    let エンティティ反応要求IDで探す ID 台帳 =
        台帳
        |> エンティティ反応状態変更記録一覧
        |> List.tryFind (fun 記録 -> エンティティ反応要求.ID 記録.由来要求 = ID)

    let private 項目因果操作ID = function
        | 統合因果追記項目.位置変更候補 候補 -> 候補.因果操作ID
        | 統合因果追記項目.地盤振動記録 記録 -> 記録.因果操作ID
        | 統合因果追記項目.エンティティ反応状態変更記録 記録 -> 記録.因果操作ID

    let private 項目Tick = function
        | 統合因果追記項目.位置変更候補 候補 -> 候補.Tick
        | 統合因果追記項目.地盤振動記録 記録 -> 記録.Tick
        | 統合因果追記項目.エンティティ反応状態変更記録 記録 -> 記録.Tick

    let private 項目信号ID = function
        | 統合因果追記項目.位置変更候補 _ -> None
        | 統合因果追記項目.地盤振動記録 記録 -> Some 記録.信号.ID
        | 統合因果追記項目.エンティティ反応状態変更記録 _ -> None

    let private 項目反応要求ID = function
        | 統合因果追記項目.エンティティ反応状態変更記録 記録 ->
            Some(エンティティ反応要求.ID 記録.由来要求)
        | 統合因果追記項目.位置変更候補 _ -> None
        | 統合因果追記項目.地盤振動記録 _ -> None

    let private 項目固有検証と正式化 = function
        | 統合因果追記項目.位置変更候補 候補 ->
            match 因果台帳.単一候補を正式化する 候補 with
            | Ok 記録 -> Ok(統合因果記録.Entity位置変更記録 記録)
            | Error 理由一覧 ->
                理由一覧
                |> List.map (fun 理由 -> $"位置変更候補: {理由}")
                |> Error
        | 統合因果追記項目.地盤振動記録 記録 ->
            match 地盤振動発生記録.検証する 記録 with
            | Ok () -> Ok(統合因果記録.地盤振動記録 記録)
            | Error 理由一覧 ->
                理由一覧
                |> List.map (fun 理由 -> $"地盤振動発生記録: {理由}")
                |> Error
        | 統合因果追記項目.エンティティ反応状態変更記録 記録 ->
            match エンティティ反応状態変更記録.検証する 記録 with
            | Ok () -> Ok(統合因果記録.エンティティ反応状態変更記録 記録)
            | Error 理由一覧 ->
                理由一覧
                |> List.map (fun 理由 -> $"エンティティ反応状態変更記録: {理由}")
                |> Error

    let private 出現回数を作る 値を得る 項目一覧 =
        項目一覧
        |> List.fold
            (fun 出現回数 項目 ->
                let 値 = 値を得る 項目

                出現回数
                |> Map.change 値 (fun 現在回数 ->
                    現在回数
                    |> Option.defaultValue 0
                    |> fun 回数 -> Some(回数 + 1)))
            Map.empty

    let private 信号ID出現回数を作る 項目一覧 =
        項目一覧
        |> List.choose 項目信号ID
        |> List.fold
            (fun 出現回数 ID ->
                出現回数
                |> Map.change ID (fun 現在回数 ->
                    現在回数
                    |> Option.defaultValue 0
                    |> fun 回数 -> Some(回数 + 1)))
            Map.empty

    let private 反応要求ID出現回数を作る 項目一覧 =
        項目一覧
        |> List.choose 項目反応要求ID
        |> List.fold
            (fun 出現回数 ID ->
                出現回数
                |> Map.change ID (fun 現在回数 ->
                    現在回数
                    |> Option.defaultValue 0
                    |> fun 回数 -> Some(回数 + 1)))
            Map.empty

    let private 直前Tick一覧を作る 既存記録一覧 項目一覧 =
        let 既存末尾Tick =
            既存記録一覧
            |> List.tryLast
            |> Option.map 統合因果記録.Tick

        let rec 作る 直前Tick 未処理一覧 =
            match 未処理一覧 with
            | [] -> []
            | 項目 :: 残り ->
                直前Tick :: 作る (Some(項目Tick 項目)) 残り

        作る 既存末尾Tick 項目一覧

    let 一括追記する
        (現在台帳: 統合因果台帳)
        (項目一覧: 統合因果追記項目 list)
        : 統合因果台帳追記結果 =
        let 既存記録一覧 = 記録一覧 現在台帳

        let 既存因果操作ID一覧 =
            既存記録一覧
            |> List.map 統合因果記録.因果操作ID
            |> Set.ofList

        let 既存信号ID一覧 =
            既存記録一覧
            |> List.choose 統合因果記録.地盤振動発生記録を得る
            |> List.map (fun 記録 -> 記録.信号.ID)
            |> Set.ofList

        let 既存反応要求ID一覧 =
            既存記録一覧
            |> List.choose 統合因果記録.エンティティ反応状態変更記録を得る
            |> List.map (fun 記録 -> エンティティ反応要求.ID 記録.由来要求)
            |> Set.ofList

        let 因果操作ID出現回数 = 出現回数を作る 項目因果操作ID 項目一覧
        let 信号ID出現回数 = 信号ID出現回数を作る 項目一覧
        let 反応要求ID出現回数 = 反応要求ID出現回数を作る 項目一覧
        let 直前Tick一覧 = 直前Tick一覧を作る 既存記録一覧 項目一覧

        let 検証結果 =
            List.zip 項目一覧 直前Tick一覧
            |> List.mapi (fun index (項目, 直前Tick) ->
                let 固有結果 = 項目固有検証と正式化 項目

                let 固有理由一覧 =
                    match 固有結果 with
                    | Ok _ -> []
                    | Error 理由一覧 -> 理由一覧

                let 因果ID = 項目因果操作ID 項目
                let 信号ID = 項目信号ID 項目
                let 反応要求ID = 項目反応要求ID 項目

                let 理由一覧 =
                    [
                        yield! 固有理由一覧

                        if 直前Tick |> Option.exists (fun tick -> 項目Tick 項目 < tick) then
                            "統合因果台帳のTick順序が逆行しています。"

                        if Set.contains 因果ID 既存因果操作ID一覧 then
                            "統合因果台帳に同じ因果操作IDが既に存在します。"

                        if Map.find 因果ID 因果操作ID出現回数 > 1 then
                            "統合因果追記項目一覧内で因果操作IDが重複しています。"

                        match 信号ID with
                        | Some id when Set.contains id 既存信号ID一覧 ->
                            "統合因果台帳に同じ伝播信号IDが既に存在します。"
                        | _ -> ()

                        match 信号ID with
                        | Some id when Map.find id 信号ID出現回数 > 1 ->
                            "統合因果追記項目一覧内で伝播信号IDが重複しています。"
                        | _ -> ()

                        match 反応要求ID with
                        | Some id when Set.contains id 既存反応要求ID一覧 ->
                            "統合因果台帳に同じエンティティ反応要求IDが既に存在します。"
                        | _ -> ()

                        match 反応要求ID with
                        | Some id when Map.find id 反応要求ID出現回数 > 1 ->
                            "統合因果追記項目一覧内でエンティティ反応要求IDが重複しています。"
                        | _ -> ()
                    ]

                index, 因果ID, 固有結果, 理由一覧)

        match 検証結果 |> List.tryFind (fun (_, _, _, 理由一覧) -> not (List.isEmpty 理由一覧)) with
        | Some(index, 因果ID, _, 理由一覧) ->
            統合因果台帳追記結果.失敗(現在台帳, index, 因果ID, 理由一覧)
        | None ->
            let 追記記録一覧 =
                検証結果
                |> List.choose (fun (_, _, 固有結果, _) ->
                    match 固有結果 with
                    | Ok 記録 -> Some 記録
                    | Error _ -> None)

            統合因果台帳追記結果.成功(
                統合因果台帳(既存記録一覧 @ 追記記録一覧)
            )

    let 位置変更候補一覧を追記する 台帳 候補一覧 =
        候補一覧
        |> List.map 統合因果追記項目.位置変更候補
        |> 一括追記する 台帳

    let 地盤振動発生記録一覧を追記する 台帳 記録一覧 =
        記録一覧
        |> List.map 統合因果追記項目.地盤振動記録
        |> 一括追記する 台帳

    let エンティティ反応状態変更記録一覧を追記する 台帳 記録一覧 =
        記録一覧
        |> List.map 統合因果追記項目.エンティティ反応状態変更記録
        |> 一括追記する 台帳

    let 既存因果台帳から移行する 台帳 =
        let 記録一覧 =
            台帳
            |> 因果台帳.記録一覧
            |> List.map 統合因果記録.Entity位置変更記録

        統合因果台帳 記録一覧
