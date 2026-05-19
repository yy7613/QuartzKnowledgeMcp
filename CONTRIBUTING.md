# Contributing

## Scope
この repository では medallion pipeline、MCP tool surface、ダッシュボード、Sample quality harness を一体で管理します。コード変更だけでなく、関連 docs と tests の更新も同じ pull request または同じ作業単位に含めてください。

## Prerequisites
- .NET 10 SDK
- PowerShell
- ローカルで SQLite file を扱える環境

## Local setup
```powershell
dotnet restore src/QuartzKnowledgeMcp.slnx
dotnet build src/QuartzKnowledgeMcp.slnx --no-restore
```

API を起動する場合:

```powershell
dotnet run --project src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj --urls http://localhost:5080
```

Sample smoke を流す場合:

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080
```

## ワークスペース Skill
VS Code / Copilot 向けのワークスペース共有 Skill は `.github/skills/` に配置します。

- `quartz-knowledge-mcp`: ローカル MCP サーバーの起動、seed、quality harness、MCP debugging
- `quartz-knowledge-oss-publication-audit`: GitHub 公開前のドキュメント、生成物、secret placeholder、品質指標の監査
- `quartz-knowledge-doc-snapshot-hygiene`: 現行ドキュメントと履歴文書の切り分け
- `quartz-knowledge-release-validation`: build、test、coverage、git hygiene の最終検証

repo 固有ワークフローを追加したら、関連 Skill の `description` と手順も更新してください。

## Test expectations
最低限、変更範囲に応じて以下を実行してください。

```powershell
dotnet test src/QuartzKnowledgeMcp.slnx --no-build
dotnet test src/QuartzKnowledgeMcp.slnx --collect:"XPlat Code Coverage"
```

Line coverage は 85% 未満に下げないでください。やむを得ず下がる場合は、理由と回復計画を pull request に明記してください。

## Docs expectations
以下に影響がある変更では docs も更新してください。
- API / MCP surface を変えたとき: `docs/api.md`, `src/README.md`
- ダッシュボード挙動を変えたとき: `src/README.md`, 必要なら `readme.md`
- Sample / quality harness を変えたとき: `Sample/README.md`
- baseline や coverage を変えたとき: `implementation/phase-status.md`

## Pull request guidance
個人プロジェクトとして直接コミットする場合も、以下と同じ情報をコミットメッセージ、リリースメモ、または作業メモに残してください。

- 変更理由を最初に書く
- ユーザー影響または運用影響を書く
- 実行した tests と coverage 結果を書く
- ダッシュボード UI を変えた場合はスクリーンショットかブラウザー スモーク結果を書く
- 未解決のリスクがあれば明記する
