# Phase 08 完了報告書

## 実装した内容の概要
- [src/QuartzKnowledgeMcp.Api/Domain/Ports/IKnowledgeRepository.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/IKnowledgeRepository.cs)、[src/QuartzKnowledgeMcp.Api/Domain/Ports/IHistoryRepository.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/IHistoryRepository.cs)、[src/QuartzKnowledgeMcp.Api/Domain/Ports/IUnitOfWork.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/IUnitOfWork.cs)、[src/QuartzKnowledgeMcp.Api/Domain/Ports/GoldCatalogProjectionRow.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/GoldCatalogProjectionRow.cs) を追加し、Silver/Gold の永続化依存を Port として切り出した。
- [src/QuartzKnowledgeMcp.Api/Persistence/SqliteKnowledgeRepository.cs](src/QuartzKnowledgeMcp.Api/Persistence/SqliteKnowledgeRepository.cs)、[src/QuartzKnowledgeMcp.Api/Persistence/SqliteHistoryRepository.cs](src/QuartzKnowledgeMcp.Api/Persistence/SqliteHistoryRepository.cs)、[src/QuartzKnowledgeMcp.Api/Persistence/EfUnitOfWork.cs](src/QuartzKnowledgeMcp.Api/Persistence/EfUnitOfWork.cs) を追加し、既定の SQLite 実装を Adapter 側へ閉じ込めた。
- [src/QuartzKnowledgeMcp.Api/Silver/SilverDraftService.cs](src/QuartzKnowledgeMcp.Api/Silver/SilverDraftService.cs) と [src/QuartzKnowledgeMcp.Api/Gold/GoldCatalogService.cs](src/QuartzKnowledgeMcp.Api/Gold/GoldCatalogService.cs) を DbContext 直接依存から Port 依存へ差し替え、Fake Repository でも SQLite Adapter でも同じユースケースが通る形にした。
- [src/QuartzKnowledgeMcp.Api/Program.cs](src/QuartzKnowledgeMcp.Api/Program.cs) の DI を更新し、Repository / UnitOfWork をスコープ登録した。あわせて MCP transport options 内に誤って入っていた DI 登録を外へ戻し、ホスト起動時の read-only service collection 例外を解消した。
- [src/QuartzKnowledgeMcp.Tests/Infrastructure/FakePersistence.cs](src/QuartzKnowledgeMcp.Tests/Infrastructure/FakePersistence.cs) と Application テストを追加し、Port 契約違反の検出と Fake Repository 経由の organize / publish を確認できるようにした。
- [src/QuartzKnowledgeMcp.Tests/Silver/SilverDraftServiceTests.cs](src/QuartzKnowledgeMcp.Tests/Silver/SilverDraftServiceTests.cs) に再 organize 回帰テストを追加し、実運用 DB で見つかった SilverToolDraft の更新競合を再発防止した。

## 最小で動く到達点の確認
- Application が Repository Port 越しに organize / publish / update / history を処理すること: 確認済み
- SQLite Adapter が既定実装として既存 API と MCP surface を維持すること: 確認済み
- Fake Repository でも Application サービスが動作すること: 確認済み
- Port 契約違反を明示的に検出できること: 確認済み
- Sample から HTTP / MCP フローを通して主要機能を操作できること: 確認済み

## テストカバー率
- 計測コマンド: `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- 目標値: 80%
- 結果: Line coverage 87.94%、Branch coverage 69.72%

## 実行したユニットテスト
- `SilverDraftApplicationServiceFakeRepositoryTests.OrganizeAsync_Works_WithFakeRepository`
- `SilverDraftApplicationServiceFakeRepositoryTests.ListAsync_Throws_WhenRepositoryViolatesCollectionContract`
- `CatalogCurationApplicationServiceFakeRepositoryTests.PublishAsync_Works_WithFakeRepository`
- `SilverDraftServiceTests.OrganizeAsync_CreatesAndListsSilverDraft_WhenBronzeExists`
- `SilverDraftServiceTests.OrganizeAsync_UpdatesExistingDraft_WhenBronzeAlreadyOrganized`
- `GoldCatalogServiceTests.PublishAsync_CreatesGoldEntry_WithHistory`
- `GoldCatalogServiceTests.UpdateAsync_UpdatesExistingGoldEntry_AndAppendsHistory`

## 実行した API テスト
- `OperationalCatalogFlowApiTests.EndToEndWorkflow_ExercisesPrimaryApisAndSearchFeatures`
- `GoldApiTests.PublishDetailAndHistory_ReturnExpectedGoldPayloads`
- `GoldApiTests.UpdateCatalog_AndReplaceTags_PersistChangesAndHistory`
- `McpToolsTests.BronzeTools_CreateListGetAndOrganize_WorkAsExpected`
- `McpToolsTests.CatalogTools_PublishUpdateTagsAndHistory_WorkAsExpected`
- `McpToolsTests.SearchTools_SearchSuggestionsAndFacets_WorkAsExpected`

## 実行した動作検証
- `dotnet run --project src/QuartzKnowledgeMcp.Api --no-launch-profile --urls http://localhost:5080`
- `dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080`
- Sample で HTTP フロー: health、bronze create/list/detail、silver organize/detail、gold publish/list/detail/update/tags/history、search を確認
- Sample で MCP フロー: `get_health`、`create_bronze_source`、`search_catalog`、`get_search_suggestions` を確認

## 実行した回帰テスト
- `dotnet build src\QuartzKnowledgeMcp.slnx --no-restore`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`

## フェーズ進行ゲート確認
- カバレッジ率の記録: yes
- 回帰テストの記録: yes
- 完了レポート作成: yes
- `phase-status.md` 更新: yes

## 既存テストへの影響
Port 抽象化を追加しても既存の HTTP API / MCP / 検索テストは維持できた。Fake Repository の追加と Silver organize 再実行の回帰テスト追加により、全体のテスト数は 61 件になり、`dotnet test src\QuartzKnowledgeMcp.slnx --no-build` は 61/61 成功した。

## docs との差異
- docs の意図どおり DB 差し替え境界を Port 化したが、SQLite Adapter の段階では EF Core の entity をそのまま扱っているため、完全な永続化 DTO 分離まではまだ進めていない。
- runtime の Sample 検証で見つかった Silver organize 再実行時の競合を Phase 08 内で是正したため、仕様上の「既存 API の振る舞い維持」を実運用に近い経路でも確認できた。

## 次フェーズへの申し送り事項・既知の課題
- Phase 09 では LLM 抽象化を `IOrganizationAgent` と capability endpoint に切り出し、rule-based organize を fallback 実装として残す。
- `IHistoryRepository` と `IKnowledgeRepository` は entity ベースのままなので、将来的に別 DB 実装へ広げる場合は Port 入出力モデルの分離を次段階で検討する。
- Phase 12 の `POST /api/search/query` と related entries は引き続き未完了である。