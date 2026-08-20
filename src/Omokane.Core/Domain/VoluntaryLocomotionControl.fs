namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 自発移動制御種別 =
    | 許可
    | 抑制

[<RequireQualifiedAccess>]
type 自発移動制御理由 =
    | 反応状態なし
    | その場警戒中

type 自発移動制御判断 =
    private
    | 自発移動制御判断 of
        評価Tick: int64 *
        実行者ID: エンティティID *
        種別: 自発移動制御種別 *
        理由: 自発移動制御理由 *
        由来反応状態: エンティティ反応状態 option

module 自発移動制御判断 =

    let 評価Tick (自発移動制御判断(評価Tick, _, _, _, _)) =
        評価Tick

    let 実行者ID (自発移動制御判断(_, 実行者ID, _, _, _)) =
        実行者ID

    let 種別 (自発移動制御判断(_, _, 種別, _, _)) =
        種別

    let 理由 (自発移動制御判断(_, _, _, 理由, _)) =
        理由

    let 自発移動を許可する 判断 =
        種別 判断 = 自発移動制御種別.許可

    let 自発移動を抑制する 判断 =
        種別 判断 = 自発移動制御種別.抑制

    let 由来反応状態 (自発移動制御判断(_, _, _, _, 由来反応状態)) =
        由来反応状態

    let 由来成立因果操作ID 判断 =
        判断
        |> 由来反応状態
        |> Option.map エンティティ反応状態.成立因果操作ID

    let 由来成立Tick 判断 =
        判断
        |> 由来反応状態
        |> Option.map エンティティ反応状態.成立Tick

    let 由来反応要求ID 判断 =
        判断
        |> 由来反応状態
        |> Option.map エンティティ反応状態.由来要求ID

    let 由来行動候補ID 判断 =
        判断
        |> 由来反応状態
        |> Option.map エンティティ反応状態.由来行動候補ID

    let 由来対象仮説 判断 =
        判断
        |> 由来反応状態
        |> Option.map エンティティ反応状態.対象仮説

    let 由来優先度 判断 =
        判断
        |> 由来反応状態
        |> Option.map エンティティ反応状態.優先度

module 自発移動制御判断生成 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let ゲーム状態から作る
        (対象EntityID: エンティティID)
        (状態: ゲーム状態)
        : Result<自発移動制御判断, string list> =
        let (エンティティID 対象EntityID値) = 対象EntityID

        let 対象Entity一覧 =
            状態.エンティティ一覧
            |> List.filter (fun entity -> entity.ID = 対象EntityID)

        let 対象反応状態一覧 =
            状態.エンティティ反応状態一覧
            |> List.filter (fun 反応状態 ->
                エンティティ反応状態.実行者ID 反応状態 = 対象EntityID)

        let 対象反応状態 =
            match 対象反応状態一覧 with
            | [ 反応状態 ] -> Some 反応状態
            | _ -> None

        let エラー一覧 =
            [
                if System.String.IsNullOrWhiteSpace 対象EntityID値 then
                    "自発移動制御対象EntityIDが空です。"

                if 状態.Tick < 0L then
                    "自発移動制御判断のゲーム状態Tickが負です。"

                if List.isEmpty 対象Entity一覧 then
                    "自発移動制御対象Entityが存在しません。"

                if List.length 対象Entity一覧 > 1 then
                    "自発移動制御対象EntityIDがゲーム状態内で重複しています。"

                yield!
                    状態.エンティティ反応状態一覧
                    |> エンティティ反応状態一覧.検証する
                    |> 結果エラーを得る

                match 対象反応状態 with
                | Some 反応状態 when エンティティ反応状態.成立Tick 反応状態 > 状態.Tick ->
                    "エンティティ反応状態の成立Tickがゲーム状態Tickより後です。"
                | _ -> ()
            ]

        if not (List.isEmpty エラー一覧) then
            Error エラー一覧
        else
            match 対象反応状態 with
            | None ->
                Ok(
                    自発移動制御判断(
                        状態.Tick,
                        対象EntityID,
                        自発移動制御種別.許可,
                        自発移動制御理由.反応状態なし,
                        None
                    )
                )
            | Some 反応状態 ->
                match エンティティ反応状態.種別 反応状態 with
                | エンティティ反応種別.その場警戒 ->
                    Ok(
                        自発移動制御判断(
                            状態.Tick,
                            対象EntityID,
                            自発移動制御種別.抑制,
                            自発移動制御理由.その場警戒中,
                            Some 反応状態
                        )
                    )
