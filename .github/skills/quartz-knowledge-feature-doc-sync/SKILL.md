---
name: quartz-knowledge-feature-doc-sync
description: "Use when: synchronizing repository docs after QuartzKnowledge feature work; updating README files, API docs, phase status, and validation notes so auth, dashboard, runtime, and deployment changes are reflected consistently."
argument-hint: "What feature or change should the docs reflect?"
---

# QuartzKnowledge Feature Doc Sync

## Use When
- You have finished implementation and need to bring docs up to date before stopping.
- Features changed runtime behavior, auth, dashboard UX, deployment, or validation results.
- You want a repeatable order for repo-specific doc updates.

## Update Order
1. Update the root `readme.md` with user-facing capabilities, quick start, deployment, and current regression status.
2. Update `src/README.md` with implementation-facing behavior, runtime caveats, test locations, and operational scripts.
3. Update `docs/api.md` if endpoints, auth rules, or request/response contracts changed.
4. Update `implementation/phase-status.md` with post-phase notes, new pass counts, and any coverage caveats.
5. Review the diff for wording drift or outdated numbers.

## Decision Points
- If coverage was not recollected, keep the last recorded coverage value and state that explicitly instead of inventing a new number.
- If browser verification caught a runtime-only issue, record the verification result in phase status even when the code change was small.
- If a new script or deployment asset was added, mention both the file and the intended use, not only the path.
- If auth is optional, document both the default anonymous behavior and the enabled-mode caveats.

## Repository Hotspots
- `readme.md`
- `src/README.md`
- `docs/api.md`
- `implementation/phase-status.md`

## Review Checklist
- Feature list reflects the actual shipped behavior.
- Quick start still works as written.
- Auth docs describe the enabled and disabled modes correctly.
- Container docs list the real files and environment variables.
- Test counts and validation notes match the latest executed checks.

## Completion Criteria
- User-facing and implementation-facing docs agree.
- API docs cover any new route or auth rule.
- Phase status records the latest validated outcomes.
- A final diff review finds no obvious stale wording, numbers, or missing assets.