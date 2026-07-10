# ADR-007: 日本語API・日本神話モチーフ命名と既存Omokane互換

- Status: Accepted
- Date: 2026-07-10

## Context

プロジェクトの正式和名は「思兼神エンジン」であり、公開F# APIには日本語を用いる。
一方、既存Namespace、project名、リポジトリ内の英数字表記は `Omokane.*`、`OmokaneEngine` で定着している。
一般的なローマ字表記として `Omoikane Engine` も候補になるが、一括改名は大きな破壊的変更になる。

## Decision

- 正式和名は「思兼神エンジン」とする。
- 「思兼命思想」「思兼命式」は思想・設計哲学名として使用できる。
- 神話名はサブシステム名へ使い、公開F# APIは意味が分かる日本語にする。
- ファイル名、project名、Namespaceは英数字を維持する。
- 既存 `Omokane.*` と `OmokaneEngine` は今回維持する。
- `Omoikane.*` への改名は今回行わず、必要なら別ADR・別タスクで互換計画を立てる。
- 御魂Stateと御身Stateを混同しない。

## Consequences

- 綴り差は意図された互換判断として文書化される。
- 新規コードは既存Namespaceへ追加できる。
- 黙った一括改名やフォルダ移動を禁止する。
