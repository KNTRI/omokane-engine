namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 地盤振動発生操作 =
    {
        因果: 因果操作
        信号ID: 伝播信号ID
        発生位置: 位置
        方向: 速度 option
        強度: float
        方式: 伝播方式
        寿命Tick: int64
    }

[<RequireQualifiedAccess>]
type 地盤振動発生記録 =
    {
        因果操作ID: 因果操作ID
        Tick: int64
        実行者ID: エンティティID option
        概要: string
        信号: 伝播信号
        発行Event一覧: ゲームイベント list
    }

type 地盤振動発生結果 =
    | 発生成功 of
        更新: 更新結果 *
        信号: 伝播信号 *
        記録: 地盤振動発生記録
    | 発生失敗 of
        状態: ゲーム状態 *
        分類: 因果操作失敗分類 *
        理由一覧: string list

type 地盤振動発生列結果 =
    | 一括発生成功 of
        更新: 更新結果 *
        信号一覧: 伝播信号 list *
        記録一覧: 地盤振動発生記録 list
    | 一括発生失敗 of
        状態: ゲーム状態 *
        失敗位置: int *
        失敗操作ID: 因果操作ID *
        分類: 因果操作失敗分類 *
        理由一覧: string list

module 地盤振動発生記録 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let 検証する (記録: 地盤振動発生記録) : Result<unit, string list> =
        let (因果操作ID 因果ID値) = 記録.因果操作ID

        let 信号エラー =
            伝播信号.検証する 記録.信号
            |> 結果エラーを得る

        let エラー一覧 =
            [
                if System.String.IsNullOrWhiteSpace 因果ID値 then
                    "地盤振動発生記録の因果操作IDが空です。"

                if 記録.Tick < 0L then
                    "地盤振動発生記録のTickが負です。"

                if System.String.IsNullOrWhiteSpace 記録.概要 then
                    "地盤振動発生記録の概要が空です。"

                yield! 信号エラー

                if 記録.信号.原因因果ID <> Some 記録.因果操作ID then
                    "地盤振動発生記録の信号原因因果IDが因果操作IDと一致しません。"

                if 記録.信号.発生源EntityID <> 記録.実行者ID then
                    "地盤振動発生記録の信号発生源EntityIDが実行者IDと一致しません。"

                if 記録.信号.量種別 <> 地盤振動 then
                    "地盤振動発生記録の信号量種別が地盤振動ではありません。"

                if 記録.信号.媒体 <> 地盤 then
                    "地盤振動発生記録の信号媒体が地盤ではありません。"
            ]

        if List.isEmpty エラー一覧 then
            Ok ()
        else
            Error エラー一覧

module private 地盤振動発生内部 =

    let 結果エラーを得る = function
        | Ok () -> []
        | Error エラー一覧 -> エラー一覧

    let 信号を構築する (要求: 地盤振動発生操作) : 伝播信号 =
        {
            ID = 要求.信号ID
            原因因果ID = Some 要求.因果.ID
            発生源EntityID = 要求.因果.実行者ID
            量種別 = 地盤振動
            媒体 = 地盤
            発生位置 = 要求.発生位置
            方向 = 要求.方向
            強度 = 要求.強度
            方式 = 要求.方式
            寿命Tick = 要求.寿命Tick
        }

    let 契約エラーを集める (要求: 地盤振動発生操作) =
        let 因果エラー =
            因果操作.検証する 要求.因果
            |> 結果エラーを得る

        let 信号エラー =
            要求
            |> 信号を構築する
            |> 伝播信号.検証する
            |> 結果エラーを得る

        [
            yield! 因果エラー

            if 要求.因果.種別 <> 伝播信号生成 then
                "因果操作種別が伝播信号生成ではありません。"

            if Option.isSome 要求.因果.対象EntityID then
                "地盤振動発生操作には対象EntityIDを指定できません。"

            yield! 信号エラー
        ]

    let ゲーム内エラーを集める
        (現在状態: ゲーム状態)
        (要求: 地盤振動発生操作)
        =
        let 実行者一覧 =
            match 要求.因果.実行者ID with
            | None -> []
            | Some 実行者ID ->
                現在状態.エンティティ一覧
                |> List.filter (fun entity -> entity.ID = 実行者ID)

        [
            if 要求.因果.Tick <> 現在状態.Tick then
                "地盤振動発生操作のTickが現在状態のTickと一致しません。"

            if Option.isSome 現在状態.終了状態 then
                "終了状態のため地盤振動を発生できません。"

            match 要求.因果.実行者ID with
            | Some _ when List.isEmpty 実行者一覧 ->
                "実行者Entityが存在しません。"
            | _ -> ()

            match 要求.因果.実行者ID with
            | Some _ when List.length 実行者一覧 > 1 ->
                "実行者EntityIDがゲーム状態内で重複しています。"
            | _ -> ()
        ]

    let 記録を構築する
        (要求: 地盤振動発生操作)
        (信号: 伝播信号)
        : 地盤振動発生記録 =
        {
            因果操作ID = 要求.因果.ID
            Tick = 要求.因果.Tick
            実行者ID = 要求.因果.実行者ID
            概要 = 要求.因果.概要
            信号 = 信号
            発行Event一覧 = 要求.因果.発行Event一覧
        }

