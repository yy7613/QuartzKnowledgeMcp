# 2026-05-19 MCP サーバー受け入れ・実用性検査レポート

## 概要
このレポートは、[.vscode/mcp.json](.vscode/mcp.json) で設定した `quartz-knowledge-local` を対象に、ローカル MCP サーバーとして期待どおりに動作するか、また実際に使って実用的かを検査した結果をまとめたものである。

検査時点の結論は次のとおり。
- ローカル開発用途では実用的
- VS Code からの初回利用は「server を先に起動する」前提を理解していないとつまずく
- HTTP API と MCP tool surface の機能差が残っており、MCP 完成度は中立からやや否定寄り

## 対象
- MCP endpoint: `http://localhost:5080/mcp`
- Health endpoint: `http://localhost:5080/health`
- VS Code 設定: [.vscode/mcp.json](.vscode/mcp.json)
- 起動 task: [.vscode/tasks.json](.vscode/tasks.json)
- モッククライアント: [Sample/QuartzKnowledgeMcp.MockClient/Program.cs](Sample/QuartzKnowledgeMcp.MockClient/Program.cs)
- 接続ガイド: [Sample/McpClientConfigs/README.md](Sample/McpClientConfigs/README.md)

## 検査チェックリスト
- [x] Health endpoint が 200 を返す
- [x] HTTP の主要フローが最後まで通る
- [x] MCP の主要フローが最後まで通る
- [x] VS Code 用の `mcp.json` が妥当な JSON として読める
- [x] VS Code から起動するための task が妥当な JSON として読める
- [ ] `.vscode/mcp.json` だけで ASP.NET Core の server が自動起動する
- [ ] HTTP API の主要機能が MCP tool と 1 対 1 で揃っている
- [ ] ローカル以外の client でも追加インフラなしでそのまま実用投入できる

## 実施した検査
### 1. 到達性確認
- `Invoke-WebRequest http://localhost:5080/health`
- 結果: `200 OK`
- 応答本文: `status=ok`, `componentName=QuartzKnowledgeMcp.Api`, `environment=Development`

### 2. HTTP フロー確認
実行コマンド:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080 --http-only
```

結果:
- Bronze 登録、一覧、詳細: 成功
- Silver preview: 成功
- Silver organize、detail: 成功
- Gold publish、list、update、tags、history: 成功
- Search GET、Search POST、suggestions、facets: 成功
- related entries endpoint: 成功。ただしサンプルでは空配列
- capabilities 取得: 成功

観測事項:
- `useLlm=true` の preview でも既定構成では `usedLlm=false` で rule-based fallback になった
- `Embedding` は既定で `enabled=false`
- `gold.related` はサンプルデータが 1 件中心のため実用的な関連候補までは示せなかった

### 3. MCP フロー確認
実行コマンド:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080 --mcp-only
```

結果:
- `tools/list`: 成功
- `get_health`: 成功
- `create_bronze_source`: 成功
- `search_catalog`: 成功
- `get_search_suggestions`: 成功

実際に列挙された tool:
- `create_bronze_source`
- `get_bronze_source`
- `get_gold_catalog_entry`
- `get_gold_catalog_history`
- `get_health`
- `get_search_facets`
- `get_search_suggestions`
- `get_silver_server_draft`
- `list_bronze_sources`
- `list_gold_catalog`
- `list_silver_server_drafts`
- `organize_bronze_source`
- `publish_silver_server_draft`
- `replace_gold_catalog_tags`
- `search_catalog`
- `update_gold_catalog_entry`

### 4. VS Code 接続前提の確認
- [.vscode/mcp.json](.vscode/mcp.json) は JSON として妥当
- [.vscode/tasks.json](.vscode/tasks.json) は JSON として妥当
- `run quartz-knowledge-mcp` task で server 起動手順を workspace に内包した
- 実際には `.vscode/mcp.json` は接続先定義であり、server 自体は自動起動しない

### 5. 失敗・摩擦の確認
実際に観測した摩擦:
- server 未起動の状態では VS Code 側で `Error sending message to http://localhost:5080/mcp: TypeError: fetch failed` が発生した
- 5080 をすでに別プロセスが掴んでいる状態で再起動すると `address already in use` で失敗した
- HTTP API にある `POST /api/search/query` と `GET /api/gold/catalog/{entryId}/related` に対応する MCP tool は現時点で存在しない
- `get_system_capabilities` 相当の MCP tool も未実装

## Mermaid サマリー
```mermaid
flowchart TD
    A[VS Code mcp.json] --> B[ASP.NET Core server on :5080]
    B --> C[/health 200]
    B --> D[HTTP flow pass]
    B --> E[MCP flow pass]
    D --> F[Local development use is practical]
    E --> F
    A --> G[Server not started]
    G --> H[fetch failed in VS Code]
    D --> I[HTTP features richer than MCP tools]
    E --> I
    F --> J[Overall verdict: Neutral to Positive for local use]
    H --> J
    I --> J
```

## 実用性判断
### 肯定的な立場
- ローカルで server を起動している限り、MCP tool の基本操作は安定している
- health、取り込み、検索、候補提示までのコア導線は実動確認できた
- HTTP API 側は Bronze -> Silver -> Gold -> Search まで一通り通っており、知識カタログサーバーとしての基礎機能は十分にある
- VS Code 用の workspace 設定と task を用意したことで、開発者の再現手順はかなり明確になった

### 中立的な立場
- 現状は「ローカル開発・検証用 MCP サーバー」としては妥当だが、「他クライアントに広く配る完成済み MCP 製品」とまでは言いづらい
- LLM と Embedding が既定で無効なため、検索や整理は現時点では rule-based 主体である
- related entries は endpoint 自体は動くが、サンプルデータでは価値が伝わりにくい
- VS Code 連携は構成できるが、利用者が server 起動責務を理解していることが前提になる

### 否定的な立場
- `.vscode/mcp.json` だけで使い始められるわけではなく、未起動時は `fetch failed` になるため初見の体験は弱い
- HTTP API と MCP tool surface が揃っていないため、「HTTP でできることがそのまま MCP でもできる」とは言えない
- Claude 系の remote connector など、ローカル外の client で使うには HTTPS 公開や wrapper が必要で、そのままでは実用投入しにくい
- capabilities や advanced search、related entries が MCP から触れない点は、MCP サーバーとしての一貫性を下げている

## 総合判定
総合判定は **中立寄りに肯定** とする。

理由:
- ローカル開発用 MCP サーバーとしては十分に動作し、基本ユースケースは実測で通った
- ただし、起動手順の明示、HTTP と MCP の機能差、remote 利用時の追加要件が残っているため、「誰でもすぐ実用」とまでは言えない

## 次の改善候補
1. `get_system_capabilities`、advanced search、related entries を MCP tool として追加する
2. server 未起動時の導線をさらに改善する。例: 起動 task の README 冒頭移動、トラブルシュート強化
3. Claude 系 remote connector 向けに HTTPS 公開手順または stdio wrapper を追加する
4. related entries の価値が見えるサンプルデータを用意する
