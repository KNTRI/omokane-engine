# Codex Run Report

title: 思兼神Core v0.1 禊Smoke project scaffold

generated_at: 2026-06-25T13:10:20+09:00

## Summary

- Added a dependency-free 禊Smoke console project for 思兼神Core v0.1.
- The Smoke project references Omokane.Core and constructs values for input, space, entity, game state, 言霊Event, update result, and validation result.
- No game update, collision, movement, damage, 権能Asset execution, Renderer connection, or device input logic was implemented.

## Changed Files

- tests/Omokane.Core.Smoke/Omokane.Core.Smoke.fsproj
- tests/Omokane.Core.Smoke/Program.fs
- docs/codex/runs/latest.md
- docs/codex/runs/2026-06-25_1302_omokane-core-smoke.md
- docs/codex/runs/2026-06-25_1303_omokane-core-smoke.md

## Verification Commands

dotnet build .\src\Omokane.Core\Omokane.Core.fsproj
dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
python tools/codex_ops/print_latest_report.py --max-chars 4000
python - <<'PY' ... mojibake marker check ... PY
python tools/codex_ops/copy_latest_report.py

## Verification Result

- Omokane.Core build succeeded.
- Omokane.Core.Smoke build succeeded.
- Smoke run succeeded and printed 思兼神Core Smoke OK, Inputs: 2, Tick: 0, Entities: 1, Events: 1, Validation: 正常.
- Python Run Report helper scripts compiled successfully.
- print_latest_report.py printed latest.md with readable 日本語.
- Mojibake marker check found no markers in the Smoke project files or latest.md.
- An intermediate Run Report was corrected to use 禊Smoke consistently.
- copy_latest_report.py copied latest.md to the clipboard successfully.

## Errors

- None.

## Warnings

- dotnet build/run emitted NU1900 because NuGet vulnerability metadata could not be loaded from https://api.nuget.org/v3/index.json.
- No external NuGet package was added; no xUnit, Expecto, or FsUnit project was introduced.
- Existing src/Omokane.Core type definitions were not changed.
- Generated bin/obj and __pycache__ directories remain because the cleanup escalation was unavailable; these are build/cache outputs, not source changes.

## Next Candidates

- Add formal 禊Test project only when a test framework decision is made.
- Implement the first fixed-tick update function in 思兼神Core as a separate task.

## Human Confirmation

Please confirm that the 禊Smoke project is sufficient as a pre-test-framework wiring check.
