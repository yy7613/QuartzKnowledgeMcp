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