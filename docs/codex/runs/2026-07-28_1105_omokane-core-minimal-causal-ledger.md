# Codex Run Report

title: 思兼神Core 因果台帳の最小追記

generated_at: 2026-07-28T11:06:13+09:00

## Summary

- 成功したEntity位置変更の因果記録候補を正式な因果台帳記録へ変換する最小APIを追加した
- インメモリのイミュータブル因果台帳へ入力順で原子的に一括追記する
- 失敗記録、既存台帳との因果操作ID重複、候補一覧内のID重複を拒否する
- 言霊Event、因果台帳、神託Debug、記紀Replayの責務を分離した
- 既存Movement、因果操作実行器、Smokeの挙動を変更していない

## Changed Files

- src/Omokane.Core/Domain/CausalLedger.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/CausalLedgerTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-008-observability-replay.md
- docs/architecture/current_system_mapping.md
- docs/design/inga_log_shintaku_debug_minimum_spec.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-07-28_1105_omokane-core-minimal-causal-ledger.md

## Public API

- `因果台帳記録`
- `因果台帳`
- `因果台帳追記結果`
- `因果台帳.空`
- `因果台帳.記録一覧`
- `因果台帳.件数`
- `因果台帳.追記する`

## Test Count

- 98件成功、失敗0
- 既存75件を維持
- 因果台帳の正式禊Testを23件追加

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

Core build成功（警告0、エラー0）。Smoke build/run成功。禊Testは98件成功、失敗0（既存75件 + 新規23件）。Run Report補助スクリプト3件のpy_compile成功。外部依存追加なし。NU1900なし。

## Errors

- なし

## Warnings

- 因果台帳はインメモリの独立値であり、ゲーム状態・更新結果・セーブデータへは未統合
- git diff --checkでは既存設定によるLF/CRLF変換予告のみ

## Design Decisions

- 因果台帳はWorld Truthに属する正式履歴であり、言霊Event・神託Debug・記紀Replayとは別責務とする
- 全候補を静的検証してから成功記録だけを正式記録へ変換し、既存記録の後ろへ入力順で一括追記する
- 失敗時は入力台帳をそのまま返し、部分追記を公開しない
- 因果操作IDは既存台帳と今回候補一覧の双方で重複を拒否する

## Non-Goals

- ファイル保存、シリアライズ、セーブデータ統合
- Replay、巻き戻し、Snapshot、ハッシュチェーン、暗号署名、台帳圧縮
- 神託Debug、言霊Event型変更、ゲーム状態・更新結果への台帳埋め込み
- 既存Movementの因果操作化、異種因果操作、結界Lock、天津裁定

## Next Candidates

- 決定論的再生
- 伝播信号から感知値
- 観測から信念候補
- 信念候補から警戒意図
- 地盤振動の技術垂直スライス

## Human Confirmation

正式履歴へ成功記録だけが入力順で原子的に追記され、失敗記録とID重複が拒否される境界を確認する
