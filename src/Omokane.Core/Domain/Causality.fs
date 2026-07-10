namespace Omokane.Core.Domain

type 因果操作ID =
    | 因果操作ID of string

type 因果操作種別 =
    | 状態変更
    | 伝播信号生成
    | 痕跡生成
    | 権能操作

[<RequireQualifiedAccess>]
type 因果操作 =
    {
        ID: 因果操作ID
        Tick: int64
        種別: 因果操作種別
        原因因果ID: 因果操作ID option
        実行者ID: エンティティID option
        対象EntityID: エンティティID option
        概要: string
        発行Event一覧: ゲームイベント list
    }

module 因果操作 =

    let 検証する (操作: 因果操作) : Result<unit, string list> =
        let (因果操作ID id) = 操作.ID

        let エラー一覧 =
            [
                System.String.IsNullOrWhiteSpace id, "因果操作IDが空です。"
                操作.Tick < 0L, "因果操作のTickが負です。"
                System.String.IsNullOrWhiteSpace 操作.概要, "因果操作の概要が空です。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧
