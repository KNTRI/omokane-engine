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
- [ADR-010: 禊Testの初期フレームワークとしてExpectoを採用する](../adr/ADR-010-expecto-misogi-test-framework.md)
- [ADR-011: 因果台帳の最小再生は記録結果の検証付き再適用とする](../adr/ADR-011-causal-ledger-result-replay.md)
- [ADR-012: 伝播信号の最小感知変換は明示設定と線形距離減衰を使う](../adr/ADR-012-minimal-signal-sensing.md)
- [ADR-013: 観測からの最小信念候補生成は明示的な根拠・反証と軟証拠集約を使う](../adr/ADR-013-minimal-belief-candidate.md)
- [ADR-014: 信念候補からの最小警戒意図生成は単一の明示閾値を使う](../adr/ADR-014-minimal-alert-intent.md)
- [ADR-015: 地盤振動の技術垂直スライスは既存の純粋認識変換を順次合成する](../adr/ADR-015-ground-vibration-alert-vertical-slice.md)
- [ADR-016: 警戒意図からの最小行動候補生成はその場警戒だけを提示する](../adr/ADR-016-minimal-alert-action-candidate.md)
- [ADR-017: 地盤振動発生因果操作はゲーム状態を直接変更せず権威伝播信号を生成する](../adr/ADR-017-causal-ground-vibration-emission.md)
- [ADR-018: 因果起点から行動候補までの閉ループは既存の純粋境界を合成する](../adr/ADR-018-causal-cognitive-action-loop.md)
- [ADR-019: 異種因果結果は統合因果台帳へ判別共用体として保存する](../adr/ADR-019-integrated-causal-ledger.md)
- [ADR-020: 統合因果Replayは状態変更と伝播信号生成を同一因果順で検証付き再適用する](../adr/ADR-020-integrated-causal-replay.md)

## 既存設計との接続

- [Tick実行順序](../design/tick_execution_order.md)
- [権能ライフサイクル仕様](../design/kengou_lifecycle_spec.md)
- [因果Log / 神託Debug最小型設計](../design/inga_log_shintaku_debug_minimum_spec.md)
- [禊Test方針](../design/misogi_test_policy.md)
- [権能Asset仕様書](../design/kengou_asset_spec.md)
- [権能タグ設計](../design/kengou_tag_design.md)
- [プロシージャル箱庭最小動作モデル](../design/procedural_sandbox_minimum_model.md)
- [因果認識閉ループ v0.1](../design/causal_ground_vibration_cognitive_loop.md)
- [統合因果台帳 v0.1と異種因果結果Replay](../design/integrated_causal_ledger_and_replay.md)
- [常態収束機仕様書](../design/jotai_convergence_machine_spec.md)
- [用語集](../design/glossary.md)

## 文書の優先関係

1. 採択済み神議ADRは、採択した設計判断の正本とする。
2. このディレクトリの文書は、上位アーキテクチャと責務境界の正本とする。
3. `docs/design/` は個別機能とv0.1仕様の正本として継続利用する。
4. 実装との差分は [現行システム対応表](current_system_mapping.md) に記録し、黙って既存契約を読み替えない。
