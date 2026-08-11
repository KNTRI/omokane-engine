# Codex Run Report

title: 思兼神Core エンティティ反応要求

generated_at: 2026-08-11T13:13:25+09:00

## Summary

- privateな `エンティティ反応要求` と純粋な生成APIを追加した。
- 選択済み行動候補へ、呼び出し側が明示した要求IDと発行Tickを付与する。
- `その場警戒` を意味的な反応種別として保持し、速度ゼロ、位置固定、巡路停止へ変換しない。
- 実行者、優先度、対象仮説、候補一覧、選択位置、選択理由、却下候補を構造的に保持する。
- ゲーム状態、Entity適格性、Input、Event、因果操作、統合因果台帳を生成APIへ持ち込んでいない。
- 因果認識経路と統合Replayから、候補選択を経て反応要求まで接続した。
- 既存483件を維持し、新規63件を加えた546件の禊Testをすべて通過した。
- `Input.fs`、`Systems/Omokane.fs`、既存Smokeは変更していない。

## Changed Files

- `src/Omokane.Core/Domain/EntityReactionRequest.fs`
- `src/Omokane.Core/Omokane.Core.fsproj`
- `tests/Omokane.Core.Misogi/EntityReactionRequestTests.fs`
- `tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj`
- `tests/Omokane.Core.Misogi/Program.fs`
- `docs/adr/ADR-003-causal-transaction.md`
- `docs/adr/ADR-006-ai-non-omniscience.md`
- `docs/adr/ADR-016-minimal-alert-action-candidate.md`
- `docs/adr/ADR-018-causal-cognitive-action-loop.md`
- `docs/adr/ADR-021-deterministic-action-candidate-selection.md`
- `docs/adr/ADR-022-entity-reaction-request.md`
- `docs/architecture/index.md`
- `docs/architecture/current_system_mapping.md`
- `docs/architecture/world_intelligence_kernel.md`
- `docs/design/action_candidate_selection.md`
- `docs/design/causal_ground_vibration_cognitive_loop.md`
- `docs/design/entity_reaction_request.md`
- `docs/design/glossary.md`
- `docs/design/integrated_causal_ledger_and_replay.md`
- `docs/design/procedural_sandbox_minimum_model.md`
- `docs/design/tick_execution_order.md`
- `docs/codex/runs/latest.md`
- `docs/codex/runs/2026-08-11_1313_omokane-core-entity-reaction-request.md`

## Public API

- `エンティティ反応要求ID`
- `エンティティ反応種別.その場警戒`
- privateな `エンティティ反応要求`
- `エンティティ反応要求.ID`
- `エンティティ反応要求.Tick`
- `エンティティ反応要求.実行者ID`
- `エンティティ反応要求.種別`
- `エンティティ反応要求.優先度`
- `エンティティ反応要求.対象仮説`
- `エンティティ反応要求.由来選択済み行動候補`
- `エンティティ反応要求.由来行動候補`
- `エンティティ反応要求.由来行動候補ID`
- `エンティティ反応要求.由来候補一覧`
- `エンティティ反応要求.由来選択位置`
- `エンティティ反応要求.由来却下候補一覧`
- `エンティティ反応要求.由来同率最高候補一覧`
- `エンティティ反応要求.由来選択理由`
- `エンティティ反応要求生成.選択済み行動候補から作る : エンティティ反応要求ID -> int64 -> 選択済み行動候補 -> Result<エンティティ反応要求, string list>`

## Architecture Audit

- 現行Entity契約にNPC専用種別はない。
- 行動候補と選択済み行動候補は判断層の派生値である。
- `選択済み行動候補` はprivateで、候補一覧・選択位置・選択候補を保持する。
- 行動候補種別は `その場警戒` 一種類である。
- 候補優先度は由来警戒意図の警戒度と一致する。
- 統合因果台帳は位置変更と地盤振動発生だけを正式履歴として扱う。
- 既存Movementは直接更新互換経路であり、ADR-003は `Proposed` のままである。

## Naming Decision

正式API名は `エンティティ反応要求` とした。
存在しないNPC型を装わず、Entity適格性を後続の因果操作変換・実行器へ残す。

## Reaction Semantics

`行動候補種別.その場警戒` は明示matchで `エンティティ反応種別.その場警戒` へ変換する。
wildcard変換はなく、将来の候補種別追加時には明示対応が必要となる。
`その場警戒` を物理速度ゼロ、位置固定、入力なし、巡路停止へ変換していない。

## Request Identity

要求IDは呼び出し側が明示する。
空白IDを拒否し、GUID、時刻、乱数、hash、行動候補IDから自動生成しない。

## Tick Semantics

