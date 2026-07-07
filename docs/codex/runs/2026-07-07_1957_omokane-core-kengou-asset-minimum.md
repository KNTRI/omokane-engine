# Codex Run Report

title: 思兼神Core v0.1 権能Asset最小型

generated_at: 2026-07-07T19:59:28+09:00

## Summary

- 権能Assetの最小型を追加した
- 権能ID、権能タグ、権能Assetを追加した
- タグ確認補助関数を追加した
- 権能Assetの最小検証関数を追加した
- Smokeでタグ確認と最小検証を確認した
- 権能実行、条件判定、評価、結界Lock、天津裁定、因果Log、神託Debugは実装していない

## Changed Files

- src/Omokane.Core/Domain/KengouAsset.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Smoke/Program.fs
- docs/codex/runs/latest.md
- docs/codex/runs/2026-07-07_1957_omokane-core-kengou-asset-minimum.md

## Verification Commands

dotnet build .\src\Omokane.Core\Omokane.Core.fsproj
dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
python tools/codex_ops/print_latest_report.py --max-chars 4000
python -c <mojibake marker check>

## Verification Result

Core build, Smoke build, Smoke run, Run Report helper compile, latest report print, and mojibake marker check succeeded. Smoke output included Movement and KengouAsset OK messages.

## Errors

- None.

## Warnings

- NU1900 warning occurred because NuGet vulnerability metadata could not be loaded from https://api.nuget.org/v3/index.json; no external packages were added

## Next Candidates

- 権能Asset最小型のコミット
- 権能Assetの発行Event確認または権能候補収集の最小設計

## Human Confirmation

Clipboard send will be attempted after final verification
