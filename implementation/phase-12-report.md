# Phase 12 完了報告書

## 実装した内容の概要
- [src/QuartzKnowledgeMcp.Api/Search/SearchDtos.cs](src/QuartzKnowledgeMcp.Api/Search/SearchDtos.cs) に `SearchQueryRequest`、`RelatedCatalogEntryResponse`、`RelatedCatalogEntryResultResponse` を追加し、高度検索と related entries の API 契約を定義した。
- [src/QuartzKnowledgeMcp.Api/Search/CatalogSearchService.cs](src/QuartzKnowledgeMcp.Api/Search/CatalogSearchService.cs) に `QueryAsync(...)` を追加し、既存の structured search ロジックを `POST /api/search/query` からも利用できるようにした。
- [src/QuartzKnowledgeMcp.Api/Search/SearchEndpointExtensions.cs](src/QuartzKnowledgeMcp.Api/Search/SearchEndpointExtensions.cs) に `POST /api/search/query` を追加し、複合条件検索 DTO を HTTP API として公開した。
- [src/QuartzKnowledgeMcp.Api/Domain/Ports/IRelationProjector.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/IRelationProjector.cs)、[src/QuartzKnowledgeMcp.Api/Domain/Ports/RelationProjectorDocument.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/RelationProjectorDocument.cs)、[src/QuartzKnowledgeMcp.Api/Domain/Ports/RelatedCatalogEntryProjection.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/RelatedCatalogEntryProjection.cs) を追加し、related entries 計算を DB や LLM から独立した port と projection DTO に切り出した。
- [src/QuartzKnowledgeMcp.Api/Search/StructuredRelationProjector.cs](src/QuartzKnowledgeMcp.Api/Search/StructuredRelationProjector.cs) を追加し、共有 tag / tool / client の構造化スコアを主系統にした related entries 計算を実装した。Embedding が有効な場合だけ `IEmbeddingGenerator` 経由の補助点数を上乗せできる拡張点も追加した。
- [src/QuartzKnowledgeMcp.Api/Search/CatalogRelationService.cs](src/QuartzKnowledgeMcp.Api/Search/CatalogRelationService.cs) を追加し、Gold entry から relation projector を呼び出すアプリケーション側の読み取りサービスを実装した。
- [src/QuartzKnowledgeMcp.Api/Gold/GoldEndpointExtensions.cs](src/QuartzKnowledgeMcp.Api/Gold/GoldEndpointExtensions.cs) に `GET /api/gold/catalog/{entryId}/related` を追加した。
- [src/QuartzKnowledgeMcp.Api/Program.cs](src/QuartzKnowledgeMcp.Api/Program.cs) に `IRelationProjector` と `CatalogRelationService` の DI 登録を追加した。
- [src/QuartzKnowledgeMcp.Api/Capabilities/SystemCapabilitiesService.cs](src/QuartzKnowledgeMcp.Api/Capabilities/SystemCapabilitiesService.cs) を更新し、`search.supportsRelatedEntries=true` を返すようにした。
- [Sample/QuartzKnowledgeMcp.MockClient/Program.cs](Sample/QuartzKnowledgeMcp.MockClient/Program.cs) を更新し、HTTP flow で `POST /api/search/query` と `GET /api/gold/catalog/{id}/related` を検証できるようにした。

## 最小で動く到達点の確認
- 高度検索、検索補完、ファセット、related entries が揃っていること: 確認済み
- `POST /api/search/query` が既存 structured search と同じ条件合成で動くこと: テスト確認済み
- related entries が Embedding 無効時でも構造化情報だけで計算できること: テスト確認済み
- Embedding 有効時のみ補助スコアを合算できる拡張点があること: 実装確認済み
- DB と LLM に過剰依存しない relation projector になっていること: 確認済み

## テストカバー率
- 計測コマンド: `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- 目標値: 80%
- 結果: Line coverage 87.35%、Branch coverage 68.51%

## 実行したユニットテスト
- `CatalogSearchServiceTests.QueryAsync_ComposesMultipleConditions_FromRequestDto`
- `CatalogSearchServiceTests.GetSuggestionsAsync_ReturnsDeterministicItems_WithClampedLimit`
- `CatalogSearchServiceTests.GetFacetsAsync_ReturnsCountsMatchingCurrentFilters`
- `StructuredRelationProjectorTests.ProjectAsync_ComputesRelatedEntries_WhenEmbeddingIsDisabled`
- `SystemCapabilitiesServiceTests.GetCapabilities_ReturnsConfiguredProviders_AndSearchFlags`
- `SystemCapabilitiesServiceTests.GetCapabilities_DefaultsMissingFeatureSettings_ToDisabled`

## 実行した API テスト
- `SearchApiTests.SearchCatalog_FiltersAndSortsResults`
- `SearchApiTests.SearchQueryPost_FiltersStructuredResults`
- `SearchApiTests.SearchSuggestions_ReturnExpectedScopedCandidates`
- `SearchApiTests.SearchFacets_ReturnCountsForCurrentConditions`
- `SearchApiTests.RelatedEntries_ReturnStructuredMatches_WhenEmbeddingIsDisabled`
- `SystemApiTests.GetCapabilities_ReturnsRuntimeFlags`

## 実行した動作検証
- `dotnet run --project src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj --no-launch-profile --urls http://localhost:5080`
- `dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080`
- Sample HTTP フローで `search.query`、`search.query.post`、`search.suggestions`、`search.facets`、`gold.related` を確認
- Sample MCP フローで `search_catalog` と `get_search_suggestions` が引き続き動くことを確認

## 実行した回帰テスト
- `dotnet build src\QuartzKnowledgeMcp.Api\QuartzKnowledgeMcp.Api.csproj --no-restore`
- `dotnet test src\QuartzKnowledgeMcp.Tests\QuartzKnowledgeMcp.Tests.csproj --filter "CatalogSearchServiceTests|StructuredRelationProjectorTests|SearchApiTests|SystemCapabilitiesServiceTests|SystemApiTests" --verbosity normal`
- `dotnet build src\QuartzKnowledgeMcp.slnx --no-restore`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`

## フェーズ進行ゲート確認
- カバレッジ率の記録: yes
- 回帰テストの記録: yes
- 完了レポート作成: yes
- `phase-status.md` 更新: yes

## 既存テストへの影響
Phase 12 の query / related / capabilities 追加後も既存の HTTP API / MCP / Gold / Search 回帰は維持できた。focused tests と API tests を追加した結果、全体テスト数は 78 件になり、`dotnet test src\QuartzKnowledgeMcp.slnx --no-build` は 78/78 成功した。

## docs との差異
- related entries は構造化情報を主系統にし、Embedding は補助点数に限定した。vector-only な検索体験にはしていない。
- Sample runtime では `gold.related` が空配列になるケースも確認された。これは当該サンプル入力に十分な共有 tag / client がないためであり、structured-first の仕様として妥当である。

## 次フェーズへの申し送り事項・既知の課題
- `SearchMcpTools` に `POST /api/search/query` 相当や related entries 相当の MCP surface を追加したい場合は、今回追加した DTO / relation service をそのまま再利用できる。
- Embedding 実装を差し替える場合でも、related entries の主系統は構造化スコアのまま維持し、Embedding は補助点数に留める。
- Phase 13 では docs / sample / operator 観点の仕上げを優先する。
