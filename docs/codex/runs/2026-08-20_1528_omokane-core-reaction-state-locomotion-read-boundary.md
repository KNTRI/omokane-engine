# Codex Run Report

title: 思兼神Core 反応状態から自発移動制御判断を導出する読み取り境界

generated_at: 2026-08-20T15:28:48+09:00

## Summary

- World Truth上の `エンティティ反応状態` からprivateな `自発移動制御判断` を導出する純粋APIを追加した。
- 反応状態なしは自発移動許可、`その場警戒` 中は自発移動抑制とする。
- 自発移動と重力、落下、慣性、ノックバック、押し出しなどの外力を明確に分離した。
- 完全因果経路と統合Replay後のGameStateから、構造的に同じ判断を再生成できることを固定した。
- 既存664件を維持し、新規70件を加えた734件の禊Testをすべて通過した。
- 正常1152ケースと未来Tick不正36ケース、合計1188ケースの決定論Matrixを各2回検証した。
- `Input.fs`、Movementロジック、統合台帳、統合Replayは変更していない。

## Changed Files

- `src/Omokane.Core/Domain/VoluntaryLocomotionControl.fs`
- `src/Omokane.Core/Omokane.Core.fsproj`
- `tests/Omokane.Core.Misogi/VoluntaryLocomotionControlTests.fs`
- `tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj`
- `tests/Omokane.Core.Misogi/Program.fs`
- `docs/adr/ADR-023-entity-reaction-state.md`
- `docs/adr/ADR-025-reaction-aware-voluntary-locomotion-control.md`
- `docs/architecture/index.md`
- `docs/architecture/current_system_mapping.md`
- `docs/architecture/world_intelligence_kernel.md`
- `docs/design/entity_reaction_state_and_causal_application.md`
- `docs/design/reaction_state_locomotion_read_boundary.md`
- `docs/design/procedural_sandbox_minimum_model.md`
- `docs/design/tick_execution_order.md`
- `docs/design/glossary.md`
- `docs/codex/runs/latest.md`
- `docs/codex/runs/2026-08-20_1528_omokane-core-reaction-state-locomotion-read-boundary.md`

## Public API

- `自発移動制御種別.許可` / `抑制`
- `自発移動制御理由.反応状態なし` / `その場警戒中`
- privateな `自発移動制御判断`
- 評価Tick、実行者ID、種別、理由、許可・抑制boolアクセサ
- 由来反応状態、成立因果操作ID、成立Tick、要求ID、候補ID、仮説、優先度アクセサ
- `自発移動制御判断生成.ゲーム状態から作る`

## Architecture Audit

- `エンティティ反応状態` は因果適用成功後にGameStateへ保持されるWorld Truthである。
- 反応状態はprivateで、一Entityにつき最大一件というInvariantを持つ。
- `その場警戒` は位置、速度、HP、巡路から独立した意味状態である。
- 統合因果台帳とReplayは反応状態変更を第三種として保存・再構築済みである。
- 現行Movementは入力から位置を直接更新する互換経路で、反応状態を読まない。
- 採択済みADRとarchitecture invariantsは、権威状態と下流実行を明示境界で分離することを要求する。
- checkoutと添付テキスト領域から独立したPersistent Causal World引き継ぎ文書は発見できなかったため、ADR-003/023/024、architecture invariants、現行実装、および今回の明示要件を正として監査した。

## Persistent Causal World Alignment

反応状態は因果操作、正式記録、統合Replayを通して成立・再現される。
今回の判断はReplay可能なWorld Truthを純粋に読むだけで、状態を別経路から変更せず、判断自体も正式履歴へ保存しない。

## Voluntary Locomotion Definition

自発移動は主体自身の判断、入力、将来のlocomotion意図によって開始される移動である。
重力、落下、慣性、ノックバック、押し出し、移動床、外部因果による位置変更は含まない。

## Control Decision Model

判断値は評価Tick、実行者ID、許可または抑制、理由、由来反応状態optionをprivateに保持する。
GameState全体やEntity本体を保存せず、現在World Truthの一時的なread modelとして扱う。

## Validation Order

1. 対象EntityIDの空白
2. GameState.Tickの負値
3. 対象Entity不存在
4. 対象EntityID重複
5. 反応状態一覧Invariant
6. 対象反応状態の未来成立Tick

全エラーを固定順で収集し、不正入力から判断を生成しない。

## Reaction Mapping

