namespace Omokane.Core.Domain

type 因果台帳再生結果 =
    | 再生成功 of
        更新: 更新結果
    | 再生失敗 of
        状態: ゲーム状態 *
        失敗位置: int *
        失敗操作ID: 因果操作ID *
        理由一覧: string list

module internal Entity位置変更記録再生 =

    let private 対象一覧
        (対象ID: エンティティID)
        (状態: ゲーム状態)
        =
        状態.エンティティ一覧
        |> List.filter (fun entity -> entity.ID = 対象ID)

    let private エラーを集める
        (現在状態: ゲーム状態)
        (記録: 因果台帳記録)
        =
        let 対象Entity一覧 = 対象一覧 記録.対象EntityID 現在状態

        [
            記録.Tick < 現在状態.Tick,
            "因果台帳記録のTickが現在の再生状態より前です。"
            Option.isSome 現在状態.終了状態,
            "終了状態のため因果台帳を再生できません。"
            List.isEmpty 対象Entity一覧,
            "対象Entityが存在しません。"
            List.length 対象Entity一覧 > 1,
            "対象EntityIDがゲーム状態内で重複しています。"
            (match 対象Entity一覧 with
             | [ 対象Entity ] -> 対象Entity.位置 <> 記録.変更前位置
             | _ -> false),
            "対象Entityの現在位置が因果台帳記録の変更前位置と一致しません。"
        ]
        |> List.choose (fun (不正, メッセージ) ->
            if 不正 then Some メッセージ else None)

    let 適用する
        (現在状態: ゲーム状態)
        (記録: 因果台帳記録)
        : Result<ゲーム状態, string list> =
        let 理由一覧 = エラーを集める 現在状態 記録

        if not (List.isEmpty 理由一覧) then
            Error 理由一覧
        else
            let 更新後Entity一覧 =
                現在状態.エンティティ一覧
                |> List.map (fun entity ->
                    if entity.ID = 記録.対象EntityID then
                        { entity with 位置 = 記録.変更後位置 }
                    else
                        entity)

            Ok
                {
                    現在状態 with
                        Tick = 記録.Tick
                        エンティティ一覧 = 更新後Entity一覧
                }

module 因果台帳再生 =

    let 再生する
        (初期状態: ゲーム状態)
        (台帳: 因果台帳)
        : 因果台帳再生結果 =
        let rec 再生を進める
            index
            仮状態
            逆順Event一覧
            未再生記録一覧
            =
            match 未再生記録一覧 with
            | [] ->
                let Event一覧 =
                    逆順Event一覧
                    |> List.rev
                    |> List.concat

                再生成功
                    {
                        状態 = 仮状態
                        イベント一覧 = Event一覧
                    }
            | 記録 :: 残り ->
                match Entity位置変更記録再生.適用する 仮状態 記録 with
                | Error 理由一覧 ->
                    再生失敗(
                        初期状態,
                        index,
                        記録.因果操作ID,
                        理由一覧
                    )
                | Ok 更新後状態 ->
                    再生を進める
                        (index + 1)
                        更新後状態
                        (記録.発行Event一覧 :: 逆順Event一覧)
                        残り

        台帳
        |> 因果台帳.記録一覧
        |> 再生を進める 0 初期状態 []
