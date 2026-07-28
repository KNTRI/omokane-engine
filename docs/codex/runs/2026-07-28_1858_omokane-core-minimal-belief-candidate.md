# Codex Run Report

title: 思兼神Core 観測から信念候補の最小生成

generated_at: 2026-07-28T18:59:01+09:00

## Summary

- 観測から決定論的な信念候補を生成する純粋関数を追加した。
- 仮説、事前確率、根拠観測、反証観測を明示入力とした。
- 同一観測者を検証し、異なるTickと感知種別の観測を統合可能にした。
- 観測確信度をPhase 1の軟証拠量として集約した。
- 正式禊Testを27件追加し、既存154件と合わせて181件が成功した。
- 信念採択、永続状態、忘却、時間減衰、AI判断、外部依存追加は行っていない。

## Changed Files

- src/Omokane.Core/Domain/Belief.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/BeliefCandidateTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-013-minimal-belief-candidate.md
- docs/adr/ADR-006-ai-non-omniscience.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/architecture/world_intelligence_kernel.md
- docs/design/tick_execution_order.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-07-28_1858_omokane-core-minimal-belief-candidate.md

## Public API

- `信念候補生成.観測から作る`
- 型: `string -> float -> 観測 list -> 観測 list -> Result<信念候補, string list>`

## Belief Semantics

- 仮説、事前確率、根拠観測、反証観測を呼び出し側が明示する。
- 観測概要を解析して根拠・反証を自動分類しない。
- 異なるTickと感知種別を許可し、入力順と重複を維持する。
- 候補生成だけを行い、採択、永続化、忘却、AI判断は行わない。

## Probability Formula

```text
根拠総量 = 根拠観測の確信度の合計
反証総量 = 反証観測の確信度の合計
確率 = (事前確率 + 根拠総量) / (1.0 + 根拠総量 + 反証総量)
```

この確率はPhase 1の決定論的な軟証拠集約であり、完全なベイズ事後確率ではない。

## Observer Boundary

- 根拠一覧、反証一覧の順に連結した全観測の観測者ID一致を要求する。
- ゲーム状態、世界真実、Entity一覧、因果台帳、時刻、乱数は参照しない。
- 観測者IDは新しいフィールドを増やさず、保存された観測から追跡する。

## Test Count

- 既存禊Test: 154件
- 新規信念候補生成Test: 27件
- 合計: 181件成功、0失敗、0エラー

## Verification Commands

dotnet build .\\src\\Omokane.Core\\Omokane.Core.fsproj
dotnet build .\\tests\\Omokane.Core.Smoke\\Omokane.Core.Smoke.fsproj
dotnet run --project .\\tests\\Omokane.Core.Smoke\\Omokane.Core.Smoke.fsproj
dotnet build .\\tests\\Omokane.Core.Misogi\\Omokane.Core.Misogi.fsproj
dotnet run --project .\\tests\\Omokane.Core.Misogi\\Omokane.Core.Misogi.fsproj
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
git diff --check

## Verification Result

- Core build成功: 警告0、エラー0。
- Smoke成功: Movement、KengouAsset、WorldIntelligence Contracts、Validation正常。
- 禊Test成功: 181件成功、0失敗、0エラー。
- Run Report補助スクリプト3件のpy_compile成功。
- `git diff --check` 成功。
- NU1900なし。外部依存追加なし。

## Errors

- なし。

## Warnings

- 算出確率はPhase 1の決定論的な軟証拠集約であり、完全なベイズ事後確率ではない。

## Design Decisions

- 根拠・反証の分類を明示入力とした。
- 事前確率を強さ1.0として観測確信度を軟証拠量へ加えた。
- 同一観測者なら異なるTickと感知種別を統合可能とした。
- 入力順を保存し、重複観測を入力回数どおり集約した。
- ADR-006とADR-013へ実装境界を記録した。

## Non-Goals

- 自然言語解析、観測概要の自動分類、ベイズネットワーク
- 信念候補の採択・優先順位付け、既存信念との統合
- 永続的な信念状態、忘却、時間減衰、矛盾解消
- 観測の重複排除、観測ID、AI判断、警戒意図
- ゲーム状態への埋め込み、保存、シリアライズ、Renderer接続

## Next Candidates

- 信念候補から警戒意図
- 地盤振動の技術垂直スライス

## Human Confirmation

根拠・反証の分類は呼び出し側が明示し、観測概要の自動解析を行わない。
