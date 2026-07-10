# AGENTS.md

## 思兼神エンジンの基本方針

このプロジェクトはF#製日本語API独自ゲームエンジン「思兼神エンジン」である。

- 中核ルールエンジンは `思兼神Core` と呼ぶ。
- 描画サブシステムは `天照Renderer` と呼ぶ。
- 戦闘サブシステムは `建御雷Battle` と呼ぶ。
- 入力サブシステムは `猿田彦Input` と呼ぶ。
- 移動サブシステムは `天鳥船Motion` と呼ぶ。
- イベントサブシステムは `言霊Event` と呼ぶ。
- テスト・検証サブシステムは `禊Test` と呼ぶ。

ゲームエンジン本体、ゲームルール、固定tick更新、入力処理、当たり判定、既存テストの意味を不用意に変更しない。

## Run Report運用ルール

作業完了時は必ず `docs/codex/runs/latest.md` を更新する。

Run Reportには以下を含める。

- Summary
- Changed Files
- Verification Commands
- Verification Result
- Errors
- Warnings
- Next Candidates
- Human Confirmation

`Verification Commands` は1コマンド1行で書く。

作業完了時は必ず `latest.md` のクリップボード送信を試行する。
クリップボード送信に失敗した場合は、`latest.md` の内容または要約を最終返信に貼る。
クリップボード送信に失敗した場合は、手動コピー用PowerShellコマンドを報告する。

```powershell
Get-Content .\docs\codex\runs\latest.md -Raw -Encoding UTF8 | Set-Clipboard
```

スクリーンショット共有を前提にしない。
文字化け防止のため、Run Report見出しはASCII英語固定にする。

Pythonでは `encoding="utf-8"` を明示する。
PowerShellでは `Get-Content -Raw -Encoding UTF8` を使う。

外部依存パッケージを追加しない。
依存追加、project file変更、lockfile変更、install系コマンド実行が必要になった場合は、事前に理由と差分を報告する。

## 世界知能カーネル統合ルール

新規機能は、`World Truth`、`Observation`、`Presentation` のどこに属するかを設計またはコードレビューで明記する。

永続的な世界変更は、因果操作または同等のTransactionを通る経路を明記する。
AIが参照する情報は、世界真実から物理情報伝播、感知値、観測、信念へ至る生成経路を明記し、世界真実の直接参照を標準経路にしない。

高コスト機能は、LOD、性能予算、保存、再現、神託Debugによる確認方法を同時に設計する。
LODでは細部を省略しても、主要因果、総量、方向、危険、秩序、観測不確実性、将来への影響を保持する。

既存仕様との衝突は黙って上書きせず、`docs/adr/` の神議ADRへ記録する。
既存の `Omokane.*` Namespace、project名、ファイル名は今回 `Omoikane.*` へ改名しない。

上位アーキテクチャは `docs/architecture/index.md` を入口とし、アーキテクチャ不変条件は `docs/architecture/architecture_invariants.md` を正とする。

今回のRun Report補助機構の追加では、ゲーム本体の仕様変更を行わない。

## 企画書・命名規則・運用ルール

このプロジェクトはF#製日本語API独自ゲームエンジン「思兼神エンジン」である。
英数字表記は `OmokaneEngine` とする。

- 中核ルールエンジンは `思兼神Core` と呼ぶ。
- 描画サブシステムは `天照Renderer` と呼ぶ。
- 戦闘サブシステムは `建御雷Battle` と呼ぶ。
- 入力サブシステムは `猿田彦Input` と呼ぶ。
- 移動サブシステムは `天鳥船Motion` と呼ぶ。
- カメラ・表示変換サブシステムは `八咫鏡View` と呼ぶ。
- シーン遷移サブシステムは `天岩戸Scene` と呼ぶ。
- アセット管理サブシステムは `稲荷Asset` と呼ぶ。
- イベントサブシステムは `言霊Event` と呼ぶ。
- セーブ・復元サブシステムは `御魂State` と呼ぶ。
- テスト・検証サブシステムは `禊Test` と呼ぶ。

ゲームエンジン本体、ゲームルール、固定tick更新、入力処理、当たり判定、既存テストの意味を不用意に変更しない。

作業完了時は必ず既存のRun Report機構を使い、`docs/codex/runs/latest.md` を更新する。
作業完了時は必ずクリップボード送信を試行する。
スクリーンショット共有を前提にしない。

Pythonでは `encoding="utf-8"` を明示する。
PowerShellでは `Get-Content -Raw -Encoding UTF8` を使う。

外部依存パッケージを追加しない。
依存追加、project file変更、lockfile変更、install系コマンド実行が必要になった場合は、事前に理由と差分を報告する。
