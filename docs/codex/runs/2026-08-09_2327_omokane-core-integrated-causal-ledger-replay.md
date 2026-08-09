# Codex Run Report

title: 思兼神Core 統合因果台帳 v0.1と異種因果結果Replay

generated_at: 2026-08-09T23:27:20+09:00

## Summary

- Entity位置変更と地盤振動発生を、種類の意味を保ったまま一つの正式因果順へ保存する `統合因果台帳` を追加した。
- ゲーム状態、記録済みEvent、地盤振動の正式信号を保存順で原子的に再構築する `統合因果台帳再生` を追加した。
- 位置変更専用の既存 `因果台帳`、`因果台帳再生`、331件の既存禊Testを維持した。
- 新規禊Test 91件と216履歴の決定論Matrixを追加し、全422件が成功した。
- Namespaceは `Omokane.*` のまま、外部依存追加、Movement変更、push、PR、mergeは行っていない。

## Changed Files

Production:

- `src/Omokane.Core/Domain/CausalLedger.fs`
- `src/Omokane.Core/Domain/CausalReplay.fs`
- `src/Omokane.Core/Domain/IntegratedCausalLedger.fs`
- `src/Omokane.Core/Domain/IntegratedCausalReplay.fs`
- `src/Omokane.Core/Omokane.Core.fsproj`

Misogi:

- `tests/Omokane.Core.Misogi/IntegratedCausalLedgerTests.fs`
- `tests/Omokane.Core.Misogi/IntegratedCausalReplayTests.fs`
- `tests/Omokane.Core.Misogi/IntegratedCausalHistoryCompatibilityTests.fs`
- `tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj`
- `tests/Omokane.Core.Misogi/Program.fs`

ADR and design:

- `docs/adr/ADR-003-causal-transaction.md`
- `docs/adr/ADR-008-observability-replay.md`
- `docs/adr/ADR-011-causal-ledger-result-replay.md`
- `docs/adr/ADR-017-causal-ground-vibration-emission.md`
- `docs/adr/ADR-018-causal-cognitive-action-loop.md`
- `docs/adr/ADR-019-integrated-causal-ledger.md`
- `docs/adr/ADR-020-integrated-causal-replay.md`
- `docs/architecture/index.md`
- `docs/architecture/current_system_mapping.md`
- `docs/architecture/world_intelligence_kernel.md`
- `docs/design/integrated_causal_ledger_and_replay.md`
- `docs/design/inga_log_shintaku_debug_minimum_spec.md`
- `docs/design/causal_ground_vibration_cognitive_loop.md`
- `docs/design/tick_execution_order.md`
- `docs/design/procedural_sandbox_minimum_model.md`
- `docs/design/glossary.md`

Run Report:

- `docs/codex/runs/latest.md`
- `docs/codex/runs/2026-08-09_2327_omokane-core-integrated-causal-ledger-replay.md`

## Public API

- `統合因果記録種別`
- private union `統合因果記録` と共通・種別固有アクセサ
- `統合因果追記項目`
- private `統合因果台帳`
- `統合因果台帳追記結果`
- `統合因果台帳.空 / 記録一覧 / 件数`
- 種別別件数・記録抽出、因果操作ID検索、伝播信号ID検索
- 原子的な `統合因果台帳.一括追記する` と種別別wrapper
- `統合因果台帳.既存因果台帳から移行する`
- `統合因果台帳再生結果`
- `統合因果台帳再生.再生する`

## Architecture Audit

- 既存 `因果台帳` と `因果台帳再生` はEntity位置変更専用だった。
- `地盤振動発生記録` は専用の成功結果だが、正式台帳へ未保存だった。
- 言霊Eventは通知であり、World Truthではない。
- 地盤振動発生は状態を変更せず、伝播信号を独立生成物として返す。
- 既存Movementは `思兼神.更新する` の直接更新経路として残る。
- ADR-003は全面Transaction化未完了のため `Proposed` を維持した。

## Legacy Compatibility

- 既存公開型・関数の名前とシグネチャを変更していない。
- 位置候補の一件正式化だけを既存検証・変換規則からinternal共有した。
- 位置変更Replayの一件検証・適用だけをinternal helperへ抽出し、既存Replayも同じhelperを使用する。
- 共有化直後に既存331件が成功した。
- 既存台帳と移行済み統合台帳について、成功結果と失敗index・ID・理由の等価性を正式禊Testで確認した。

## Integrated Record Model

- `統合因果記録` はEntity位置変更記録または地盤振動発生記録をprivate unionで保持する。
- 地盤振動へダミー位置を与えず、位置変更へダミー信号を与えない。
- 共通メタデータはアクセサ、種別固有値はOptionアクセサから取得する。
- 不正な正式記録一覧を外部から直接構築できない。

## Append Atomicity

- 全追記項目を入力順で静的検証してから正式化する。
- 一件でも不正なら元台帳、最小index、因果操作ID、固定順理由だけを返す。
- 後方失敗でも部分記録や部分件数を公開しない。
- 全件成功時だけ、既存記録の後ろへ入力順で一括追記する。
- 空一覧は恒等操作として成功する。

## Global Invariants

- 因果操作IDは位置変更・地盤振動を通じて一意。
- Tickは統合台帳全体で非減少。同一Tickは許可。
- 地盤振動の伝播信号IDは台帳全体で一意。
- 記録、Event、信号は入力順を維持し、IDやTickで再ソートしない。
- MapとSetは検索にだけ使い、その列挙順を正式出力へ使用しない。

## Migration

- 既存因果台帳から統合因果台帳への一方向移行を追加した。
- 記録順、件数、Tick、Eventを維持し、すべてEntity位置変更種別となる。
- 元の既存因果台帳は変更せず、obsolete化も行っていない。

