# ADR-010: 禊Testの初期フレームワークとしてExpectoを採用する

- Status: Accepted
- Date: 2026-07-27

## Context

Smokeへ詳細な仕様検証を増やすと、通電確認という責務を越えて肥大化する。
思兼神Coreには、F#の構造的等値と日本語テスト名を自然に扱える正式な仕様検証基盤が必要である。

## Decision

禊Testの初期フレームワークとしてExpectoを採用する。

- `tests/Omokane.Core.Misogi/` をCoreの正式な仕様検証に使用する。
- Smokeは最小の通電確認に限定する。
- 初期段階ではF# console runnerとして `dotnet run --project .\tests\Omokane.Core.Misogi\Omokane.Core.Misogi.fsproj` で実行する。
- 外部依存はExpecto単体とし、Expecto.TestSdkや追加アサーションライブラリは導入しない。

## Alternatives

- xUnit
- xUnitとFsUnitの併用
- 独自テストランナー
- Smokeでの仕様検証継続

## Consequences

- 外部NuGet依存が1件増える。
- 当面の正式実行方法は `dotnet run --project` になる。
- 日本語テスト名とF#の構造的等値を使った仕様検証を記述しやすくなる。
- `dotnet test` やCIへ統合するときは、Expecto.TestSdkまたは別ランナーを再評価する。