- 対象反応状態なし: `許可` / `反応状態なし` / 由来 `None`
- 対象が `その場警戒` 中: `抑制` / `その場警戒中` / 由来 `Some 状態`

反応種別は明示的な判別共用体matchで処理し、wildcardによる暗黙変換は行わない。

## Tick Semantics

- 判断.評価Tickは現在の `ゲーム状態.Tick` とする。
- 由来成立Tickは反応状態を成立させた要求Tickとする。
- 成立Tickが評価Tickより後の対象状態は拒否する。
- 他Entityの未来成立状態は対象判断へ適用しない。

## Provenance

抑制判断から成立因果操作ID、成立Tick、反応要求ID、行動候補ID、対象仮説、優先度を既存反応状態アクセサ経由で辿れる。
完全経路では地盤振動因果IDから信号、感知、信念、意図、候補、要求、反応状態、制御判断までの由来鎖を確認した。

## Entity Boundary

プレイヤー、敵、弾、障害物をすべて型上許可する。
Entity種別、所有者、HP、当たり判定からNPCやlocomotion適格性を推測しない。

## Physical Force Boundary

位置、速度、HP、当たり判定、所有者を判断材料にせず、変更もしない。
抑制は自発移動意図を将来gateする意味であり、速度ゼロ、位置固定、重力停止、外力停止ではない。

## Game End Boundary

ゲームオーバーまたはクリアでも現在World Truthの判断を読み取れる。
終了状態による全体停止は上位tick実行順序の責務で、判断理由やErrorには混ぜない。

## World Truth Boundary

GameState、Entity一覧、反応状態一覧、Tick、プレイヤーID、乱数Seed、終了状態を変更しない。
判断はWorld Truthではなく、World Truthから導出される一時値である。

## Input Boundary

`入力` を受け取らず、filter、抑制、`入力なし` への変換を行わない。
`src/Omokane.Core/Domain/Input.fs` は無変更である。

## Movement Boundary

`Systems/Omokane.fs` は無変更であり、既存Movementは今回の判断をまだ参照しない。
プレイヤー位置更新、Tick進行、既存Event生成は従来どおりである。

## Event And Causal Boundary

判断生成はEvent、因果操作、更新結果、因果記録を返さない。
統合因果台帳へ追記せず、統合Replayも変更しない。

## Full Cognitive Path

地盤振動発生因果、正式信号Replay、感知、観測、信念、警戒意図、行動候補、選択、反応要求、反応状態変更因果、World Truth適用、自発移動抑制判断まで完走した。
反応状態適用前は許可、適用後は抑制となり、物理Entity値は前後で不変だった。

## Replay Integration

地盤振動発生記録と反応状態変更記録を統合台帳へ保存し、初期状態からReplayした。
live適用後判断とReplay後判断は構造的に一致し、Replay信号、Event順、統合台帳は判断生成によって変化しなかった。

## Determinism Audit

追加本番コードで `DateTime.Now`、`DateTime.UtcNow`、`Guid.NewGuid`、Random、Task、Parallel、Thread、TickCount、Stopwatch、Dictionary、HashSet、Map、Setは0件。
sort、groupBy、重複排除、物理状態書換え、Movement呼出し、Event・因果・台帳・Replay処理も0件。

## Test Count

- 既存禊Test: 664件
- 新規禊Test: 70件
- 最終総数: 734件
- 正常決定論Matrix: 1152ケース、各2回
- 未来Tick不正Matrix: 36ケース、各2回
- Matrix合計: 1188ケース
- passed: 734
- ignored: 0
- failed: 0
- errored: 0

## Repeated Verification

- Run 1: 734 passed / 0 ignored / 0 failed / 0 errored
- Run 2: 734 passed / 0 ignored / 0 failed / 0 errored
- Run 3: 734 passed / 0 ignored / 0 failed / 0 errored
- Run 4: 734 passed / 0 ignored / 0 failed / 0 errored
- Run 5: 734 passed / 0 ignored / 0 failed / 0 errored

## Verification Commands

