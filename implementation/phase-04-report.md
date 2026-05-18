# Phase 04 完了報告書

## 実装した内容の概要
- `GoldCatalogEntry` と `EntryHistory` の永続化モデル、および Phase 04 用 Migration を追加した。
- `GoldCatalogService` を追加し、Silver 下書きから Gold エントリへの publish と、再 publish 時の更新履歴追記を実装した。
- `POST /api/silver/server-drafts/{silverId}:publish` と `GET /api/gold/catalog/{entryId}` を追加した。
- publish 時に `publishedBy` / `updatedBy`、`publishedAtUtc` / `updatedAtUtc` を最小管理するようにした。
- Gold 詳細でタグ、tool summaries、references、historyCount を返す最小 DTO を追加した。

## 最小で動く到達点の確認
- Silver から Gold への publish: 確認済み
- 初回 publish 時の履歴 1 件記録: 確認済み
- 再 publish 時の履歴追記: 確認済み
- Gold 詳細 API: 確認済み
- Bronze -> Silver -> Gold の最短フロー: 確認済み

## テストカバー率
- 計測コマンド: `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- 目標値: 70%
- 結果: Line coverage 79.85%、Branch coverage 58.95%

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

## 実行した回帰テスト
- `dotnet build src\QuartzKnowledgeMcp.slnx --no-restore`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- API 起動後、`POST /api/bronze/sources` が Bronze ID を返すことを確認
- API 起動後、`POST /api/bronze/sources/{bronzeId}:organize` が Silver ID を返すことを確認
- API 起動後、`POST /api/silver/server-drafts/{silverId}:publish` が Gold ID を返すことを確認
- API 起動後、`GET /api/gold/catalog/{entryId}` が HTTP 200 と `historyCount=1`、`publishedBy=smoke-publisher`、`toolSummaries.Count=1` を返すことを確認

## フェーズ進行ゲート確認
- カバレッジ率の記録: yes
- 回帰テストの記録: yes
- 完了レポート作成: yes
- `phase-status.md` 更新: yes

## 既存テストへの影響
Phase 01 から Phase 03 までの既存テストは継続して成功。テスト総数は 13 件から 16 件に増加した。

## docs との差異
なし。

## 次フェーズへの申し送り事項・既知の課題
- Gold は現在 details のみで、一覧・検索・タグ更新 API は未実装。
- `setupGuide`、`supportedClients`、`references` は publish 時の最小自動補完であり、手動編集フローは次フェーズ以降で拡張する。
- 履歴は publish / republish の追記までで、履歴取得 API 自体は未実装。