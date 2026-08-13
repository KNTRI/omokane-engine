namespace Omokane.Core.Domain

type エンティティ反応状態変更操作 =
    private
    | エンティティ反応状態変更操作 of
        因果: 因果操作 *
        要求: エンティティ反応要求

module エンティティ反応状態変更操作 =

    let 因果 (エンティティ反応状態変更操作(因果, _)) =
        因果

    let 要求 (エンティティ反応状態変更操作(_, 要求)) =
        要求

module エンティティ反応状態変更操作生成 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let 反応要求から作る
        (新因果操作ID: 因果操作ID)
        (原因因果ID: 因果操作ID option)
        (発行Event一覧: ゲームイベント list)
        (要求: エンティティ反応要求)
        : Result<エンティティ反応状態変更操作, string list> =
        let (因果操作ID 因果操作ID値) = 新因果操作ID

        let 因果: 因果操作 =
            {
                ID = 新因果操作ID
                Tick = エンティティ反応要求.Tick 要求
                種別 = 状態変更
                原因因果ID = 原因因果ID
                実行者ID = Some(エンティティ反応要求.実行者ID 要求)
                対象EntityID = Some(エンティティ反応要求.実行者ID 要求)
                概要 = "エンティティ反応状態をその場警戒へ変更する"
                発行Event一覧 = 発行Event一覧
            }

        let 因果エラー一覧 =
            因果
            |> 因果操作.検証する
            |> 結果エラーを得る
            |> List.filter (fun エラー ->
                not (System.String.IsNullOrWhiteSpace 因果操作ID値 && エラー = "因果操作IDが空です。"))

        let 原因因果IDが空 =
            原因因果ID
            |> Option.exists (fun (因果操作ID ID) -> System.String.IsNullOrWhiteSpace ID)

        let エラー一覧 =
            [
                if System.String.IsNullOrWhiteSpace 因果操作ID値 then
                    "因果操作IDが空です。"

                if 原因因果IDが空 then
                    "原因因果IDが空です。"

                if 原因因果ID = Some 新因果操作ID then
                    "因果操作IDと原因因果IDを同じにできません。"

                yield! 因果エラー一覧
            ]

        if List.isEmpty エラー一覧 then
            Ok(エンティティ反応状態変更操作(因果, 要求))
        else
            Error エラー一覧

[<RequireQualifiedAccess>]
type エンティティ反応状態変更記録 =
    {
        因果操作ID: 因果操作ID
        原因因果ID: 因果操作ID option
        Tick: int64
        実行者ID: エンティティID option
        対象EntityID: エンティティID
        概要: string
        変更前状態: エンティティ反応状態 option
        変更後状態: エンティティ反応状態
        由来要求: エンティティ反応要求
        発行Event一覧: ゲームイベント list
    }

module エンティティ反応状態変更記録 =

    let 検証する (記録: エンティティ反応状態変更記録) : Result<unit, string list> =
        let (因果操作ID 因果操作ID値) = 記録.因果操作ID
        let (エンティティID 対象EntityID値) = 記録.対象EntityID

        let 原因因果IDが空 =
            記録.原因因果ID
            |> Option.exists (fun (因果操作ID ID) -> System.String.IsNullOrWhiteSpace ID)

        let エラー一覧 =
            [
                if System.String.IsNullOrWhiteSpace 因果操作ID値 then
                    "エンティティ反応状態変更記録の因果操作IDが空です。"

                if 原因因果IDが空 then
                    "エンティティ反応状態変更記録の原因因果IDが空です。"

                if 記録.原因因果ID = Some 記録.因果操作ID then
                    "エンティティ反応状態変更記録の因果操作IDと原因因果IDを同じにできません。"

                if 記録.Tick < 0L then
                    "エンティティ反応状態変更記録のTickが負です。"

                if Option.isNone 記録.実行者ID then
                    "エンティティ反応状態変更記録の実行者IDが指定されていません。"

                if System.String.IsNullOrWhiteSpace 対象EntityID値 then
                    "エンティティ反応状態変更記録の対象EntityIDが空です。"

                if System.String.IsNullOrWhiteSpace 記録.概要 then
                    "エンティティ反応状態変更記録の概要が空です。"

                if エンティティ反応状態.成立因果操作ID 記録.変更後状態 <> 記録.因果操作ID then
                    "エンティティ反応状態変更記録の変更後状態.成立因果操作IDが因果操作IDと一致しません。"

                if エンティティ反応状態.成立Tick 記録.変更後状態 <> 記録.Tick then
                    "エンティティ反応状態変更記録の変更後状態.成立TickがTickと一致しません。"

                if エンティティ反応状態.実行者ID 記録.変更後状態 <> 記録.対象EntityID then
                    "エンティティ反応状態変更記録の変更後状態.実行者IDが対象EntityIDと一致しません。"

                if エンティティ反応状態.由来要求 記録.変更後状態 <> 記録.由来要求 then
                    "エンティティ反応状態変更記録の変更後状態.由来要求が由来要求と一致しません。"

                if エンティティ反応要求.Tick 記録.由来要求 <> 記録.Tick then
                    "エンティティ反応状態変更記録の由来要求.TickがTickと一致しません。"

                if エンティティ反応要求.実行者ID 記録.由来要求 <> 記録.対象EntityID then
                    "エンティティ反応状態変更記録の由来要求.実行者IDが対象EntityIDと一致しません。"

                if 記録.実行者ID <> Some 記録.対象EntityID then
                    "エンティティ反応状態変更記録の実行者IDが対象EntityIDと一致しません。"

                match 記録.変更前状態 with
                | Some 状態 when エンティティ反応状態.実行者ID 状態 <> 記録.対象EntityID ->
                    "エンティティ反応状態変更記録の変更前状態.実行者IDが対象EntityIDと一致しません。"
                | _ -> ()
            ]

        if List.isEmpty エラー一覧 then Ok () else Error エラー一覧

