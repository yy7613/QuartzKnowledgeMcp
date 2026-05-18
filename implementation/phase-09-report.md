# Phase 09 完了報告書

## 実装した内容の概要
- [src/QuartzKnowledgeMcp.Api/Domain/Ports/IOrganizationAgent.cs](src/QuartzKnowledgeMcp.Api/Domain/Ports/IOrganizationAgent.cs) を追加し、Bronze から Silver への整理呼び出し境界を LLM 利用可否を含む Port として切り出した。
- [src/QuartzKnowledgeMcp.Api/Silver/RuleBasedOrganizationAgent.cs](src/QuartzKnowledgeMcp.Api/Silver/RuleBasedOrganizationAgent.cs) を追加し、既存の規則ベース整理を `IOrganizationAgent` 実装へ載せ替えた。現段階では `useLlm=true` を要求しても rule-based fallback で処理し、`usedLlm=false` を返す。
- [src/QuartzKnowledgeMcp.Api/Silver/SilverDraftService.cs](src/QuartzKnowledgeMcp.Api/Silver/SilverDraftService.cs) を更新し、`preview` 時は永続化せずに下書き提案だけを返し、commit 時のみ Bronze status と Silver draft を保存するようにした。
- [src/QuartzKnowledgeMcp.Api/Capabilities/SystemCapabilitiesService.cs](src/QuartzKnowledgeMcp.Api/Capabilities/SystemCapabilitiesService.cs) と [src/QuartzKnowledgeMcp.Api/Capabilities/SystemEndpointExtensions.cs](src/QuartzKnowledgeMcp.Api/Capabilities/SystemEndpointExtensions.cs) を追加し、`GET /api/system/capabilities` で DB provider、LLM、Embedding、検索機能の runtime 能力を返せるようにした。
- [src/QuartzKnowledgeMcp.Api/Program.cs](src/QuartzKnowledgeMcp.Api/Program.cs) の DI に `IOrganizationAgent` と capabilities service を登録し、HTTP API へ system endpoint を追加した。
- [Sample/QuartzKnowledgeMcp.MockClient/Program.cs](Sample/QuartzKnowledgeMcp.MockClient/Program.cs) を更新し、Sample HTTP フローで `system.capabilities` と `silver.preview` を実行できるようにした。

## 最小で動く到達点の確認
- Rule-based organize が `IOrganizationAgent` 越しに動作すること: 確認済み
- `useLlm=true` 要求時に rule-based fallback して `usedLlm=false` を返すこと: 確認済み
- `preview` で永続化せず提案のみ返すこと: 確認済み
- `GET /api/system/capabilities` で runtime 能力を確認できること: 確認済み
- Sample から capabilities / preview / commit / MCP フローを順に動かせること: 確認済み

## テストカバー率
- 計測コマンド: `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- 目標値: 80%
- 結果: Line coverage 88.57%、Branch coverage 70.92%

## 実行したユニットテスト
- `SilverDraftServiceTests.OrganizeAsync_Preview_DoesNotPersistChanges`
- `SilverDraftServiceTests.OrganizeAsync_FallsBackToRuleBased_WhenLlmIsRequested`
- `SilverDraftServiceTests.OrganizeAsync_UpdatesExistingDraft_WhenBronzeAlreadyOrganized`
- `SystemCapabilitiesServiceTests.GetCapabilities_ReturnsConfiguredProviders_AndSearchFlags`
- `SystemCapabilitiesServiceTests.GetCapabilities_DefaultsMissingFeatureSettings_ToDisabled`
- `SilverDraftApplicationServiceFakeRepositoryTests.OrganizeAsync_Works_WithFakeRepository`

## 実行した API テスト
- `SilverApiTests.OrganizeListAndDetail_ReturnExpectedSilverPayloads`
- `SilverApiTests.OrganizePreview_ReturnsDraftWithoutPersisting`
- `SystemApiTests.GetCapabilities_ReturnsRuntimeFlags`
- `OperationalCatalogFlowApiTests.EndToEndWorkflow_ExercisesPrimaryApisAndSearchFeatures`
- `McpToolsTests.BronzeTools_CreateListGetAndOrganize_WorkAsExpected`

## 実行した動作検証
- `dotnet run --project src/QuartzKnowledgeMcp.Api --no-launch-profile --urls http://localhost:5080`
- `dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080`
- Sample HTTP フローで `system.capabilities`、`silver.preview`、`bronze.detail.after-preview`、`silver.organize`、`gold.publish`、`search` を確認
- Sample MCP フローで `get_health`、`create_bronze_source`、`search_catalog`、`get_search_suggestions` を確認

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
`IOrganizationAgent` と capabilities API を追加しても既存の HTTP API / MCP / Gold / Search 回帰は維持できた。preview と capabilities の unit/API 検証を追加した結果、全体のテスト数は 67 件になり、`dotnet test src\QuartzKnowledgeMcp.slnx --no-build` は 67/67 成功した。

## docs との差異
- docs で想定している LLM provider の接続自体はまだ未実装で、Phase 09 では fallback 方針と capability 照会の枠だけを固定した。
- `GET /api/system/capabilities` の `search.supportsRelatedEntries` は現状 `false` で返す。related entries は Phase 12 の未完項目に残っている。

## 次フェーズへの申し送り事項・既知の課題
- Phase 10 では MCP host / tool surface と LLM adapter の接続方針を広げる場合でも、`IOrganizationAgent` と capabilities API の契約は維持する。
- 実 LLM adapter を追加する際は `useLlm=true` かつ provider 利用可能時のみ `usedLlm=true` へ切り替え、fallback の挙動を継続保証する。
- Phase 12 の `POST /api/search/query` と related entries は引き続き未完了である。