module EntityReactionRequestTests

open System
open Expecto
open Omokane.Core.Domain
open IntegratedCausalLedgerTests.TestData

let private 主体A = エンティティID "reaction.actor.a"
let private 主体B = エンティティID "reaction.actor.b"

let private 感知値を作る id 主体 tick 種別 : 感知値 =
    {
        観測者ID = 主体
        信号ID = 伝播信号ID($"reaction.signal.{id}")
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

let private 要求を得る = function
    | Ok 要求 -> 要求
    | Error エラー一覧 -> failtestf "反応要求生成が失敗しました: %A" エラー一覧

let private エラー一覧を得る = function
    | Error エラー一覧 -> エラー一覧
    | Ok 要求 -> failtestf "反応要求生成エラーを期待しました: %A" 要求

let private 選択する 候補一覧 =
    候補一覧
    |> 行動候補選択.選ぶ
    |> 選択済みを得る

let private 要求する 要求ID Tick 候補一覧 =
    候補一覧
    |> 選択する
    |> エンティティ反応要求生成.選択済み行動候補から作る
        (エンティティ反応要求ID 要求ID)
        Tick
    |> 要求を得る

let private 候補の信号ID (候補: 行動候補) =
    候補.由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head.信号ID

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
                概要 = "反応要求用の地盤振動"
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
            発生操作 = 地盤振動操作 ($"reaction.causal.{signalID}") signalID 強度 []
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

    match 行動候補生成.警戒意図から作る (行動候補ID $"reaction.replay.action.{index}") 意図 with
    | Ok 候補 -> 候補
    | Error 理由一覧 -> failtestf "Replay信号から候補を生成できませんでした: %A" 理由一覧

let private Replay接続を作る () =
    let 初期状態 = 状態 10L

    let _, _, 弱記録 =
        地盤振動発生実行.適用する
            初期状態
            (地盤振動操作 "reaction.replay.causal.weak" "reaction.replay.signal.weak" 5.0 [ Tick進行 10L ])
        |> 発生成功を得る

    let _, _, 強記録 =
        地盤振動発生実行.適用する
            初期状態
            (地盤振動操作
                "reaction.replay.causal.strong"
                "reaction.replay.signal.strong"
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
            let 仮説 =
                if hypothesisMode = 0 then
                    "共通仮説"
                else
                    $"仮説{index}"

            yield
                候補を作る
                    $"reaction.matrix.{caseIndex}.id.{ID番号 index}"
                    主体A
                    (優先度 index)
                    仮説
                    (int64 index)
                    (if index % 2 = 0 then 地盤感知 else 聴覚)
    ]

[<Tests>]
let 全テスト =
    testList
        "選択済み行動候補からエンティティ反応要求を生成する最小境界"
        [
            testCase "正常な選択済み候補から要求を生成できる" <| fun _ ->
                要求する "request.normal" 10L [ 基本候補 "normal" 0.8 ] |> ignore

            testCase "明示要求IDを保存する" <| fun _ ->
                let 要求 = 要求する "request.explicit" 10L [ 基本候補 "explicit" 0.8 ]
                Expect.equal (エンティティ反応要求.ID 要求) (エンティティ反応要求ID "request.explicit") "明示ID"

            testCase "明示Tickを保存する" <| fun _ ->
                let 要求 = 要求する "request.tick" 25L [ 基本候補 "tick" 0.8 ]
                Expect.equal (エンティティ反応要求.Tick 要求) 25L "明示Tick"

            testCase "Tick 0を許可する" <| fun _ ->
                let 要求 = 要求する "request.tick.zero" 0L [ 基本候補 "tick.zero" 0.8 ]
                Expect.equal (エンティティ反応要求.Tick 要求) 0L "下限"

            testCase "Tick Int64.MaxValueを許可する" <| fun _ ->
                let 要求 = 要求する "request.tick.max" Int64.MaxValue [ 基本候補 "tick.max" 0.8 ]
                Expect.equal (エンティティ反応要求.Tick 要求) Int64.MaxValue "上限"

            testCase "空白要求IDを拒否する" <| fun _ ->
                let selected = 選択する [ 基本候補 "blank.id" 0.8 ]
                let errors =
                    エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "") 10L selected
                    |> エラー一覧を得る
                Expect.equal errors [ "エンティティ反応要求IDが空です。" ] "空文字"

            testCase "whitespace要求IDを拒否する" <| fun _ ->
                let selected = 選択する [ 基本候補 "whitespace.id" 0.8 ]
                let errors =
                    エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID " \t") 10L selected
                    |> エラー一覧を得る
                Expect.equal errors [ "エンティティ反応要求IDが空です。" ] "空白文字"

            testCase "負Tickを拒否する" <| fun _ ->
                let selected = 選択する [ 基本候補 "negative.tick" 0.8 ]
                let errors =
                    エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.negative") -1L selected
                    |> エラー一覧を得る
                Expect.equal errors [ "エンティティ反応要求のTickが負です。" ] "負Tick"

            testCase "IDエラーをTickエラーより先に返す" <| fun _ ->
                let selected = 選択する [ 基本候補 "error.order" 0.8 ]
                let errors =
                    エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "") -1L selected
                    |> エラー一覧を得る
                Expect.equal errors.Head "エンティティ反応要求IDが空です。" "固定順"

            testCase "IDとTickの両エラーを返す" <| fun _ ->
                let selected = 選択する [ 基本候補 "all.errors" 0.8 ]
                let errors =
                    エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID " ") -1L selected
                    |> エラー一覧を得る
                Expect.equal errors [ "エンティティ反応要求IDが空です。"; "エンティティ反応要求のTickが負です。" ] "全入力エラー"

            testCase "実行者IDを選択済み値から保存する" <| fun _ ->
                let 要求 = 要求する "request.actor" 10L [ 基本候補 "actor" 0.8 ]
                Expect.equal (エンティティ反応要求.実行者ID 要求) 主体A "選択済み主体"

            testCase "種別がその場警戒になる" <| fun _ ->
                let 要求 = 要求する "request.kind" 10L [ 基本候補 "kind" 0.8 ]
                Expect.equal (エンティティ反応要求.種別 要求) エンティティ反応種別.その場警戒 "明示変換"

            testCase "優先度を選択候補から保存する" <| fun _ ->
                let 要求 = 要求する "request.priority" 10L [ 基本候補 "low" 0.2; 基本候補 "high" 0.9 ]
                Expect.equal (エンティティ反応要求.優先度 要求) 0.9 "再計算しない"

            testCase "対象仮説を選択候補から保存する" <| fun _ ->
                let selected = 候補を作る "hypothesis" 主体A 0.9 "橋が崩れる" 10L 地盤感知
                let 要求 = 要求する "request.hypothesis" 10L [ selected ]
                Expect.equal (エンティティ反応要求.対象仮説 要求) "橋が崩れる" "文字列を保持"

            testCase "由来選択済み行動候補を保存する" <| fun _ ->
                let selected = 選択する [ 基本候補 "selected.origin" 0.8 ]
                let 要求 =
                    エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.origin") 10L selected
                    |> 要求を得る
                let actual = エンティティ反応要求.由来選択済み行動候補 要求
                Expect.equal actual selected "構造的に保持"
                Expect.isTrue (Object.ReferenceEquals(actual, selected)) "再構築しない"

            testCase "由来行動候補を保存する" <| fun _ ->
                let candidate = 基本候補 "action.origin" 0.8
                let 要求 = 要求する "request.action.origin" 10L [ candidate ]
                let actual = エンティティ反応要求.由来行動候補 要求
                Expect.equal actual candidate "選択候補"
                Expect.isTrue (Object.ReferenceEquals(actual, candidate)) "候補を再構築しない"

            testCase "由来行動候補IDを保存する" <| fun _ ->
                let 要求 = 要求する "request.action.id" 10L [ 基本候補 "origin.action.id" 0.8 ]
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "origin.action.id") "由来ID"

            testCase "由来候補一覧を入力順で保存する" <| fun _ ->
                let candidates = [ 基本候補 "third" 0.3; 基本候補 "first" 0.9; 基本候補 "second" 0.6 ]
                let 要求 = 要求する "request.candidates" 10L candidates
                Expect.equal (エンティティ反応要求.由来候補一覧 要求) candidates "入力順"

            testCase "由来選択位置を保存する" <| fun _ ->
                let 要求 = 要求する "request.index" 10L [ 基本候補 "a" 0.2; 基本候補 "b" 0.9; 基本候補 "c" 0.4 ]
                Expect.equal (エンティティ反応要求.由来選択位置 要求) 1 "0始まりindex"

            testCase "由来却下候補一覧を入力順で返す" <| fun _ ->
                let a = 基本候補 "a" 0.2
                let b = 基本候補 "b" 0.9
                let c = 基本候補 "c" 0.4
                let 要求 = 要求する "request.rejected" 10L [ a; b; c ]
                Expect.equal (エンティティ反応要求.由来却下候補一覧 要求) [ a; c ] "選択位置だけ除外"

            testCase "由来同率最高候補一覧を入力順で返す" <| fun _ ->
                let z = 基本候補 "z" 0.8
                let middle = 基本候補 "m" 0.4
                let a = 基本候補 "a" 0.8
                let 要求 = 要求する "request.tied" 10L [ z; middle; a ]
                Expect.equal (エンティティ反応要求.由来同率最高候補一覧 要求) [ z; a ] "同率最高だけ"

            testCase "単独最高選択理由を保持する" <| fun _ ->
                let 要求 = 要求する "request.unique.reason" 10L [ 基本候補 "a" 0.2; 基本候補 "b" 0.9 ]
                Expect.equal (エンティティ反応要求.由来選択理由 要求) 行動候補選択理由.単独最高優先度 "単独最高"

            testCase "同率最高入力順理由を保持する" <| fun _ ->
                let 要求 = 要求する "request.tie.reason" 10L [ 基本候補 "z" 0.8; 基本候補 "a" 0.8 ]
                Expect.equal (エンティティ反応要求.由来選択理由 要求) 行動候補選択理由.同率最高入力順 "同率先着"

            testCase "後方最高候補の選択結果を保持する" <| fun _ ->
                let 要求 = 要求する "request.later.high" 10L [ 基本候補 "a" 0.2; 基本候補 "b" 0.4; 基本候補 "c" 1.0 ]
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "c") "末尾最高"
                Expect.equal (エンティティ反応要求.由来選択位置 要求) 2 "選択位置"

            testCase "先頭同率候補の選択結果を保持する" <| fun _ ->
                let 要求 = 要求する "request.first.tie" 10L [ 基本候補 "z" 0.8; 基本候補 "a" 0.8 ]
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "z") "入力先着"
                Expect.equal (エンティティ反応要求.由来選択位置 要求) 0 "先頭"

            testCase "候補を再選択しない" <| fun _ ->
                let candidates = [ 基本候補 "low" 0.2; 基本候補 "high" 0.9 ]
                let selected = 選択する candidates
                let before = 選択済み行動候補.選択候補 selected
                let 要求 =
                    エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.no.reselect") 10L selected
                    |> 要求を得る
                Expect.equal (エンティティ反応要求.由来行動候補 要求) before "既存選択へ委譲"

            testCase "優先度を再計算しない" <| fun _ ->
                let candidate = 基本候補 "exact.priority" 0.625
                let 要求 = 要求する "request.exact.priority" 10L [ candidate ]
                Expect.equal (エンティティ反応要求.優先度 要求) candidate.優先度 "完全一致"
                Expect.equal (エンティティ反応要求.優先度 要求) candidate.由来警戒意図.警戒度 "警戒度との由来"

            testCase "仮説を変更しない" <| fun _ ->
                let hypothesis = "  文字列を正規化しない  "
                let candidate = 候補を作る "raw.hypothesis" 主体A 0.8 hypothesis 10L 地盤感知
                let 要求 = 要求する "request.raw.hypothesis" 10L [ candidate ]
                Expect.equal (エンティティ反応要求.対象仮説 要求) hypothesis "trimしない"

            testCase "警戒意図を再構築しない" <| fun _ ->
                let candidate = 基本候補 "intent.identity" 0.8
                let 要求 = 要求する "request.intent.identity" 10L [ candidate ]
                let actual = (エンティティ反応要求.由来行動候補 要求).由来警戒意図
                Expect.isTrue (Object.ReferenceEquals(actual, candidate.由来警戒意図)) "由来候補をそのまま保持"

            testCase "候補一覧を再ソートしない" <| fun _ ->
                let candidates = [ 基本候補 "z" 0.2; 基本候補 "a" 0.9; 基本候補 "m" 0.4 ]
                let 要求 = 要求する "request.no.sort" 10L candidates
                Expect.equal (エンティティ反応要求.由来候補一覧 要求 |> List.map _.ID) (candidates |> List.map _.ID) "候補順"

            testCase "却下候補を重複排除しない" <| fun _ ->
                let selected = 基本候補 "selected" 1.0
                let rejectedA = 基本候補 "rejected.a" 0.5
                let rejectedB = { rejectedA with ID = 行動候補ID "rejected.b" }
                let 要求 = 要求する "request.no.distinct" 10L [ selected; rejectedA; rejectedB ]
                Expect.equal (エンティティ反応要求.由来却下候補一覧 要求) [ rejectedA; rejectedB ] "位置だけで除外"

            testCase "同じ内容で異なるIDの候補を保持する" <| fun _ ->
                let first = 基本候補 "same.a" 0.8
                let second = { first with ID = 行動候補ID "same.b" }
                let 要求 = 要求する "request.same.content" 10L [ first; second ]
                Expect.equal (エンティティ反応要求.由来候補一覧 要求) [ first; second ] "二件保持"

            testCase "異なる仮説の候補一覧を保持する" <| fun _ ->
                let first = 候補を作る "hypothesis.a" 主体A 0.4 "仮説A" 10L 地盤感知
                let second = 候補を作る "hypothesis.b" 主体A 0.9 "仮説B" 10L 地盤感知
                let 要求 = 要求する "request.hypotheses" 10L [ first; second ]
                Expect.equal (エンティティ反応要求.由来候補一覧 要求 |> List.map _.対象仮説) [ "仮説A"; "仮説B" ] "分類しない"

            testCase "異なる観測Tickを持つ候補を許可する" <| fun _ ->
                let first = 候補を作る "tick.a" 主体A 0.4 "仮説A" 1L 地盤感知
                let second = 候補を作る "tick.b" 主体A 0.9 "仮説B" 99L 地盤感知
                let 要求 = 要求する "request.observation.ticks" 25L [ first; second ]
                let ticks =
                    エンティティ反応要求.由来候補一覧 要求
                    |> List.map (fun candidate -> candidate.由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head.Tick)
                Expect.equal ticks [ 1L; 99L ] "観測Tickを保持"

            testCase "異なる感知種別を持つ候補を許可する" <| fun _ ->
                let first = 候補を作る "sense.a" 主体A 0.4 "仮説A" 10L 地盤感知
                let second = 候補を作る "sense.b" 主体A 0.9 "仮説B" 10L 聴覚
                let 要求 = 要求する "request.senses" 10L [ first; second ]
                let kinds =
                    エンティティ反応要求.由来候補一覧 要求
                    |> List.map (fun candidate -> candidate.由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head.種別)
                Expect.equal kinds [ 地盤感知; 聴覚 ] "感知種別を保持"

            testCase "プレイヤー種別を推測して拒否しない" <| fun _ ->
                let candidate = 候補を作る "player.actor" プレイヤーID 0.8 "プレイヤーの危険" 10L 地盤感知
                let 要求 = 要求する "request.player.actor" 10L [ candidate ]
                Expect.equal (エンティティ反応要求.実行者ID 要求) プレイヤーID "Entity種別を参照しない"

            testCase "Entity存在を検査しない" <| fun _ ->
                let missing = エンティティID "reaction.actor.not.in.state"
                let candidate = 候補を作る "missing.actor" missing 0.8 "不在Entityの仮説" 10L 地盤感知
                let 要求 = 要求する "request.missing.actor" 10L [ candidate ]
                Expect.equal (エンティティ反応要求.実行者ID 要求) missing "ゲーム状態を受け取らない"

            testCase "入力選択済み値を変更しない" <| fun _ ->
                let candidates = [ 基本候補 "a" 0.2; 基本候補 "b" 0.9 ]
                let selected = 選択する candidates
                let beforeCandidates = 選択済み行動候補.候補一覧 selected
                let beforePosition = 選択済み行動候補.選択位置 selected
                エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.immutable") 10L selected |> ignore
                Expect.equal (選択済み行動候補.候補一覧 selected) beforeCandidates "候補一覧不変"
                Expect.equal (選択済み行動候補.選択位置 selected) beforePosition "選択位置不変"

            testCase "同じ入力から同じ要求を返す" <| fun _ ->
                let selected = 選択する [ 基本候補 "a" 0.2; 基本候補 "b" 0.9 ]
                let first = エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.deterministic") 10L selected
                let second = エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.deterministic") 10L selected
                Expect.equal second first "構造的決定論"

            testCase "要求IDだけを変えると他の由来値は同じ" <| fun _ ->
                let selected = 選択する [ 基本候補 "id.change" 0.8 ]
                let first = エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.a") 10L selected |> 要求を得る
                let second = エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.b") 10L selected |> 要求を得る
                Expect.notEqual (エンティティ反応要求.ID first) (エンティティ反応要求.ID second) "IDだけ異なる"
                Expect.equal (エンティティ反応要求.Tick first) (エンティティ反応要求.Tick second) "Tick同一"
                Expect.equal (エンティティ反応要求.由来選択済み行動候補 first) (エンティティ反応要求.由来選択済み行動候補 second) "由来同一"

            testCase "Tickだけを変えると他の由来値は同じ" <| fun _ ->
                let selected = 選択する [ 基本候補 "tick.change" 0.8 ]
                let first = エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.tick.change") 10L selected |> 要求を得る
                let second = エンティティ反応要求生成.選択済み行動候補から作る (エンティティ反応要求ID "request.tick.change") 25L selected |> 要求を得る
                Expect.notEqual (エンティティ反応要求.Tick first) (エンティティ反応要求.Tick second) "Tickだけ異なる"
                Expect.equal (エンティティ反応要求.ID first) (エンティティ反応要求.ID second) "ID同一"
                Expect.equal (エンティティ反応要求.由来行動候補 first) (エンティティ反応要求.由来行動候補 second) "由来同一"

            testCase "観測Tickと異なる要求Tickを保存する" <| fun _ ->
                let candidate = 候補を作る "different.tick" 主体A 0.8 "Tick差" 10L 地盤感知
                let 要求 = 要求する "request.different.tick" 25L [ candidate ]
                let observationTick = (エンティティ反応要求.由来行動候補 要求).由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head.Tick
                Expect.equal observationTick 10L "観測Tick"
                Expect.equal (エンティティ反応要求.Tick 要求) 25L "要求Tickを推測しない"

            testCase "優先度0を保持する" <| fun _ ->
                let 要求 = 要求する "request.priority.zero" 10L [ 基本候補 "priority.zero" 0.0 ]
                Expect.equal (エンティティ反応要求.優先度 要求) 0.0 "下限"

            testCase "優先度1を保持する" <| fun _ ->
                let 要求 = 要求する "request.priority.one" 10L [ 基本候補 "priority.one" 1.0 ]
                Expect.equal (エンティティ反応要求.優先度 要求) 1.0 "上限"

            testCase "Unicode要求IDを許可する" <| fun _ ->
                let id = エンティティ反応要求ID "要求.警戒.一"
                let selected = 選択する [ 基本候補 "unicode.id" 0.8 ]
                let 要求 = エンティティ反応要求生成.選択済み行動候補から作る id 10L selected |> 要求を得る
                Expect.equal (エンティティ反応要求.ID 要求) id "日本語ID"

            testCase "因果認識経路の複数候補から要求を生成する" <| fun _ ->
                let low = 因果経路候補を作る "reaction.causal.action.low" "reaction.causal.signal.low" 5.0 0.5
                let high = 因果経路候補を作る "reaction.causal.action.high" "reaction.causal.signal.high" 10.0 0.5
                let 要求 = 要求する "request.causal.path" 10L [ low; high ]
                Expect.equal (エンティティ反応要求.由来行動候補 要求) high "既存純粋経路を合成"

            testCase "強い信号由来候補の要求を生成する" <| fun _ ->
                let low = 因果経路候補を作る "reaction.weak" "reaction.signal.weak" 5.0 0.5
                let high = 因果経路候補を作る "reaction.strong" "reaction.signal.strong" 10.0 0.5
                let 要求 = 要求する "request.strong.signal" 10L [ low; high ]
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "reaction.strong") "高優先度"
                Expect.isGreaterThan (エンティティ反応要求.優先度 要求) low.優先度 "強信号由来"

            testCase "因果認識経路の信号IDをProvenanceから辿れる" <| fun _ ->
                let low = 因果経路候補を作る "reaction.path.low" "reaction.path.signal.low" 5.0 0.5
                let high = 因果経路候補を作る "reaction.path.high" "reaction.path.signal.high" 10.0 0.5
                let 要求 = 要求する "request.causal.provenance" 10L [ low; high ]
                Expect.equal (エンティティ反応要求.由来行動候補 要求 |> 候補の信号ID) (伝播信号ID "reaction.path.signal.high") "信号ID"
                Expect.equal (エンティティ反応要求.実行者ID 要求) 観測者ID "観測者から実行者"

            testCase "統合Replay信号の複数候補から要求を生成する" <| fun _ ->
                let _, _, _, _, candidates = Replay接続を作る ()
                let 要求 = 要求する "request.replay" 10L candidates
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "reaction.replay.action.1") "強Replay信号"

            testCase "Replay由来信号IDを保持する" <| fun _ ->
                let _, _, _, signals, candidates = Replay接続を作る ()
                let 要求 = 要求する "request.replay.signal" 10L candidates
                Expect.equal (エンティティ反応要求.由来行動候補 要求 |> 候補の信号ID) signals[1].ID "正式Replay信号を再構築しない"

            testCase "Replay後もゲーム状態不変" <| fun _ ->
                let initial, _, update, _, candidates = Replay接続を作る ()
                let before = update.状態
                要求する "request.replay.state" 10L candidates |> ignore
                Expect.equal update.状態 before "要求生成で変更しない"
                Expect.equal update.状態 initial "同一Tick信号Replay"

            testCase "Replay後もEvent一覧不変" <| fun _ ->
                let _, _, update, _, candidates = Replay接続を作る ()
                let before = update.イベント一覧
                要求する "request.replay.events" 10L candidates |> ignore
                Expect.equal update.イベント一覧 before "Eventを追加しない"
                Expect.equal before [ Tick進行 10L; 衝突発生(発生者ID, 観測者ID) ] "Replay Event順"

            testCase "Replay後も統合台帳不変" <| fun _ ->
                let _, ledger, _, _, candidates = Replay接続を作る ()
                let before = 統合因果台帳.記録一覧 ledger
                要求する "request.replay.ledger" 10L candidates |> ignore
                Expect.equal (統合因果台帳.記録一覧 ledger) before "正式履歴へ追記しない"

            testCase "反応要求生成はEventを生成しない" <| fun _ ->
                let _, _, update, _, candidates = Replay接続を作る ()
                let before = update.イベント一覧
                let request = 要求する "request.no.event" 10L candidates
                Expect.equal update.イベント一覧 before "生成前後でEvent不変"
                Expect.equal (エンティティ反応要求.由来行動候補ID request) (行動候補ID "reaction.replay.action.1") "要求だけを返す"

            testCase "反応要求生成は因果操作を生成しない" <| fun _ ->
                let selected = 選択する [ 基本候補 "no.causal.operation" 0.8 ]
                let result: Result<エンティティ反応要求, string list> =
                    エンティティ反応要求生成.選択済み行動候補から作る
                        (エンティティ反応要求ID "request.no.causal.operation")
                        10L
                        selected
                result |> 要求を得る |> ignore

            testCase "同率候補の先着を要求へ保持する" <| fun _ ->
                let z = 因果経路候補を作る "z.action" "reaction.tie.signal.z" 10.0 0.5
                let a = 因果経路候補を作る "a.action" "reaction.tie.signal.a" 10.0 0.5
                let 要求 = 要求する "request.tie.first" 10L [ z; a ]
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "z.action") "入力先着"
                Expect.equal (エンティティ反応要求.由来選択理由 要求) 行動候補選択理由.同率最高入力順 "理由"

            testCase "ID辞書順を反応要求生成で使わない" <| fun _ ->
                let z = 基本候補 "z.action" 0.8
                let a = 基本候補 "a.action" 0.8
                let 要求 = 要求する "request.no.id.sort" 10L [ z; a ]
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "z.action") "辞書順ではない"
                Expect.equal (エンティティ反応要求.由来候補一覧 要求) [ z; a ] "入力順"

            testCase "要求生成後も却下候補順を維持する" <| fun _ ->
                let a = 基本候補 "a" 0.1
                let b = 基本候補 "b" 0.9
                let c = 基本候補 "c" 0.2
                let d = 基本候補 "d" 0.3
                let 要求 = 要求する "request.rejected.order" 10L [ a; b; c; d ]
                Expect.equal (エンティティ反応要求.由来却下候補一覧 要求) [ a; c; d ] "入力順"

            testCase "100件候補から選択した結果を要求へ保持する" <| fun _ ->
                let candidates =
                    [
                        for index in 0 .. 99 do
                            yield 基本候補 $"hundred.{index}" (if index = 73 then 1.0 else float (index % 10) / 20.0)
                    ]
                let 要求 = 要求する "request.hundred" 100L candidates
                Expect.equal (エンティティ反応要求.由来選択位置 要求) 73 "既存選択位置"
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "hundred.73") "選択候補"
                Expect.equal (エンティティ反応要求.由来候補一覧 要求) candidates "全候補保持"

            testCase "由来選択理由と選択位置が矛盾しない" <| fun _ ->
                let unique = 要求する "request.consistent.unique" 10L [ 基本候補 "a" 0.2; 基本候補 "b" 0.9 ]
                let tied = 要求する "request.consistent.tie" 10L [ 基本候補 "z" 0.8; 基本候補 "a" 0.8 ]
                Expect.equal (エンティティ反応要求.由来選択位置 unique) 1 "単独最高位置"
                Expect.equal (エンティティ反応要求.由来選択理由 unique) 行動候補選択理由.単独最高優先度 "単独理由"
                Expect.equal (エンティティ反応要求.由来選択位置 tied) 0 "同率先着位置"
                Expect.equal (エンティティ反応要求.由来選択理由 tied) 行動候補選択理由.同率最高入力順 "同率理由"

            testCase "要求IDを行動候補IDから自動生成しない" <| fun _ ->
                let candidate = 基本候補 "source.action.id" 0.8
                let 要求 = 要求する "explicit.request.id" 10L [ candidate ]
                Expect.equal (エンティティ反応要求.ID 要求) (エンティティ反応要求ID "explicit.request.id") "呼び出し側ID"
                Expect.equal (エンティティ反応要求.由来行動候補ID 要求) (行動候補ID "source.action.id") "由来IDは別"

            testCase "要求Tickを信号寿命や観測Tickから推測しない" <| fun _ ->
                let candidate = 因果経路候補を作る "reaction.explicit.tick" "reaction.explicit.tick.signal" 10.0 0.5
                let 要求 = 要求する "request.explicit.tick" 25L [ candidate ]
                let sensedTick = candidate.由来警戒意図.由来信念候補.根拠一覧.Head.根拠一覧.Head.Tick
                Expect.equal sensedTick 10L "感知Tick"
                Expect.equal (エンティティ反応要求.Tick 要求) 25L "発行Tick"

            testCase "864反応要求Matrixが決定論的である" <| fun _ ->
                let counts = [ 1; 2; 3; 5; 8; 16 ]
                let ticks = [ 0L; 10L; 100L; Int64.MaxValue ]
                let combinations =
                    [
                        for count in counts do
                            for pattern in 0 .. 5 do
                                for idOrder in 0 .. 2 do
                                    for hypothesisMode in 0 .. 1 do
                                        for tick in ticks do
                                            yield count, pattern, idOrder, hypothesisMode, tick
                    ]

                Expect.equal combinations.Length 864 "250種類以上"

                combinations
                |> List.iteri (fun caseIndex (count, pattern, idOrder, hypothesisMode, tick) ->
                    let candidates = 候補Matrixを作る caseIndex count pattern idOrder hypothesisMode
                    let selected = 選択する candidates
                    let selectedID = selected |> 選択済み行動候補.選択候補 |> _.ID
                    let requestID = エンティティ反応要求ID $"reaction.matrix.request.{caseIndex}"
                    let first = エンティティ反応要求生成.選択済み行動候補から作る requestID tick selected
                    let second = エンティティ反応要求生成.選択済み行動候補から作る requestID tick selected
                    Expect.equal second first $"Matrix {caseIndex}の構造的決定論"

                    let request = first |> 要求を得る
                    Expect.equal (エンティティ反応要求.由来候補一覧 request) candidates $"Matrix {caseIndex}の入力順"
                    Expect.equal (エンティティ反応要求.由来行動候補ID request) selectedID $"Matrix {caseIndex}の選択候補"
                    Expect.equal (エンティティ反応要求.Tick request) tick $"Matrix {caseIndex}の要求Tick")
        ]
