# Codex Run Report

title: 思兼神Core 因果認識閉ループ v0.1

generated_at: 2026-08-09T10:38:05+09:00

## Summary

- 正式な因果操作から地盤振動の権威伝播信号を生成し、既存の認識境界を経てその場警戒行動候補まで到達する純粋な閉ループを追加した。
- 地盤振動発生の単体実行と原子的バッチ、専用発生記録、Provenance Chainを実装した。
- 既存Movement、位置変更因果実行器、位置変更専用因果台帳と再生、既存認識数式は変更していない。

## Changed Files

- src/Omokane.Core/Domain/GroundVibrationCausalEmission.fs
- src/Omokane.Core/Domain/CausalGroundVibrationCognitiveLoop.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/GroundVibrationCausalEmissionTests.fs
- tests/Omokane.Core.Misogi/CausalGroundVibrationCognitiveLoopTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-003-causal-transaction.md
- docs/adr/ADR-006-ai-non-omniscience.md
- docs/adr/ADR-009-propagation-entropy-foundations.md
- docs/adr/ADR-015-ground-vibration-alert-vertical-slice.md
- docs/adr/ADR-016-minimal-alert-action-candidate.md
- docs/adr/ADR-017-causal-ground-vibration-emission.md
- docs/adr/ADR-018-causal-cognitive-action-loop.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/architecture/world_intelligence_kernel.md
- docs/architecture/physical_information_propagation.md
- docs/design/tick_execution_order.md
- docs/design/inga_log_shintaku_debug_minimum_spec.md
- docs/design/procedural_sandbox_minimum_model.md
- docs/design/glossary.md
- docs/design/causal_ground_vibration_cognitive_loop.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-08-09_1036_omokane-core-causal-ground-vibration-cognitive-loop.md

## Public API

- `地盤振動発生操作`
- `地盤振動発生記録` と `地盤振動発生記録.検証する`
- `地盤振動発生結果` と `地盤振動発生実行.適用する`
- `地盤振動発生列結果` と `地盤振動発生列実行.一括適用する`
- `因果地盤振動行動入力`
- `因果地盤振動行動結果` と `因果地盤振動行動経路.実行する`

## Architecture Audit

- 採択済みADR、公開型、既存禊Testを優先して、因果実行、位置変更専用台帳・再生、伝播、感知、観測、信念、警戒意図、行動候補の境界を確認した。
- 現行因果台帳と再生はEntity位置変更結果専用であり、地盤振動をダミー位置変更として保存できないことを確認した。
- 現行Movementは `思兼神.更新する` の直接更新経路に残るため、ADR-003は `Proposed` のまま維持した。
- 新規本番ファイルは `更新結果` と `因果操作失敗分類` を使うため、F#コンパイル順を `CausalExecution.fs` の後へ置いた。

## Causal Emission

- `因果操作種別.伝播信号生成` と対象EntityID未指定を要求する。
- 原因因果ID、発生源EntityID、量種別、媒体を実行器が固定し、呼び出し側による由来偽装を防ぐ。
- 成功時もゲーム状態とTickは不変で、伝播信号と専用発生記録を独立した生成物として返す。
- 専用発生記録は公開検証を持つが、まだ正式な異種因果台帳記録ではない。

## Batch Atomicity

- 全操作の静的契約を先に入力順で検証し、因果操作IDと伝播信号IDの重複を拒否する。
- 全件成功時だけ信号、記録、Eventを入力順で公開する。
- 一件でも失敗した場合は元状態だけを返し、途中の信号、記録、Eventを公開しない。
- 空一覧は状態不変、Event空、信号空、記録空の恒等操作として成功する。

## Closed Loop

```text
地盤振動発生実行
→ 地盤振動警戒経路
→ 行動候補生成
```

- 既存の感知、観測、信念候補、警戒意図、行動候補生成を順次合成した。
- 非感知、警戒不成立、行動候補成立を正常結果として区別した。
- 発生、認識、行動候補の失敗理由へ固定した段階名を付けた。

## Provenance Chain

- 因果操作ID = 発生記録の因果操作ID = 信号の原因因果ID。
- 因果実行者ID = 発生記録の実行者ID = 信号の発生源EntityID。
- 発生操作の信号ID = 正式信号ID = 感知値の信号ID。
- 観測者IDは感知値、観測、警戒意図、行動候補まで一致する。
- 仮説は信念候補、警戒意図、行動候補まで一致する。
- 信念確率 = 警戒度 = 行動候補優先度を再計算せず保持する。

## World Truth Boundary

- 発生、認識、候補生成の全段階でゲーム状態、Entity一覧、Tick、乱数Seed、終了状態を変更しない。
- 認識経路へゲーム状態やEntity一覧を渡さず、正式な権威伝播信号だけを渡す。
- 行動候補はWorld Truth、入力、行動結果、因果操作ではない。

## Event Boundary

- 発生因果操作が明示したEventだけを入力順で返す。
- 認識、信念、警戒意図、行動候補生成はEventを追加しない。
- 失敗したバッチは先行仮成功Eventも公開しない。

## Determinism Audit

