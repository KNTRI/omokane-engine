# Codex Run Report

title: 思兼神Core 警戒意図から最小行動候補の生成

generated_at: 2026-08-09T09:44:35+09:00

## Summary

- 警戒意図から実行前の行動候補を生成する純粋関数を追加した
- Phase 1の行動候補種別はその場警戒1種類だけとした
- 警戒意図と由来信念候補、行動候補と由来警戒意図の整合性を公開検証で防御した
- 行動候補IDを明示入力し、実行者、優先度、仮説、由来意図を加工せず保持した
- 正式禊Testを27件追加し、既存237件を含む全264件が成功した
- 候補選択、行動実行、Entity・ゲーム状態更新、因果操作生成、外部依存追加は行っていない

## Changed Files

- src/Omokane.Core/Domain/ActionCandidate.fs
- src/Omokane.Core/Domain/AlertIntent.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/AlertActionCandidateTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-016-minimal-alert-action-candidate.md
- docs/adr/ADR-006-ai-non-omniscience.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/architecture/world_intelligence_kernel.md
- docs/design/tick_execution_order.md
- docs/design/glossary.md
- docs/design/procedural_sandbox_minimum_model.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-08-09_0944_omokane-core-minimal-alert-action-candidate.md

## Public API

- `警戒意図.検証する : 警戒意図 -> Result<unit, string list>`
- `行動候補.検証する : 行動候補 -> Result<unit, string list>`
- `行動候補生成.警戒意図から作る : 行動候補ID -> 警戒意図 -> Result<行動候補, string list>`

## Alert Intent Validation

- 警戒意図の観測者、仮説、警戒度と由来信念候補を固定順で検証する
- 根拠・反証観測の順序を維持し、観測者ID、仮説、確率との由来整合性を防御する
- 既存の警戒意図生成は正常系を維持し、生成後に公開検証を通す

## Action Candidate Semantics

- 明示されたIDを使い、`その場警戒` 候補を1件だけ生成する
- 実行者ID、優先度、対象仮説は警戒意図から再計算せず保存する
- 行動候補は選択・実行・因果操作化される前の一時的な候補である

## Provenance Invariants

- 行動候補の実行者IDは由来警戒意図の観測者IDと一致する
- 行動候補の対象仮説は由来警戒意図の対象仮説と一致する
- 行動候補の優先度は由来警戒意図の警戒度と一致する
- 由来警戒意図と由来信念候補を構造的に保持する

## Test Count

- 新規行動候補Test: 27件
- 禊Test総数: 264件

## Verification Commands

dotnet build .\src\Omokane.Core\Omokane.Core.fsproj
dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet build .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
dotnet run --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
git diff --check
python -c <UTF-8 mojibake marker check>

## Verification Result

- Core build、Smoke build/run、Misogi build/runが成功した
- 新規27件を含む全264件の禊Testが成功した
- 警告0、エラー0、NU1900なし、外部依存追加なしを確認した
- Run Report補助スクリプトの構文確認とgit diff --checkが成功した

## Errors

- なし

## Warnings

- Phase 1はその場警戒候補1種類だけを生成し、候補選択、実行、ヒステリシス、因果操作化は未実装

## Design Decisions

- 行動候補IDは呼び出し側が明示し、時刻、GUID、乱数へ依存しない
- 候補生成と候補選択、実行、因果操作化を分離する
- 優先度は警戒度をそのまま使用し、この段階で評価し直さない
- `Omokane.*` Namespaceと既存のMovement・入力契約を維持する

## Non-Goals

- 複数行動候補、候補選択、候補評価、ヒステリシス
- 停止、移動、迂回、退避、攻撃、経路探索
- 入力生成、Entity・ゲーム状態更新、因果操作生成
- Event発行、因果台帳追記、保存、シリアライズ、Renderer接続

## Next Candidates

- 地盤振動発生の因果操作接続
- 行動候補の最小選択
- 選択された候補からNPC反応の状態反映

## Human Confirmation

行動候補はWorld Truth、入力、行動結果、因果操作ではなく、生成だけではEntityとゲーム状態を変更しない
