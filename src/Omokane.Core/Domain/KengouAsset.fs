namespace Omokane.Core.Domain

type 権能ID = 権能ID of string

type 権能タグ =
    // 目的
    | 攻撃系
    | 観察系
    | 食事系
    | 休息系
    | 移動系
    | 会話系
    | 祈祷系
    | 料理系
    | 環境利用系

    // 世界への作用
    | ログのみ
    | 使用者を更新する
    | 対象を更新する
    | 対象を削除する
    | Entityを生成する
    | 世界状態を更新する

    // 対象
    | 生物対象
    | 食料対象
    | 焚き火対象
    | 場所対象
    | 道具対象
    | 任意対象

    // 関係性
    | 敵対行動
    | 友好行動
    | 中立行動
    | 自己対象行動

    // 運用
    | AI選択可能
    | プレイヤー選択可能
    | デバッグ用
    | 一回限り
    | 繰り返し可能

[<RequireQualifiedAccess>]
type 権能Asset =
    {
        ID: 権能ID
        名前: string
        タグ: Set<権能タグ>
        発行Event一覧: ゲームイベント list
    }

module 権能Asset =

    let タグを持つか (タグ: 権能タグ) (権能: 権能Asset) : bool =
        権能.タグ |> Set.contains タグ

    let 検証する (権能: 権能Asset) : Result<unit, string list> =
        let (権能ID id) = 権能.ID

        let エラー一覧 =
            [
                if System.String.IsNullOrWhiteSpace id then
                    "権能IDが空です。"

                if System.String.IsNullOrWhiteSpace 権能.名前 then
                    "権能名が空です。"

                if Set.isEmpty 権能.タグ then
                    "権能タグが空です。"
            ]

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧
