# 日本語・日本神話モチーフ命名対照表

## 1. 命名原則

- 正式和名は「思兼神エンジン」とする。
- 「思兼命思想」「思兼命式」は思想・設計哲学名として使用できる。
- 神話名はサブシステム名へ使い、公開F# APIは意味が分かる日本語にする。
- ファイル名、project名、Namespaceは英数字を維持する。
- 既存の `Omokane.*`、`OmokaneEngine` は互換契約として維持する。
- 英字表示名 `Omoikane Engine` は候補に留め、今回一括改名しない。

詳細な互換判断は [ADR-007](../adr/ADR-007-japanese-api-omokane-compatibility.md) を正とする。

## 2. 基盤

| 概念 | 神話名 | 日本語API |
|---|---|---|
| 世界知能カーネル | 思兼神Core | 世界知能核 |
| Architecture Invariants | 神律Invariant | 不変条件 |
| ADR | 神議ADR | 設計判断記録 |
| WorldTransaction | 因果Transaction | 因果操作 |
| Causal Ledger | 因果Ledger | 因果台帳 |
| DataBridge | 天浮橋DataBridge | 外部接続 |
| Schema / Versioning | 神名帳Schema | 型式台帳 / 版管理 |

## 3. 状態場・伝播・散逸

| 概念 | 神話名 | 日本語API |
|---|---|---|
| Field Fabric | 八百万Field | 状態場 |
| Environment State | 風土State | 環境状態 |
| Physical Information Propagation | 風土Propagation | 物理情報伝播 |
| Entropy Framework | 黄泉Entropy | 散逸基盤 |
| Homeostasis | 直毘Homeostasis | 常態維持 |

## 4. 観測・記憶

| 概念 | 神話名 | 日本語API |
|---|---|---|
| Sensor Layer | 八咫烏Observe | 感知層 |
| SensorSample | - | 感知値 |
| Observation | - | 観測 |
| Belief | 記紀Memory | 信念 |
| BeliefCandidate | - | 信念候補 |

## 5. 地表・痕跡

| 概念 | 神話名 | 日本語API |
|---|---|---|
| Trace & Surface Memory | 国津Memory | 地表記憶 |
| SurfaceTrace | - | 地表痕跡 |
| Authority Trace | - | 権能痕跡 |

## 6. 身体

| 概念 | 神話名 | 日本語API |
|---|---|---|
| Embodied State | 御身State | 身体状態 |
| BodyMaterialZone | 御身MaterialZone | 身体材質域 |
| EquipmentConstraint | 神衣Constraint | 装具拘束 |
| RawContact | - | 生接触 |
| InterpretedContact | - | 解釈接触 |

`御魂State` はセーブ、復元、スナップショットを扱う。
`御身State` は身体、装具、接触、感情表面を扱う。
両者を同一のStateやサブシステムとして扱わない。

## 7. 権能・競合

| 概念 | 神話名 | 日本語API |
|---|---|---|
| Authority Layer | 神座Authority | 権限層 |
| AuthorityOperation | 神威Operation | 権能操作 |
| Conflict Resolution | 天津裁定 | 競合裁定 |
| Lock / Reservation | 結界Lock | 実行予約 |
| Order Maintenance Operation | 直毘Operation | 秩序維持操作 |

## 8. 座標・時間・LOD・常駐

| 概念 | 神話名 | 日本語API |
|---|---|---|
| World Coordinate | 天御柱Coordinate | 世界座標 |
| World Time | 天暦Time | 世界時刻 |
| Meaning-Preserving LOD | 神髄LOD | 意味保存LOD |
| Residency Scheduler | 鎮座Scheduler | 常駐制御 |
| Detailed Simulation | 現世Sim | 詳細シミュレーション |
| Distant Simulation | 幽世Sim | 遠景シミュレーション |
| Residency Priority | 神域Priority | 常駐優先度 |
| Dormant State | 眠りState | 休眠状態 |

## 9. 可観測性

| 概念 | 神話名 | 日本語API |
|---|---|---|
| Observability | 神託Debug | 判断可視化 |
| Replay | 記紀Replay | 再生履歴 |
| Telemetry | 神託Telemetry | 計測記録 |

## 10. 既存サブシステム

| 神話名 | 責務 |
|---|---|
| 思兼神Core | 判断、状態、固定tick、実行順序統括 |
| 天照Renderer | 描画、光、画面出力 |
| 建御雷Battle | 戦闘、攻撃、衝突、ダメージ |
| 猿田彦Input | 入力、操作、案内 |
| 天鳥船Motion | 移動、速度、座標 |
| 八咫鏡View | カメラ、表示変換、座標投影 |
| 天岩戸Scene | シーン遷移、画面状態 |
| 稲荷Asset | Asset定義、参照、生成 |
| 言霊Event | Event、通知、システム間メッセージ |
| 御魂State | セーブ、復元、スナップショット |
| 禊Test | 仕様、不変条件、決定論の検証 |

## 11. 予約済み名称

| 名称 | 予約する意味 |
|---|---|
| 御魂 | セーブ、復元、スナップショット |
| 御身 | 身体、装具、接触、感情表面 |
| 禍津 | 災害、事故、異常Event |
| 黄泉 | 継続的な散逸、風化、不可逆変化 |
| 言霊 | Event、通知、システム間メッセージ |
| 記紀 | 記憶、履歴、再生 |
| 神託 | 開発者向け説明、可視化、Telemetry |
| 稲荷 | Asset体系、参照、定義、生成 |
| 神座 | 権限、優先度、競合解決 |
| 直毘 | 修復、秩序維持、常態維持 |
