# Codex Run Report

title: 思兼神エンジン企画書・命名規則・AGENTS運用ルールの追加

generated_at: 2026-06-25T10:52:03+09:00

## Summary

- 思兼神エンジンの企画書、設計方針、日本語命名ルール、用語集を docs/design/ に追加しました。
- AGENTS.md には既存内容を壊さず、OmokaneEngine表記、サブシステム命名規則、既存Run Report運用を使う方針を末尾追記しました。
- Run Report + Clipboard共有機構は再実装せず、既存の tools/codex_ops/ と scripts/copy_latest_run_report.ps1 を尊重しました。
- 今回の作業はドキュメントとCodex運用ルールのみで、src/、tests/、ゲーム本体、F#実装、既存テストには触れていません。

## Changed Files

- AGENTS.md
- docs/design/思兼神エンジン企画書.md
- docs/design/思兼神エンジン設計方針.md
- docs/design/日本語命名ルール.md
- docs/design/用語集.md
- docs/codex/runs/2026-06-25_1050_project-brief-naming-agents.md
- docs/codex/runs/latest.md

## Verification Commands

python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
python tools/codex_ops/print_latest_report.py --max-chars 4000
python tools/codex_ops/copy_latest_report.py

## Verification Result

既存Run Report補助スクリプトの構文チェック、latest.md のUTF-8表示、クリップボード送信試行はいずれも成功しました。

## Errors

- None.

## Warnings

- Run Report + Clipboard共有機構は再実装していません。
- ゲーム本体、F#ソース、固定tick更新、描画、戦闘、入力、既存テストの仕様変更は行っていません。

## Next Candidates

- 思兼神Core v0.1 の最小実装へ進む場合は、今回追加した設計方針と日本語命名ルールに従って別作業として開始してください。
- F#プロジェクト本体が追加された後は、dotnet test の結果もRun Reportへ記録してください。

## Human Confirmation

追加した docs/design/ の企画書・設計方針・命名ルール・用語集を確認し、今後の実装作業の前提として扱ってよいか確認してください。
