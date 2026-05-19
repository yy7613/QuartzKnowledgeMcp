---
name: quartz-knowledge-auth-container-rollout
description: "Use when: adding, debugging, or validating optional API key auth and container deployment support in this QuartzKnowledge repository; wiring `/api` and `/mcp` protection, focused auth tests, Docker assets, and Kubernetes sample manifests."
argument-hint: "What auth or container change is needed?"
---

# QuartzKnowledge Auth And Container Rollout

## Use When
- You need to protect `/api` and `/mcp` with an optional API key.
- You are debugging auth behavior in tests or runtime.
- You are packaging the API for container or Kubernetes deployment.

## Auth Workflow
1. Bind configuration into a dedicated options type under `Authentication:ApiKey`.
2. Register the auth handler through the named scheme, not only with inline option binding.
3. Gate requests in middleware based on configured protected prefixes.
4. Add focused tests for missing key, correct key, and anonymous health.
5. Document the runtime caveat that dashboard shell is anonymous while `/api/dashboard/*` is protected.

## Critical Decision Points
- When using custom auth options, read them with the scheme name. In this repo, middleware must use `IOptionsMonitor.Get(ApiKeyAuthenticationDefaults.Scheme)`.
- If a test host override is ignored, suspect unnamed versus named options binding first.
- If the dashboard must still work under auth, plan for reverse proxy or ingress header injection because the static shell alone is not enough.

## Container Workflow
1. Add a multi-stage `Dockerfile`.
2. Add `.dockerignore` so build outputs, databases, and test artifacts are not copied.
3. Add a container-specific appsettings file that writes SQLite under `/data`.
4. Add a sample manifest with Secret, PVC, Deployment, and Service.
5. Surface all security-sensitive values through environment variables or Kubernetes secrets.

## Validation Strategy
- Run focused auth tests first.
- If a local API process already holds `QuartzKnowledgeMcp.Api.exe`, use `dotnet test --no-build` or stop the process before rebuilding.
- Confirm `GET /health` stays anonymous when auth is enabled.
- If container changes are configuration-only, validate file presence and documented environment variables even if you cannot deploy from the current environment.

## Completion Criteria
- Missing API key returns `401` for protected routes.
- Correct API key returns success for protected routes.
- `/health` remains anonymous.
- Container assets are present and point SQLite to `/data`.
- Docs mention the dashboard under-auth caveat and the required environment variables.

## Current Repository Pattern
- `Dockerfile`
- `.dockerignore`
- `src/QuartzKnowledgeMcp.Api/appsettings.Container.json`
- `deploy/kubernetes/quartz-knowledge.sample.yaml`