発行Tickは呼び出し側が明示し、0以上だけを要求する。
感知・観測Tick、信号寿命、統合台帳末尾Tickから推測しない。
正式禊Testで観測Tick 10と要求Tick 25の独立性を固定した。

## Selected Candidate Mapping

要求生成は候補を再選択せず、privateな選択済み値の既存契約を信頼する。
実行者ID、種別、優先度、対象仮説を選択候補からそのまま保存する。
候補一覧、選択位置、却下候補、同率最高候補、選択理由は既存アクセサへ委譲する。

## Provenance

- 要求実行者ID = 選択済み実行者ID = 選択候補実行者ID
- 要求対象仮説 = 選択候補対象仮説 = 由来警戒意図対象仮説
- 要求優先度 = 選択候補優先度 = 由来警戒意図警戒度
- 要求由来行動候補ID = 選択候補ID
- 要求由来候補一覧・選択位置・却下候補一覧・選択理由 = 選択済み値の既存アクセサ結果

## Actor Eligibility Boundary

生成APIはゲーム状態を受け取らない。
NPC、プレイヤー、敵、弾、障害物、Entity存在、重複、所有者を判定しない。
これらは将来の因果操作変換または実行器で検証する。

## World Truth Boundary

反応要求はWorld Truthではない。
Entity、ゲーム状態、Tick、位置、速度、HP、終了状態、乱数Seedを参照・変更しない。

## Event Boundary

反応要求生成はEventを返さず、既存Event一覧へ追加せず、dispatchしない。
統合Replay接続Testで要求生成前後のEvent列が同一であることを確認した。

## Input Boundary

`Input.fs` は変更していない。
反応要求を `入力なし` や既存プレイヤー入力へ変換していない。

## Causal Boundary

反応要求生成は因果操作ID、因果操作、更新結果を返さず、因果実行器を呼ばない。
統合因果台帳へ追記せず、Replay契約も変更していない。

## Integrated Cognitive Path

同じ観測者について強弱二件の正式地盤振動因果認識経路を実行し、二件の行動候補から高優先度候補を選択して要求へ変換した。
観測者ID、信号ID、仮説、確率、警戒度、優先度、選択位置、選択理由、却下候補順を保持した。

## Replay Integration

二件の地盤振動発生記録を統合因果台帳へ追記し、統合Replayが再出力した正式信号を既存認識経路へ渡した。
候補選択と要求生成後も、Replay状態、Event列、統合台帳は構造的に不変だった。
Replay信号を要求生成側で再構築していない。

## Determinism Audit

追加本番コードに次のAPI・型は0件だった。

- `DateTime.Now` / `DateTime.UtcNow`
- `Guid.NewGuid`
- `Random` / `System.Random`
- `Task.Run` / `Parallel.` / `Thread`
- `Environment.TickCount` / `Stopwatch`
- `Dictionary` / `HashSet` / `Map`

再選択、sort、`List.maxBy`、優先度再計算、警戒意図再評価、候補IDタイブレーク、重複排除、速度ゼロ変換も0件だった。
864通りのMatrixで各入力を2回生成し、構造的完全一致、候補順、選択候補ID、要求Tickを確認した。

## Test Count

- 既存禊Test: 483件
- 新規禊Test: 63件
- 最終総数: 546件
- 反応要求Matrix: 864通り
- passed: 546
- ignored: 0
- failed: 0
- errored: 0

## Repeated Verification

`--no-build --no-restore` で禊Testを5回連続実行した。

- Run 1: 546 passed / 0 ignored / 0 failed / 0 errored
- Run 2: 546 passed / 0 ignored / 0 failed / 0 errored
- Run 3: 546 passed / 0 ignored / 0 failed / 0 errored
- Run 4: 546 passed / 0 ignored / 0 failed / 0 errored
- Run 5: 546 passed / 0 ignored / 0 failed / 0 errored

## Verification Commands

