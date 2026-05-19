# Sample

`QuartzKnowledgeMcp.MockClient` はローカルで起動した API / MCP サーバーに対して、主要フローを順に叩くモッククライアントです。

## 実行例

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080
```

HTTP のみ確認する場合:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080 --http-only
```

MCP のみ確認する場合:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080 --mcp-only
```

`--mcp-only` は preview organize の非永続化確認を行った後、bronze -> silver -> gold -> search/history/related までの MCP smoke を通します。

大きめのデータを投入しながら 24 回の MCP 品質ループを回す場合:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080 --quality-only --seed-count 24 --inspection-loops 24
```

繰り返し品質検査では quality 専用 DB を使う方が安全です。

1. `Tasks: Run Task` で `reset quartz-knowledge-quality-db` を実行する
2. `Tasks: Run Task` で `run quartz-knowledge-mcp (quality-db)` を起動する
3. 上の `--quality-only` コマンドを実行する

Quality DB の接続先は API project の content root 基準に固定されています。起動 directory に依存しません。

## クライアント設定例

VS Code や他の MCP client から接続するための設定例は [Sample/McpClientConfigs/README.md](Sample/McpClientConfigs/README.md) を参照してください。

VS Code 用の `mcp.json` サンプルは [Sample/McpClientConfigs/vscode.mcp.json](Sample/McpClientConfigs/vscode.mcp.json) に置いてあります。