---
name: quartz-knowledge-release-validation
description: "Use when: running the final pre-merge or pre-release validation loop for this repository; checking targeted builds, full tests, coverage baseline, git hygiene, optional health smoke, and sample-client regressions."
argument-hint: "What kind of change needs release validation?"
---

# QuartzKnowledge Release Validation

## Use When
- You have changed code, docs, config, or sample assets and want a final release-quality check.
- You want to decide whether a change only needs doc validation, a focused project build, or the full baseline.
- You want a standard order for validating this repo after edits.

## Validation Strategy
1. Start with the narrowest check that can invalidate the current change.
2. If that passes, run the repository baseline.
3. If the change touches runtime behavior, add HTTP or MCP smoke.
4. Finish by checking git hygiene so generated outputs do not contaminate the release diff.

## Change-based Routing
- Docs-only changes:
  - run file diagnostics on edited docs
  - run text searches if you intentionally removed risky wording
- Sample client changes:
  - build `Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj`
- API or application changes:
  - run the most focused tests first, then full solution tests
- Search, MCP, or dashboard behavior changes:
  - run full solution tests and add runtime smoke if behavior changed

## Baseline Commands
```powershell
dotnet build src/QuartzKnowledgeMcp.slnx --no-restore
dotnet test src/QuartzKnowledgeMcp.slnx --no-build
dotnet test src/QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage" --results-directory src/TestResultsCoverage
git status --short
```

## Optional Runtime Checks
### Health smoke
```powershell
Invoke-RestMethod -Uri http://localhost:5080/health | ConvertTo-Json -Depth 5
```

### HTTP and MCP sample smoke
```powershell
dotnet run --project Sample/QuartzKnowledgeMcp.MockClient/QuartzKnowledgeMcp.MockClient.csproj -- --base-url http://localhost:5080
```

Use the `quartz-knowledge-mcp` skill when you need deeper MCP-only validation or repeated inspection loops.

## Decision Points
- If the first focused validation fails but points to the same slice, fix that slice and rerun the same focused check before expanding.
- If a build warning appears in a touched project, treat it as release debt and clear it when practical.
- If `dotnet run` has a lock on the apphost, prefer `dotnet test --no-build` or stop the process before rebuilding.

## Completion Criteria
- Focused validation for the changed slice passes.
- Full solution build and tests pass.
- Coverage remains at or above the current 85% line baseline.
- Generated coverage output stays ignored by git.
- Optional runtime smoke passes for behavior changes.