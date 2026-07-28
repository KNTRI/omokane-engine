# Codex Run Report

title: 思兼神Core 決定論的な因果台帳結果再生

generated_at: 2026-07-28T12:26:15+09:00

## Summary

- Entity位置変更の因果台帳記録を入力順に検証付きで再適用する純粋な再生APIを追加した
- 変更前位置の一致を検証し、台帳と初期状態の分岐を検出する
- 記録Tickへ状態を進め、記録済みEvent列だけを決定論的に再出力する
- 再生失敗時は初期状態を返し、途中状態と途中Eventを公開しない
- 因果台帳追記へTick非減少順序Invariantを追加した
- 入力・AI・権能・乱数を再実行する完全な記紀Replayは実装していない

## Changed Files

- src/Omokane.Core/Domain/CausalLedger.fs
- src/Omokane.Core/Domain/CausalReplay.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/CausalLedgerTests.fs
- tests/Omokane.Core.Misogi/CausalReplayTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-008-observability-replay.md
- docs/adr/ADR-011-causal-ledger-result-replay.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/design/inga_log_shintaku_debug_minimum_spec.md
- docs/design/tick_execution_order.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-07-28_1225_omokane-core-deterministic-causal-replay.md

## Public API

- `因果台帳再生結果`
- `因果台帳再生.再生する`
- 強化した既存API: `因果台帳.追記する`

## Replay Semantics

- 因果台帳記録を入力順に読み、変更前位置の一致を検証して変更後位置を再適用する
- 入力、AI判断、権能評価、因果操作実行器、乱数消費を再実行しない
- 記録済みEventだけを台帳順・記録内順で再出力する
- 一件でも失敗した場合は初期状態を返し、途中状態と途中Eventを公開しない

## Tick Semantics

- 台帳追記では既存末尾から候補入力順までTick非減少を要求する
- 再生時は仮状態のTickを記録Tickへ進める
- 同一TickとTickの飛びを許可する
- 飛ばしたTickの処理や `Tick進行` Eventを自動生成しない

## Test Count

- 124件成功、失敗0
- 既存98件を維持
- 因果台帳結果再生とTick順序の正式禊Testを26件追加

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
git status --short --ignored
git diff --stat

## Verification Result

Core build成功（警告0、エラー0）。Smoke build/run成功。禊Testは124件成功、失敗0（既存98件 + 新規26件）。Run Report補助スクリプト3件のpy_compile成功。外部依存追加なし。NU1900なし。

## Errors

- なし

## Warnings

- 今回の再生は正式記録済みのEntity位置変更結果だけを再適用し、入力・AI・権能・乱数を再実行する完全な記紀Replayではない
- 因果台帳はインメモリであり、ファイル保存・Snapshot・巻き戻しは未実装
- git diff --checkでは既存設定によるLF/CRLF変換予告のみ

## Design Decisions

- Phase 1の再生は入力や判断の再シミュレーションではなく、正式記録結果の検証付き再適用とする
- 変更前位置を分岐検出の事前条件として使用する
- Eventは通知として再出力するだけで、EventからWorld Truthを再構築しない
- 因果操作実行器を再生経路から呼び出さない
- ADR-011をAcceptedとして追加した

## Non-Goals

- 台帳のファイル保存、シリアライズ、セーブデータ統合
- Snapshot、巻き戻し、途中位置からの再生、再生シーク、速度制御
- 入力・AI・権能・乱数の再実行、既存Movementの統合
- 異種因果操作、Event dispatch、結界Lock、天津裁定

## Next Candidates

- 伝播信号から感知値
- 観測から信念候補
- 信念候補から警戒意図
- 地盤振動の技術垂直スライス

## Human Confirmation

変更前位置の不一致を分岐として検出し、成功時だけ最終状態と記録済みEvent列を公開する再生境界を確認する
