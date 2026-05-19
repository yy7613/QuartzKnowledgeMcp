# QuartzKnowledge MCP Server

QuartzKnowledge MCP Server は、MCP server 情報を Bronze / Silver / Gold の medallion flow で取り込み、HTTP API と MCP の両面から検索・運用できる .NET 10 ベースの knowledge server です。

人間向けには dark glass のダッシュボードを持ち、検索結果プレビュー、メダリオン別の推移、Gold entry の詳細 / 履歴 / 関連情報を PC 画面で確認できます。

## 主な機能
- Bronze ingestion から Silver organize、Gold catalog publish までの medallion pipeline
- HTTP API と MCP tool surface の両対応
- 運用向けダッシュボード。Search / Graph / Inspect の 3 タブと検索結果プレビュー ダイアログを持ち、クエリ文字列と localStorage で状態を復元
- `/api` と `/mcp` を設定で保護できる optional API key auth
- `Dockerfile`、container appsettings、Kubernetes sample manifest を含む配備ひな形
- Microsoft Learn の Agent Framework ページを curated ingest する PowerShell script
- Sample mock client と repeated quality harness による runtime smoke と MCP 品質反復
- WebApplicationFactory、service-level tests、coverage gate を含む回帰基盤

## Quick Start
1. API を起動します。

```powershell
dotnet run --project src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj --urls http://localhost:5080
```

2. ダッシュボードを開きます。

```text
http://localhost:5080/dashboard
```

3. Sample client で日本語を含む seed を投入し、HTTP と MCP の主要フローを確認します。

```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080
```

4. Agent Framework の Learn データを curated ingest する場合は次を実行します。

```powershell
pwsh ./work/ingest-agent-framework-learn.ps1
```

## 認証と配備
- API key auth は既定で無効です。`Authentication__ApiKey__Enabled=true` と `Authentication__ApiKey__ApiKey=<secret>` を設定すると `/api` と `/mcp` を保護します。
- 既定 header 名は `X-QuartzKnowledge-Api-Key` です。`/health` と静的な `/dashboard` シェルは匿名のままですが、ダッシュボードが読む `/api/dashboard/*` も保護対象なので、認証有効時のブラウザー利用はリバースプロキシや Ingress などで header を注入する前提です。
- Container 実行用に `Dockerfile`、`.dockerignore`、`src/QuartzKnowledgeMcp.Api/appsettings.Container.json`、`deploy/kubernetes/quartz-knowledge.sample.yaml` を同梱しています。
- 文書中の API key や Secret 値はすべて無効なプレースホルダーです。本番では secret manager または orchestration platform の Secret 機能でランダム値を注入してください。

```powershell
docker build -t quartz-knowledge-mcp .
docker run --rm -p 8080:8080 -v quartzknowledge-data:/data -e ASPNETCORE_ENVIRONMENT=Container -e Authentication__ApiKey__Enabled=true -e Authentication__ApiKey__ApiKey=<generate-random-api-key> quartz-knowledge-mcp
```

## 品質基準
- 直近の全体回帰: 2026-05-20 時点で 100 / 100 tests passing
- 直近の記録済み line coverage: 2026-05-20 時点で 88.81%
- coverage baseline: 85% 以上を維持

ローカル確認コマンド:

```powershell
dotnet build src/QuartzKnowledgeMcp.slnx --no-restore
dotnet test src/QuartzKnowledgeMcp.slnx --no-build
dotnet test src/QuartzKnowledgeMcp.slnx --collect:"XPlat Code Coverage"
```

GitHub Actions でも同じ基準を [.github/workflows/ci.yml](.github/workflows/ci.yml) で検証します。

## ドキュメント
- [docs/spec.md](docs/spec.md): 要求と仕様
- [docs/architecture.md](docs/architecture.md): 全体アーキテクチャ
- [docs/api.md](docs/api.md): HTTP API surface
- [src/README.md](src/README.md): ソースツリー、ダッシュボード、テスト運用
- [Sample/README.md](Sample/README.md): Sample mock client と quality harness
- [deploy/kubernetes/quartz-knowledge.sample.yaml](deploy/kubernetes/quartz-knowledge.sample.yaml): Kubernetes 配備サンプル
- [CONTRIBUTING.md](CONTRIBUTING.md): 開発参加時のセットアップと品質基準
- [SECURITY.md](SECURITY.md): 脆弱性報告ポリシー
- [implementation/phase-status.md](implementation/phase-status.md): coverage / regression 管理表

`implementation` と `ideas` は履歴と検討経緯を残す補助文書です。現行の公開仕様、運用前提、品質基準は `docs` とこの README 群を優先してください。

## リポジトリ構成
| Path | Role |
|:--|:--|
| `src` | API、本体実装、テスト |
| `Sample` | runtime smoke と MCP quality harness |
| `docs` | 仕様、設計、ADR |
| `implementation` | 段階実装の計画と履歴。phase report の数値は各時点のスナップショット |
| `ideas` | 初期構想のアーカイブ。現行仕様は `docs` を参照 |
| `work` | ローカル運用メモと補助スクリプト。公開 API / ドキュメント契約の対象外 |
| `.github` | CI、PR 運用テンプレート、Copilot workspace skills |

## 開発フロー
この repo では `ideas` -> `docs` -> `implementation` -> `src` の順で要求を具体化します。新しい機能を追加するときは、コードだけでなく関連 docs、tests、coverage への影響も同時に更新してください。
