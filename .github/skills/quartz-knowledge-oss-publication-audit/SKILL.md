---
name: quartz-knowledge-oss-publication-audit
description: "Use when: auditing this repository for GitHub or OSS publication quality; checking public docs, tracked generated artifacts, placeholder secrets, stale quality metrics, internal-only wording, and finishing with build/test/coverage validation."
argument-hint: "What publication risk or release check do you want to run?"
---

# QuartzKnowledge OSS Publication Audit

## Use When
- You want to know whether this repository is safe and polished enough for GitHub publication.
- You need to clean up tracked generated files, local artifacts, or temporary outputs before release.
- You suspect public docs still point at work notes, stale metrics, or internal-only wording.
- You want a repeatable audit, fix, and validate loop instead of a one-pass review.

## Audit Scope
- Authoritative public docs: `readme.md`, `docs/`, `src/README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `deploy/`
- Historical and supporting docs: `implementation/`, `ideas/`, `work/`
- Release gates: git hygiene, doc hygiene, secret placeholders, build, tests, and coverage

## Procedure
1. Start from the most public surfaces first: `readme.md`, `docs/spec.md`, `CONTRIBUTING.md`, `SECURITY.md`, `src/README.md`.
2. Search for publication risks:
   - generated artifacts such as `bin/`, `obj/`, `TestResults/`, `TestResultsCoverage/`, `*.db`
   - temporary wording such as `TODO`, `WIP`, `temporary`, `一時`, `draft`
   - internal or private wording such as `社内`, `internal`, `private`
   - stale or misleading metrics, test counts, or dated quality notes
   - links from public docs into `work/` or similarly local-only material
3. Inspect git tracking status. If generated outputs are tracked, fix ignore rules first, then remove the tracked artifacts.
4. Review sample configs and manifests for secrets. Convert sample values to explicit placeholders and document safe secret injection.
5. Review historical docs. If old phase reports or idea memos can be mistaken for current truth, label them as snapshot or history and point readers back to authoritative docs.
6. After each substantive edit, run the narrowest validation that can falsify the change.
7. Finish with full repo validation: build, test, coverage, and `git status`.

## Decision Points
- If a public doc links to `work/` or dated operational notes, remove the public dependency or restate the stable guidance in authoritative docs.
- If a metric is a point-in-time measurement, add a date or move the current truth to `implementation/phase-status.md` and the root README.
- If a file is historical but still worth keeping, relabel it as history instead of deleting it.
- If a finding only appears in generated output under `bin/`, `obj/`, or `TestResults*`, treat it as repository hygiene rather than product behavior.

## Completion Criteria
- No tracked generated artifacts remain part of the intended release contents.
- Public docs do not depend on `work/` notes or ambiguous internal-only wording.
- Sample secrets are obvious placeholders and not reusable values.
- Historical docs are clearly marked as snapshots when needed.
- `dotnet build`, `dotnet test`, and the coverage baseline all pass.
- New coverage output does not appear as untracked in `git status`.

## Validation Baseline
```powershell
dotnet build src/QuartzKnowledgeMcp.slnx --no-restore
dotnet test src/QuartzKnowledgeMcp.slnx --no-build
dotnet test src/QuartzKnowledgeMcp.slnx --no-build --collect:"XPlat Code Coverage" --results-directory src/TestResultsCoverage
git status --short
```

## Repo-specific Cautions
- Coverage output belongs under `src/TestResultsCoverage` and should remain ignored.
- `implementation/phase-status.md` is the current quality ledger. Phase reports are historical snapshots.
- For MCP runtime smoke beyond build and test, use the `quartz-knowledge-mcp` skill.