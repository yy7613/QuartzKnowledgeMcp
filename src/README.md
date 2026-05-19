# src

アプリケーションコードとテストはこのディレクトリに配置します。

正式な solution は `QuartzKnowledgeMcp.slnx` です。

## 開発コマンド

```powershell
dotnet build src/QuartzKnowledgeMcp.slnx --no-restore
dotnet test src/QuartzKnowledgeMcp.slnx --no-build
dotnet test src/QuartzKnowledgeMcp.slnx --collect:"XPlat Code Coverage"
```

## ダッシュボード

API を起動した後は `/dashboard` で人間向けの参照画面を開けます。

- glass-style の運用 shell で、PC 画面での一覧性を優先している
- Search tab : medallion 横断検索。`stage` / `tag` / `freshness` / `sort` と query なし browse を利用可能
- Search result preview : タイトルクリックで Bronze / Silver / Gold の detail JSON を modal dialog で確認可能
- Graph tab : bronze / silver / gold の件数、鮮度、recent items、3d / 7d trend を表示
- Inspect tab : gold detail / history / related を一画面で確認可能
- State persistence : query string と localStorage の両方で検索条件、active tab、trend、inspect 対象を復元可能
- `/api/dashboard/summary` : summary data、tag cloud、trend、detail path
- `/api/dashboard/search` : dashboard 用の browse / search API

## 認証

API key auth は `src/QuartzKnowledgeMcp.Api/appsettings.json` の `Authentication:ApiKey` で構成します。既定値は無効です。

```json
{
	"Authentication": {
		"ApiKey": {
			"Enabled": true,
			"HeaderName": "X-QuartzKnowledge-Api-Key",
			"ApiKey": "change-me",
			"ProtectedPrefixes": ["/api", "/mcp"]
		}
	}
}
```

- `/api` と `/mcp` 配下は API key が必須になり、未指定または不正な key では `401` を返す
- `/health` と静的な `/dashboard` shell は匿名のまま
- ただし dashboard 本体は `/api/dashboard/*` を読むため、認証有効時の browser 利用は reverse proxy / ingress などで同じ header を注入する前提
- 文書中の API key はプレースホルダーであり、そのまま配備しない

## コンテナ配備

- `Dockerfile` : .NET 10 SDK / ASP.NET runtime の multi-stage build。`8080` を listen し、`/data` を volume として公開
- `src/QuartzKnowledgeMcp.Api/appsettings.Container.json` : SQLite の保存先を `/data/quartz-knowledge.db` に切り替える container 用設定
- `deploy/kubernetes/quartz-knowledge.sample.yaml` : Secret、PVC、Deployment、Service をまとめた sample manifest

```powershell
docker build -t quartz-knowledge-mcp .
docker run --rm -p 8080:8080 -v quartzknowledge-data:/data -e ASPNETCORE_ENVIRONMENT=Container -e Authentication__ApiKey__Enabled=true -e Authentication__ApiKey__ApiKey=<generate-random-api-key> quartz-knowledge-mcp
```

## 運用スクリプト

- `work/ingest-agent-framework-learn.ps1` : Microsoft Learn の Agent Framework Your First Agent / Agent Types / Workflows / Integrations を Bronze / Silver / Gold に curated ingest する

## テストの見どころ
- `QuartzKnowledgeMcp.Tests/Dashboard` : shell contract、DashboardService、search / summary の回帰
- `QuartzKnowledgeMcp.Tests/Security/ApiKeyAuthenticationTests.cs` : optional auth の 401 / 200 / anonymous health 回帰
- `QuartzKnowledgeMcp.Tests/Workflows/DashboardWorkflowApiTests.cs` : dashboard summary/search/detail/history/related の end-to-end flow
- `QuartzKnowledgeMcp.Tests/Workflows/OperationalCatalogFlowApiTests.cs` : medallion の主要ワークフロー回帰