`git status --short`
`git branch --show-current`
`git log -1 --oneline`
`git remote -v`
`git switch main`
`git fetch origin main`
`git merge --ff-only origin/main`
`git merge-base --is-ancestor a430046abfed56344b1eeecf252b5c988d89642a HEAD`
`dotnet build .\src\Omokane.Core\Omokane.Core.fsproj`
`dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj`
`dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj`
`dotnet build .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`python -m py_compile tools\codex_ops\write_run_report.py`
`python -m py_compile tools\codex_ops\print_latest_report.py`
`python -m py_compile tools\codex_ops\copy_latest_report.py`
`dotnet list .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj package --no-restore`
`python -c "from pathlib import Path; import subprocess; entries=subprocess.check_output(['git','status','--porcelain','-z']).decode('utf-8').split(chr(0)); files=[Path(e[3:]) for e in entries if e and Path(e[3:]).suffix in {'.fs','.fsproj','.md'}]; markers=[chr(x) for x in (0x7e5d,0x7e3a,0x8b41,0x8b0c,0x9015,0x83a8,0x8737,0xfffd)]; bad=[(str(p),hex(ord(m))) for p in files for m in markers if m in p.read_text(encoding='utf-8')]; print(f'UTF-8 scan: {len(files)} changed text files, mojibake markers: {len(bad)}'); print(bad); raise SystemExit(1 if bad else 0)"`
`rg -n "DateTime\.(Now|UtcNow)|Guid\.NewGuid|System\.Random|\bRandom\b|Task\.Run|Parallel\.|\bThread\b|Environment\.TickCount|\bStopwatch\b|\bDictionary\b|\bHashSet\b|\bMap\b" .\src\Omokane.Core\Domain\EntityReactionRequest.fs`
`rg -n "ゲーム状態|エンティティ\s+list|速度\s*[:=]|位置\s*:\s*位置|\bHP\b|\b入力\b|ゲームイベント|因果操作|統合因果台帳|因果台帳|更新結果" .\src\Omokane.Core\Domain\EntityReactionRequest.fs`
`rg -n "行動候補選択\.選ぶ|List\.(sort|sortBy|sortByDescending|maxBy|distinct|distinctBy|find|findIndex)|警戒意図生成|優先度\s*[+*/-]|速度|エンティティ種別|思兼神\.更新する" .\src\Omokane.Core\Domain\EntityReactionRequest.fs`
`git diff --check`
`git status --short --ignored`
`git diff --stat`
`python tools\codex_ops\copy_latest_report.py`

## Verification Result

- Expected main `a430046abfed56344b1eeecf252b5c988d89642a` を確認し、`feature/entity-reaction-request` を作成した。
- Core build: 成功、警告0、エラー0。
- Smoke build: 成功、警告0、エラー0。
- Smoke run: Movement、KengouAsset、WorldIntelligence Contracts、Validationがすべて成功。
- Misogi build: 成功、警告0、エラー0。
- Misogi run: 546 passed、0 ignored、0 failed、0 errored。
- Python Run Report helper 3件: py_compile成功。
- Package audit: 既存Expecto 11.1.0とFSharp.Coreのみ。外部依存追加なし。
- NU1900: なし。
- `Input.fs` と `Systems/Omokane.fs`: 差分なし。
- `bin/`、`obj/`、`__pycache__/`: ignoredのまま。
- UTF-8: 変更対象を明示UTF-8で読取可能。文字化けmarker 0件。
- `git diff --check`: whitespace error 0件。
- Clipboard: `copy_latest_report.py` で送信成功。

## Errors

- なし。

## Warnings

- Core、Smoke、Misogiのコンパイラ警告は0件。
- `git diff --check` はWindowsの `core.autocrlf` によるLF→CRLF変換予告を表示したが、whitespace errorは0件だった。

## Design Decisions

- 現行DomainにNPC専用型がないため、公開名を `エンティティ反応要求` とした。
- 反応要求IDとTickを明示入力とし、自動生成・推測を禁止した。
- private表現の選択済み値を信頼し、要求生成で候補検証や再選択を繰り返さない。
- `その場警戒` は意味的要求として保持し、物理速度ゼロへ短絡しない。
- ADR-003は `Proposed` のまま維持し、要求生成をTransaction化完了とは扱わない。

## Non-Goals

- NPC専用Entity型、AI Component、Behavior Tree、Utility AI
- Entity存在・種別・重複・所有者・終了状態の検証
- 速度、位置、巡路、警戒状態の変更
- Input生成、`思兼神.更新する` 変更、既存Movement変更
- 因果操作生成・実行、Event発行、統合因果台帳追記、Replay変更
- 保存、Snapshot、Renderer接続

## Deferred Work

`その場警戒` を具体的に何へ写像するかは未決定である。
物理速度、locomotion意図、独立した警戒状態、巡路状態を比較してから、反応要求の因果操作化を設計する。

## Next Candidates

1. `その場警戒` 反応要求を具体的な権威状態へ写像する契約
2. エンティティ反応要求を正式な因果操作へ変換
3. 反応因果操作をWorld Truthへ適用
4. 反応結果を統合因果台帳へ保存・Replay
5. 実際の巡路停止・迂回・復帰
6. 複数反応種別

技術的には1を最優先とする。意味要求を物理速度へ直結させず、locomotion意図・警戒状態・巡路状態を比較して権威境界を先に確定すべきである。今回はいずれも採択していない。

## Human Confirmation

- `その場警戒` の権威表現を、物理速度、locomotion意図、独立警戒状態、巡路状態のどれに置くか確認が必要。
- ローカル実装・検証・Run Report・commitまでCodexが担当する。
- GitHub認証、push、PR、merge、GitHub Actionsは今回の責務外。