`git status --short`
`git branch --show-current`
`git log -1 --oneline`
`git remote -v`
`git switch main`
`git fetch origin main`
`git merge --ff-only origin/main`
`git merge-base --is-ancestor f5ee4295e861e476b20a5ec4db9785b14c3e4584 main`
`git switch -c feature/reaction-state-locomotion-read-boundary`
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
`rg -n <non-deterministic-api-patterns> src\Omokane.Core\Domain\VoluntaryLocomotionControl.fs`
`rg -n <mutation-and-boundary-patterns> src\Omokane.Core\Domain\VoluntaryLocomotionControl.fs`
`git diff -- src\Omokane.Core\Domain\Input.fs`
`git diff -- src\Omokane.Core\Systems\Omokane.fs`
`python -c <strict-utf8-and-mojibake-scan>`
`git diff --check`
`git status --short --ignored`
`git diff --stat`
`python tools/codex_ops/copy_latest_report.py`

## Verification Result

- Expected main `f5ee4295e861e476b20a5ec4db9785b14c3e4584`: 完全一致、包含確認成功。
- Core build: 成功、警告0、エラー0。
- Smoke build: 成功、警告0、エラー0。
- Smoke run: Movement、KengouAsset、WorldIntelligence Contracts、Validationすべて成功。
- Misogi build: 成功、警告0、エラー0。
- Misogi run: 734 passed、0 ignored、0 failed、0 errored。
- 5回連続Misogi: 全回734 passed、失敗0。
- Python helper 3件: py_compile成功。
- Package audit: 既存Expecto 11.1.0とFSharp.Core 10.1.301のみ。
- NU1900: なし。
- 外部依存追加: なし。
- UTF-8: 変更テキスト17ファイルをstrict UTF-8で読取可能、文字化けmarker 0件。
- `git diff --check`: whitespace error 0件。
- `Input.fs`: diffなし。
- `Systems/Omokane.fs`: diffなし。
- Clipboard: `copy_latest_report.py` で送信成功。

## Errors

最終エラーはなし。
初回の新規Test buildでは、存在しない当たり判定case `円` をテストで参照して1件失敗した。既存契約が `矩形` のみであることを確認し、異なる矩形値の比較へ修正した。
初回Matrix runでは実際の直積1152件に対して期待件数を576件としており1件失敗した。直積内容を維持して期待件数とTest名を1152件へ修正し、以降は全件成功した。
初回の `git diff --cached --check` は新規ADRと設計文書のEOF余分空行を2件検出した。末尾だけを修正して全検証を再実行し、最終cached checkは成功した。

## Warnings

Core、Smoke、Misogiのコンパイラ警告は0件。
GitはWindowsの `core.autocrlf` によるLFからCRLFへの変換予告を表示したが、whitespace errorはない。
独立したPersistent Causal World引き継ぎ文書はcheckoutと添付テキスト領域から発見できなかった。採択済み契約と今回の明示要件で判断し、機能実装を妨げる矛盾はなかった。

## Design Decisions

- `その場警戒` は物理速度ではなく、自発移動だけを抑制するread modelへ投影する。
- 判断値をprivateにし、許可・抑制・理由・由来状態の不整合を外部構築させない。
- 評価Tickと反応状態成立Tickを区別する。
- 対象Entityだけを判断し、他Entity状態を集約しない。
- 終了状態とEntity種別は制御理由へ混ぜない。
- Movement実接続は、自発移動意図型とgateの契約確定後へ送る。

## Non-Goals

- 自発移動意図型、locomotion command、Movement Adapter
- 入力filter、プレイヤー移動抑制、NPC巡路停止
- 速度ゼロ、位置固定、重力・外力・落下・ノックバック停止
- 反応状態解除、通常復帰、期限、ヒステリシス、cooldown
- Event、因果操作、因果記録、統合台帳追記、Replay変更
- Field契約、SDF、Navigation更新、Renderer接続

## Deferred Work

自発移動意図を何として表し、許可判断だけをどの時点でMovement Adapterへ通すかは後続設計とする。
その場警戒の解除、巡路保留・復帰、物理外力との合成規則も今回決定していない。

## Next Candidates

1. 自発移動意図の最小契約
2. 自発移動制御判断による意図gate
3. Movement Adapterへの接続
4. その場警戒状態の解除要求と解除因果操作
5. 巡路進行の保留・復帰
6. Persistent Causal Worldの最小Field契約

技術的には1を最優先とする。入力やAI由来の移動を外力から分ける明示型が先にあれば、抑制判断を物理速度ゼロへ短絡せず安全にgateできる。

## Human Confirmation

- 自発移動意図の最小契約と、どのEntityがlocomotion適格かは次作業で確認が必要。
- 終了状態の全体停止は既存上位実行順序へ残し、この判断の責務には含めていない。
- GitHub認証、push、PR、merge、GitHub Actionsは今回の責務外。
