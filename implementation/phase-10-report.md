# Phase 10 完了報告書

## 実装した内容の概要
- [src/QuartzKnowledgeMcp.Api/Silver/FoundryOrganizationOptions.cs](src/QuartzKnowledgeMcp.Api/Silver/FoundryOrganizationOptions.cs) を追加し、Foundry organize の有効化、provider、project endpoint、model、instructions、fallback 方針を runtime configuration から受け取れるようにした。
- [src/QuartzKnowledgeMcp.Api/Silver/FoundrySilverDraftResponse.cs](src/QuartzKnowledgeMcp.Api/Silver/FoundrySilverDraftResponse.cs) を追加し、LLM の JSON 応答を Silver draft へ正規化する DTO を実装した。tag 重複除去、tool 名重複除去、空 description の補完、長さ制限をここで固定した。
- [src/QuartzKnowledgeMcp.Api/Silver/IFoundryOrganizationClient.cs](src/QuartzKnowledgeMcp.Api/Silver/IFoundryOrganizationClient.cs)、[src/QuartzKnowledgeMcp.Api/Silver/MafFoundryOrganizationAgent.cs](src/QuartzKnowledgeMcp.Api/Silver/MafFoundryOrganizationAgent.cs)、[src/QuartzKnowledgeMcp.Api/Silver/MafFoundryOrganizationClient.cs](src/QuartzKnowledgeMcp.Api/Silver/MafFoundryOrganizationClient.cs) を追加し、Foundry 応答を `IOrganizationAgent` 契約へ接続する adapter を実装した。
- [src/QuartzKnowledgeMcp.Api/Silver/OrganizationAgentSelector.cs](src/QuartzKnowledgeMcp.Api/Silver/OrganizationAgentSelector.cs) を追加し、`useLlm` と Foundry 設定の有無に応じて rule-based / Foundry を切り替え、Foundry 例外時は設定に従って rule-based fallback するようにした。
- [src/QuartzKnowledgeMcp.Api/Program.cs](src/QuartzKnowledgeMcp.Api/Program.cs) を更新し、Foundry options、selector、Foundry client を DI に登録した。
- [src/QuartzKnowledgeMcp.Api/Capabilities/SystemCapabilitiesService.cs](src/QuartzKnowledgeMcp.Api/Capabilities/SystemCapabilitiesService.cs) を更新し、`GET /api/system/capabilities` が Foundry 設定から LLM 可用性と model を返すようにした。
- [src/QuartzKnowledgeMcp.Api/appsettings.json](src/QuartzKnowledgeMcp.Api/appsettings.json)、[src/QuartzKnowledgeMcp.Api/appsettings.Development.json](src/QuartzKnowledgeMcp.Api/appsettings.Development.json)、[src/QuartzKnowledgeMcp.Api/appsettings.Test.json](src/QuartzKnowledgeMcp.Api/appsettings.Test.json) に `FoundryOrganization` セクションを追加し、未設定時は安全に無効のまま起動できるようにした。

## 実装上の判断
- 現行の [src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj](src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj) が参照している `Azure.AI.Projects` 2.1.0-beta.1 を反射確認した結果、旧来の `AIProjectClient.CreateAIAgentAsync` / `GetAgentsClient()` 系の surface は存在しなかった。
- そのため Phase 10 では exact SDK surface に合わせて `AIProjectClient.ProjectOpenAIClient.GetProjectResponsesClientForModel(...)` を使う構成へ切り替えた。model 選択は options から行い、instructions は prompt 前置で渡す。
- これにより docs の到達点である「設定時のみ有効になる Foundry adapter」「未設定環境でもアプリ本体が壊れない」「fallback 方針を統一する」は維持しつつ、実際に compile 可能な SDK surface へ寄せた。

## 最小で動く到達点の確認
- Foundry 設定がない環境で既存の organize / preview / MCP が壊れないこと: 確認済み
- `useLlm=true` かつ Foundry 未設定時に rule-based fallback すること: 確認済み
- Foundry 設定ありの場合に selector が Foundry adapter を選択すること: テスト確認済み
- LLM JSON 出力を Silver draft DTO へ正規化できること: テスト確認済み
- LLM 例外時に統一された fallback 方針で rule-based へ戻ること: テスト確認済み

