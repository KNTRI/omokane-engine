# Codex Run Report

title: 思兼神Core 行動候補の決定論的な最小選択

generated_at: 2026-08-11T12:07:42+09:00

## Summary

- 同一主体の妥当な行動候補一覧から最高優先度を一件選ぶ純粋APIを追加した。
- 同率最高時は入力一覧で最初の候補を選び、候補ID、仮説、種別をタイブレークに使わない。
- 候補全件検証、実行者ID一致、候補ID一意性を選択前に固定順で検証する。
- privateな選択済み値が入力候補一覧、選択位置、選択候補を保持する。
- 因果認識閉ループと統合Replayから生成した複数候補を選択できることを確認した。
- 選択はWorld Truth、Event、因果操作、統合因果台帳を変更しない。

## Changed Files

- src/Omokane.Core/Domain/ActionCandidateSelection.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/ActionCandidateSelectionTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-006-ai-non-omniscience.md
- docs/adr/ADR-016-minimal-alert-action-candidate.md
- docs/adr/ADR-018-causal-cognitive-action-loop.md
- docs/adr/ADR-021-deterministic-action-candidate-selection.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/architecture/world_intelligence_kernel.md
- docs/design/action_candidate_selection.md
- docs/design/tick_execution_order.md
- docs/design/procedural_sandbox_minimum_model.md
- docs/design/causal_ground_vibration_cognitive_loop.md
- docs/design/integrated_causal_ledger_and_replay.md
- docs/design/glossary.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-08-11_1207_omokane-core-deterministic-action-candidate-selection.md

## Public API

- `行動候補選択理由.単独最高優先度`
- `行動候補選択理由.同率最高入力順`
- private表現の `選択済み行動候補`
- `選択済み行動候補.実行者ID`
- `選択済み行動候補.候補一覧`
- `選択済み行動候補.候補数`
- `選択済み行動候補.選択位置`
- `選択済み行動候補.選択候補`
- `選択済み行動候補.却下候補一覧`
- `選択済み行動候補.同率最高候補一覧`
- `選択済み行動候補.選択理由`
- `行動候補選択.選ぶ : 行動候補 list -> Result<選択済み行動候補 option, string list>`

## Architecture Audit

- 行動候補は判断層の派生値であり、World Truth、入力、行動結果、因果操作ではない。
- 優先度は警戒意図の警戒度を保持し、由来警戒意図は構造的に保存される。
- 統合因果台帳は位置変更と地盤振動発生だけを保存し、候補や選択済み値を保存しない。
- 既存Movementは直接更新の互換経路として維持した。
- `Input.fs` と `Systems/Omokane.fs` は変更していない。

## Candidate Validation

- 空一覧は `Ok None` とする。
- 非空一覧は全候補を入力順で `行動候補.検証する` に通す。
- 候補エラーへ `行動候補[index]:` を付け、全件を固定順で収集する。
- 一件でも不正なら優先度を比較しない。

## Actor Boundary

先頭候補の実行者IDを基準に全候補の一致を要求する。
複数主体を自動groupByせず、不一致を契約エラーとして返す。

## Candidate ID Invariant

一覧内の行動候補ID重複を拒否する。
Mapは出現回数の検索だけに使い、その列挙順を候補順や選択順へ使わない。

## Selection Rule

候補一覧をsortせず一回走査し、現在候補より優先度が大きい場合だけ選択を置き換える。
優先度、警戒意図、仮説、候補種別を再計算または再評価しない。

## Tie Break

同率時は置き換えず、入力一覧で最初の最高候補を選ぶ。
候補IDの辞書順、仮説、種別、Tick、感知種別は使用しない。

## Selected Candidate Model

選択済み値は実行者ID、入力候補一覧、0始まりの選択位置、その位置の候補を保持する。
却下候補一覧は選択位置だけを除外し、同率最高候補一覧は最高優先度の候補だけを入力順で返す。

## Provenance

入力候補と由来警戒意図を再構築せず保持する。
因果認識経路とReplay経路の信号ID、観測者ID、仮説、確率、警戒度、優先度の由来を正式禊Testで確認した。

## World Truth Boundary

選択APIはゲーム状態、Entity一覧、現在Tick、乱数Seed、因果台帳を受け取らず、状態を変更しない。

## Event Boundary

選択APIはEventを返さず、生成もdispatchもしない。
統合ReplayのEvent一覧が選択前後で不変であることを確認した。

## Causal Boundary

選択APIは因果操作を生成せず、統合因果台帳へ追記しない。
選択済み候補は正式な世界変更や世界史ではない。

## Integrated Cognitive Path

同一観測者について強度の異なる二つの正式因果認識経路から候補を生成し、高い優先度の候補を選択した。
同率統合経路ではID辞書順と逆の先着候補を選択した。

## Replay Integration

