# Codex Run Report

title: 思兼神Core 自発移動意図 v0.1と自発移動制御Gate

generated_at: 2026-08-20T18:50:53+09:00

## Summary

- Inputまたは明示方向からprivateな `自発移動意図` を生成する純粋APIを追加した。
- 左、右、上、下を意味値として扱い、物理速度、変位、移動距離とは分離した。
- `自発移動制御判断` と意図の実行者ID・Tickを検証し、通過または抑制するprivateな結果を追加した。
- 一括Gateは空一覧を許可し、入力順、意図ID一意性、原子的公開を保証する。
- 抑制された意図も保持し、方向の合成、相殺、優先順位付けは行わない。
- 完全因果経路と統合Replay後の判断へ同じ意図を適用し、構造的に同じ抑制結果を得た。
- 既存734件を維持し、新規99件を加えた833件の禊Testをすべて通過した。
- 正常3840ケースと不正88ケース、合計3928ケースの決定論Matrixを各2回検証した。
- `Input.fs`、`Systems/Omokane.fs`、統合台帳、統合Replayは変更していない。

## Changed Files

- `src/Omokane.Core/Domain/VoluntaryLocomotionIntent.fs`
- `src/Omokane.Core/Domain/VoluntaryLocomotionGate.fs`
- `src/Omokane.Core/Omokane.Core.fsproj`
- `tests/Omokane.Core.Misogi/VoluntaryLocomotionIntentGateTests.fs`
- `tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj`
- `tests/Omokane.Core.Misogi/Program.fs`
- `docs/adr/ADR-003-causal-transaction.md`
- `docs/adr/ADR-023-entity-reaction-state.md`
- `docs/adr/ADR-025-reaction-aware-voluntary-locomotion-control.md`
- `docs/adr/ADR-026-voluntary-locomotion-intent.md`
- `docs/adr/ADR-027-voluntary-locomotion-intent-gate.md`
- `docs/architecture/index.md`
- `docs/architecture/current_system_mapping.md`
- `docs/architecture/world_intelligence_kernel.md`
- `docs/design/entity_reaction_state_and_causal_application.md`
- `docs/design/reaction_state_locomotion_read_boundary.md`
- `docs/design/voluntary_locomotion_intent_and_gate.md`
- `docs/design/procedural_sandbox_minimum_model.md`
- `docs/design/tick_execution_order.md`
- `docs/design/glossary.md`
- `docs/codex/runs/latest.md`
- `docs/codex/runs/2026-08-20_1850_omokane-core-voluntary-locomotion-intent-gate.md`

## Public API

- `自発移動意図ID`
- `自発移動方向.左` / `右` / `上` / `下`
- `自発移動意図由来.明示指定` / `入力由来`
- privateな `自発移動意図` と各アクセサ
- `自発移動意図生成.方向から作る`
- `自発移動意図生成.入力から作る`
- privateな `自発移動意図制御結果` と各アクセサ
- `自発移動意図制御.適用する`
- `自発移動意図制御.一括適用する`

## Architecture Audit

- `入力` は左右上下、ジャンプ、攻撃、入力なしの7ケースである。
- 現行Movementは左右入力だけをプレイヤーX座標へ直接適用する互換経路である。
- `自発移動制御判断` はGameState由来のprivate read modelで、反応状態なしを許可、`その場警戒` を抑制へ写像する。
- 制御判断は位置、速度、HP、所有者、当たり判定、終了状態を制御理由へ使わない。
- `エンティティ反応状態` だけが今回の経路で正式履歴へ保存されるWorld Truthである。
- 添付 `Omoikane_Persistent_Causal_World_Context.md` は設計背景として読み、World Truthと派生判断・実行計画の分離、独立規則の合成、Mesh・速度・Inputを唯一のWorld Truthにしない方針を拘束条件として採用した。
- 添付文書の将来構想は今回の実装指示として扱わず、Field、SDF、Navigationは非目標に維持した。

## Persistent Causal World Alignment

正式履歴へ保存するのは因果適用された反応状態である。
制御判断、自発移動意図、Gate結果は、そのWorld Truthと明示入力から再導出できる一時的なDerived DecisionまたはExecution Planning値とした。
結果を直接スクリプトせず、反応状態読取、意図生成、Gateを独立した純粋規則として合成した。

## Intent Model

`自発移動意図` は、主体が自分から特定方向へ移動しようとする意味値である。
GameState、Entity本体、位置、速度、力、Event、因果操作、台帳を保持しない。
private表現により不整合な組合せを外部構築させない。

## Intent Identity

意図IDは呼び出し側が明示する。
GUID、現在時刻、乱数、hash、実行者ID、Tick、Input case名から自動生成しない。
一括Gateでは一覧内の意図ID重複を拒否する。

## Direction Semantics

Phase 1は左、右、上、下だけを扱う。
方向は物理速度、変位、加速度、移動距離ではない。
右と左、上と下を相殺せず、複数方向を斜めへ合成しない。

## Intent Origin

