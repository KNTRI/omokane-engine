# 思兼神エンジン Codex Workflow Rules

この文書は、F#製日本語API独自ゲームエンジン「思兼神エンジン」におけるCodex作業共有フローを定める。
Run Report + Clipboard共有機構はCodex運用補助層であり、ゲームエンジン本体の仕様や挙動には含めない。

## Run Report共有フロー

1. Codex作業後に `docs/codex/runs/latest.md` を更新する。
2. 必要に応じて `docs/codex/runs/YYYY-MM-DD_HHMM_<slug>.md` に個別Run Reportを保存する。
3. `latest.md` のクリップボード送信を必ず試行する。
4. クリップボード送信に失敗しても、`latest.md` がUTF-8で読めるなら作業自体は失敗扱いにしない。
5. 別チャットへ共有するときは、スクリーンショットではなくMarkdown本文を共有する。

## latest.md の役割

`docs/codex/runs/latest.md` は、他のChatGPTチャットへ貼り付けるための最新共有用Run Reportである。
常に直近のCodex作業結果を表し、作業完了時に必ず更新する。

## 個別Run Reportの役割

`docs/codex/runs/YYYY-MM-DD_HHMM_<slug>.md` は、過去のCodex作業結果を追跡するための履歴ファイルである。
`latest.md` と同じ内容を保存し、後から作業内容を確認できるようにする。

## write_run_report.py の使い方

引数なしで実行すると、サンプルRun Reportを生成し、`latest.md` も更新する。

```powershell
python tools/codex_ops/write_run_report.py
```

タイトル、変更ファイル、検証コマンドなどを指定する例:

```powershell
python tools/codex_ops/write_run_report.py `
  --title "思兼神エンジン用 Run Report更新" `
  --slug "codex-run-report" `
  --summary "思兼神エンジンのCodex運用補助機構を更新した。" `
  --changed-file "docs/codex/runs/latest.md" `
  --verification-command "python tools/codex_ops/print_latest_report.py --max-chars 4000" `
  --verification-result "latest.md をUTF-8で表示できた。"
```

## print_latest_report.py の使い方

`latest.md` をUTF-8で読み、標準出力へ表示する。

```powershell
python tools/codex_ops/print_latest_report.py
```

最大表示文字数を指定する例:

```powershell
python tools/codex_ops/print_latest_report.py --max-chars 4000
```

別のパスを指定する例:

```powershell
python tools/codex_ops/print_latest_report.py --path docs/codex/runs/latest.md
```

## copy_latest_report.py の使い方

WindowsではPowerShellの `Set-Clipboard` を使って `latest.md` をクリップボードへ送る。
作業完了時は必ずこの送信を試行する。

```powershell
python tools/codex_ops/copy_latest_report.py
```

クリップボード送信に失敗した場合でも、`latest.md` が存在しUTF-8で読めるなら、作業自体は失敗扱いにしない。

## PowerShell手動コピー方法

クリップボード送信に失敗した場合は、次のコマンドで手動コピーする。

```powershell
Get-Content .\docs\codex\runs\latest.md -Raw -Encoding UTF8 | Set-Clipboard
```

PowerShell補助スクリプトを使う場合:

```powershell
.\scripts\copy_latest_run_report.ps1
```

## 文字化け防止ルール

- Run Reportの見出しはASCII英語固定にする。
- Run Report本文には日本語を含めてもよい。
- Pythonでファイルを読む、または書く場合は `encoding="utf-8"` を明示する。
- PowerShellでは `Get-Content -Raw -Encoding UTF8` を使う。
- `Verification Commands` は1コマンド1行で書く。

## スクリーンショットに依存しない運用

Codex作業共有はMarkdownのRun Reportを基本とし、スクリーンショット共有を前提にしない。
UI表示ではなく、保存済みのテキストを他のChatGPTチャットへ貼り付けられる状態にする。

## ゲームエンジン本体との分離

Run Report + Clipboard共有機構はCodex運用補助ツールである。
次のサブシステムを不用意に変更しない。

- `思兼神Core`: 判断、状態、固定tick、イベント統括
- `天照Renderer`: 描画、光、画面出力
- `建御雷Battle`: 戦闘、攻撃、衝突、ダメージ
- `猿田彦Input`: 入力、操作、案内
- `天鳥船Motion`: 移動、速度、座標、物理もどき
- `言霊Event`: イベント、通知、ログ
- `禊Test`: 検証、テスト、不変条件チェック

F#ソース、ゲームルール、固定tick更新、既存テストの意味を不用意に変更しない。
