module ActionCandidateSelectionTests

open System
open Expecto
open Omokane.Core.Domain
open IntegratedCausalLedgerTests.TestData

let private 主体A = エンティティID "selection.actor.a"
let private 主体B = エンティティID "selection.actor.b"

let private 感知値を作る id 主体 tick 種別 : 感知値 =
    {
        観測者ID = 主体
        信号ID = 伝播信号ID($"selection.signal.{id}")
        種別 = 種別
        測定値 = 1.0
        確信度 = 1.0
        Tick = tick
    }

let private 候補を作る id 主体 優先度 仮説 tick 種別 : 行動候補 =
    let 感知 = 感知値を作る id 主体 tick 種別

    let 観測: 観測 =
        {
            観測者ID = 主体
            概要 = $"{仮説}の観測"
            確信度 = 1.0
            根拠一覧 = [ 感知 ]
        }

    let 信念候補: 信念候補 =
        {
            仮説 = 仮説
            確率 = 優先度
            根拠一覧 = [ 観測 ]
            反証一覧 = []
        }

    let 警戒意図: 警戒意図 =
        {
            観測者ID = 主体
            対象仮説 = 仮説
            警戒度 = 優先度
            由来信念候補 = 信念候補
        }

    {
        ID = 行動候補ID id
        実行者ID = 主体
        種別 = 行動候補種別.その場警戒
        優先度 = 優先度
        対象仮説 = 仮説
        由来警戒意図 = 警戒意図
    }

let private 基本候補 id 優先度 =
    候補を作る id 主体A 優先度 "危険が接近中" 10L 地盤感知

let private 選択済みを得る = function
    | Ok(Some 選択済み) -> 選択済み
    | Ok None -> failtest "候補なしではなく選択成立を期待しました。"
    | Error エラー一覧 -> failtestf "候補選択が失敗しました: %A" エラー一覧

let private エラー一覧を得る = function
    | Error エラー一覧 -> エラー一覧
    | Ok 結果 -> failtestf "エラーを期待しました: %A" 結果

let private 選択候補を得る 候補一覧 =
    候補一覧
    |> 行動候補選択.選ぶ
    |> 選択済みを得る
    |> 選択済み行動候補.選択候補

let private 地盤振動操作 id signalID 強度 event一覧 : 地盤振動発生操作 =
    {
        因果 =
            {
                ID = 因果操作ID id
                Tick = 10L
                種別 = 伝播信号生成
                原因因果ID = None
                実行者ID = Some 発生者ID
                対象EntityID = None
                概要 = "選択候補用の地盤振動"
                発行Event一覧 = event一覧
            }
        信号ID = 伝播信号ID signalID
        発生位置 = 位置 8.0 8.0
        方向 = None
        強度 = 強度
        方式 = 波動
        寿命Tick = 5L
    }

let private 感知設定: 感知設定 =
    {
        種別 = 地盤感知
        対象量種別 = 地盤振動
        対象媒体 = 地盤
        最大距離 = 10.0
        感度倍率 = 1.0
        感知閾値 = 0.0
        確信飽和値 = 10.0
    }

let private 因果経路候補を作る candidateID signalID 強度 事前確率 =
    let 入力: 因果地盤振動行動入力 =
        {
            発生操作 = 地盤振動操作 ($"causal.{signalID}") signalID 強度 []
            観測者ID = 観測者ID
            観測位置 = 位置 8.0 8.0
            感知設定 = 感知設定
            観測概要 = $"{signalID}を感知"
            仮説 = $"{signalID}由来の危険"
            事前確率 = 事前確率
            警戒設定 = { 発生閾値 = 0.0 }
            行動候補ID = 行動候補ID candidateID
        }

    match 因果地盤振動行動経路.実行する (状態 10L) 入力 with
    | Ok(因果地盤振動行動結果.行動候補成立(_, _, _, _, _, _, _, 候補)) -> 候補
    | 結果 -> failtestf "因果認識経路で候補を生成できませんでした: %A" 結果

let private 発生成功を得る = function
    | 発生成功(更新, 信号, 記録) -> 更新, 信号, 記録
    | 発生失敗(_, 分類, 理由一覧) -> failtestf "地盤振動発生失敗: %A %A" 分類 理由一覧

