# Phase Status

## 目的
各フェーズの進行可否を、テストカバレッジ率、回帰テスト、完了レポートの観点で管理する。

## 運用ルール
- フェーズ開始時に `Phase status` を `in-progress` に更新する
- フェーズ完了時にカバレッジ実測値、回帰テスト結果、完了レポート有無を更新する
- `Coverage recorded`、`Regression done`、`Report done` がすべて `yes` になったときだけ `Next phase unlocked` を `yes` にする

## 一覧
| Phase | Phase status | Coverage target | Coverage actual | Coverage recorded | Regression done | Report done | Next phase unlocked | Notes |
|:--|:--|:--|:--|:--|:--|:--|:--|:--|
| Phase 01 | completed | 70% | 70.00% | yes | yes | yes | yes | `GET /health` verified with HTTP 200. |
| Phase 02 | completed | 70% | 95.94% | yes | yes | yes | yes | Bronze registration/list/detail verified with SQLite and HTTP API. |
| Phase 03 | completed | 70% | 77.40% | yes | yes | yes | yes | Silver organize/list/detail verified with SQLite, unit tests, and HTTP smoke. |
| Phase 04 | completed | 70% | 79.85% | yes | yes | yes | yes | Gold publish/detail and history verified with SQLite, unit tests, and HTTP smoke. |
| Phase 05 | completed | 75% | 86.37% | yes | yes | yes | yes | Gold list/search and API integration tests verified with SQLite and WebApplicationFactory. |
| Phase 06 | completed | 75% | 86.31% | yes | yes | yes | yes | Gold update, tag replacement, and history paging verified with unit/API tests. |
| Phase 07 | completed | 80% | 88.33% | yes | yes | yes | yes | Domain/Application split, HTTP MCP tool surface, Sample mock client, and 57 tests verified. |
| Phase 08 | completed | 80% | 87.94% | yes | yes | yes | yes | Repository ports, SQLite adapters, fake repositories, and runtime re-organize regression verified. |
| Phase 09 | completed | 80% | 88.57% | yes | yes | yes | yes | IOrganizationAgent, preview organize, capabilities API, and updated Sample verified. |
| Phase 10 | completed | 80% | 87.96% | yes | yes | yes | yes | Foundry organize adapter, selector/fallback, capabilities wiring, and HTTP/MCP runtime smoke verified on the exact Azure.AI.Projects 2.1.0-beta.1 surface. |
| Phase 11 | completed | 80% | 87.98% | yes | yes | yes | yes | Embedding ports, no-op adapters, Gold indexing hooks, and default HTTP/MCP runtime smoke were verified without changing existing search behavior. |
| Phase 12 | completed | 80% | 87.35% | yes | yes | yes | yes | Structured search query, suggestions, facets, related entries, and HTTP/MCP runtime smoke were verified with structured-first relation scoring. |
| Phase 13 | completed | 85% | 88.86% | yes | yes | yes | yes | Phase 13 完了時点では、regression baseline、dashboard redesign follow-up、OSS publication docs、final HTTP/MCP/browser smoke を 97/97 passing tests で確認した。 |

## Post Phase 13 Updates
- 2026-05-20: optional API key auth を `/api` と `/mcp` に追加し、`ApiKeyAuthenticationTests` で `401` / `200` / anonymous health を回帰確認した
- 2026-05-20: dashboard search result preview dialog を追加し、hero の旧説明文を除去した。browser verification では modal preview の表示まで確認した
- 2026-05-20: `Dockerfile`、`appsettings.Container.json`、`deploy/kubernetes/quartz-knowledge.sample.yaml` を追加し、container 配備用のひな形を整備した
- 2026-05-20: `work/ingest-agent-framework-learn.ps1` で Microsoft Learn の Agent Framework 関連 4 ページを curated ingest し、dashboard search と related entry を確認した
- 2026-05-20: 公開前再検証として全体回帰と full coverage を再実行し、100 / 100 tests passing、line coverage 88.81%、branch coverage 70.66% を確認した
