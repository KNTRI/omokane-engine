# Codex Run Report

title: 思兼神Core 伝播信号から感知値の最小生成

generated_at: 2026-07-28T18:22:12+09:00

## Summary

- 伝播信号から観測者固有の感知値を生成する純粋関数を追加した。
- 明示的な感知設定、安定した直線距離、線形距離減衰、感度倍率、感知閾値、確信度飽和を実装した。
- 量種別・媒体不一致、範囲外、閾値以下を正常な非感知として契約エラーと分離した。
- 伝播信号の発生位置と方向の有限値検証を追加した。
- 正式禊Testを30件追加し、既存124件と合わせて154件が成功した。
- ゲーム状態、観測生成、外部依存、Omokane Namespaceは変更していない。

## Changed Files

- src/Omokane.Core/Domain/Sensing.fs
- src/Omokane.Core/Domain/Propagation.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/SensingTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-012-minimal-signal-sensing.md
- docs/adr/ADR-006-ai-non-omniscience.md
- docs/adr/ADR-009-propagation-entropy-foundations.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/architecture/physical_information_propagation.md
- docs/design/tick_execution_order.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-07-28_1821_omokane-core-minimal-signal-sensing.md

## Public API

- `感知設定`
- `感知設定.検証する`
- `感知値生成.伝播信号から作る`

## Sensing Semantics

- 入力は現在Tick、観測者ID、観測位置、感知設定、伝播信号に限定する。
- ゲーム状態、Entity一覧、発生源Entityの存在、乱数Seedは参照しない。
- 同じ入力から構造的に同じ結果を返す純粋関数とする。

## Attenuation Formula

```text
距離係数 = max 0.0 (1.0 - 距離 / 最大距離)
測定値 = 信号強度 × 感度倍率 × 距離係数
```

最大距離0では、同一位置だけ距離係数1.0として扱う。

## Detection Boundary

- 量種別・媒体不一致は `Ok None`。
- 距離が最大距離以上なら `Ok None`。
- 測定値が感知閾値以下なら `Ok None`。
- 不正入力または非有限な計算結果だけを `Error` とする。

## Test Count

- 既存禊Test: 124件
- 新規感知値生成Test: 30件
- 合計: 154件成功、0失敗、0エラー

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
- 禊Test成功: 154件成功、0失敗、0エラー。
- Run Report補助スクリプト3件のpy_compile成功。
- `git diff --check` 成功。
- NU1900なし。外部依存追加なし。

## Errors

- なし。

## Warnings

- 完全な媒体伝播、遮蔽、遅延、誤差、信号寿命更新、複数信号合成は未実装。

## Design Decisions

- 線形距離減衰をPhase 1の簡易モデルとして採用した。
- 正常な非感知と契約エラーを分離した。
- 方向、伝播方式、寿命Tickは検証対象だが今回の測定式には使わない。
- ADR-006、ADR-009、ADR-012へ実装境界を記録した。

## Non-Goals

- 信号移動、媒体Field、移流、拡散、伝導、波動
- 遮蔽、反射、混合、複数信号合成
- センサー誤差、乱数ノイズ、知覚遅延
- 信号寿命更新、観測生成変更、信念候補生成、AI判断
- Renderer、音響、保存、シリアライズ

## Next Candidates

- 観測から信念候補
- 信念候補から警戒意図
- 地盤振動の技術垂直スライス

## Human Confirmation

線形距離減衰をPhase 1の簡易モデルとして採用し、完全な伝播物理とは区別している。