let private 統合再生成功を得る = function
    | 統合因果台帳再生結果.成功(更新, 信号一覧) -> 更新, 信号一覧
    | 統合因果台帳再生結果.失敗(_, index, id, 理由一覧) ->
        failtestf "統合Replay失敗: index=%d id=%A reasons=%A" index id 理由一覧

let private Replay信号から候補を作る index tick (信号: 伝播信号) =
    let 入力: 地盤振動警戒入力 =
        {
            現在Tick = tick
            観測者ID = 観測者ID
            観測位置 = 信号.発生位置
            感知設定 = 感知設定
            信号 = 信号
            観測概要 = $"Replay信号{index}を感知"
            仮説 = $"Replay信号{index}由来の危険"
            事前確率 = 0.5
            警戒設定 = { 発生閾値 = 0.0 }
        }

    let 意図 =
        match 地盤振動警戒経路.実行する 入力 with
        | Ok(警戒成立(_, _, _, 意図)) -> 意図
        | 結果 -> failtestf "Replay信号から警戒意図を生成できませんでした: %A" 結果

    match 行動候補生成.警戒意図から作る (行動候補ID $"replay.action.{index}") 意図 with
    | Ok 候補 -> 候補
    | Error 理由一覧 -> failtestf "Replay信号から候補を生成できませんでした: %A" 理由一覧

let private Replay接続を作る () =
    let 初期状態 = 状態 10L

    let _, _, 弱記録 =
        地盤振動発生実行.適用する
            初期状態
            (地盤振動操作 "replay.causal.weak" "replay.signal.weak" 5.0 [ Tick進行 10L ])
        |> 発生成功を得る

    let _, _, 強記録 =
        地盤振動発生実行.適用する
            初期状態
            (地盤振動操作
                "replay.causal.strong"
                "replay.signal.strong"
                10.0
                [ 衝突発生(発生者ID, 観測者ID) ])
        |> 発生成功を得る

    let 台帳 = 統合台帳を作る [ 振動項目 弱記録; 振動項目 強記録 ]
    let 更新, 信号一覧 = 統合因果台帳再生.再生する 初期状態 台帳 |> 統合再生成功を得る

    let 候補一覧 =
        信号一覧
        |> List.mapi (fun index 信号 -> Replay信号から候補を作る index 更新.状態.Tick 信号)

    初期状態, 台帳, 更新, 信号一覧, 候補一覧

let private 候補Matrixを作る caseIndex count pattern idOrder hypothesisMode =
    let 優先度 index =
        match pattern with
        | 0 -> if count <= 1 then 0.5 else float (index + 1) / float count
        | 1 -> if count <= 1 then 0.5 else float (count - index) / float count
        | 2 -> 0.5
        | 3 -> if count <= 1 || index = 0 || index = count - 1 then 1.0 else 0.25
        | 4 -> if index = count / 2 then 1.0 else 0.25
        | _ -> if index % 2 = 0 then 0.9 else 0.2

    let ID番号 index =
        match idOrder with
        | 0 -> index
        | 1 -> count - index - 1
        | _ -> if index % 2 = 0 then index / 2 else count - 1 - index / 2

    [
        for index in 0 .. count - 1 do
            let hypothesis =
                if hypothesisMode = 0 then
                    "共通仮説"
                else
                    $"仮説{index}"

            yield
                候補を作る
                    $"matrix.{caseIndex}.id.{ID番号 index}"
                    主体A
                    (優先度 index)
                    hypothesis
                    (int64 index)
                    (if index % 2 = 0 then 地盤感知 else 聴覚)
    ]

