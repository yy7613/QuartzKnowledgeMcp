# Sample

`QuartzKnowledgeMcp.MockClient` はローカルで起動した API / MCP サーバーに対して、主要フローを順に叩くモッククライアントです。

HTTP フローと MCP フローの両方で、日本語を含むシード データをブロンズ -> シルバー -> ゴールドへ流し込みます。ダッシュボードの検索やインスペクターを人間が試す前のランタイム スモークとして使えます。

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

`--mcp-only` はプレビュー整理の非永続化確認を行った後、ブロンズ -> シルバー -> ゴールド -> search/history/related までの MCP スモークを通します。

大きめのデータを投入しながら 24 回の MCP 品質ループを回す場合:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient -- --base-url http://localhost:5080 --quality-only --seed-count 24 --inspection-loops 24
```

`--quality-only` では日本語ラベルを含む品質シードを使い、search / history / related / プレビュー整理の反復検査をまとめて検証します。

繰り返し品質検査では品質専用 DB を使う方が安全です。

1. `Tasks: Run Task` で `reset quartz-knowledge-quality-db` を実行する
2. `Tasks: Run Task` で `run quartz-knowledge-mcp (quality-db)` を起動する
3. 上の `--quality-only` コマンドを実行する

品質 DB の接続先は API プロジェクトのコンテンツ ルート基準に固定されています。起動ディレクトリに依存しません。

## 何が確認できるか
- HTTP の create / organize / publish / update / タグ全置換 / search の一連フロー
- MCP のツール一覧とヘルスチェック
- MCP の作成 / 整理プレビュー / 詳細 / 公開 / 高度検索 / 履歴 / 関連
- ダッシュボードで確認しやすい日本語エントリの投入

## クライアント設定例

VS Code や他の MCP クライアントから接続するための設定例は [Sample/McpClientConfigs/README.md](Sample/McpClientConfigs/README.md) を参照してください。

VS Code 用の `mcp.json` サンプルは [Sample/McpClientConfigs/vscode.mcp.json](Sample/McpClientConfigs/vscode.mcp.json) に置いてあります。