[<RequireQualifiedAccess>]
type エンティティ反応状態変更結果 =
    | 成功 of
        更新: 更新結果 *
        記録: エンティティ反応状態変更記録
    | 失敗 of
        状態: ゲーム状態 *
        分類: 因果操作失敗分類 *
        理由一覧: string list

module エンティティ反応状態変更実行 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let private 契約エラーを集める (操作: エンティティ反応状態変更操作) =
        let 因果 = エンティティ反応状態変更操作.因果 操作
        let 要求 = エンティティ反応状態変更操作.要求 操作
        let 要求実行者ID = エンティティ反応要求.実行者ID 要求

        [
            yield! 因果操作.検証する 因果 |> 結果エラーを得る

            if 因果.種別 <> 状態変更 then
                "エンティティ反応状態変更操作の種別が状態変更ではありません。"

            if 因果.Tick <> エンティティ反応要求.Tick 要求 then
                "エンティティ反応状態変更操作のTickが由来要求と一致しません。"

            if 因果.実行者ID <> Some 要求実行者ID then
                "エンティティ反応状態変更操作の実行者IDが由来要求と一致しません。"

            if 因果.対象EntityID <> Some 要求実行者ID then
                "エンティティ反応状態変更操作の対象EntityIDが由来要求と一致しません。"

            if 因果.原因因果ID = Some 因果.ID then
                "因果操作IDと原因因果IDを同じにできません。"
        ]

    let private ゲーム内エラーを集める
        (現在状態: ゲーム状態)
        (因果: 因果操作)
        (対象ID: エンティティID)
        =
        let 対象一覧 =
            現在状態.エンティティ一覧
            |> List.filter (fun entity -> entity.ID = 対象ID)

        [
            if 因果.Tick <> 現在状態.Tick then
                "エンティティ反応状態変更操作のTickが現在状態のTickと一致しません。"

            if Option.isSome 現在状態.終了状態 then
                "終了状態のためエンティティ反応状態を変更できません。"

            if List.isEmpty 対象一覧 then
                "反応対象Entityが存在しません。"

            if List.length 対象一覧 > 1 then
                "反応対象EntityIDがゲーム状態内で重複しています。"

            yield!
                現在状態.エンティティ反応状態一覧
                |> エンティティ反応状態一覧.検証する
                |> 結果エラーを得る
        ]

    let 適用する
        (現在状態: ゲーム状態)
        (操作: エンティティ反応状態変更操作)
        : エンティティ反応状態変更結果 =
        let 契約エラー一覧 = 契約エラーを集める 操作

        if not (List.isEmpty 契約エラー一覧) then
            エンティティ反応状態変更結果.失敗(現在状態, 契約不正, 契約エラー一覧)
        else
            let 因果 = エンティティ反応状態変更操作.因果 操作
            let 要求 = エンティティ反応状態変更操作.要求 操作
            let 対象ID = エンティティ反応要求.実行者ID 要求
            let ゲーム内エラー一覧 = ゲーム内エラーを集める 現在状態 因果 対象ID

            if not (List.isEmpty ゲーム内エラー一覧) then
                エンティティ反応状態変更結果.失敗(現在状態, ゲーム内失敗, ゲーム内エラー一覧)
            else
                let 変更前状態 =
                    現在状態.エンティティ反応状態一覧
                    |> エンティティ反応状態一覧.実行者IDで探す 対象ID
                    |> Result.defaultValue None

                let 変更後状態 = エンティティ反応状態.作る 因果.ID 要求

                let 更新後状態 =
                    {
                        現在状態 with
                            エンティティ反応状態一覧 =
                                現在状態.エンティティ反応状態一覧
                                |> エンティティ反応状態.置換または追加する 変更後状態
                    }

                let 記録: エンティティ反応状態変更記録 =
                    {
                        因果操作ID = 因果.ID
                        原因因果ID = 因果.原因因果ID
                        Tick = 因果.Tick
                        実行者ID = 因果.実行者ID
                        対象EntityID = 対象ID
                        概要 = 因果.概要
                        変更前状態 = 変更前状態
                        変更後状態 = 変更後状態
                        由来要求 = 要求
                        発行Event一覧 = 因果.発行Event一覧
                    }

                match エンティティ反応状態変更記録.検証する 記録 with
                | Error エラー一覧 ->
                    エンティティ反応状態変更結果.失敗(現在状態, 契約不正, エラー一覧)
                | Ok () ->
                    エンティティ反応状態変更結果.成功(
                        {
                            状態 = 更新後状態
                            イベント一覧 = 因果.発行Event一覧
                        },
                        記録
                    )
