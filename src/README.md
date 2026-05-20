# src

アプリケーションコードとテストはこのディレクトリに配置します。

正式なソリューションは `QuartzKnowledgeMcp.slnx` です。

## 開発コマンド

```powershell
dotnet build src/QuartzKnowledgeMcp.slnx --no-restore
dotnet test src/QuartzKnowledgeMcp.slnx --no-build
dotnet test src/QuartzKnowledgeMcp.slnx --collect:"XPlat Code Coverage"
```

## ダッシュボード

API を起動した後は `/dashboard` で人間向けの参照画面を開けます。

- グラススタイルの運用シェルで、PC 画面での一覧性を優先している
- Search タブ : メダリオン横断検索。`stage` / `tag` / `freshness` / `sort` とクエリなしブラウズを利用可能
- 検索結果プレビュー : タイトルクリックでブロンズ / シルバー / ゴールドの詳細 JSON をモーダル ダイアログで確認可能
- Graph タブ : ブロンズ / シルバー / ゴールドの件数、鮮度、最近の項目、3d / 7d トレンドを表示
- Inspect タブ : ゴールドの詳細 / 履歴 / 関連情報を一画面で確認可能
- 状態保持 : クエリ文字列と localStorage の両方で検索条件、アクティブ タブ、トレンド、Inspect 対象を復元可能
- `/api/dashboard/summary` : サマリー データ、タグクラウド、トレンド、詳細パス
- `/api/dashboard/search` : ダッシュボード用のブラウズ / 検索 API

## 認証

API キー認証は `src/QuartzKnowledgeMcp.Api/appsettings.json` の `Authentication:ApiKey` で構成します。既定値は無効です。

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

- `/api` と `/mcp` 配下は API キーが必須になり、未指定または不正なキーでは `401` を返す
- `/health` と静的な `/dashboard` シェルは匿名のまま
- ただしダッシュボード本体は `/api/dashboard/*` を読むため、認証有効時のブラウザー利用はリバースプロキシや Ingress などで同じヘッダーを注入する前提
- 文書中の API キーはプレースホルダーであり、そのまま配備しない

## コンテナ配備

- `Dockerfile` : .NET 10 SDK / ASP.NET ランタイムのマルチステージ ビルド。`8080` で待ち受け、`/data` をボリュームとして公開
- `src/QuartzKnowledgeMcp.Api/appsettings.Container.json` : SQLite の保存先を `/data/quartz-knowledge.db` に切り替えるコンテナー用設定
- `deploy/kubernetes/quartz-knowledge.sample.yaml` : Secret、PVC、Deployment、Service をまとめたサンプル マニフェスト

```powershell
docker build -t quartz-knowledge-mcp .
docker run --rm -p 8080:8080 -v quartzknowledge-data:/data -e ASPNETCORE_ENVIRONMENT=Container -e Authentication__ApiKey__Enabled=true -e Authentication__ApiKey__ApiKey=<generate-random-api-key> quartz-knowledge-mcp
```

## 運用スクリプト

- `work/ingest-agent-framework-learn.ps1` : Microsoft Learn の Agent Framework Your First Agent / Agent Types / Workflows / Integrations をブロンズ / シルバー / ゴールドにキュレーション取り込みする

## テストの見どころ
- `QuartzKnowledgeMcp.Tests/Dashboard` : シェル契約、DashboardService、search / summary の回帰
- `QuartzKnowledgeMcp.Tests/Security/ApiKeyAuthenticationTests.cs` : オプション認証の 401 / 200 / 匿名ヘルスチェック回帰
- `QuartzKnowledgeMcp.Tests/Workflows/DashboardWorkflowApiTests.cs` : ダッシュボード summary/search/detail/history/related のエンドツーエンド フロー
- `QuartzKnowledgeMcp.Tests/Workflows/OperationalCatalogFlowApiTests.cs` : メダリオンの主要ワークフロー回帰
