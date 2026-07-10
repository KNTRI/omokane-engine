# 思兼神エンジン アーキテクチャ索引

このディレクトリは、思兼神エンジンを世界知能カーネルとして発展させるための上位アーキテクチャをまとめる。
既存のv0.1仕様は破棄せず、現在の実装と将来構造の差分はマッピングと神議ADRで管理する。

## 中核文書

- [世界知能カーネル](world_intelligence_kernel.md)
- [アーキテクチャ不変条件](architecture_invariants.md)
- [物理情報伝播層](physical_information_propagation.md)
- [エントロピー基盤](entropy_framework.md)
- [日本語・日本神話モチーフ命名対照表](japanese_myth_naming_map.md)
- [現行システム対応表](current_system_mapping.md)

## 神議ADR

- [ADR-001: 思兼神エンジンを世界知能カーネルとして定義](../adr/ADR-001-world-intelligence-kernel.md)
- [ADR-002: 世界真実・観測・表現を分離](../adr/ADR-002-truth-observation-presentation.md)
- [ADR-003: 永続的な世界変更を因果操作経由とする](../adr/ADR-003-causal-transaction.md)
- [ADR-004: 権威層と表現層を分離](../adr/ADR-004-authority-presentation-separation.md)
- [ADR-005: 意味保存LODを採用](../adr/ADR-005-meaning-preserving-lod.md)
- [ADR-006: AIは世界真実を直接参照しない](../adr/ADR-006-ai-non-omniscience.md)
- [ADR-007: 日本語API・日本神話モチーフ命名と既存Omokane互換](../adr/ADR-007-japanese-api-omokane-compatibility.md)
- [ADR-008: 可観測性と再生を初期要件とする](../adr/ADR-008-observability-replay.md)
- [ADR-009: 物理情報伝播層とエントロピー基盤を導入](../adr/ADR-009-propagation-entropy-foundations.md)

## 既存設計との接続

- [Tick実行順序](../design/tick_execution_order.md)
- [権能ライフサイクル仕様](../design/kengou_lifecycle_spec.md)
- [因果Log / 神託Debug最小型設計](../design/inga_log_shintaku_debug_minimum_spec.md)
- [禊Test方針](../design/misogi_test_policy.md)
- [権能Asset仕様書](../design/kengou_asset_spec.md)
- [権能タグ設計](../design/kengou_tag_design.md)
- [プロシージャル箱庭最小動作モデル](../design/procedural_sandbox_minimum_model.md)
- [常態収束機仕様書](../design/jotai_convergence_machine_spec.md)
- [用語集](../design/glossary.md)

## 文書の優先関係

1. 採択済み神議ADRは、採択した設計判断の正本とする。
2. このディレクトリの文書は、上位アーキテクチャと責務境界の正本とする。
3. `docs/design/` は個別機能とv0.1仕様の正本として継続利用する。
4. 実装との差分は [現行システム対応表](current_system_mapping.md) に記録し、黙って既存契約を読み替えない。