[<Tests>]
let 全テスト =
    testList
        "行動候補の決定論的な最小選択"
        [
            testCase "空一覧は正常な候補なし" <| fun _ ->
                Expect.equal (行動候補選択.選ぶ []) (Ok None) "ダミー候補を生成しない"

            testCase "一件から選択できる" <| fun _ ->
                行動候補選択.選ぶ [ 基本候補 "single" 0.5 ] |> 選択済みを得る |> ignore

            testCase "選択候補を取得できる" <| fun _ ->
                let 候補 = 基本候補 "selected" 0.5
                let 選択済み = 行動候補選択.選ぶ [ 候補 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.選択候補 選択済み) 候補 "入力候補を返す"

            testCase "実行者IDを取得できる" <| fun _ ->
                let 選択済み = 行動候補選択.選ぶ [ 基本候補 "actor" 0.5 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.実行者ID 選択済み) 主体A "同一主体"

            testCase "候補一覧を入力順で保持する" <| fun _ ->
                let 候補一覧 = [ 基本候補 "third" 0.3; 基本候補 "first" 0.9; 基本候補 "second" 0.6 ]
                let 選択済み = 行動候補選択.選ぶ 候補一覧 |> 選択済みを得る
                Expect.equal (選択済み行動候補.候補一覧 選択済み) 候補一覧 "再ソートしない"

            testCase "候補数を返す" <| fun _ ->
                let 選択済み = 行動候補選択.選ぶ [ 基本候補 "a" 0.2; 基本候補 "b" 0.8 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.候補数 選択済み) 2 "二件"

            testCase "選択位置を返す" <| fun _ ->
                let 選択済み = 行動候補選択.選ぶ [ 基本候補 "a" 0.2; 基本候補 "b" 0.8; 基本候補 "c" 0.4 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.選択位置 選択済み) 1 "0始まりindex"

            testCase "却下候補一覧を入力順で返す" <| fun _ ->
                let 第一 = 基本候補 "a" 0.2
                let 選択 = 基本候補 "b" 0.8
                let 第三 = 基本候補 "c" 0.4
                let 選択済み = 行動候補選択.選ぶ [ 第一; 選択; 第三 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.却下候補一覧 選択済み) [ 第一; 第三 ] "選択位置だけ除外"

            testCase "単独最高優先度理由を返す" <| fun _ ->
                let 選択済み = 行動候補選択.選ぶ [ 基本候補 "a" 0.2; 基本候補 "b" 0.8 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.選択理由 選択済み) 行動候補選択理由.単独最高優先度 "最高は一件"

            testCase "同率最高入力順理由を返す" <| fun _ ->
                let 選択済み = 行動候補選択.選ぶ [ 基本候補 "a" 0.8; 基本候補 "b" 0.8 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.選択理由 選択済み) 行動候補選択理由.同率最高入力順 "同率"

            testCase "同率最高候補一覧を入力順で返す" <| fun _ ->
                let 第一 = 基本候補 "z" 0.8
                let 非最高 = 基本候補 "m" 0.5
                let 第二 = 基本候補 "a" 0.8
                let 選択済み = 行動候補選択.選ぶ [ 第一; 非最高; 第二 ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.同率最高候補一覧 選択済み) [ 第一; 第二 ] "最高だけ入力順"

            testCase "二候補から高優先度を選択する" <| fun _ ->
                Expect.equal (選択候補を得る [ 基本候補 "low" 0.2; 基本候補 "high" 0.8 ]).ID (行動候補ID "high") "高優先度"

            testCase "三候補から高優先度を選択する" <| fun _ ->
                let selected = 選択候補を得る [ 基本候補 "low" 0.2; 基本候補 "high" 0.9; 基本候補 "middle" 0.5 ]
                Expect.equal selected.ID (行動候補ID "high") "三件比較"

            testCase "後方の最高候補を選択する" <| fun _ ->
                let selected = 選択候補を得る [ 基本候補 "a" 0.1; 基本候補 "b" 0.2; 基本候補 "c" 1.0 ]
                Expect.equal selected.ID (行動候補ID "c") "末尾最高"

            testCase "先頭の最高候補を選択する" <| fun _ ->
                let selected = 選択候補を得る [ 基本候補 "a" 1.0; 基本候補 "b" 0.8; 基本候補 "c" 0.9 ]
                Expect.equal selected.ID (行動候補ID "a") "先頭最高"

            testCase "優先度0を許可する" <| fun _ ->
                Expect.equal (選択候補を得る [ 基本候補 "zero" 0.0 ]).優先度 0.0 "下限"

            testCase "優先度1を許可する" <| fun _ ->
                Expect.equal (選択候補を得る [ 基本候補 "one" 1.0 ]).優先度 1.0 "上限"

            testCase "同率二候補で先頭を選択する" <| fun _ ->
                Expect.equal (選択候補を得る [ 基本候補 "first" 0.8; 基本候補 "second" 0.8 ]).ID (行動候補ID "first") "先着"

            testCase "同率三候補で先頭を選択する" <| fun _ ->
                let selected = 選択候補を得る [ 基本候補 "first" 0.8; 基本候補 "second" 0.8; 基本候補 "third" 0.8 ]
                Expect.equal selected.ID (行動候補ID "first") "三件同率"

            testCase "候補ID辞書順で再ソートしない" <| fun _ ->
                Expect.equal (選択候補を得る [ 基本候補 "z" 0.8; 基本候補 "a" 0.8 ]).ID (行動候補ID "z") "IDをタイブレークにしない"

            testCase "仮説文字列で再ソートしない" <| fun _ ->
                let z = 候補を作る "first" 主体A 0.8 "Z仮説" 10L 地盤感知
                let a = 候補を作る "second" 主体A 0.8 "A仮説" 10L 地盤感知
                Expect.equal (選択候補を得る [ z; a ]) z "仮説を解析しない"

            testCase "由来値で再ソートしない" <| fun _ ->
                let first = 候補を作る "first" 主体A 0.8 "同率" 30L 聴覚
                let second = 候補を作る "second" 主体A 0.8 "同率" 1L 地盤感知
                Expect.equal (選択候補を得る [ first; second ]) first "Tickや感知種別を使わない"

            testCase "入力候補一覧を変更しない" <| fun _ ->
                let candidates = [ 基本候補 "a" 0.2; 基本候補 "b" 0.8 ]
                let before = candidates
                行動候補選択.選ぶ candidates |> ignore
                Expect.equal candidates before "イミュータブル入力"

            testCase "選択候補を再構築しない" <| fun _ ->
                let candidate = 基本候補 "physical" 0.8
                let selected = 選択候補を得る [ candidate ]
                Expect.isTrue (Object.ReferenceEquals(candidate, selected)) "入力レコードを保持"

            testCase "由来警戒意図を構造的に保持する" <| fun _ ->
                let candidate = 基本候補 "provenance" 0.8
                let selected = 選択候補を得る [ candidate ]
                Expect.equal selected.由来警戒意図 candidate.由来警戒意図 "由来情報"

            testCase "同じ仮説で異なるIDの候補を許可する" <| fun _ ->
                let result = 行動候補選択.選ぶ [ 基本候補 "a" 0.3; 基本候補 "b" 0.7 ]
                result |> 選択済みを得る |> ignore

            testCase "同じ内容で異なるIDの候補を許可する" <| fun _ ->
                let first = 基本候補 "a" 0.5
                let second = { first with ID = 行動候補ID "b" }
                let selected = 行動候補選択.選ぶ [ first; second ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.候補数 selected) 2 "重複排除しない"

            testCase "異なる仮説を許可する" <| fun _ ->
                let first = 候補を作る "a" 主体A 0.3 "仮説A" 10L 地盤感知
                let second = 候補を作る "b" 主体A 0.7 "仮説B" 10L 地盤感知
                Expect.equal (選択候補を得る [ first; second ]).対象仮説 "仮説B" "優先度だけ比較"

            testCase "異なる観測Tickを許可する" <| fun _ ->
                let first = 候補を作る "a" 主体A 0.3 "仮説A" 1L 地盤感知
                let second = 候補を作る "b" 主体A 0.7 "仮説B" 99L 地盤感知
                行動候補選択.選ぶ [ first; second ] |> 選択済みを得る |> ignore

            testCase "異なる感知種別を許可する" <| fun _ ->
                let first = 候補を作る "a" 主体A 0.3 "仮説A" 10L 地盤感知
                let second = 候補を作る "b" 主体A 0.7 "仮説B" 10L 聴覚
                行動候補選択.選ぶ [ first; second ] |> 選択済みを得る |> ignore

            testCase "空白候補IDをindex付きで拒否する" <| fun _ ->
                let bad = { 基本候補 "valid" 0.5 with ID = 行動候補ID " " }
                Expect.equal (行動候補選択.選ぶ [ bad ] |> エラー一覧を得る) [ "行動候補[0]: 行動候補IDが空です。" ] "index"

            testCase "空白実行者IDをindex付きで拒否する" <| fun _ ->
                let bad = { 基本候補 "bad.actor" 0.5 with 実行者ID = エンティティID " " }
                let errors = 行動候補選択.選ぶ [ bad ] |> エラー一覧を得る
                Expect.equal errors.Head "行動候補[0]: 行動候補の実行者IDが空です。" "先頭エラー"

            testCase "優先度NaNをindex付きで拒否する" <| fun _ ->
                let bad = { 基本候補 "nan" 0.5 with 優先度 = Double.NaN }
                let errors = 行動候補選択.選ぶ [ bad ] |> エラー一覧を得る
                Expect.equal errors.Head "行動候補[0]: 行動候補の優先度が有限値ではありません。" "NaN比較前に拒否"

            testCase "優先度Infinityをindex付きで拒否する" <| fun _ ->
                let bad = { 基本候補 "infinity" 0.5 with 優先度 = Double.PositiveInfinity }
                let errors = 行動候補選択.選ぶ [ bad ] |> エラー一覧を得る
                Expect.equal errors.Head "行動候補[0]: 行動候補の優先度が有限値ではありません。" "Infinity比較前に拒否"

            testCase "優先度負値をindex付きで拒否する" <| fun _ ->
                let bad = { 基本候補 "negative" 0.5 with 優先度 = -0.1 }
                let errors = 行動候補選択.選ぶ [ bad ] |> エラー一覧を得る
                Expect.equal errors.Head "行動候補[0]: 行動候補の優先度は0.0以上1.0以下である必要があります。" "負値"

            testCase "優先度1超をindex付きで拒否する" <| fun _ ->
                let bad = { 基本候補 "over" 0.5 with 優先度 = 1.1 }
                let errors = 行動候補選択.選ぶ [ bad ] |> エラー一覧を得る
                Expect.equal errors.Head "行動候補[0]: 行動候補の優先度は0.0以上1.0以下である必要があります。" "上限超過"

            testCase "空白対象仮説をindex付きで拒否する" <| fun _ ->
                let bad = { 基本候補 "blank.hypothesis" 0.5 with 対象仮説 = "" }
                let errors = 行動候補選択.選ぶ [ bad ] |> エラー一覧を得る
                Expect.equal errors.Head "行動候補[0]: 行動候補の対象仮説が空です。" "空白仮説"

            testCase "不正な由来警戒意図をindex付きで拒否する" <| fun _ ->
                let candidate = 基本候補 "bad.intent" 0.5
                let intent = { candidate.由来警戒意図 with 対象仮説 = "" }
                let bad = { candidate with 由来警戒意図 = intent }
                let errors = 行動候補選択.選ぶ [ bad ] |> エラー一覧を得る
                Expect.contains errors "行動候補[0]: 由来警戒意図: 警戒意図の対象仮説が空です。" "由来エラー接頭辞"

            testCase "後方候補が不正でも選択しない" <| fun _ ->
                let valid = 基本候補 "valid" 1.0
                let bad = { 基本候補 "bad" 0.1 with ID = 行動候補ID "" }
                match 行動候補選択.選ぶ [ valid; bad ] with
                | Error _ -> ()
                | result -> failtestf "部分選択しました: %A" result

            testCase "複数候補エラーを入力順で返す" <| fun _ ->
                let first = { 基本候補 "first" 0.5 with ID = 行動候補ID "" }
                let second = { 基本候補 "second" 0.5 with 対象仮説 = "" }
                let errors = 行動候補選択.選ぶ [ first; second ] |> エラー一覧を得る
                Expect.equal errors[0] "行動候補[0]: 行動候補IDが空です。" "候補0"
                Expect.equal errors[1] "行動候補[1]: 行動候補の対象仮説が空です。" "候補1"

            testCase "候補エラーを主体不一致より先に返す" <| fun _ ->
                let first = { 基本候補 "first" 0.5 with ID = 行動候補ID "" }
                let second = 候補を作る "second" 主体B 0.8 "別主体" 10L 地盤感知
                let errors = 行動候補選択.選ぶ [ first; second ] |> エラー一覧を得る
                Expect.equal errors [ "行動候補[0]: 行動候補IDが空です。"; "行動候補選択に使用する候補の実行者IDが一致しません。" ] "固定順"

            testCase "主体不一致を拒否する" <| fun _ ->
                let first = 基本候補 "first" 0.5
                let second = 候補を作る "second" 主体B 0.8 "別主体" 10L 地盤感知
                Expect.equal (行動候補選択.選ぶ [ first; second ] |> エラー一覧を得る) [ "行動候補選択に使用する候補の実行者IDが一致しません。" ] "一主体限定"

            testCase "候補ID重複を拒否する" <| fun _ ->
                let first = 基本候補 "same" 0.5
                let second = 基本候補 "same" 0.8
                Expect.equal (行動候補選択.選ぶ [ first; second ] |> エラー一覧を得る) [ "行動候補選択に使用する行動候補IDが重複しています。" ] "ID一意"

            testCase "主体不一致をID重複より先に返す" <| fun _ ->
                let first = 基本候補 "same" 0.5
                let second = 候補を作る "same" 主体B 0.8 "別主体" 10L 地盤感知
                Expect.equal
                    (行動候補選択.選ぶ [ first; second ] |> エラー一覧を得る)
                    [
                        "行動候補選択に使用する候補の実行者IDが一致しません。"
                        "行動候補選択に使用する行動候補IDが重複しています。"
                    ]
                    "主体からID"

            testCase "全入力エラーを固定順で返す" <| fun _ ->
                let first = { 基本候補 "first" 0.5 with ID = 行動候補ID "" }
                let secondBase = 候補を作る "second" 主体B 0.5 "別主体" 10L 地盤感知
                let second = { secondBase with ID = 行動候補ID ""; 対象仮説 = "" }
                Expect.equal
                    (行動候補選択.選ぶ [ first; second ] |> エラー一覧を得る)
                    [
                        "行動候補[0]: 行動候補IDが空です。"
                        "行動候補[1]: 行動候補IDが空です。"
                        "行動候補[1]: 行動候補の対象仮説が空です。"
                        "行動候補[1]: 行動候補の対象仮説が由来警戒意図の対象仮説と一致しません。"
                        "行動候補選択に使用する候補の実行者IDが一致しません。"
                        "行動候補選択に使用する行動候補IDが重複しています。"
                    ]
                    "候補順、主体、ID"

            testCase "同じ入力から同じ結果を返す" <| fun _ ->
                let candidates = [ 基本候補 "a" 0.4; 基本候補 "b" 0.8; 基本候補 "c" 0.8 ]
                Expect.equal (行動候補選択.選ぶ candidates) (行動候補選択.選ぶ candidates) "構造的決定論"

            testCase "100件候補でも決定論的に選択する" <| fun _ ->
                let candidates =
                    [
                        for index in 0 .. 99 do
                            yield 基本候補 $"hundred.{index}" (if index = 73 then 1.0 else float (index % 10) / 20.0)
                    ]
                let selected = 行動候補選択.選ぶ candidates |> 選択済みを得る
                Expect.equal (選択済み行動候補.選択位置 selected) 73 "明示最高"
                Expect.equal (行動候補選択.選ぶ candidates) (行動候補選択.選ぶ candidates) "二回一致"

            testCase "昇順優先度で末尾を選択する" <| fun _ ->
                let selected = 選択候補を得る [ 基本候補 "a" 0.1; 基本候補 "b" 0.5; 基本候補 "c" 0.9 ]
                Expect.equal selected.ID (行動候補ID "c") "末尾"

            testCase "降順優先度で先頭を選択する" <| fun _ ->
                let selected = 選択候補を得る [ 基本候補 "a" 0.9; 基本候補 "b" 0.5; 基本候補 "c" 0.1 ]
                Expect.equal selected.ID (行動候補ID "a") "先頭"

            testCase "交互優先度で最初の最高を選択する" <| fun _ ->
                let selected = 選択候補を得る [ 基本候補 "a" 0.9; 基本候補 "b" 0.1; 基本候補 "c" 0.9; 基本候補 "d" 0.2 ]
                Expect.equal selected.ID (行動候補ID "a") "同率後勝ちにしない"

            testCase "因果認識経路から生成した複数候補を選択する" <| fun _ ->
                let low = 因果経路候補を作る "causal.action.low" "causal.signal.low" 5.0 0.5
                let high = 因果経路候補を作る "causal.action.high" "causal.signal.high" 10.0 0.5
                Expect.equal (選択候補を得る [ low; high ]) high "既存経路を合成"

            testCase "統合Replay信号から生成した複数候補を選択する" <| fun _ ->
                let _, _, _, _, candidates = Replay接続を作る ()
                Expect.equal (選択候補を得る candidates).ID (行動候補ID "replay.action.1") "強信号候補"

            testCase "Replay後も選択候補のProvenanceを維持する" <| fun _ ->
                let _, _, _, signals, candidates = Replay接続を作る ()
                let selected = 選択候補を得る candidates
                let sensed = selected.由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head
                Expect.equal selected.実行者ID 観測者ID "観測者から実行者"
                Expect.equal sensed.信号ID signals[1].ID "正式Replay信号"
                Expect.equal selected.優先度 selected.由来警戒意図.警戒度 "警戒度を保持"

            testCase "選択後もゲーム状態不変" <| fun _ ->
                let initial, _, update, _, candidates = Replay接続を作る ()
                let before = update.状態
                行動候補選択.選ぶ candidates |> ignore
                Expect.equal update.状態 before "選択APIは状態を受け取らない"
                Expect.equal update.状態 initial "同一Tickの信号Replay"

            testCase "選択後も統合台帳不変" <| fun _ ->
                let _, ledger, _, _, candidates = Replay接続を作る ()
                let before = 統合因果台帳.記録一覧 ledger
                行動候補選択.選ぶ candidates |> ignore
                Expect.equal (統合因果台帳.記録一覧 ledger) before "正式履歴へ保存しない"

            testCase "選択処理はEventを生成しない" <| fun _ ->
                let _, _, update, _, candidates = Replay接続を作る ()
                let before = update.イベント一覧
                行動候補選択.選ぶ candidates |> ignore
                Expect.equal update.イベント一覧 before "Event境界"

            testCase "同率統合経路で入力先着を選択する" <| fun _ ->
                let z = 因果経路候補を作る "z.action" "tie.signal.z" 10.0 0.5
                let a = 因果経路候補を作る "a.action" "tie.signal.a" 10.0 0.5
                Expect.equal (選択候補を得る [ z; a ]).ID (行動候補ID "z.action") "辞書順ではない"

            testCase "候補ID順を逆転しても非同率では最高優先度を選択する" <| fun _ ->
                let z = 基本候補 "z" 0.4
                let a = 基本候補 "a" 0.9
                Expect.equal (選択候補を得る [ z; a ]).ID (行動候補ID "a") "優先度だけを比較"

            testCase "候補一覧の却下順を維持する" <| fun _ ->
                let a = 基本候補 "a" 0.1
                let b = 基本候補 "b" 0.9
                let c = 基本候補 "c" 0.2
                let d = 基本候補 "d" 0.3
                let selected = 行動候補選択.選ぶ [ a; b; c; d ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.却下候補一覧 selected) [ a; c; d ] "位置除外後の入力順"

            testCase "同率最高候補一覧に非最高候補を含めない" <| fun _ ->
                let a = 基本候補 "a" 0.8
                let b = 基本候補 "b" 0.7
                let c = 基本候補 "c" 0.8
                let selected = 行動候補選択.選ぶ [ a; b; c ] |> 選択済みを得る
                Expect.equal (選択済み行動候補.同率最高候補一覧 selected) [ a; c ] "非最高除外"

            testCase "252候補一覧Matrixが決定論的である" <| fun _ ->
                let counts = [ 0; 1; 2; 3; 5; 8; 16 ]
                let combinations =
                    [
                        for count in counts do
                            for pattern in 0 .. 5 do
                                for idOrder in 0 .. 2 do
                                    for hypothesisMode in 0 .. 1 do
                                        yield count, pattern, idOrder, hypothesisMode
                    ]

                Expect.equal combinations.Length 252 "100種類以上"

                combinations
                |> List.iteri (fun caseIndex (count, pattern, idOrder, hypothesisMode) ->
                    let candidates = 候補Matrixを作る caseIndex count pattern idOrder hypothesisMode
                    let first = 行動候補選択.選ぶ candidates
                    let second = 行動候補選択.選ぶ candidates
                    Expect.equal second first $"Matrix {caseIndex}の構造的決定論"

                    match first with
                    | Ok None -> Expect.isEmpty candidates $"Matrix {caseIndex}の空一覧"
                    | Ok(Some selected) ->
                        Expect.equal (選択済み行動候補.候補一覧 selected) candidates $"Matrix {caseIndex}の入力順"
                    | Error reasons -> failtestf "有効Matrixが失敗しました: index=%d reasons=%A" caseIndex reasons)
        ]
