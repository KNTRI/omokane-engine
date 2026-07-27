# Codex Run Report

title: 思兼神Core 正式禊Test基盤と観測生成テスト

generated_at: 2026-07-27T18:36:32+09:00

## Summary

- Expectoを正式な禊Testフレームワークとして採用した
- Omokane.Core.Misogiを追加した
- 観測生成の正常系、境界値、不一致、決定論、エラー順序を検証した
- 既存Smokeは通電確認として維持した
- 直接追加した外部依存はExpectoだけである
- Expecto.TestSdk、FsUnit、FsCheck、xUnitは追加していない
- 既存公開型、Movement、KengouAsset、WorldIntelligence Contractsの挙動は変更していない

## Changed Files

- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/ObservationGenerationTests.fs
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-010-expecto-misogi-test-framework.md
- docs/architecture/index.md
- docs/design/misogi_test_policy.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-07-27_1836_formal-misogi-observation-tests.md

## Verification Commands

git status --short --ignored
git log --oneline -3
dotnet build .\src\Omokane.Core\Omokane.Core.fsproj
dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet build .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
dotnet run --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
dotnet list .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj package
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
python tools/codex_ops/print_latest_report.py --max-chars 4000
python -c <UTF-8 mojibake marker check>
git diff --check
git status --short --ignored
git diff --stat
git diff --cached --stat
git diff --cached
git commit -m Add formal Misogi tests for observation generation
git log --oneline -1
git status --short --ignored
python tools/codex_ops/copy_latest_report.py

## Verification Result

Core build、Smoke build/run、Misogi build/run、パッケージ確認、Run Report補助スクリプト確認に成功した。Expecto 11.1.0で観測生成テスト23件がすべて成功し、既存Smokeの4成功表示も維持した。

## Errors

- None.

## Warnings

- 世界状態不変とEvent未発行は、対象APIが世界状態を受け取らずResult<観測, string list>だけを返す型境界で保証されるため、Eventバスのモックは追加していない。
- 既存Smokeの観測生成通電確認は今回維持した。将来、禊Testとの重複を安全に縮小できる。
- Expecto.TestSdkとCI統合は未導入であり、当面はdotnet run --projectで実行する。

## Next Candidates

- Smoke内の重複仕様確認を安全に縮小
- 因果操作最小実行器の仕様
- 伝播信号から感知値への最小変換

## Human Confirmation

全検証成功後、意図した禊Test・ADR・文書・Run Reportだけをコミットし、latest.mdのClipboard送信を試行する。
