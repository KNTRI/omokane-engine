# Codex Run Report

title: 思兼神Core v0.1 仕様書の追加

generated_at: 2026-06-25T12:05:24+09:00

## Summary

- 思兼神エンジンの中核である思兼神Core v0.1 の仕様書を docs/design/omokane_core_v0_1_spec.md に追加しました。
- 固定tick更新、入力モデル、ゲーム状態、言霊Event、天鳥船Motion、建御雷Battle、権能Assetとの概念接続、禊Test検証方針を文書化しました。
- glossary.md に思兼神Core v0.1、固定tick避けゲー、更新結果、ゲームイベントを追記しました。
- 今回の作業はドキュメント追加のみで、F#実装、ゲームエンジン本体、src/、tests/、Run Report補助スクリプトは変更していません。

## Changed Files

- docs/design/omokane_core_v0_1_spec.md
- docs/design/glossary.md
- docs/codex/runs/2026-06-25_1202_omokane-core-v0-1-spec.md
- docs/codex/runs/latest.md

## Verification Commands

python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
python tools/codex_ops/print_latest_report.py --max-chars 4000
Get-Content .\docs\design\omokane_core_v0_1_spec.md -Raw -Encoding UTF8
Get-Content .\docs\design\glossary.md -Raw -Encoding UTF8
python - <<PY mojibake marker check PY
python tools/codex_ops/copy_latest_report.py

## Verification Result

Run Report補助スクリプトの構文チェック、latest.md表示、思兼神Core v0.1仕様書・用語集のUTF-8読み込み、文字化け断片検出はいずれも成功しました。クリップボード送信も成功し、貼り付け内容に思兼神Core v0.1が含まれ、代表的な文字化け断片が含まれないことを確認しました。

## Errors

- None.

## Warnings

- PowerShellから python - へ日本語リテラルを渡す検査では文字列が壊れる可能性があるため、文字化け検出はUnicodeエスケープを使ったASCIIのみの検査コードで実施しました。
- F#実装、ゲームエンジン本体、src/、tests/、Run Report補助スクリプトは変更していません。

## Next Candidates

- 思兼神Core v0.1 のF#型設計を別作業として開始する。
- 固定tick、入力、状態、言霊Event、禊Test の最小構成を実装前にレビューする。

## Human Confirmation

omokane_core_v0_1_spec.md の範囲が、v0.1の最小中核仕様として妥当か確認してください。
