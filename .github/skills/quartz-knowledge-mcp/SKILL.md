---
name: quartz-knowledge-mcp
description: "Use when: operating, seeding, validating, or debugging the local QuartzKnowledgeMcp server; querying the catalog via MCP; running the MCP quality harness; checking capabilities, advanced search, related entries, or VS Code MCP setup."
---

# QuartzKnowledgeMcp Skill

## Use When
- You want to operate the local QuartzKnowledgeMcp server from VS Code or another MCP client.
- You need to seed catalog data through MCP tools instead of calling the HTTP API directly.
- You want to validate MCP quality with repeated search, suggestions, facets, history, capabilities, and related-entry checks.
- You need to debug why MCP works in one client but fails in VS Code.

## Preconditions
- Start the server first.
- Preferred workspace task: `run quartz-knowledge-mcp`
- For repeated quality runs, prefer `run quartz-knowledge-mcp (quality-db)`
- Reset task for a clean quality store: `reset quartz-knowledge-quality-db`
- Direct command:

```powershell
dotnet run --project src/QuartzKnowledgeMcp.Api/QuartzKnowledgeMcp.Api.csproj --no-launch-profile --urls http://localhost:5080
```

- Workspace MCP config: `.vscode/mcp.json`
- Local MCP endpoint: `http://localhost:5080/mcp`

## Fast Checks
### Basic MCP smoke
```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080 --mcp-only
```

### 24-loop quality harness
```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080 --quality-only --seed-count 24 --inspection-loops 24
```

Recommended sequence for repeated quality runs:
1. Run `reset quartz-knowledge-quality-db`
2. Run `run quartz-knowledge-mcp (quality-db)`
3. Execute the 24-loop quality harness

This harness seeds larger MCP documents, publishes them through MCP tools, and then runs repeated inspection loops against:
- `get_system_capabilities`
- `search_catalog`
- `search_catalog_advanced`
- `get_search_suggestions`
- `get_search_facets`
- `get_gold_catalog_entry`
- `get_gold_catalog_history`
- `get_related_entries`

## Current MCP Tool Surface
### System
- `get_health`
- `get_system_capabilities`

### Bronze / Silver / Gold
- `create_bronze_source`
- `list_bronze_sources`
- `get_bronze_source`
- `organize_bronze_source`
- `list_silver_server_drafts`
- `get_silver_server_draft`
- `publish_silver_server_draft`
- `list_gold_catalog`
- `get_gold_catalog_entry`
- `update_gold_catalog_entry`
- `replace_gold_catalog_tags`
- `get_gold_catalog_history`
- `get_related_entries`

### Search
- `search_catalog`
- `search_catalog_advanced`
- `get_search_suggestions`
- `get_search_facets`

## Recommended Workflow
1. Start the local server.
2. Run the MCP smoke flow.
3. If search parity or relation quality matters, run the quality harness with at least 20 loops.
4. If a failure appears, inspect whether it is a server issue, a data-shape issue, or an MCP client startup issue.
5. Re-run the same quality harness after each fix.

## Known Caveats
- `.vscode/mcp.json` does not auto-start the ASP.NET Core server.
- VS Code fails with `fetch failed` when the server is not already listening on port 5080.
- Claude-style remote connectors need a client-reachable HTTPS URL instead of `localhost`.
- Large repeated harness runs will intentionally grow the local SQLite data set unless you use the quality DB reset flow.
