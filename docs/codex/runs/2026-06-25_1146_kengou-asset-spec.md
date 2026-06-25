# Codex Run Report

title: 思兼神エンジン 権能Asset仕様書の追加

generated_at: 2026-06-25T11:48:26+09:00

## Summary

- 思兼神エンジンの設計文書として、docs/design/kengou_asset_spec.md に権能Asset仕様書を追加しました。
- 権能Assetを、意味・条件・能力・イベント接続・検証規則を持つF#向けのルール付きアセットとして整理しました。
- glossary.md に権能Asset、通常Asset、使用条件、発動条件、権能、依存Asset、イベント接続、検証規則を追記しました。
- japanese_naming_rules.md に、スマートアセット相当の概念を権能Assetと呼び、御魂Assetは使わない方針を追記しました。
- 今回の作業はドキュメント追加のみで、F#実装、ゲームエンジン本体、src/、tests/、Run Report補助スクリプトは変更していません。

## Changed Files

- docs/design/kengou_asset_spec.md
- docs/design/glossary.md
- docs/design/japanese_naming_rules.md
- docs/codex/runs/2026-06-25_1146_kengou-asset-spec.md
- docs/codex/runs/latest.md

## Verification Commands

python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
python tools/codex_ops/print_latest_report.py --max-chars 4000
Get-Content .\docs\design\kengou_asset_spec.md -Raw -Encoding UTF8
Get-Content .\docs\design\glossary.md -Raw -Encoding UTF8
Get-Content .\docs\design\japanese_naming_rules.md -Raw -Encoding UTF8
python - <<PY mojibake marker check PY
python tools/codex_ops/copy_latest_report.py

## Verification Result

Run Report補助スクリプトの構文チェック、latest.md表示、権能Asset仕様書・用語集・命名ルールのUTF-8読み込み、文字化け断片検出はいずれも成功しました。クリップボード送信も成功し、貼り付け内容に思兼神エンジンと権能Assetが含まれ、代表的な文字化け断片が含まれないことを確認しました。

## Errors

- None.

## Warnings

- PowerShellから python - へ日本語リテラルを渡す検査では文字列が壊れる可能性があるため、文字化け検出はUnicodeエスケープを使ったASCIIのみの検査コードで実施しました。
- F#実装、ゲームエンジン本体、src/、tests/、Run Report補助スクリプトは変更していません。

## Next Candidates

- 思兼神Core v0.1 実装時に、権能AssetをF#型として定義するか検討してください。
- 権能Assetの検証器、固定tick上でのイベント変換、禊Testでの不変条件チェックを別作業として設計してください。

## Human Confirmation

kengou_asset_spec.md の権能Asset定義が、思兼神エンジンのアセット設計方針として妥当か確認してください。
