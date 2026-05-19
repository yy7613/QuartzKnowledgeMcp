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
| Phase 13 | completed | 85% | 87.35% | yes | yes | yes | yes | Regression workflow baseline, coverage policy, report template, and final HTTP/MCP smoke were verified with 79/79 passing tests. |
