---
name: quartz-knowledge-dashboard-browser-validation
description: "Use when: validating QuartzKnowledge dashboard HTML, CSS, or JavaScript changes; verifying search behavior, preview dialogs, inspect flows, stale cache issues, and browser-visible regressions after dashboard edits."
argument-hint: "What dashboard behavior changed?"
---

# QuartzKnowledge Dashboard Browser Validation

## Use When
- You changed dashboard markup, styles, state restoration, or client-side behavior.
- You need a browser smoke that is narrower than full release validation.
- Search result clicks, preview dialogs, tabs, or query-string restoration changed.

## Validation Order
1. Reuse a running API if possible. If the apphost is locked by `dotnet run`, prefer `dotnet test --no-build` for code validation and `dotnet run --no-build` for runtime restart.
2. Open a dashboard URL that targets the changed slice directly, for example a specific `q`, `stage`, `tag`, or `tab`.
3. Check the visible shell text first so stale content is caught before deeper interaction.
4. Exercise the changed behavior in the browser.
5. Finish with a focused dashboard test run if one exists.

## Browser Checks
- Search tab: query, stage filter, tag filter, freshness, and sort.
- Search result click: confirm it opens the preview dialog instead of navigating away.
- Inspect actions: confirm Gold detail, history, and related data still render.
- Graph tab: confirm counts, freshness, and trend still load.
- State restoration: reload and confirm query string or local storage restore only the intended state.

## Decision Points
- If the browser shows removed text that is no longer in `index.html`, suspect stale browser assets before changing code again.
- If result-title clicks navigate to raw JSON instead of opening the dialog, verify the browser loaded the current JavaScript bundle.
- If HTML and JS drift during local testing, add a version query string to the static asset URLs so the browser does not mix old JS with new HTML.
- If browser state from previous sessions interferes with checks, clear local storage and reopen the page with a cache-busting query parameter.

## Focused Checks
- `DashboardApiTests` for shell contract and static markup changes.
- Browser smoke for click interception, dialog rendering, and actual user-visible text.
- `GET /api/dashboard/search` when UI results look suspicious; confirm whether the problem is payload-side or rendering-side.

## Completion Criteria
- The changed UI state is reproducible in the browser.
- Search result title clicks open the intended preview dialog.
- Removed or updated copy is no longer visible after cache-busting.
- The relevant dashboard tests still pass.

## Known Repository Pitfall
- This repo already hit a stale-cache case where new HTML loaded with old JavaScript. If browser behavior contradicts current source, suspect asset cache before further code changes.