namespace Omokane.Core.Domain

type 自発移動意図ID =
    | 自発移動意図ID of string

[<RequireQualifiedAccess>]
type 自発移動方向 =
    | 左
    | 右
    | 上
    | 下

[<RequireQualifiedAccess>]
type 自発移動意図由来 =
    | 明示指定
    | 入力由来 of 入力

type 自発移動意図 =
    private
    | 自発移動意図 of
        ID: 自発移動意図ID *
        Tick: int64 *
        実行者ID: エンティティID *
        方向: 自発移動方向 *
        由来: 自発移動意図由来

module 自発移動意図 =

    let ID (自発移動意図(ID, _, _, _, _)) =
        ID

    let Tick (自発移動意図(_, Tick, _, _, _)) =
        Tick

    let 実行者ID (自発移動意図(_, _, 実行者ID, _, _)) =
        実行者ID

    let 方向 (自発移動意図(_, _, _, 方向, _)) =
        方向

    let 由来 (自発移動意図(_, _, _, _, 由来)) =
        由来

    let 由来入力 意図 =
        match 由来 意図 with
        | 自発移動意図由来.明示指定 -> None
        | 自発移動意図由来.入力由来 入力 -> Some 入力

    let 明示指定である 意図 =
        match 由来 意図 with
        | 自発移動意図由来.明示指定 -> true
        | 自発移動意図由来.入力由来 _ -> false

    let 入力由来である 意図 =
        match 由来 意図 with
        | 自発移動意図由来.明示指定 -> false
        | 自発移動意図由来.入力由来 _ -> true

module 自発移動意図生成 =

    let private 作る
        (自発移動意図ID ID値 as ID)
        Tick
        (エンティティID 実行者ID値 as 実行者ID)
        方向
        由来
        : Result<自発移動意図, string list> =
        let エラー一覧 =
            [
                if System.String.IsNullOrWhiteSpace ID値 then
                    "自発移動意図IDが空です。"

                if Tick < 0L then
                    "自発移動意図のTickが負です。"

                if System.String.IsNullOrWhiteSpace 実行者ID値 then
                    "自発移動意図の実行者IDが空です。"
            ]

        if List.isEmpty エラー一覧 then
            Ok(自発移動意図(ID, Tick, 実行者ID, 方向, 由来))
        else
            Error エラー一覧

    let 方向から作る ID Tick 実行者ID 方向 =
        作る ID Tick 実行者ID 方向 自発移動意図由来.明示指定

    let 入力から作る ID Tick 実行者ID 入力 =
        let 方向 =
            match 入力 with
            | 左へ移動 -> Some 自発移動方向.左
            | 右へ移動 -> Some 自発移動方向.右
            | 上へ移動 -> Some 自発移動方向.上
            | 下へ移動 -> Some 自発移動方向.下
            | ジャンプ -> None
            | 攻撃 -> None
            | 入力なし -> None

        match 方向 with
        | None -> Ok None
        | Some 移動方向 ->
            作る ID Tick 実行者ID 移動方向 (自発移動意図由来.入力由来 入力)
            |> Result.map Some
