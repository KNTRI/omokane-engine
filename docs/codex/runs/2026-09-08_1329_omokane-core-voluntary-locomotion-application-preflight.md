# Codex Run Report

title: 互換移動命令のGameState適用前検証 v0.1

generated_at: 2026-09-08T13:29:25+09:00

## Summary

互換移動命令のGameState適用前検証 v0.1を実装。現在判断を再導出し、検証した不変状態と非空命令をprivate準備へ保持する。Movementは実行しない。

## Changed Files

- src/Omokane.Core/Domain/VoluntaryLocomotionApplicationPreflight.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/VoluntaryLocomotionPreflightTestData.fs
- tests/Omokane.Core.Misogi/VoluntaryLocomotionApplicationPreflightTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-003-causal-transaction.md
- docs/adr/ADR-025-reaction-aware-voluntary-locomotion-control.md
- docs/adr/ADR-026-voluntary-locomotion-intent.md
- docs/adr/ADR-027-voluntary-locomotion-intent-gate.md
- docs/adr/ADR-028-voluntary-locomotion-compatibility-adapter.md
- docs/adr/ADR-029-voluntary-locomotion-application-preflight.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/architecture/world_intelligence_kernel.md
- docs/design/voluntary_locomotion_application_preflight.md
- docs/design/voluntary_locomotion_compatibility_adapter.md
- docs/design/voluntary_locomotion_intent_and_gate.md
- docs/design/reaction_state_locomotion_read_boundary.md
- docs/design/procedural_sandbox_minimum_model.md
- docs/design/tick_execution_order.md
- docs/design/glossary.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-09-08_1329_omokane-core-voluntary-locomotion-application-preflight.md

## Public API

`互換移動適用準備` と13アクセサ、`互換移動適用前検証.単一を準備する` / `一括を準備する`。GameStateを先、命令を後に取る。

## Architecture Audit

基準main adca8cea9c200d05e3058feef41b4fe3f3597235、remote一致、初期tracked cleanを確認。既存Input・Entity・GameState・意図・Gate・互換命令・Movement、既存禊/Smoke、ADR-003/023/025-028、設計文書を監査。現行MovementはプレイヤーIDかつプレイヤー種別の左右Inputだけを位置へ適用し、終了前の空InputでもTickが進む。

## Persistent Causal World Alignment

添付背景の独立規則合成と権威/派生値分離に整合。Observationから続く実行計画の適用前境界であり、新しいWorld TruthやPresentationではない。

## Existing Movement Risk

主体なしInputの無条件実行、同Tick警戒成立後の古い許可命令、検証状態と適用状態の取り違えを防ぐための境界。

## Preflight Model

private DUは対象GameState、対象Entity、現在判断、非空命令一覧を保持。外部構築はF#コンパイラFS1093で拒否することを確認。

## GameState Snapshot

検証に渡した状態と対象Entityをそのまま保存。別の後続状態は準備に逆流しない。単回使用や現在世界へのcommit保証は未実装。

## Empty Batch

`Ok None`。負Tick、空主体、不在、終了、反応重複でもGameStateを検証しない。空準備やダミーInputなし。

## Current Control Revalidation

既存判断生成APIへ委譲。存在・一意性・反応一覧・未来成立Tickの規則を複製せず、既存順序に `現在の自発移動制御判断: ` を付ける。

## Stale Command Rejection

命令生成後に同じTickで警戒が成立したlive状態・Replay状態の双方で、古い命令を抑制Errorとして拒否。

## Game End Boundary

ゲームオーバーとクリアを拒否。終了状態を読み取り判断の抑制理由へ混ぜない。

## Player Entity Boundary

既存判断成功で存在・一意性を保証したEntityにプレイヤー種別を追加要求。別IDのプレイヤー種別は許可。

## Actor Boundary

全命令主体がGameState.プレイヤーIDと一致することを要求。非プレイヤー命令を昇格させない。

## Tick Boundary

全命令Tickと現在Tickの完全一致を要求。古い/未来命令を補正しない。0とInt64.MaxValueも準備可能。

## Control Decision Boundary

現在許可と由来判断の構造的一致を確認。導出失敗時は種別・抑制・比較を省略。現在抑制時と当該命令の主体/Tick不一致時は二次比較エラーを省略。

## Intent ID Invariant

全命令の由来意図ID重複を拒否。同方向・異IDは保持。

## Single Preflight

一件成功では準備を返す。一件一括と同じ非空検証を使い、成功・失敗とも同値。

## Batch Preflight

非空成功はOk(Some準備)。全件の検証完了後だけ一つの準備を公開。

## Atomicity

後方主体/Tick不正やID重複でも部分準備を返さない。既存判断、終了、種別、全Tick、全主体、抑制、由来比較、重複の固定順で全エラー収集。

## Order Preservation

命令とInputの順序を保存。sort、distinct、group化、同方向圧縮、左右相殺なし。

## Physical State Boundary