- `明示指定`: 呼び出し側が方向を直接指定した。
- `入力由来`: 実際の変換元Inputをそのまま保持する。

AI専用由来は追加していない。

## Input Mapping

- `左へ移動` から `自発移動方向.左`
- `右へ移動` から `自発移動方向.右`
- `上へ移動` から `自発移動方向.上`
- `下へ移動` から `自発移動方向.下`

Input全caseをwildcardなしで網羅的にmatchした。
現行Movementが上下未対応でも上下の意味的意図を生成する。

## Non Movement Input

ジャンプ、攻撃、入力なしは正常な `Ok None` とする。
意図を生成しないため、意図ID、Tick、実行者IDが不正でも検証しない。

## Gate Model

Gateは保存済み制御判断を再生成・再評価しない。
意図と判断をprivateな通過または抑制結果へ構造的に保持する。
GameStateやEntity一覧を受け取らない。

## Single Gate

固定順で実行者ID一致、Tick一致を検証する。
両方不一致なら両エラーをこの順で返す。
許可判断では通過、抑制判断では抑制となる。

## Batch Gate

空一覧は `Ok []` とする。
実行者IDエラーを入力順、Tickエラーを入力順、意図ID重複の順で全件検証する。
全件成功時だけ入力順の結果を公開する。

## Actor And Tick Boundary

Gateは同一主体・同一評価Tickだけを扱う。
Entity存在、種別、locomotion適格性は検査しない。
古い意図、未来意図、複数主体を自動調整・group化しない。

## Atomicity

後方意図が不正でも、前方の結果、部分成功件数、途中結果を公開しない。

## Order Preservation

一括結果は入力意図一覧と同じ順序を維持する。
sortや非安定な列挙順を使わず、同じ内容でもIDが異なる意図は重複排除しない。

## Suppressed Intent Semantics

抑制結果は意図を削除しない。
主体が何をしようとしたかと、なぜ下流へ通らなかったかを追跡できる。
抑制は物理速度ゼロ、位置固定、外力停止を意味しない。

## World Truth Boundary

意図とGate結果はWorld Truthではない。
GameState、Entity一覧、反応状態一覧、Tick、Seed、終了状態を変更せず、統合因果台帳へ保存しない。

## Physical Force Boundary

位置、速度、HP、当たり判定、所有者、重力、慣性、ノックバック、押し出し、移動床を参照・変更しない。

## Input Boundary

`Input.fs` は無変更である。
Input caseを追加、削除、改名せず、入力一覧をfilterしない。

## Movement Boundary

`Systems/Omokane.fs` は無変更である。
現行Movementは意図とGate結果を参照しない。
上下Movement、位置更新抑制、Tick進行、Event生成を変更していない。

## Event And Causal Boundary

意図生成とGateはEvent、因果操作、更新結果、因果記録を返さない。
統合因果台帳へ追記せず、統合Replayも変更しない。

## Full Cognitive Path

正式地盤振動因果、統合台帳、正式信号Replay、感知、観測、信念、警戒意図、行動候補、選択、反応要求、反応状態変更因果、World Truth適用、制御判断、右移動意図、Gateまで完走した。
反応状態適用前は同じ意図が通過し、適用後は抑制された。
地盤振動因果IDから抑制意図までProvenanceを辿れる。

## Replay Integration

live状態とReplay状態から導出した判断へ同じ意図を適用し、構造的に同じ抑制結果を得た。
Replay信号、Event列、統合台帳、GameStateはGateにより変化しなかった。

## Determinism Audit

追加本番コードで `DateTime.Now`、`DateTime.UtcNow`、`Guid.NewGuid`、Random、Task、Parallel、Thread、TickCount、Stopwatch、Dictionary、HashSetは0件。
sort、maxBy、groupBy、distinct、方向合成、相殺、物理状態書換え、Movement、Event、因果、台帳、Replay呼出しも0件。
`List.countBy` は意図ID重複の有無だけに使い、列挙順を結果順へ利用していない。

## Test Count

- 既存禊Test: 734件
- 新規禊Test: 99件
- 最終総数: 833件
- 正常決定論Matrix: 3840ケース、各2回
- 不正決定論Matrix: 88ケース、各2回
- Matrix合計: 3928ケース
- passed: 833
- ignored: 0
- failed: 0
- errored: 0

## Repeated Verification

- Run 1: 833 passed / 0 ignored / 0 failed / 0 errored
- Run 2: 833 passed / 0 ignored / 0 failed / 0 errored
- Run 3: 833 passed / 0 ignored / 0 failed / 0 errored
- Run 4: 833 passed / 0 ignored / 0 failed / 0 errored
- Run 5: 833 passed / 0 ignored / 0 failed / 0 errored

Matrixの意味修正後にCore、Smoke、Misogi、補助スクリプト、package auditと5回連続禊Testをすべて再実行した。

## Verification Commands

