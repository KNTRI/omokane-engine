# Codex Run Report

title: 思兼神Core エンティティ反応状態 v0.1と因果適用・統合Replay

generated_at: 2026-08-13T11:06:30+09:00

## Summary

- `その場警戒` を位置・速度・HP・巡路から独立した `エンティティ反応状態` としてWorld Truthへ追加した。
- 反応要求からprivateな状態変更因果操作を生成し、契約不正とゲーム内失敗を分離して適用する。
- 変更前後状態と全Provenanceを第三種の統合因果記録へ保存し、変更前状態の検証付きで原子的にReplayする。
- 統合台帳は因果操作ID、Tick、伝播信号IDに加え、反応要求IDの一意性を保証する。
- 既存546件を維持し、新規118件を加えた664件の禊Testをすべて通過した。
- 2160ケースの決定論Matrixと5回連続回帰を完走した。
- `Input.fs` は無変更、Movementロジックは初期状態の空一覧追加以外無変更、外部依存追加なし。

## Changed Files

- `src/Omokane.Core/Domain/GameOutcome.fs`
- `src/Omokane.Core/Domain/EntityReactionState.fs`
- `src/Omokane.Core/Domain/GameState.fs`
- `src/Omokane.Core/Domain/EntityReactionCausalExecution.fs`
- `src/Omokane.Core/Domain/IntegratedCausalLedger.fs`
- `src/Omokane.Core/Domain/IntegratedCausalReplay.fs`
- `src/Omokane.Core/Omokane.Core.fsproj`
- `src/Omokane.Core/Systems/Omokane.fs`
- `tests/Omokane.Core.Misogi/EntityReactionStateCausalTests.fs`
- `tests/Omokane.Core.Misogi/IntegratedEntityReactionHistoryTests.fs`
- `tests/Omokane.Core.Misogi/CausalGroundVibrationCognitiveLoopTests.fs`
- `tests/Omokane.Core.Misogi/CausalLedgerTests.fs`
- `tests/Omokane.Core.Misogi/CausalOperationBatchExecutionTests.fs`
- `tests/Omokane.Core.Misogi/CausalOperationExecutionTests.fs`
- `tests/Omokane.Core.Misogi/CausalReplayTests.fs`
- `tests/Omokane.Core.Misogi/GroundVibrationCausalEmissionTests.fs`
- `tests/Omokane.Core.Misogi/IntegratedCausalLedgerTests.fs`
- `tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj`
- `tests/Omokane.Core.Misogi/Program.fs`
- `docs/adr/ADR-003-causal-transaction.md`
- `docs/adr/ADR-008-observability-replay.md`
- `docs/adr/ADR-016-minimal-alert-action-candidate.md`
- `docs/adr/ADR-018-causal-cognitive-action-loop.md`
- `docs/adr/ADR-019-integrated-causal-ledger.md`
- `docs/adr/ADR-020-integrated-causal-replay.md`
- `docs/adr/ADR-021-deterministic-action-candidate-selection.md`
- `docs/adr/ADR-022-entity-reaction-request.md`
- `docs/adr/ADR-023-entity-reaction-state.md`
- `docs/adr/ADR-024-entity-reaction-causal-application.md`
- `docs/architecture/index.md`
- `docs/architecture/current_system_mapping.md`
- `docs/architecture/world_intelligence_kernel.md`
- `docs/design/action_candidate_selection.md`
- `docs/design/causal_ground_vibration_cognitive_loop.md`
- `docs/design/entity_reaction_request.md`
- `docs/design/entity_reaction_state_and_causal_application.md`
- `docs/design/glossary.md`
- `docs/design/integrated_causal_ledger_and_replay.md`
- `docs/design/procedural_sandbox_minimum_model.md`
- `docs/design/tick_execution_order.md`
- `docs/codex/runs/latest.md`
- `docs/codex/runs/2026-08-13_1106_omokane-core-entity-reaction-state-causal-application.md`

## Public API

