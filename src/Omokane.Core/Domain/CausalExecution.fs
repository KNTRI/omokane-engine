namespace Omokane.Core.Domain

type エンティティ位置変更操作 =
    {
        因果: 因果操作
        変更後位置: 位置
    }

type 因果操作失敗分類 =
    | 契約不正
    | ゲーム内失敗

type 因果記録候補 =
    {
        因果操作ID: 因果操作ID
        Tick: int64
        成功: bool
        実行者ID: エンティティID option
        対象EntityID: エンティティID option
        概要: string
        変更前位置: 位置 option
        変更後位置: 位置 option
        発行Event一覧: ゲームイベント list
        失敗理由一覧: string list
    }

type 因果操作結果 =
    | 適用成功 of 更新: 更新結果 * 記録: 因果記録候補
    | 適用失敗 of
        状態: ゲーム状態 *
        分類: 因果操作失敗分類 *
        理由一覧: string list *
        記録: 因果記録候補

module 因果操作実行 =

    let private 有限値か 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 対象数 (対象ID: エンティティID) (状態: ゲーム状態) =
        状態.エンティティ一覧
        |> List.filter (fun entity -> entity.ID = 対象ID)
        |> List.length

    let private 失敗記録 (操作: 因果操作) 理由一覧 =
        {
            因果操作ID = 操作.ID
            Tick = 操作.Tick
            成功 = false
            実行者ID = 操作.実行者ID
            対象EntityID = 操作.対象EntityID
            概要 = 操作.概要
            変更前位置 = None
            変更後位置 = None
            発行Event一覧 = []
            失敗理由一覧 = 理由一覧
        }

    let private 契約エラーを集める (現在状態: ゲーム状態) (要求: エンティティ位置変更操作) =
        let 因果検証エラー =
            match 因果操作.検証する 要求.因果 with
            | Ok () -> []
            | Error エラー一覧 -> エラー一覧

        let 追加エラー =
            [
                要求.因果.種別 <> 状態変更, "因果操作種別が状態変更ではありません。"
                Option.isNone 要求.因果.対象EntityID, "対象EntityIDが指定されていません。"
                not (有限値か 要求.変更後位置.X), "変更後位置.Xが有限値ではありません。"
                not (有限値か 要求.変更後位置.Y), "変更後位置.Yが有限値ではありません。"
                (match 要求.因果.対象EntityID with
                 | Some 対象ID -> 対象数 対象ID 現在状態 > 1
                 | None -> false),
                "対象EntityIDがゲーム状態内で重複しています。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        因果検証エラー @ 追加エラー

    let private ゲーム内エラーを集める
        (現在状態: ゲーム状態)
        (操作: 因果操作)
        (対象ID: エンティティID)
        =
        [
            操作.Tick <> 現在状態.Tick, "因果操作のTickが現在状態のTickと一致しません。"
            Option.isSome 現在状態.終了状態, "終了状態のため因果操作を適用できません。"
            対象数 対象ID 現在状態 = 0, "対象Entityが存在しません。"
        ]
        |> List.choose (fun (不正, メッセージ) ->
            if 不正 then Some メッセージ else None)

    let 適用する
        (現在状態: ゲーム状態)
        (要求: エンティティ位置変更操作)
        : 因果操作結果 =
        let 契約エラー一覧 = 契約エラーを集める 現在状態 要求

        if not (List.isEmpty 契約エラー一覧) then
            適用失敗(
                現在状態,
                契約不正,
                契約エラー一覧,
                失敗記録 要求.因果 契約エラー一覧
            )
        else
            let 対象ID = 要求.因果.対象EntityID |> Option.get
            let ゲーム内エラー一覧 = ゲーム内エラーを集める 現在状態 要求.因果 対象ID

            if not (List.isEmpty ゲーム内エラー一覧) then
                適用失敗(
                    現在状態,
                    ゲーム内失敗,
                    ゲーム内エラー一覧,
                    失敗記録 要求.因果 ゲーム内エラー一覧
                )
            else
                let 対象 =
                    現在状態.エンティティ一覧
                    |> List.find (fun entity -> entity.ID = 対象ID)

                let 更新後Entity一覧 =
                    現在状態.エンティティ一覧
                    |> List.map (fun entity ->
                        if entity.ID = 対象ID then
                            { entity with 位置 = 要求.変更後位置 }
                        else
                            entity)

                let 更新後状態 =
                    {
                        現在状態 with
                            エンティティ一覧 = 更新後Entity一覧
                    }

                let 更新 =
                    {
                        状態 = 更新後状態
                        イベント一覧 = 要求.因果.発行Event一覧
                    }

                let 記録 =
                    {
                        因果操作ID = 要求.因果.ID
                        Tick = 要求.因果.Tick
                        成功 = true
                        実行者ID = 要求.因果.実行者ID
                        対象EntityID = 要求.因果.対象EntityID
                        概要 = 要求.因果.概要
                        変更前位置 = Some 対象.位置
                        変更後位置 = Some 要求.変更後位置
                        発行Event一覧 = 要求.因果.発行Event一覧
                        失敗理由一覧 = []
                    }

                適用成功(更新, 記録)

type 因果操作列結果 =
    | 一括適用成功 of
        更新: 更新結果 *
        記録一覧: 因果記録候補 list
    | 一括適用失敗 of
        状態: ゲーム状態 *
        失敗位置: int *
        失敗操作ID: 因果操作ID *
        分類: 因果操作失敗分類 *
        理由一覧: string list *
        失敗記録: 因果記録候補

module 因果操作列実行 =

    let private 有限値か 値 =
        not (System.Double.IsNaN 値 || System.Double.IsInfinity 値)

    let private 対象数 (対象ID: エンティティID) (状態: ゲーム状態) =
        状態.エンティティ一覧
        |> List.filter (fun entity -> entity.ID = 対象ID)
        |> List.length

    let private 失敗記録 (操作: 因果操作) 理由一覧 =
        {
            因果操作ID = 操作.ID
            Tick = 操作.Tick
            成功 = false
            実行者ID = 操作.実行者ID
            対象EntityID = 操作.対象EntityID
            概要 = 操作.概要
            変更前位置 = None
            変更後位置 = None
            発行Event一覧 = []
            失敗理由一覧 = 理由一覧
        }

    let private 操作ID出現回数を作る (操作一覧: エンティティ位置変更操作 list) =
        操作一覧
        |> List.fold
            (fun 回数一覧 操作 ->
                回数一覧
                |> Map.change 操作.因果.ID (fun 現在回数 ->
                    現在回数
                    |> Option.defaultValue 0
                    |> fun 回数 -> Some(回数 + 1)))
            Map.empty

    let private 静的契約エラーを集める
        (開始状態: ゲーム状態)
        (操作ID出現回数: Map<因果操作ID, int>)
        (要求: エンティティ位置変更操作)
        =
        let 因果検証エラー =
            match 因果操作.検証する 要求.因果 with
            | Ok () -> []
            | Error エラー一覧 -> エラー一覧

        let 追加エラー =
            [
                要求.因果.種別 <> 状態変更, "因果操作種別が状態変更ではありません。"
                Option.isNone 要求.因果.対象EntityID, "対象EntityIDが指定されていません。"
                not (有限値か 要求.変更後位置.X), "変更後位置.Xが有限値ではありません。"
                not (有限値か 要求.変更後位置.Y), "変更後位置.Yが有限値ではありません。"
                (match 要求.因果.対象EntityID with
                 | Some 対象ID -> 対象数 対象ID 開始状態 > 1
                 | None -> false),
                "対象EntityIDがゲーム状態内で重複しています。"
                (操作ID出現回数 |> Map.find 要求.因果.ID) > 1,
                "因果操作一覧内で因果操作IDが重複しています。"
            ]
            |> List.choose (fun (不正, メッセージ) ->
                if 不正 then Some メッセージ else None)

        因果検証エラー @ 追加エラー

    let 一括適用する
        (現在状態: ゲーム状態)
        (操作一覧: エンティティ位置変更操作 list)
        : 因果操作列結果 =
        let 操作ID出現回数 = 操作ID出現回数を作る 操作一覧

        let 静的検証結果 =
            操作一覧
            |> List.mapi (fun index 操作 ->
                index,
                操作,
                静的契約エラーを集める 現在状態 操作ID出現回数 操作)

        match 静的検証結果 |> List.tryFind (fun (_, _, エラー一覧) -> not (List.isEmpty エラー一覧)) with
        | Some(index, 操作, エラー一覧) ->
            一括適用失敗(
                現在状態,
                index,
                操作.因果.ID,
                契約不正,
                エラー一覧,
                失敗記録 操作.因果 エラー一覧
            )
        | None ->
            let rec 仮適用する
                index
                仮状態
                逆順Event一覧
                逆順記録一覧
                未適用一覧
                =
                match 未適用一覧 with
                | [] ->
                    let Event一覧 =
                        逆順Event一覧
                        |> List.rev
                        |> List.concat

                    一括適用成功(
                        {
                            状態 = 仮状態
                            イベント一覧 = Event一覧
                        },
                        List.rev 逆順記録一覧
                    )
                | 操作 :: 残り ->
                    match 因果操作実行.適用する 仮状態 操作 with
                    | 適用成功(更新, 記録) ->
                        仮適用する
                            (index + 1)
                            更新.状態
                            (更新.イベント一覧 :: 逆順Event一覧)
                            (記録 :: 逆順記録一覧)
                            残り
                    | 適用失敗(_, 分類, 理由一覧, 失敗記録) ->
                        一括適用失敗(
                            現在状態,
                            index,
                            操作.因果.ID,
                            分類,
                            理由一覧,
                            失敗記録
                        )

            仮適用する 0 現在状態 [] [] 操作一覧
