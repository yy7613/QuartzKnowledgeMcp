---
name: quartz-knowledge-doc-snapshot-hygiene
description: "Use when: reviewing README, docs, implementation, or ideas content for public readers; separating current documentation from historical notes; removing stale links, internal-only phrasing, and ambiguous snapshot metrics."
argument-hint: "Which docs or wording problem do you want to clean up?"
---

# QuartzKnowledge Doc Snapshot Hygiene

## Use When
- Public-facing docs need cleanup for third-party readers.
- Readers may confuse `implementation/` or `ideas/` with the current spec.
- You are updating metrics, test counts, coverage notes, or release-readiness text.
- You want a repeatable rule set for deciding whether a doc is authoritative or historical.

## Authority Order
1. `docs/spec.md`, `docs/api.md`, `docs/architecture.md`
2. `readme.md`, `src/README.md`, `CONTRIBUTING.md`, `SECURITY.md`
3. `implementation/phase-status.md` for the current quality snapshot
4. `implementation/phase-xx-report.md` and `ideas/` as history unless explicitly promoted

## Procedure
1. Classify each touched document as authoritative, historical, or local-only.
2. For authoritative docs:
   - remove dated work-note links and unstable references
   - replace style metaphors or product-comparison wording that does not describe actual behavior
   - add dates to point-in-time metrics such as test counts or coverage values
   - keep provider or product names only when they are architectural facts
3. For historical docs:
   - add a short note that the file is an archive or snapshot
   - point the reader to the current authoritative docs
   - preserve historical numbers if they are useful, but make the time boundary explicit
4. For local-only docs:
   - keep them out of authoritative documentation paths
   - avoid linking to them from public README files unless they are explicitly documented as supporting material
5. Validate with targeted text searches and file diagnostics.

## Decision Points
- If the same fact appears in both the root README and a phase report, the README should carry the current value and the phase report should keep the historical value with a snapshot note.
- If a detail only matters to maintainers during local operations, prefer `work/` over authoritative docs.
- If a product name is a real dependency such as Microsoft Agent Framework, keep it. If it is only a visual analogy or comparison label, replace it with behavior-focused wording.

## Completion Criteria
- Public docs read coherently to third parties without project-history context.
- Historical docs cannot be mistaken for current requirements or current quality numbers.
- No internal-only wording, local-path leakage, or unstable work-note dependency remains in authoritative docs.

## Suggested Checks
```powershell
rg -n "TODO|WIP|temporary|一時|社内|internal|private|work/" readme.md docs src/README.md CONTRIBUTING.md SECURITY.md implementation ideas
rg -n "tests passing|coverage|snapshot|履歴" readme.md implementation ideas
```