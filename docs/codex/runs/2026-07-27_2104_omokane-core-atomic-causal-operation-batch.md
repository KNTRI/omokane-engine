# Codex Run Report

title: 思兼神Core 因果操作列の原的一括適用

generated_at: 2026-07-27T21:04:35+09:00

## Summary

- 複数のEntity位置変更操作を原子的に一括適用するAPIを追加した
- 全件静的検証後に入力順で仮適用する
- 全件成功または全件失敗とし、静的契約不正とゲーム内失敗を分離した
- 因果操作ID重複を契約不正として拒否する
- 成功時だけEventと記録一覧を公開する
- 正式禊Testを26件追加した
- 既存単一実行器・Movement・Smokeを変更していない

## Changed Files

- src/Omokane.Core/Domain/CausalExecution.fs
- tests/Omokane.Core.Misogi/CausalOperationBatchExecutionTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/architecture/current_system_mapping.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-07-27_2104_omokane-core-atomic-causal-operation-batch.md

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

Core build成功（警告0、エラー0）。Smoke build/run成功（既存4成功表示を維持）。Misogi build/run成功（75件成功、失敗0: 既存49件 + 新規26件）。Run Report補助スクリプト3件のpy_compile成功。git diff --checkで空白エラーなし。

## Errors

- なし

## Warnings

- git diff --checkで既存の改行コード設定に由来するLF/CRLF変換予告あり
- 失敗時は仮成功記録を公開せず、詳細な仮適用経路は将来の神託Debugへ送る
- 因果台帳、再生、結界Lock、天津裁定は未実装

## Next Candidates

- 因果台帳への最小記録
- 異種因果操作を扱う共通操作契約
- 結界Lockと天津裁定を通す競合解決仕様

## Human Confirmation

一括適用の原子性、同一対象の入力順上書き、失敗時の公開境界を確認する