`git status --short`
`git branch --show-current`
`git log -1 --oneline`
`git remote -v`
`git fetch origin main`
`git merge --ff-only origin/main`
`git merge-base --is-ancestor 42bc52b0bc4a2c96352da613a2f39e0225f849ed main`
`git switch -c feature/voluntary-locomotion-intent-gate`
`dotnet build .\src\Omokane.Core\Omokane.Core.fsproj`
`dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj`
`dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj`
`dotnet build .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj`
`dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj` (5回)
`python -m py_compile tools/codex_ops/write_run_report.py`
`python -m py_compile tools/codex_ops/print_latest_report.py`
`python -m py_compile tools/codex_ops/copy_latest_report.py`
`dotnet list .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj package --no-restore`
`rg -n <non-deterministic-api-patterns> src\Omokane.Core\Domain\VoluntaryLocomotionIntent.fs src\Omokane.Core\Domain\VoluntaryLocomotionGate.fs`
`rg -n <scope-and-algorithm-patterns> src\Omokane.Core\Domain\VoluntaryLocomotionIntent.fs src\Omokane.Core\Domain\VoluntaryLocomotionGate.fs`
`git diff -- src\Omokane.Core\Domain\Input.fs`
`git diff -- src\Omokane.Core\Systems\Omokane.fs`
`git diff -- src\Omokane.Core\Domain\IntegratedCausalLedger.fs src\Omokane.Core\Domain\IntegratedCausalReplay.fs`
`git diff --check`
`git status --short --ignored`
`git diff --stat`
`python tools/codex_ops/copy_latest_report.py`

## Verification Result

- Expected main `42bc52b0bc4a2c96352da613a2f39e0225f849ed`: 完全一致、包含確認成功。
- Core build: 成功、警告0、エラー0。
- Smoke build/run: 成功、警告0、エラー0。
- Misogi build/run: 833 passed、0 ignored、0 failed、0 errored。
- 5回連続Misogi: 全回833 passed、失敗0。
- Python helper 3件: py_compile成功。
- Package audit: 既存Expecto 11.1.0とFSharp.Core 10.1.301のみ。
- NU1900: なし。
- 外部依存追加: なし。
- `Input.fs`、`Systems/Omokane.fs`、統合台帳、統合Replay: diffなし。

## Errors

最終エラーはなし。
文書更新の初回パッチはADR-025の既存文言差により適用前検証で原子的に拒否された。実ファイルへ部分反映されていないことを確認し、文書単位に分割して適用した。
セルフレビューで、不正Matrixの96組合せ中8件が正常条件だったことを検出した。少なくとも一Invariant違反を持つ88件へ修正し、正式全検証と5回連続禊Testを最初からやり直した。

## Warnings

Core、Smoke、Misogiのコンパイラ警告は0件。
GitはWindowsの `core.autocrlf` によるLFからCRLFへの変換予告を表示したが、whitespace errorはない。
添付Persistent Causal World文書は構想と背景制約であり、今回の実装指示としては扱っていない。

## Design Decisions

- 自発移動意図を物理速度ではなく四方向の意味値とした。
- 移動Inputだけを意図へ変換し、非移動Inputは正常な意図なしとした。
- 意図と判断の主体・Tick完全一致をPhase 1のGate契約とした。
- 抑制意図を破棄せず、説明可能性を保持した。
- 一括Gateは入力順を維持し、意図ID重複を拒否して原子的に失敗する。
- 方向の合成、相殺、優先順位付けは後続責務とした。
- ADR-003は現行Movementの直接更新が残るため `Proposed` を維持した。

## Non-Goals

- 斜め、任意方向ベクトル、意図強度、目的位置、経路、巡路
- locomotion command、Movement Adapter、上下Movement
- 入力一覧filter、プレイヤー位置更新抑制、NPC巡路停止
- 物理速度・位置変更、重力・外力・落下・ノックバック停止
- 意図競合解決、相殺、斜め合成、優先順位、予約、期限、持越し
- Event、因果操作、因果記録、台帳追記、Replay変更
- Field、SDF、Navigation、Renderer

## Deferred Work

通過した自発移動意図を既存Movement互換命令へ変換するpure Adapterは未実装である。
複数意図の競合・方向合成、意図の期限・持越し、その場警戒解除、巡路保留・復帰も後続とする。

## Next Candidates

1. 通過した自発移動意図を互換Movement命令へ変換するpure Adapter
2. Movement Adapterを既存Movementの横へ並行接続する
3. 自発移動意図の競合解決・方向合成
4. その場警戒状態の解除要求と解除因果操作
5. 巡路進行の保留・復帰
6. Persistent Causal Worldの最小Field契約

技術的には1を最優先とする。pure Adapterで意味意図と既存Movement命令の写像を固定してから並行接続すれば、現行互換経路を壊さず責務境界を検証できる。

## Human Confirmation

- Movement Adapterが上下意図をどう扱うか、既存左右Movementとの並行期間をどう終えるかは次作業で確認が必要。
- 意図競合解決をAdapter前後のどちらへ置くかは、複数入力と将来AI意図の要件確定後に判断する。
- GitHub認証、push、PR、merge、GitHub Actionsは今回の責務外。