- `ゲーム状態.エンティティ反応状態一覧: エンティティ反応状態 list`
- privateな `エンティティ反応状態` と由来アクセサ
- `エンティティ反応状態一覧.検証する` / `実行者IDで探す`
- `ゲーム状態.エンティティ反応状態を探す`
- privateな `エンティティ反応状態変更操作` と公開アクセサ
- `エンティティ反応状態変更操作生成.反応要求から作る`
- `エンティティ反応状態変更記録` / `検証する`
- `エンティティ反応状態変更結果`
- `エンティティ反応状態変更実行.適用する`
- 統合台帳の第三種アクセサ、件数、要求ID検索、専用追記wrapper
- `統合因果台帳再生.再生する` の既存署名は維持

## Architecture Audit

- 反応要求は判断層の非権威値で、因果適用成功後の反応状態だけがWorld Truthである。
- 現行EntityにはNPC専用型、巡路状態、locomotion意図がない。
- 既存MovementはGameStateを直接更新する互換経路なので、ADR-003は `Proposed` を維持した。
- 既存因果台帳は位置変更専用、統合台帳は意味別判別共用体を拡張点とする。
- Eventは通知、伝播信号は認識用権威情報であり、どちらからも反応状態を暗黙生成しない。

## GameState Contract Change

`ゲーム状態` 公開レコードへ `エンティティ反応状態一覧` を追加した。
既存構築箇所は空一覧で明示初期化し、既存観測可能動作を維持した。

## Compile Order Refactor

`終了状態` を公開名・case不変のまま `GameOutcome.fs` へ移した。
`EntityReactionState.fs` を要求の後、`GameState.fs` を反応状態の後、因果実行器を `CausalExecution.fs` の後へ配置し、F#依存循環を回避した。

## Reaction State Model

反応状態は成立因果操作IDと由来要求をprivateに保持する。
成立Tick、実行者、種別、優先度、仮説、要求ID、候補ID、選択位置・理由は既存Provenanceから取得する。

## Reaction State Invariant

一Entityにつき現在反応状態は最大一件。
既存主体は同じlist位置で置換し、新規主体は末尾へ追加する。IDやTickで再ソートしない。

## Request To Operation

呼び出し側が因果操作ID、任意の原因因果ID、Eventを明示する。
操作は要求Tick、要求主体、`状態変更`、固定概要へ写像し、要求を再選択・再構築しない。

## Causal Operation

新規操作IDの空白、原因IDの空白、自己原因を固定順で拒否する。
実行者IDと対象EntityIDは要求主体へ固定し、自動ID、時刻、GUID、乱数を使わない。

## Execution Validation

契約検証後だけ、現在Tick、終了状態、対象Entity存在・一意性、反応状態一覧Invariantをゲーム内検証する。
失敗時は入力状態を返し、部分状態・Event・記録を公開しない。

## World Truth Application

成功時は `エンティティ反応状態一覧` だけを変更し、操作Eventをそのまま返す。
同一Entityの同一Tick更新とTick増加更新を許可し、変更前状態を正式記録へ残す。

## Physical State Boundary

Entity一覧、位置、速度、HP、種別、所有者、当たり判定、乱数Seed、終了状態は不変。
`その場警戒` を速度ゼロ、位置固定、巡路停止、入力なしへ変換していない。

## Movement Compatibility

`Input.fs` は無変更。
`Systems/Omokane.fs` は初期GameStateへ空の反応状態一覧を追加しただけで、移動計算、Tick進行、Event生成は無変更。

## Causal Record

正式記録は因果操作ID、原因因果ID、Tick、実行者、対象、概要、変更前後状態、由来要求、Eventを保持する。
15項目の整合性を固定順で防御的に検証する。

## Integrated Ledger Third Record

統合因果記録へ `エンティティ反応状態変更` を第三種として追加した。
位置変更・地盤振動・反応状態変更は網羅的matchで分岐し、別種Option accessorやダミー値をdispatchへ使わない。

