namespace Omokane.Core.Domain

type 互換移動適用準備 =
    private
    | 互換移動適用準備 of
        対象ゲーム状態: ゲーム状態 *
        対象Entity: エンティティ *
        現在制御判断: 自発移動制御判断 *
        命令一覧: 互換移動命令 list

module 互換移動適用準備 =

    let 対象ゲーム状態 (互換移動適用準備(状態, _, _, _)) = 状態
    let 対象Entity (互換移動適用準備(_, entity, _, _)) = entity
    let 現在制御判断 (互換移動適用準備(_, _, 判断, _)) = 判断
    let 命令一覧 (互換移動適用準備(_, _, _, 命令一覧)) = 命令一覧
    let 命令数 準備 = 準備 |> 命令一覧 |> List.length

    let 単一命令 準備 =
        match 命令一覧 準備 with
        | [ 命令 ] -> Some 命令
        | _ -> None

    let 既存入力一覧 準備 = 準備 |> 命令一覧 |> List.map 互換移動命令.既存入力
    let Tick 準備 = (対象ゲーム状態 準備).Tick
    let プレイヤーID 準備 = (対象ゲーム状態 準備).プレイヤーID
    let 実行者ID 準備 = 準備 |> 現在制御判断 |> 自発移動制御判断.実行者ID
    let 自発移動意図ID一覧 準備 = 準備 |> 命令一覧 |> List.map 互換移動命令.由来自発移動意図ID
    let 命令種別一覧 準備 = 準備 |> 命令一覧 |> List.map 互換移動命令.種別
    let 自発移動方向一覧 準備 = 準備 |> 命令一覧 |> List.map 互換移動命令.由来自発移動方向

module 互換移動適用前検証 =

    let private 非空一覧を準備する (状態: ゲーム状態) 先頭 後続 =
        let 命令一覧 = 先頭 :: 後続
        let 判断結果 = 自発移動制御判断生成.ゲーム状態から作る 状態.プレイヤーID 状態

        // 判断成功時は既存契約が対象Entityの存在と一意性を保証する。
        let 検証済み対象 =
            match 判断結果 with
            | Error _ -> None
            | Ok 判断 ->
                let entity = 状態.エンティティ一覧 |> List.find (fun e -> e.ID = 状態.プレイヤーID)
                Some(entity, 判断)

        let エラー一覧 =
            [
                match 判断結果 with
                | Error 理由一覧 ->
                    yield! 理由一覧 |> List.map (fun 理由 -> "現在の自発移動制御判断: " + 理由)
                | Ok _ -> ()

                if 状態.終了状態 <> None then
                    "終了状態のため互換移動命令を準備できません。"

                match 検証済み対象 with
                | Some(entity, _) when entity.種別 <> プレイヤー ->
                    "互換移動対象Entityの種別がプレイヤーではありません。"
                | _ -> ()

                for index, 命令 in List.indexed 命令一覧 do
                    if 互換移動命令.Tick 命令 <> 状態.Tick then
                        $"互換移動命令[{index}]: Tickがゲーム状態Tickと一致しません。"

                for index, 命令 in List.indexed 命令一覧 do
                    if 互換移動命令.実行者ID 命令 <> 状態.プレイヤーID then
                        $"互換移動命令[{index}]: 実行者IDがゲーム状態のプレイヤーIDと一致しません。"

                match 検証済み対象 with
                | Some(_, 判断) ->
                    if 自発移動制御判断.自発移動を抑制する 判断 then
                        "現在の自発移動制御判断が自発移動を許可していません。"
                    else
                        for index, 命令 in List.indexed 命令一覧 do
                            if 互換移動命令.Tick 命令 = 状態.Tick
                               && 互換移動命令.実行者ID 命令 = 状態.プレイヤーID
                               && 互換移動命令.由来制御判断 命令 <> 判断 then
                                $"互換移動命令[{index}]: 由来制御判断が現在のゲーム状態から導出した判断と一致しません。"
                | None -> ()

                if 命令一覧
                   |> List.countBy 互換移動命令.由来自発移動意図ID
                   |> List.exists (fun (_, 件数) -> 件数 > 1) then
                    "互換移動命令一覧内で自発移動意図IDが重複しています。"
            ]

        match エラー一覧, 検証済み対象 with
        | [], Some(entity, 判断) -> Ok(互換移動適用準備(状態, entity, 判断, 命令一覧))
        | 理由一覧, _ -> Error 理由一覧

    let 単一を準備する 状態 命令 = 非空一覧を準備する 状態 命令 []

    let 一括を準備する 状態 命令一覧 =
        match 命令一覧 with
        | [] -> Ok None
        | 先頭 :: 後続 -> 非空一覧を準備する 状態 先頭 後続 |> Result.map Some
