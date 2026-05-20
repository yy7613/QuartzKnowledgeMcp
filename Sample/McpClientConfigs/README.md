# MCP クライアント接続例

このリポジトリの MCP サーバーは、ローカル起動時に `http://localhost:5080/mcp` で Streamable HTTP の MCP エンドポイントを公開します。

2026-05-19 時点で、以下は実動確認済みです。
- `tools/list`
- `get_health`
- `create_bronze_source`
- `organize_bronze_source` プレビュー / 非プレビュー
- `publish_silver_server_draft`
- `search_catalog`
- `search_catalog_advanced`
- `get_search_suggestions`
- `get_search_facets`
- `get_gold_catalog_entry`
- `get_gold_catalog_history`
- `get_related_entries`

## サーバー起動

```powershell
dotnet run --project src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj --no-launch-profile --urls http://localhost:5080
```

## VS Code

VS Code はワークスペースまたはユーザー プロファイルの `mcp.json` で MCP サーバーを追加できます。
このリポジトリにはサンプルとして [Sample/McpClientConfigs/vscode.mcp.json](Sample/McpClientConfigs/vscode.mcp.json) を置いてあります。

重要:
- [\.vscode/mcp.json](.vscode/mcp.json) は接続先 URL を VS Code に教えるだけで、ASP.NET Core のサーバー自体は自動起動しません
- 先に `dotnet run` で API を起動するか、ワークスペース タスクの `run quartz-knowledge-mcp` を実行してから MCP サーバーを有効化してください
- 反復品質検査では `run quartz-knowledge-mcp (quality-db)` を使うと開発用 DB を汚さずに済みます

使い方:
1. [Sample/McpClientConfigs/vscode.mcp.json](Sample/McpClientConfigs/vscode.mcp.json) の内容をワークスペースの `.vscode/mcp.json` またはユーザー プロファイルの `mcp.json` にコピーする
2. `Tasks: Run Task` から `run quartz-knowledge-mcp` を起動するか、別ターミナルで API を起動する
3. VS Code で MCP サーバーを信頼する
4. `MCP: List Servers` またはチャット ビューでサーバーが起動していることを確認する
5. Chat でツールを有効化して利用する

想定エンドポイント:
- URL: `http://localhost:5080/mcp`
- トランスポート: HTTP

## 他の MCP クライアント

HTTP または Streamable HTTP の MCP サーバーを登録できるクライアントでは、同じエンドポイントを使えます。

- MCP URL: `http://localhost:5080/mcp`
- 認証: ローカル検証では追加認証なし
- 主要ツール: `get_health`, `create_bronze_source`, `search_catalog`, `get_search_suggestions`

## Claude 系クライアントについて

Claude Desktop の公式クイックスタートは主に stdio サーバーの設定を扱っています。
一方、リモート MCP コネクターを使う構成では、一般にクライアント側から到達できる HTTPS URL が必要です。`localhost` のままでは Web 側や別端末からは到達できません。

そのため Claude 系クライアントで使う場合は次のどちらかです。
- stdio サーバーとして別途ラップする
- この API をトンネルやリバース プロキシで HTTPS 公開し、その URL をリモート コネクターに登録する

リモート コネクター用に公開する場合の登録先 URL は、ローカル URL を置き換えた次の形式になります。

```text
https://your-public-host.example.com/mcp
```

## 動作確認コマンド

MCP 接続だけ確認する場合:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080 --mcp-only
```

大きめのデータを投入しながら MCP 品質検査を回す場合:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080 --quality-only --seed-count 24 --inspection-loops 24
```

このコマンドは MCP ツールだけを使って 24 件の大きめデータを投入し、24 回の検査ループを実行します。

推奨手順:
1. `Tasks: Run Task` で `reset quartz-knowledge-quality-db` を実行する
2. `Tasks: Run Task` で `run quartz-knowledge-mcp (quality-db)` を起動する
3. 上の品質ハーネスを実行する

補足:
- 品質 DB の SQLite パスは API プロジェクトのコンテンツ ルート基準に正規化されます
- reset task は新旧の配置候補を両方削除するため、以前の起動方法で残った DB も掃除できます

## トラブルシュート

- `tools/list` に失敗する場合: まず `/health` と `/mcp` の到達性を確認する
- VS Code で見えない場合: 信頼状態、`mcp.json` の配置先、MCP 出力ログを確認する
- Claude 系クライアントで接続できない場合: `localhost` ではなくクライアントから到達可能な HTTPS URL になっているか確認する