## Reaction Request ID Invariant

既存台帳と今回追記一覧を通じ、エンティティ反応要求IDの重複を拒否する。
因果操作ID、Tick、伝播信号IDの既存Invariantと固定順で共存し、失敗時は元台帳を返す。

## Integrated Replay

反応記録では記録契約、Tick、終了状態、対象一意性、状態一覧Invariant、変更前状態を検証する。
成功時だけ変更後状態を同じlist位置へ適用し、反応記録から信号は生成しない。

## Replay Atomicity

後方失敗時は初期状態へ復帰し、途中反応状態、Event、信号を公開しない。
Eventから反応状態を作らず、三種記録の保存順・記録内Event順を維持する。

## Full Cognitive Causal Loop

正式地盤振動因果からReplay信号、感知、観測、信念、警戒意図、候補、選択、要求、反応状態変更因果、統合台帳、Replayまで完走した。
liveとReplayの最終GameState、Event、信号、反応状態が一致した。

## Provenance Chain

地盤振動因果IDを反応操作の原因因果IDへ接続した。
信号ID、観測者ID、仮説、確率、警戒度、候補優先度、要求ID、選択位置、成立因果操作IDを反応状態と正式記録から追跡できる。

## Determinism Audit

追加・変更本番コードに `DateTime.Now`、`DateTime.UtcNow`、`Guid.NewGuid`、Random、Task、Parallel、Thread、TickCount、Stopwatch、Dictionary、HashSetは0件。
sort、`Option.get` による三種dispatch、自動ID、優先度再計算、物理状態変更も0件。
Map/Setは重複検索だけに使用し、その列挙順を状態・記録・Event・信号順へ使わない。

## Test Count

- 既存禊Test: 546件
- 新規禊Test: 118件
- 最終総数: 664件
- 決定論Matrix: 2160ケース、各2回
- passed: 664
- ignored: 0
- failed: 0
- errored: 0

## Repeated Verification

- Run 1: 664 passed / 0 ignored / 0 failed / 0 errored
- Run 2: 664 passed / 0 ignored / 0 failed / 0 errored
- Run 3: 664 passed / 0 ignored / 0 failed / 0 errored
- Run 4: 664 passed / 0 ignored / 0 failed / 0 errored
- Run 5: 664 passed / 0 ignored / 0 failed / 0 errored

## Verification Commands

`git status --short`
`git branch --show-current`
`git log -1 --oneline`
`git remote -v`
`git switch main`
`git fetch origin main`
`git merge --ff-only origin/main`
`git merge-base --is-ancestor 4fa7abf4035f537cd33fe17f8d889c98627a05bc main`
`git switch -c feature/entity-reaction-state-causal-application`
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
`python -m py_compile tools/codex_ops/write_run_report.py`
`python -m py_compile tools/codex_ops/print_latest_report.py`
`python -m py_compile tools/codex_ops/copy_latest_report.py`
`dotnet list .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj package --no-restore`
`rg -n "DateTime\.Now|DateTime\.UtcNow|Guid\.NewGuid|Random|System\.Random|Task\.Run|Parallel\.|Thread|Environment\.TickCount|Stopwatch|Dictionary|HashSet" <changed-production-files>`
`rg -n "List\.sort|sortBy|maxBy|Option\.get|速度\s*=|位置\s*=|HP\s*=" <reaction-production-files>`
`git diff -- src/Omokane.Core/Domain/Input.fs`
`git diff -- src/Omokane.Core/Systems/Omokane.fs`
`python -c "from pathlib import Path; import subprocess; entries=subprocess.check_output(['git','status','--porcelain','-z']).decode('utf-8').split(chr(0)); files=[Path(e[3:]) for e in entries if e and Path(e[3:]).suffix in {'.fs','.fsproj','.md'}]; markers=[chr(x) for x in (0x7e5d,0x7e3a,0x8b41,0x8b0c,0x9015,0x83a8,0x8737,0xfffd)]; bad=[(str(p),hex(ord(m))) for p in files for m in markers if m in p.read_text(encoding='utf-8')]; print(f'UTF-8 scan: {len(files)} changed text files, mojibake markers: {len(bad)}'); print(bad); raise SystemExit(1 if bad else 0)"`
`git diff --check`
`git status --short --ignored`
`git diff --stat`
`python tools/codex_ops/copy_latest_report.py`