## Integrated Replay

- 位置変更は既存Replay helperで変更前位置と状態前提を検証して適用する。
- 地盤振動は記録契約、Tick、終了状態、発生元一意性を検証し、記録済み正式信号をそのまま再出力する。
- Tickは各記録Tickへ進め、同一Tickと飛びを許可し、逆行を拒否する。
- 信号寿命、移動、減衰、反射、遮蔽、混合を再計算しない。

## Event Boundary

- Eventは台帳順・記録内順で再出力する通知である。
- Entity生成・削除、ダメージ、ゲーム終了Eventからゲーム状態を変更しない。
- Tickの飛びでも `Tick進行` Eventを合成しない。

## Signal Boundary

- 再生信号は地盤振動発生記録に保存された値と構造的に同一である。
- Replayは信号を再構築、更新、削除、重複排除しない。
- 再生信号一覧は統合台帳内の地盤振動記録順を維持する。

## Replay Atomicity

- 一件でも失敗した場合は入力された初期状態を返す。
- 途中状態、途中Event、途中信号、成功件数を公開しない。
- 失敗位置、失敗因果操作ID、固定順理由だけを返す。

## Cognitive Reconstruction

- 地盤振動発生実行の正式記録を統合台帳へ保存し、Replay信号を既存認識経路へ渡せる。
- Replay信号から感知値、観測、信念候補、警戒意図、その場警戒行動候補まで再構築できた。
- 元の因果認識閉ループとReplay後の正式信号・警戒意図が構造的に一致した。

## Determinism Audit

- 追加・変更した本番コードに `DateTime.Now`、`DateTime.UtcNow`、`Guid.NewGuid`、`Random`、`Task.Run`、`Parallel`、`Thread`、`Environment.TickCount`、`Stopwatch`、`Dictionary`、`HashSet` はない。
- 統合層に距離減衰、感知確信度、信念確率、警戒閾値、優先度、地盤振動信号構築の数式を複製していない。
- 位置候補の検証と位置Replayは既存コードのinternal共有境界へ委譲した。
- 216種類の混在履歴をそれぞれ2回Replayし、構造的結果、Event順、信号順の一致を確認した。

## Test Count

- 既存禊Test: 331
- 新規禊Test: 91
- 最終総数: 422
- 決定論Matrix: 216混在履歴
- ignored: 0
- failed: 0
- errored: 0

## Repeated Verification

- 正式実行: 422 passed
- 連続1回目: 422 passed
- 連続2回目: 422 passed
- 連続3回目: 422 passed
- 連続4回目: 422 passed
- 連続5回目: 422 passed
- 全回で ignored 0 / failed 0 / errored 0

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
git diff --check
git status --short --ignored
git diff --stat

## Verification Result

- Core build: 成功、警告0、エラー0
- Smoke build: 成功、警告0、エラー0
- Smoke run: Movement / KengouAsset / WorldIntelligence Contracts / Validationすべて成功
- Misogi build: 成功、警告0、エラー0
- Misogi run: 422 passed
- 5回連続Misogi: 各422 passed
- NU1900: なし
- NuGet restore: 全プロジェクト最新
- 外部依存追加: なし。既存Expecto 11.1.0のみ
- `git diff --check`: whitespace errorなし。GitのLFからCRLFへの変換予告のみ
- `bin/`、`obj/`、`__pycache__/`: ignoredのまま

## Errors

- なし

## Warnings

- `git diff --check` はWindows設定によるLFからCRLFへの変換予告を表示した。whitespace errorではない。
- 統合因果台帳はインメモリであり、プロセス終了後の永続性を持たない。
- 統合Replayは記録済み結果の再適用であり、完全な入力・AI・乱数再シミュレーションではない。

## Design Decisions

- 既存台帳を置換せず、後継APIを `統合因果台帳` として追加した。
- 異種結果はダミー値を持つ共通レコードではなくprivateな判別共用体で保持した。
- 追記とReplayは全成功または全失敗とし、部分成果物を公開しない。
- EventとWorld Truth、Eventと伝播信号、神託Debugを別責務のまま維持した。
- ADR-003は現行Movement直更新が残るためProposedのまま維持した。

## Non-Goals

- 既存因果台帳の削除、改名、obsolete化
- JSON・バイナリ保存、セーブ統合、Snapshot、巻き戻し、任意位置Replay
- 入力、AI、権能、乱数を再実行する完全な記紀Replay
- 信号寿命更新、移動、反射、遮蔽、媒体Field、複数信号合成
- 行動候補選択、行動実行、NPC状態更新、既存Movement統合
- 親因果グラフ、ハッシュチェーン、暗号署名、ネットワーク、Renderer接続

## Deferred Work

- 統合因果台帳のファイル永続化とSnapshot
- 位置変更・地盤振動以外の因果結果
- 親因果IDを辿る因果グラフ
- 完全な記紀Replayと任意時点再生
- 地盤振動信号の寿命と時間発展

## Next Candidates

1. 行動候補の最小選択
2. 選択済み `その場警戒` 候補からNPC状態反応要求を生成
3. NPC状態反応を正式な因果操作として適用
4. 統合因果台帳のファイル永続化とSnapshot
5. 原因因果IDを含む親因果グラフ
6. 地盤振動信号の寿命と時間発展

技術的には、次は行動候補の最小選択が適切である。World Truthをまだ変更せず、判断候補から実行要求へ進む境界を先に固定できる。

## Human Confirmation

- ローカルcommit後のpush、PR作成、mergeはユーザー側の後続工程とする。
- 統合因果台帳の永続化、Snapshot、完全な記紀Replayは別タスクとして確認が必要である。
