# Codex Run Report

title: 主体情報付き互換移動命令Adapter v0.1

generated_at: 2026-09-08T12:40:03+09:00

## Summary

- 通過左右を主体付き命令へ変換し、制御抑制と上下未対応を区別するpure Adapterを実装。

## Changed Files

- src/Omokane.Core/Domain/VoluntaryLocomotionCompatibilityAdapter.fs
- src/Omokane.Core/Omokane.Core.fsproj
- tests/Omokane.Core.Misogi/VoluntaryLocomotionCompatibilityAdapterTests.fs
- tests/Omokane.Core.Misogi/Omokane.Core.Misogi.fsproj
- tests/Omokane.Core.Misogi/Program.fs
- docs/adr/ADR-023-entity-reaction-state.md
- docs/adr/ADR-025-reaction-aware-voluntary-locomotion-control.md
- docs/adr/ADR-026-voluntary-locomotion-intent.md
- docs/adr/ADR-027-voluntary-locomotion-intent-gate.md
- docs/adr/ADR-028-voluntary-locomotion-compatibility-adapter.md
- docs/architecture/index.md
- docs/architecture/current_system_mapping.md
- docs/architecture/world_intelligence_kernel.md
- docs/design/voluntary_locomotion_compatibility_adapter.md
- docs/design/voluntary_locomotion_intent_and_gate.md
- docs/design/reaction_state_locomotion_read_boundary.md
- docs/design/procedural_sandbox_minimum_model.md
- docs/design/tick_execution_order.md
- docs/design/glossary.md
- docs/codex/runs/latest.md
- docs/codex/runs/2026-09-08_1238_omokane-core-voluntary-locomotion-compatibility-adapter.md

## Public API

互換移動命令種別・互換移動非生成理由、privateな互換移動命令・互換移動変換結果、全指定アクセサを追加。自発移動互換変換.変換する / 一括変換する、互換移動変換結果一覧の4 Queryを公開。

## Architecture Audit

mainは6d0221cf6b899228b3bb975db2bffce63e0685e6と一致、開始時clean。Input全7case、四方向意図、private Gate、反応状態と履歴、Movement、既存Test、ADR-003/023/025/026/027、設計文書を照合。既存833件を事前実行して成功。

## Persistent Causal World Alignment

添付背景を設計資料として確認。World Truth -> Derived Decision -> Execution Planning -> Compatibility Adapterを独立規則で合成。正式な因果適用結果と再導出可能な一時値を分離する。

## Existing Movement Constraints

現行Movementはゲーム状態.プレイヤーIDに一致し、種別もプレイヤーであるEntityの左右X座標だけを更新する。上下Inputは位置へ反映されない。

## Actor Preservation

命令は元の実行者IDを由来意図から取得する。敵・弾・障害物も主体付き命令を生成できるが、プレイヤーへ適用できるとは扱わない。

## Compatibility Command Model

命令種別と保存Gateだけを持つprivate DU。Tick・主体・意図ID・方向・Inputを重複保存しない。外部FSIからの不正構築はFS1093で拒否された。

## Command Kind

左へ移動・右へ移動だけ。既存Inputへの投影も2caseの明示match。

## Conversion Result

privateな命令生成または理由付き未生成。全結果で元Gateと意図へ戻れる。外部FSIからの未生成構築もFS1093で拒否された。

## Suppression Precedence

通過意図なしを最初に判定し、自発移動抑制を返す。抑制上・下を方向未対応へ分類しない。

## Unsupported Direction

通過上・下は上方向未対応・下方向未対応。正常な非生成であり、Error・左右代替・入力なし・命令成功にしない。

## Total Conversion

Gateのprivate契約を信頼する全域pure関数。Result、再検証、Gate再実行、意図再生成を追加しない。

## Batch Conversion

入力順map。一入力一結果。空一覧、複数主体、複数Tick、同じGateの反復を許可する。

## Order Preservation

全Queryが順序と重複を維持。sort・distinct・groupBy・方向合成・相殺なし。

## Provenance

保存Gate -> 意図と判断 -> 反応状態 -> 要求 -> 選択候補 -> 警戒 -> 信念 -> 観測 -> 感知を追跡。要求ID・成立因果IDは既存アクセサへ委譲する。

## Entity Boundary

Entity存在・種別・locomotion能力の検証は次段。命令主体とゲーム状態.プレイヤーIDの照合前にInputだけを既存Movementへ渡さない。

## Input Boundary

Input.fs無変更。入力caseの追加・削除・改名なし。非移動Inputは既存意図生成でNoneとなる。

## Movement Boundary

Systems/Omokane.fs無変更。本番から思兼神.更新するを呼ばず、実際の警戒中Movement抑制は未接続。

## Physical State Boundary

位置・速度・HP・当たり判定・外力を扱わない。抑制は物理速度ゼロを意味しない。

## World Truth Boundary

互換命令と結果は一時値。GameState、Entity、Tick、Seed、終了状態を変更しない。