- 新規本番コードに `DateTime.Now`、`DateTime.UtcNow`、`Guid.NewGuid`、`Random`、`Task.Run`、`Parallel`、`Thread`、`Environment.TickCount`、ゲーム判定用 `Stopwatch`、`Dictionary`、`HashSet` は存在しない。
- Mapは重複IDの件数検索だけに使用し、その列挙順を出力へ使用していない。
- 距離減衰、信念確率、警戒閾値、優先度の計算式を新規本番コードへ複製していない。
- 168組み合わせの強度・距離・警戒閾値Matrixを各2回実行し、構造的完全一致を確認した。
- 敵対的レビューで、種別要求、原因・発生源の固定、状態不変、Event境界、バッチ原子性、既存API委譲を再確認し、Movement・既存因果台帳・再生に差分がないことを確認した。

## Test Count

- 既存禊Test: 264件
- 新規地盤振動因果発生Test: 43件
- 新規因果認識閉ループTest: 24件
- 新規Test合計: 67件
- 最終総数: 331件、成功331、ignored 0、failed 0、errored 0

## Repeated Verification

- 正式な `dotnet run --project` で331件成功を確認した。
- 復元済み成果物へ固定した連続実行1: 331件成功。
- 連続実行2: 331件成功。
- 連続実行3: 331件成功。
- 連続実行4: 331件成功。
- 連続実行5: 331件成功。

## Verification Commands

dotnet build .\\src\\Omokane.Core\\Omokane.Core.fsproj
dotnet build .\\tests\\Omokane.Core.Smoke\\Omokane.Core.Smoke.fsproj
dotnet run --project .\\tests\\Omokane.Core.Smoke\\Omokane.Core.Smoke.fsproj
dotnet build .\\tests\\Omokane.Core.Misogi\\Omokane.Core.Misogi.fsproj
dotnet run --project .\\tests\\Omokane.Core.Misogi\\Omokane.Core.Misogi.fsproj
dotnet run --no-build --no-restore --project .\\tests\\Omokane.Core.Misogi\\Omokane.Core.Misogi.fsproj (5 consecutive runs)
python -m py_compile tools/codex_ops/write_run_report.py
python -m py_compile tools/codex_ops/print_latest_report.py
python -m py_compile tools/codex_ops/copy_latest_report.py
dotnet list .\\tests\\Omokane.Core.Misogi\\Omokane.Core.Misogi.fsproj package --no-restore
rg -n "DateTime\\.(Now|UtcNow)|Guid\\.NewGuid|System\\.Random|Random|Task\\.Run|Parallel\\.|Thread|Environment\\.TickCount|Stopwatch|Dictionary|HashSet" <new production files>
rg -n "sqrt|距離係数|1\\.0\\s*-|根拠総量|反証総量|発生閾値|警戒度|優先度" <new production files>
git diff --check
git status --short --ignored
git diff --stat
python -c "<UTF-8 mojibake marker scan>" <all changed files>

## Verification Result

Core、Smoke、Misogiは警告0・エラー0。Smoke成功。禊Testは331件中331件成功し、5回連続で同一件数・失敗0を確認した。新規Testは67件。変更ファイルのUTF-8読取と文字化けマーカー検査も成功した。

## Errors

- None.

## Warnings

- サンドボックス内の最初の反復実行では既存Expectoメタデータ再照会がNU1301となったが、許可環境の正式コマンドと--no-restore反復は成功した。NU1900は発生していない。
- F#コンパイル順は更新結果と失敗分類への依存により、新規本番ファイルをCausalExecution.fsの後へ配置した。
- `git diff --check` は空白エラーなし。Windowsの既存改行設定によるLFからCRLFへの予告だけを表示した。

## Design Decisions

- 地盤振動発生を位置変更因果実行器へ混在させず、専用操作・実行結果・発生記録で表した。
- 地盤振動発生記録を位置変更専用因果台帳へ偽装保存しない。
- 閉ループは既存APIの合成に限定し、数式や判断規則の新しい正本を作らない。
- 失敗分類は発生実行器で保持し、閉ループでは巨大なError階層を増やさず段階名付き理由へ変換した。

## Non-Goals

- 既存因果台帳の全面異種化、信号のダミー位置変更保存、因果Replay全面改修
- 信号移動、反射、遮蔽、媒体Field、寿命更新、複数信号・複数観測者
- 候補選択、行動実行、NPC状態更新、新しい因果操作生成
- 既存Movement統合、Event dispatch、保存、シリアライズ、Renderer接続

## Deferred Work

- 地盤振動発生記録の正式保存と結果再生
- 行動候補の選択境界
- 選択済み候補からNPC状態反応を要求する因果操作
- 地盤振動信号の寿命と時間発展

## Next Candidates

- 1. 異種因果台帳の設計と地盤振動生成記録の正式保存。由来を永続履歴へ接続するため技術的な次優先。
- 2. 行動候補の最小選択。
- 3. 選択されたその場警戒候補からNPC状態反応を因果操作として生成。
- 4. 地盤振動信号寿命と時間発展。

## Human Confirmation

因果発生記録を位置変更専用因果台帳へ保存しない境界と、候補選択・NPC状態反映を後続とした範囲を確認してください。