## Verification Result

- Expected main `4fa7abf4035f537cd33fe17f8d889c98627a05bc` を確認し、指定branchを作成した。
- Core build: 成功、警告0、エラー0。
- Smoke build: 成功、警告0、エラー0。
- Smoke run: Movement、KengouAsset、WorldIntelligence Contracts、Validationすべて成功。
- Misogi build: 成功、警告0、エラー0。
- Misogi run: 664 passed、0 ignored、0 failed、0 errored。
- Python helper 3件: py_compile成功。
- Package audit: 既存Expecto 11.1.0とFSharp.Core 10.1.301のみ。
- NU1900: なし。
- 外部依存追加: なし。
- UTF-8: 変更テキストをstrict UTF-8で読取可能、文字化けmarker 0件。
- `git diff --check`: whitespace error 0件。
- `bin/`、`obj/`、`__pycache__/`、`*.pyc`: ignoredのまま。
- Clipboard: `copy_latest_report.py` で送信成功。

## Errors

最終エラーはなし。
最初のpy_compileはread-only sandbox内でpyc書込みを拒否されたが、同じ指定コマンドを許可された書込み環境で再実行し、3件とも成功した。
最初の `git diff --check` はRun Report末尾の余分な空行を検出したが、両レポートを修正後の再実行は成功した。

## Warnings

Core、Smoke、Misogiのコンパイラ警告は0件。
GitはWindowsの `core.autocrlf` によるLFからCRLFへの変換予告を表示したが、whitespace errorはない。

## Design Decisions

- `その場警戒` は独立した反応状態であり、物理速度や巡路状態ではない。
- 要求は非権威、因果適用成功後の反応状態だけがWorld Truthである。
- 一Entity一反応状態とlist位置維持をPhase 1のInvariantとする。
- 統合履歴は三種の意味をprivate判別共用体で保持し、要求IDを台帳全体で一意とする。
- ADR-003は現行Movementの直接更新が残るため `Proposed` のまま維持する。

## Non-Goals

- 警戒解除、通常状態復帰、複数反応状態、継続時間、cooldown、ヒステリシス
- 速度・位置・HP・巡路・経路・アニメーション変更
- Movementによる反応状態参照、Input変更、プレイヤー操作抑制
- 複数要求の競合裁定、結界Lock、天津裁定、反応状態変更バッチ
- Event新case・dispatch、ファイル保存、Snapshot、ネットワーク、Renderer接続

## Deferred Work

反応状態をMovement / locomotion意図がどう読むか、解除をどう要求・因果化するか、巡路進行をどう保留・復帰するかは後続設計とする。
現在の実装は意味を保ったWorld Truthと正式履歴・Replayまでに限定する。

## Next Candidates

1. その場警戒状態をMovement / locomotion意図が読む最小境界
2. その場警戒状態の解除要求と解除因果操作
3. 巡路進行の保留と復帰
4. 複数反応状態種別
5. 反応状態の継続時間とヒステリシス
6. 統合因果台帳の永続化とSnapshot

技術的には1を最優先とする。状態を速度ゼロへ短絡せず、Movement側が権威反応状態をどう読むかを読み取り境界として先に確定すると、解除や巡路復帰の契約を安全に設計できる。

## Human Confirmation

- GameState公開レコードへ `エンティティ反応状態一覧` を追加したため、外部利用側は空一覧を明示して構築する必要がある。
- Movement / locomotionによる `その場警戒` の解釈と解除意味論は次作業で人間確認が必要。
- GitHub認証、push、PR、merge、GitHub Actionsは今回の責務外。