二件の地盤振動発生記録を統合因果台帳へ保存し、統合Replayが再出力した正式信号から二候補を生成して選択した。
Replay信号を再構築せず、ゲーム状態、Event、台帳、由来警戒意図が選択後も不変であることを確認した。

## Determinism Audit

- 追加本番コードで時刻、GUID、乱数、Task、Parallel、Thread、Stopwatch、Dictionary、HashSetの使用は0件。
- sort、maxBy、`>=`、重複排除、候補IDタイブレークの使用は0件。
- Mapの使用は候補ID出現回数の検索だけ。
- 252種類の候補一覧Matrixを各2回実行し、構造的完全一致と入力順保持を確認した。

## Test Count

- 既存Test: 422件
- 新規Test: 61件
- 最終総数: 483件
- 候補選択Matrix: 252種類
- 削除・弱体化した既存Test: 0件

## Repeated Verification

- 1回目: 483 passed, 0 ignored, 0 failed, 0 errored
- 2回目: 483 passed, 0 ignored, 0 failed, 0 errored
- 3回目: 483 passed, 0 ignored, 0 failed, 0 errored
- 4回目: 483 passed, 0 ignored, 0 failed, 0 errored
- 5回目: 483 passed, 0 ignored, 0 failed, 0 errored

## Verification Commands

dotnet build .\src\Omokane.Core\Omokane.Core.fsproj
dotnet build .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet run --project .\tests\Omokane.Core.Smoke\Omokane.Core.Smoke.fsproj
dotnet build .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
dotnet run --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
dotnet run --no-build --no-restore --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
dotnet list .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj package --no-restore
python -c "from pathlib import Path; markers='繝縺譁譌逕莨蜷'; files=list(Path('src/Omokane.Core').rglob('*.fs'))+list(Path('tests/Omokane.Core.Misogi').rglob('*.fs'))+list(Path('docs').rglob('*.md')); bad=[(str(p),m) for p in files for m in markers if m in p.read_text(encoding='utf-8')]; print(f'UTF-8 scan: {len(files)} files, mojibake markers: {len(bad)}'); print(bad); raise SystemExit(1 if bad else 0)"
rg -n "DateTime\.Now|DateTime\.UtcNow|Guid\.NewGuid|\bRandom\b|System\.Random|Task\.Run|Parallel\.|\bThread\b|Environment\.TickCount|\bStopwatch\b|\bDictionary\b|\bHashSet\b" src/Omokane.Core/Domain/ActionCandidateSelection.fs
rg -n "sort|maxBy|maxByDescending|>=|List\.distinct|distinctBy|ゲーム状態|ゲームイベント|因果操作|統合因果台帳" src/Omokane.Core/Domain/ActionCandidateSelection.fs
git diff --check
git status --short --ignored
git diff --stat

## Verification Result

- Core build: 成功、警告0、エラー0
- Smoke build: 成功、警告0、エラー0
- Smoke run: Movement、KengouAsset、WorldIntelligence Contracts、Validationすべて成功
- Misogi build: 成功、警告0、エラー0
- Misogi run: 483 passed、0 ignored、0 failed、0 errored
- 5回連続禊Test: 全回483 passed
- NU1900: なし
- 外部依存追加: なし
- UTF-8 marker scan: 145ファイル、文字化けmarker 0件
- ignored: bin/、obj/、__pycache__/ を維持

## Errors

- なし

## Warnings

- build警告なし。
- `git diff --check` はwhitespace errorなし。WindowsのautocrlfによるLFからCRLFへの変換予告だけを表示した。
- Expecto 11.1.0とFSharp.Core 10.1.301は既存参照であり、PackageReferenceを追加していない。

## Design Decisions

- 安定した入力順を上流の正式順序として扱う。
- privateな選択済み値により候補一覧と選択位置の整合性を保つ。
- 説明可能性は候補一覧、選択位置、却下候補、同率最高候補、選択理由で確保する。
- 現在の一種類の候補種別へ暗黙の補正を追加しない。

## Non-Goals

- 候補実行、入力生成、Entity・ゲーム状態更新
- 因果操作生成、Event発行、統合因果台帳追記
- 複数主体一括選択、候補評価、種別補正
- ヒステリシス、cooldown、継続意図
- Utility AI、Behavior Tree、確率的選択

## Deferred Work

選択済み候補からNPC状態反応要求を作り、別の因果操作境界を通してWorld Truthへ反映する処理は未実装である。

## Next Candidates

1. 選択済みその場警戒候補からNPC状態反応要求を生成
2. NPC状態反応要求を正式な因果操作へ変換
3. NPC状態反応をWorld Truthへ適用
4. 反応結果を統合因果台帳へ保存・Replay
5. 複数行動候補種別
6. ヒステリシスと継続意図

技術的には、選択と実行の責務分離を維持できる1を次に進めるのが最も安全である。

## Human Confirmation

- ローカルcommitの差分、公開API、ADR-021、483件の検証結果を確認してください。
- push、PR作成、mergeは今回の責務外であり実施しません。