## Event And Causal Boundary

Event・因果操作・更新結果を生成しない。統合因果台帳とReplayの本番コードは基準SHAから無変更。

## Legacy Movement Compatibility

Test内限定で、プレイヤー左右命令の既存Input適用と直接左右Input適用の更新結果が構造的一致。

## Full Cognitive Path

正式地盤振動 -> 台帳 -> Replay信号 -> 認識 -> 候補選択 -> 反応要求 -> 状態変更因果 -> 制御判断 -> Input右意図 -> Gate -> 互換変換を完走。反応適用前は右命令、適用後は制御抑制。

## Replay Integration

live状態とReplay状態から同じ意図を評価した互換結果が構造的一致。要求ID・成立因果ID・意図を保持し、Event・信号・台帳・Entity物理値不変を確認。

## Determinism Audit

本番追加ファイルの非決定論API検索は0件。Map/Setも不使用。物理値・GameState・再生成・再適用・sort/distinct/groupBy検索も0件。単一Matrix2304 + 一括Matrix42 = 2346ケースを各2回比較。

## Test Count

既存833件 + 新規96件 = 最終929件。既存Testファイル無変更（登録用Program/fsprojの追加だけ）。Matrixはケース数をTest数へ水増しせず2件として登録。

## Repeated Verification

正式build/run後の5回連続結果（total / passed / ignored / failed / errored）:
1: 929 / 929 / 0 / 0 / 0
2: 929 / 929 / 0 / 0 / 0
3: 929 / 929 / 0 / 0 / 0
4: 929 / 929 / 0 / 0 / 0
5: 929 / 929 / 0 / 0 / 0

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
rg -n 'DateTime\.(Now|UtcNow)|Guid\.NewGuid|Random|Task\.Run|Parallel\.|Thread|Environment\.TickCount|Stopwatch|Dictionary|HashSet|Map\.|Set\.' src/Omokane.Core/Domain/VoluntaryLocomotionCompatibilityAdapter.fs
rg -n 'sort|distinct|groupBy|maxBy|ゲーム状態|更新結果|ゲームイベント|思兼神|Replay|位置|速度|HP|生成\.|適用する' src/Omokane.Core/Domain/VoluntaryLocomotionCompatibilityAdapter.fs
git diff --exit-code 6d0221cf6b899228b3bb975db2bffce63e0685e6 -- src/Omokane.Core/Domain/Input.fs src/Omokane.Core/Systems/Omokane.fs src/Omokane.Core/Domain/IntegratedCausalLedger.fs src/Omokane.Core/Domain/IntegratedCausalReplay.fs
git diff --check
git status --short --ignored
git diff --stat

## Verification Result

Core/Smoke/Misogi build成功、警告0・エラー0。Smoke成功。正式禊Test929件成功。5回連続も各929 passed / 0 ignored / 0 failed / 0 errored。NU1900なし。外部依存追加なし。
変更21ファイルのstrict UTF-8検証成功。既存依存はExpecto 11.1.0とFSharp.Core 10.1.301。Input・Movement・統合台帳・Replayは基準SHAから無変更。Run Reportのクリップボード送信成功。

## Errors

- 最終検証エラーなし。初回の新規Testビルドで既存アクセサ名を誤記した2エラーは、由来要求へ修正済み。Gitメタデータ書込拒否は許可範囲の昇格実行で解消。private構築検査のFS1093は期待どおりの拒否。

## Warnings

- Core/Smoke/Misogi警告0、NU1900なし。GitのLFからCRLFへの変換予告のみ発生。

## Design Decisions

敵対的レビューでprivate表現・主体保持・抑制優先・全域match・順序/重複保持・再計算不在を確認。命令アクセサの反応要求IDは現行許可判断ではNoneとなることも固定。ADR-028 Accepted、ADR-003 Proposedを維持。

## Non-Goals

GameState適用、Movement本番接続、プレイヤーID/Entity適格性検証、上下Movement、位置/速度変更、入力filter、意図裁定、方向合成、Event・因果・台帳保存・Replay変更。

## Deferred Work

反応解除、期限、ヒステリシス、巡路、Field、SDF、Navigation、Rendererは未実装。

## Next Candidates

1. 互換命令の主体・Tick・GameState適用前検証。誤ったプレイヤー適用を防ぐため最優先。
2. プレイヤー主体の左右命令を既存Movementへ並行接続する適用Adapter。
3. 旧Input直結経路と新経路の結果等価性検証。
4. 警戒状態によるプレイヤー自発移動抑制の実接続。
5. 上下Movementまたは上下未対応の正式継続。
6. 自発移動意図の競合解決。
7. その場警戒状態の解除。
8. Persistent Causal Worldの最小Field契約。

## Human Confirmation

実行者IDとプレイヤーIDを照合する次段契約を人間レビューする。今回は適用を実装せず、push・PR作成・feature mergeは行っていない。mainの指定ff-only同期はAlready up to dateだった。