## テストカバー率
- 計測コマンド: `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`
- 目標値: 80%
- 結果: Line coverage 87.96%、Branch coverage 70.11%

## 実行したユニットテスト
- `FoundrySilverDraftResponseTests.ToSilverDraftContent_NormalizesValues_DeduplicatesAndDefaultsToolDescription`
- `OrganizationAgentSelectorTests.OrganizeAsync_UsesFoundryAgent_WhenConfiguredAndRequested`
- `OrganizationAgentSelectorTests.OrganizeAsync_UsesRuleBased_WhenFoundryIsNotConfigured`
- `OrganizationAgentSelectorTests.OrganizeAsync_FallsBackToRuleBased_WhenFoundryThrows`
- `SystemCapabilitiesServiceTests.GetCapabilities_ReturnsConfiguredProviders_AndSearchFlags`
- `SystemCapabilitiesServiceTests.GetCapabilities_DefaultsMissingFeatureSettings_ToDisabled`

## 実行した API テスト
- `SystemApiTests.GetCapabilities_ReturnsRuntimeFlags`
- 既存 API 回帰として `dotnet test src\QuartzKnowledgeMcp.slnx --no-build` を実施し、71/71 成功を確認した。

## 実行した動作検証
- `dotnet run --project src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj --no-launch-profile --urls http://localhost:5080`
- `dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080`
- Sample HTTP フローで `health`、`system.capabilities`、`bronze.create`、`silver.preview`、`silver.organize`、`gold.publish`、`gold.update`、`gold.tags`、`search` を確認
- Sample MCP フローで `tools/list`、`get_health`、`create_bronze_source`、`search_catalog`、`get_search_suggestions` を確認

## 実行した回帰テスト
- `dotnet build src\QuartzKnowledgeMcp.Api\QuartzKnowledgeMcp.Api.csproj --no-restore`
- `dotnet test src\QuartzKnowledgeMcp.Tests\QuartzKnowledgeMcp.Tests.csproj --filter "FoundrySilverDraftResponseTests|OrganizationAgentSelectorTests|SystemCapabilitiesServiceTests|SystemApiTests" --verbosity normal`
- `dotnet build src\QuartzKnowledgeMcp.slnx --no-restore`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build`
- `dotnet test src\QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage"`

## フェーズ進行ゲート確認
- カバレッジ率の記録: yes
- 回帰テストの記録: yes
- 完了レポート作成: yes
- `phase-status.md` 更新: yes

## 既存テストへの影響
Foundry adapter と selector を追加しても既存の HTTP API / MCP / Gold / Search 回帰は維持できた。Phase 10 の focused tests を追加した結果、全体テスト数は 71 件になり、`dotnet test src\QuartzKnowledgeMcp.slnx --no-build` は 71/71 成功した。

## docs との差異
- docs は MAF / Foundry adapter という表現だが、現行 package surface では direct agent execution API ではなく `ProjectOpenAIClient` / `ProjectResponsesClient` が compile 可能な実装経路だったため、Phase 10 はその exact SDK surface に合わせて実装した。
- この repo 環境には Foundry endpoint / credential の実値がないため、設定済み環境での live Foundry organize は compile と selector tests まで確認し、runtime smoke は未設定 fallback 経路で検証した。

## 次フェーズへの申し送り事項・既知の課題
- Foundry 実環境が用意できたら、`FoundryOrganization` を有効にして `useLlm=true` organize の end-to-end 実行を 1 本追加し、live provider 動作を固定する。
- `Microsoft.Agents.AI` / `Microsoft.Agents.AI.Foundry` package は今後 agent session や tool orchestration を広げる余地として維持しているが、Phase 10 の organize 実装自体は `Azure.AI.Projects` 2.1.0-beta.1 の supported surface を優先した。
- Phase 12 の `POST /api/search/query` と related entries は引き続き未完了である。
