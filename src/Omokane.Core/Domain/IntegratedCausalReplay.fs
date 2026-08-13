namespace Omokane.Core.Domain

[<RequireQualifiedAccess>]
type 統合因果台帳再生結果 =
    | 成功 of
        更新: 更新結果 *
        再生信号一覧: 伝播信号 list
    | 失敗 of
        状態: ゲーム状態 *
        失敗位置: int *
        失敗操作ID: 因果操作ID *
        理由一覧: string list

module private 地盤振動発生記録再生 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error 理由一覧 -> 理由一覧

    let 適用する
        (現在状態: ゲーム状態)
        (記録: 地盤振動発生記録)
        : Result<ゲーム状態, string list> =
        let 実行者一覧 =
            match 記録.実行者ID with
            | None -> []
            | Some 実行者ID ->
                現在状態.エンティティ一覧
                |> List.filter (fun entity -> entity.ID = 実行者ID)

        let 理由一覧 =
            [
                yield! 地盤振動発生記録.検証する 記録 |> 結果エラーを得る

                if 記録.Tick < 現在状態.Tick then
                    "地盤振動発生記録のTickが現在の再生状態より前です。"

                if Option.isSome 現在状態.終了状態 then
                    "終了状態のため地盤振動発生記録を再生できません。"

                match 記録.実行者ID with
                | Some _ when List.isEmpty 実行者一覧 ->
                    "地盤振動発生元Entityが存在しません。"
                | _ -> ()

                match 記録.実行者ID with
                | Some _ when List.length 実行者一覧 > 1 ->
                    "地盤振動発生元EntityIDがゲーム状態内で重複しています。"
                | _ -> ()
            ]

        if List.isEmpty 理由一覧 then
            Ok { 現在状態 with Tick = 記録.Tick }
        else
            Error 理由一覧

module private エンティティ反応状態変更記録再生 =

    let private 結果エラーを得る = function
        | Ok () -> []
        | Error 理由一覧 -> 理由一覧

    let 適用する
        (現在状態: ゲーム状態)
        (記録: エンティティ反応状態変更記録)
        : Result<ゲーム状態, string list> =
        let 対象Entity一覧 =
            現在状態.エンティティ一覧
            |> List.filter (fun entity -> entity.ID = 記録.対象EntityID)

        let 対象反応状態一覧 =
            現在状態.エンティティ反応状態一覧
            |> List.filter (fun 状態 -> エンティティ反応状態.実行者ID 状態 = 記録.対象EntityID)

        let 現在反応状態 =
            match 対象反応状態一覧 with
            | [] -> Some None
            | [ 状態 ] -> Some(Some 状態)
            | _ -> None

        let 理由一覧 =
            [
                yield! エンティティ反応状態変更記録.検証する 記録 |> 結果エラーを得る

                if 記録.Tick < 現在状態.Tick then
                    "エンティティ反応状態変更記録のTickが現在の再生状態より前です。"

                if Option.isSome 現在状態.終了状態 then
                    "終了状態のためエンティティ反応状態変更記録を再生できません。"

                if List.isEmpty 対象Entity一覧 then
                    "反応対象Entityが存在しません。"

                if List.length 対象Entity一覧 > 1 then
                    "反応対象EntityIDがゲーム状態内で重複しています。"

                yield!
                    現在状態.エンティティ反応状態一覧
                    |> エンティティ反応状態一覧.検証する
                    |> 結果エラーを得る

                match 現在反応状態 with
                | Some 状態 when 状態 <> 記録.変更前状態 ->
                    "対象Entityの現在反応状態が記録の変更前状態と一致しません。"
                | _ -> ()
            ]

        if List.isEmpty 理由一覧 then
            Ok
                {
                    現在状態 with
                        Tick = 記録.Tick
                        エンティティ反応状態一覧 =
                            現在状態.エンティティ反応状態一覧
                            |> エンティティ反応状態.置換または追加する 記録.変更後状態
                }
        else
            Error 理由一覧

module 統合因果台帳再生 =

    let 再生する
        (初期状態: ゲーム状態)
        (台帳: 統合因果台帳)
        : 統合因果台帳再生結果 =
        let rec 再生を進める
            index
            仮状態
            逆順Event一覧
            逆順信号一覧
            未再生記録一覧
            =
            match 未再生記録一覧 with
            | [] ->
                let Event一覧 =
                    逆順Event一覧
                    |> List.rev
                    |> List.concat

                統合因果台帳再生結果.成功(
                    {
                        状態 = 仮状態
                        イベント一覧 = Event一覧
                    },
                    List.rev 逆順信号一覧
                )
            | 記録 :: 残り ->
                let 因果ID = 統合因果記録.因果操作ID 記録

                let 適用結果 =
                    記録
                    |> 統合因果記録.分岐する
                        (fun 位置変更記録 ->
                            Entity位置変更記録再生.適用する 仮状態 位置変更記録
                            |> Result.map (fun 更新後状態 ->
                                更新後状態,
                                位置変更記録.発行Event一覧,
                                None))
                        (fun 地盤振動記録 ->
                            地盤振動発生記録再生.適用する 仮状態 地盤振動記録
                            |> Result.map (fun 更新後状態 ->
                                更新後状態,
                                地盤振動記録.発行Event一覧,
                                Some 地盤振動記録.信号))
                        (fun 反応状態変更記録 ->
                            エンティティ反応状態変更記録再生.適用する 仮状態 反応状態変更記録
                            |> Result.map (fun 更新後状態 ->
                                更新後状態,
                                反応状態変更記録.発行Event一覧,
                                None))

                match 適用結果 with
                | Error 理由一覧 ->
                    統合因果台帳再生結果.失敗(
                        初期状態,
                        index,
                        因果ID,
                        理由一覧
                    )
                | Ok(更新後状態, event一覧, 信号) ->
                    再生を進める
                        (index + 1)
                        更新後状態
                        (event一覧 :: 逆順Event一覧)
                        (match 信号 with
                         | Some 値 -> 値 :: 逆順信号一覧
                         | None -> 逆順信号一覧)
                        残り

        台帳
        |> 統合因果台帳.記録一覧
        |> 再生を進める 0 初期状態 [] []
