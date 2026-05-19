---
name: quartz-knowledge-curated-doc-ingest
description: "Use when: importing Microsoft Learn pages, external docs, or long-form references into this QuartzKnowledge repository; curating source pages into Bronze/Silver/Gold entries; improving Japanese search hits, tags, related entries, and dashboard visibility."
argument-hint: "Which pages or docs should be ingested?"
---

# QuartzKnowledge Curated Doc Ingest

## Use When
- You want to ingest external documentation into this repository instead of seeding synthetic content.
- You need the ingested entries to be searchable from both HTTP APIs and the dashboard.
- You need Japanese queries, related entries, and tag browsing to work after ingest.

## Workflow
1. Collect the target pages and extract only the sections that matter: overview, setup flow, providers, tools, auth model, and notable code examples.
2. Rewrite each page into structured Markdown that the Bronze to Silver organizer can parse reliably.
3. Publish through the medallion flow: Bronze create, Silver organize, Gold publish.
4. Curate the Gold entry with a repository-specific overview, setup guide, references, supported clients, and final tags.
5. Validate the result in Gold catalog, dashboard search, and related-entry views.

## Recommended Source Shape
- Start with a single `# Title` heading.
- Add an `Authentication:` line when the source implies OAuth, API key, or anonymous access.
- Prefer stable sections such as `Summary`, `CSharp Notes`, `Packages`, `Tools`, `Related Pages`, or `Operational Guidance`.
- Keep headings explicit; avoid dumping raw site chrome or navigation text into `rawContent`.

## Decision Points
- Use `sourceType = docs-url` for curated documentation imports. The Bronze API rejects unsupported source types.
- If a Japanese term must hit in search, place that exact term in the Gold `overview`, not only in tags.
- Keep tags within the repository limit of 1 to 5 values.
- If related entries are weak, share one or two intentional anchors across pages: tags, clients, auth model, or references.
- If a page has too much noise, summarize it before Bronze ingest rather than relying on the organizer to clean it up.

## Gold Curation Checklist
- `overview` is concise, task-oriented, and includes important Japanese keywords.
- `setupGuide` is operational, not marketing text.
- `references` point to the canonical docs and any key sample repos.
- `supportedClients` only contains concrete clients worth filtering on.
- Tags are small, intentional, and useful for browse mode.

## Validation Steps
1. Check `GET /api/gold/catalog` for the new titles.
2. Check `GET /api/dashboard/search` with both English and Japanese queries.
3. Check `GET /api/gold/catalog/{entryId}/related` to see whether neighboring entries were created as expected.
4. If dashboard visibility matters, open `/dashboard/index.html?q=<term>&stage=gold` and verify the new entries appear.

## Completion Criteria
- Each curated page exists as a Gold entry.
- The expected English and Japanese queries return the entry.
- Tags are valid and within limits.
- Related entries are populated, or the absence is understood and acceptable.
- Dashboard search shows the new entries without extra manual cleanup.

## Notes
- `work/ingest-agent-framework-learn.ps1` is the current repository pattern for repeated curated doc ingest.