module 地盤振動発生実行 =

    let 適用する
        (現在状態: ゲーム状態)
        (要求: 地盤振動発生操作)
        : 地盤振動発生結果 =
        let 契約エラー一覧 = 地盤振動発生内部.契約エラーを集める 要求

        if not (List.isEmpty 契約エラー一覧) then
            発生失敗(現在状態, 契約不正, 契約エラー一覧)
        else
            let ゲーム内エラー一覧 =
                地盤振動発生内部.ゲーム内エラーを集める 現在状態 要求

            if not (List.isEmpty ゲーム内エラー一覧) then
                発生失敗(現在状態, ゲーム内失敗, ゲーム内エラー一覧)
            else
                let 信号 = 地盤振動発生内部.信号を構築する 要求
                let 記録 = 地盤振動発生内部.記録を構築する 要求 信号

                match 地盤振動発生記録.検証する 記録 with
                | Error エラー一覧 ->
                    発生失敗(現在状態, 契約不正, エラー一覧)
                | Ok () ->
                    発生成功(
                        {
                            状態 = 現在状態
                            イベント一覧 = 要求.因果.発行Event一覧
                        },
                        信号,
                        記録
                    )

module 地盤振動発生列実行 =

    let private 因果操作ID出現回数を作る (操作一覧: 地盤振動発生操作 list) =
        操作一覧
        |> List.countBy (fun 操作 -> 操作.因果.ID)
        |> Map.ofList

    let private 伝播信号ID出現回数を作る (操作一覧: 地盤振動発生操作 list) =
        操作一覧
        |> List.countBy (fun 操作 -> 操作.信号ID)
        |> Map.ofList

    let private 静的契約エラーを集める
        (因果ID出現回数: Map<因果操作ID, int>)
        (信号ID出現回数: Map<伝播信号ID, int>)
        (要求: 地盤振動発生操作)
        =
        [
            yield! 地盤振動発生内部.契約エラーを集める 要求

            if Map.find 要求.因果.ID 因果ID出現回数 > 1 then
                "地盤振動発生操作一覧内で因果操作IDが重複しています。"

            if Map.find 要求.信号ID 信号ID出現回数 > 1 then
                "地盤振動発生操作一覧内で伝播信号IDが重複しています。"
        ]

    let 一括適用する
        (現在状態: ゲーム状態)
        (操作一覧: 地盤振動発生操作 list)
        : 地盤振動発生列結果 =
        let 因果ID出現回数 = 因果操作ID出現回数を作る 操作一覧
        let 信号ID出現回数 = 伝播信号ID出現回数を作る 操作一覧

        let 静的検証結果 =
            操作一覧
            |> List.mapi (fun index 操作 ->
                index,
                操作,
                静的契約エラーを集める 因果ID出現回数 信号ID出現回数 操作)

        match 静的検証結果 |> List.tryFind (fun (_, _, エラー一覧) -> not (List.isEmpty エラー一覧)) with
        | Some(index, 操作, エラー一覧) ->
            一括発生失敗(
                現在状態,
                index,
                操作.因果.ID,
                契約不正,
                エラー一覧
            )
        | None ->
            let rec 仮適用する
                index
                仮状態
                逆順Event一覧
                逆順信号一覧
                逆順記録一覧
                未適用一覧
                =
                match 未適用一覧 with
                | [] ->
                    let Event一覧 =
                        逆順Event一覧
                        |> List.rev
                        |> List.concat

                    一括発生成功(
                        {
                            状態 = 仮状態
                            イベント一覧 = Event一覧
                        },
                        List.rev 逆順信号一覧,
                        List.rev 逆順記録一覧
                    )
                | 操作 :: 残り ->
                    match 地盤振動発生実行.適用する 仮状態 操作 with
                    | 発生成功(更新, 信号, 記録) ->
                        仮適用する
                            (index + 1)
                            更新.状態
                            (更新.イベント一覧 :: 逆順Event一覧)
                            (信号 :: 逆順信号一覧)
                            (記録 :: 逆順記録一覧)
                            残り
                    | 発生失敗(_, 分類, 理由一覧) ->
                        一括発生失敗(
                            現在状態,
                            index,
                            操作.因果.ID,
                            分類,
                            理由一覧
                        )

            仮適用する 0 現在状態 [] [] [] 操作一覧
