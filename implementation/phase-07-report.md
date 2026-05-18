# Phase 07 完了報告書

## 実装した内容の概要
- Gold のタグ制約、更新内容の検証、履歴生成ルールを [src/QuartzKnowledgeMcp.Api/Domain/Gold/GoldTagSet.cs](src/QuartzKnowledgeMcp.Api/Domain/Gold/GoldTagSet.cs)、[src/QuartzKnowledgeMcp.Api/Domain/Gold/GoldCatalogUpdate.cs](src/QuartzKnowledgeMcp.Api/Domain/Gold/GoldCatalogUpdate.cs)、[src/QuartzKnowledgeMcp.Api/Domain/Gold/GoldEntryHistoryFactory.cs](src/QuartzKnowledgeMcp.Api/Domain/Gold/GoldEntryHistoryFactory.cs) へ移した。
- organize と publish/update/tag replace の入口を [src/QuartzKnowledgeMcp.Api/Application/SilverDraftApplicationService.cs](src/QuartzKnowledgeMcp.Api/Application/SilverDraftApplicationService.cs) と [src/QuartzKnowledgeMcp.Api/Application/CatalogCurationApplicationService.cs](src/QuartzKnowledgeMcp.Api/Application/CatalogCurationApplicationService.cs) にまとめ、HTTP API から直接 Gold/Silver 実装へ触る箇所を減らした。
- Bronze organize、Silver publish、Gold list/detail/update/tags/history の API 配線を Application 層経由へ差し替え、既存の HTTP 契約は維持した。
- HTTP ベースの MCP tool surface を [src/QuartzKnowledgeMcp.Api/Mcp/HealthMcpTools.cs](src/QuartzKnowledgeMcp.Api/Mcp/HealthMcpTools.cs)、[src/QuartzKnowledgeMcp.Api/Mcp/BronzeMcpTools.cs](src/QuartzKnowledgeMcp.Api/Mcp/BronzeMcpTools.cs)、[src/QuartzKnowledgeMcp.Api/Mcp/CatalogMcpTools.cs](src/QuartzKnowledgeMcp.Api/Mcp/CatalogMcpTools.cs)、[src/QuartzKnowledgeMcp.Api/Mcp/SearchMcpTools.cs](src/QuartzKnowledgeMcp.Api/Mcp/SearchMcpTools.cs) と [src/QuartzKnowledgeMcp.Api/Program.cs](src/QuartzKnowledgeMcp.Api/Program.cs) に追加し、`/mcp` で stateless HTTP transport を公開した。
- [Sample/QuartzKnowledgeMcp.MockClient/Program.cs](Sample/QuartzKnowledgeMcp.MockClient/Program.cs) を追加し、HTTP API と MCP tools の両方を順に叩くモックプログラムを用意した。
- 不要になった [src/QuartzKnowledgeMcp.Api/Gold/GoldValidation.cs](src/QuartzKnowledgeMcp.Api/Gold/GoldValidation.cs) を削除し、Phase 07 後の責務に合わない重複ロジックを残さないようにした。

## 最小で動く到達点の確認
- Domain のタグ制約と更新ルールが API DTO から独立して検証可能: 確認済み
- Application サービスが organize / publish / update / tag replace を返すこと: 確認済み
- HTTP API の既存振る舞い維持: 確認済み
- HTTP MCP tool surface の起動と実行: 確認済み
- Sample から主要 API / MCP 機能を順に動かせること: 確認済み

## テストカバー率
- 計測コマンド: `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- 目標値: 80%
- 結果: Line coverage 88.33%、Branch coverage 70.04%

## 実行したユニットテスト
- `GoldDomainRulesTests.GoldTagSet_Create_NormalizesValues_AndPreservesOrder`
- `GoldDomainRulesTests.GoldCatalogUpdate_Create_TrimsValues_AndDefaultsCollections`
- `GoldDomainRulesTests.GoldEntryHistoryFactory_CreatePublished_UsesExpectedActionAndSummary`
- `CatalogCurationApplicationServiceTests.PublishAsync_CreatesGoldEntry_FromRequestBoundary`
- `CatalogCurationApplicationServiceTests.UpdateAsync_UpdatesEntry_ThroughApplicationBoundary`
- `SilverDraftApplicationServiceTests.OrganizeAsync_CreatesDraft_ThroughApplicationBoundary`
- `McpToolsTests.HealthTool_ReturnsHealthStatus`
- `McpToolsTests.BronzeTools_CreateListGetAndOrganize_WorkAsExpected`
- `McpToolsTests.CatalogTools_PublishUpdateTagsAndHistory_WorkAsExpected`
- `McpToolsTests.SearchTools_SearchSuggestionsAndFacets_WorkAsExpected`

## 実行した API テスト
- `OperationalCatalogFlowApiTests.EndToEndWorkflow_ExercisesPrimaryApisAndSearchFeatures`
- `GoldApiTests.PublishDetailAndHistory_ReturnExpectedGoldPayloads`
- `GoldApiTests.UpdateCatalog_AndReplaceTags_PersistChangesAndHistory`
- `SearchApiTests.SearchCatalog_FiltersAndSortsResults`
- `SearchApiTests.SearchSuggestions_ReturnExpectedScopedCandidates`
- `SearchApiTests.SearchFacets_ReturnCountsForCurrentConditions`

## 実行した動作検証
- `dotnet run --project src/QuartzKnowledgeMcp.Api --no-launch-profile --urls http://localhost:5080`
- `dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080`
- Sample で HTTP フロー: health、bronze create/list/detail、silver organize/detail、gold publish/list/detail/update/tags/history、search、suggestions、facets を確認
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
既存テストを壊さずに Domain / Application の分離を追加できた。新規の Domain / Application / MCP / operational API 検証を加えた結果、全体のテスト数は 57 件まで増え、`dotnet test src\QuartzKnowledgeMcp.slnx --no-build` は 57/57 成功した。

## docs との差異
- Phase 10 で予定していた MCP host / tool wiring の一部を前倒しで最小導入した。ただし現状は既存ユースケースを HTTP transport の MCP tools へ載せただけで、MAF / Foundry 連携や LLM agent 選択はまだ含めていない。
- 既存 docs では Domain / Application / Infrastructure の分離が中心だったため、Phase 07 の完了条件を満たす範囲で MCP tool surface と Sample を追加している。

## 次フェーズへの申し送り事項・既知の課題
- Phase 08 では `IKnowledgeRepository`、`IHistoryRepository`、`IUnitOfWork` を導入し、Application サービスが EF Core ではなく Port 越しに動くようにする。
- MCP は現時点で HTTP stateless transport のみ。stdio transport や MAF / Foundry host との接続は今後のフェーズで追加する。
- Phase 12 の `POST /api/search/query` と related entries は未完了のままなので、検索拡張は引き続き in-progress 扱いである。