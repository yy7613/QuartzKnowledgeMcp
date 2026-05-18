# Phase 05 完了報告書

## 実装した内容の概要
- `GET /api/gold/catalog` を追加し、タグ、認証方式、対応クライアントによる基本フィルタとページングを実装した。
- `GET /api/gold/catalog/{entryId}/history` を前倒しで追加し、Gold 履歴の参照 API を実装した。
- `GET /api/search` を追加し、名前、概要、タグ、ツール名を対象にした基本検索と、`relevance`、`updated-desc`、`name-asc` の簡易並び替えを実装した。
- Bronze の生テキストから `authType` と対応クライアントを規則ベースで導出する `CatalogMetadataExtractor` を追加した。
- `WebApplicationFactory` ベースの API テスト基盤を新設し、Health、Bronze、Silver、Gold、Search の統合テストを追加した。

## 最小で動く到達点の確認
- Gold 一覧 API: 確認済み
- Gold 詳細 API: 継続確認済み
- Gold 履歴 API: 確認済み
- 基本検索 API: 確認済み
- タグ AND 条件によるカテゴリ絞り込み: 確認済み
- 認証方式とクライアント条件の基本フィルタ: 確認済み

## テストカバー率
- 計測コマンド: `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- 目標値: 75%
- 結果: Line coverage 86.37%、Branch coverage 64.07%

## 実行したユニットテスト
- `HealthStatusServiceTests.GetStatus_ReturnsHealthyStatus`
- `HealthCheckOptionsTests.FromConfiguration_UsesDefaults_WhenHealthSectionIsMissing`
- `HealthCheckOptionsTests.FromConfiguration_TrimsConfiguredValues`
- `BronzeIngestionServiceTests.ImportAsync_RejectsMissingRequiredFields`
- `BronzeIngestionServiceTests.ImportAsync_RejectsUnsupportedSourceType`
- `BronzeIngestionServiceTests.ImportAsync_ReturnsExistingSource_WhenSourceUriAndContentMatch`
- `BronzeIngestionServiceTests.GetDetailAsync_ReturnsSource_WhenIdExists`
- `BronzeIngestionServiceTests.GetDetailAsync_ReturnsNull_WhenIdDoesNotExist`
- `BronzeIngestionServiceTests.ListAsync_ReturnsPagedSources_WithOptionalStatusFilter`
- `RuleBasedSilverNormalizerTests.Normalize_ExtractsNameSummaryTagsAndTools_FromMarkdownReadme`
- `RuleBasedSilverNormalizerTests.Normalize_ReturnsFallbackDraft_WhenInputIsIncomplete`
- `SilverDraftServiceTests.OrganizeAsync_Throws_WhenBronzeSourceDoesNotExist`
- `SilverDraftServiceTests.OrganizeAsync_CreatesAndListsSilverDraft_WhenBronzeExists`
- `GoldCatalogServiceTests.PublishAsync_CreatesGoldEntry_WhenSilverDraftExists`
- `GoldCatalogServiceTests.PublishAsync_AppendsHistory_WhenEntryAlreadyExists`
- `GoldCatalogServiceTests.PublishAsync_Throws_WhenSilverDraftDoesNotExist`
- `CatalogSearchServiceTests.SearchAsync_MatchesNameAndOverview`
- `CatalogSearchServiceTests.SearchAsync_AppliesTagAndCondition`
- `CatalogSearchServiceTests.SearchAsync_AppliesAuthAndClientFilters`
- `CatalogSearchServiceTests.SearchAsync_AppliesStableSortingAndPaging`

## 実行した API テスト
- `HealthApiTests.GetHealth_ReturnsOkStatus`
- `BronzeApiTests.CreateListAndDetail_ReturnExpectedBronzePayloads`
- `SilverApiTests.OrganizeListAndDetail_ReturnExpectedSilverPayloads`
- `GoldApiTests.PublishDetailAndHistory_ReturnExpectedGoldPayloads`
- `SearchApiTests.ListCatalogEntries_FiltersByTagAuthTypeAndClient`
- `SearchApiTests.SearchCatalog_FiltersAndSortsResults`

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
既存 20 件のユニットテストは継続して成功。API 統合テスト 6 件を追加し、総テスト数は 16 件から 26 件に増加した。

## docs との差異
- `GET /api/gold/catalog/{entryId}/history` を Phase 05 の時点で前倒し実装した。

## 次フェーズへの申し送り事項・既知の課題
- 現在の検索は構造化検索と簡易 relevance に留まり、補完、ファセット、関連エントリは未実装。
- `authType` は Gold に永続化しておらず、Bronze の生テキストから都度導出している。
- `supportedClients` も規則ベース抽出のため、README 記述が曖昧な場合は空になる。