位置・速度・HP・所有者・当たり判定を適格性へ使わず変更しない。異なる物理値でも現在許可判断が同じなら受理し現在snapshotを保持。

## World Truth Boundary

準備は非権威の短命値。GameState、Entity、Tick、Seed、反応状態を変更しない。

## Input Boundary

Input.fs無変更。検証後の既存入力一覧は命令の既存アクセサへ委譲。

## Movement Boundary

Systems/Omokane.fs無変更。本番からMovementを呼ばず、旧経路は意図的に未接続。

## Event And Causal Boundary

Event・因果操作・更新結果を生成せず、統合因果台帳とReplayは無変更。

## Legacy Movement Compatibility

Test内だけで準備の状態とInputを旧Movementへ渡し、左、右、右右左の直接Input結果と構造的等価性を確認。

## Full Cognitive Path

正式地盤振動因果→台帳→Replay正式信号→認識→候補選択→反応要求→反応因果→状態→判断→Gate→互換変換→preflightを確認。反応前は準備成功、後は新命令なし/古い命令拒否。現在抑制判断から感知・信号・因果まで由来鎖を保持。

## Replay Integration

liveとReplayで古い命令の拒否結果が一致。GameState、Entity、Event、正式信号、台帳の不変を確認。

## Determinism Audit

追加本番コードのDateTime.Now/UtcNow、Guid.NewGuid、Random、Task.Run、Parallel、Thread、Environment.TickCount、Stopwatch、Dictionary、HashSetは検索一致0。sort/distinct/groupBy、物理値参照、Movement参照も0。List.countByはID重複判定のみで出力順に不使用。正常Matrix 3,072、不正Matrix 80。

## Test Count

既存929件を無変更で維持、新規102件、最終1,031件。Matrixは各1 testCase内で反復し、ケース数を正式Test数へ水増ししない。

## Repeated Verification

| Run | Total | Passed | Ignored | Failed | Errored |
|---|---:|---:|---:|---:|---:|
| 1 | 1031 | 1031 | 0 | 0 | 0 |
| 2 | 1031 | 1031 | 0 | 0 | 0 |
| 3 | 1031 | 1031 | 0 | 0 | 0 |
| 4 | 1031 | 1031 | 0 | 0 | 0 |
| 5 | 1031 | 1031 | 0 | 0 | 0 |

## Verification Commands

```powershell
dotnet build .\src\Omokane.Core\Omokane.Core.fsproj
dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet build .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
dotnet run --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
dotnet list .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj package --no-restore
git diff --check
git status --short --ignored
git diff --stat
dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
dotnet fsi --readline-
python tools/codex_ops/copy_latest_report.py
```
`--no-build --no-restore` は5回連続実行。FSIは外部private構築の拒否確認。

## Verification Result

Core/Smoke/Misogi build/run成功、各build警告0・エラー0、NU1900なし。py_compile三件成功、依存追加なし（Expecto 11.1.0とSDK由来FSharp.Coreのみ）。UTF-8/既存CRLF維持、diff --check成功。

## Errors

実装・回帰検証の失敗なし。FSIのFS1093はprivate構築拒否を確認する期待結果。初回FSI入力は評価終端不足で未評価だったため、明示終端付きで再確認した。

## Warnings

build警告なし。作業途中のGit LF/CRLF警告は変更ファイルだけを既存CRLFへ正規化し解消。

## Design Decisions

敵対的レビューで契約違反なし。状態再導出、非空private構築、snapshot保持、固定順全検証、抑制優先、原子性、物理非依存を確認。現行許可判断では主体/Tick一致後の由来判断だけの不一致は公開APIで到達不能。防御比較は残し、偽造APIやreflectionでテストしない。ADR-029 Accepted、ADR-003 Proposed維持。

## Non-Goals

Movement実行、状態変更、上下/任意Entity移動、Input変更、物理値変更、相殺/合成、因果・Event・履歴生成、Replay変更、旧経路廃止なし。

## Deferred Work

単回消費、期限、反応解除、継続意図、Field/SDF/Navigation/Renderer、保存・ネットワークは未実装。

## Next Candidates

1. 準備だけを受け取るMovement適用Adapter。
2. プレイヤー左右命令の新経路実適用。
3. 旧Input直結経路との更新結果等価性。
4. 警戒による自発移動抑制の実接続。
5. 旧Input経路の段階的隔離。
6. 上下対応または未対応の正式継続。
7. 警戒状態解除。
8. 最小Field契約。
技術的には1を優先する。別GameStateを再受領せず、検証snapshotを適用する契約を先に固定する。今回は実装しない。

## Human Confirmation

branch: feature/voluntary-locomotion-application-preflight。今回の24ファイルのみ限定stageし、Add voluntary locomotion application preflight の単一commit対象とする。push/PR/feature mergeなし（main同期の明示許可されたff-only確認のみ）。実適用は未接続である点と次段のsnapshot利用責務を人間が確認する。
