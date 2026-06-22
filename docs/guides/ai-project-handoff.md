# AI Project Handoff

This document is the reconciliation point between the repository and any separate ChatGPT conversation used to draft future phase prompts. The repository and its tests are the source of truth. Update this file at the end of each phase before asking another conversation to design the next one.

## Verified Project State

- Monorepo: ASP.NET Core/.NET 10 API in `apps/api`, Next.js 15/npm web app in `apps/web`, learning reference in `docs`.
- Completed through Phase 5.5 on `main`: .NET 10 upgrade, CMS foundation, public CMS API, admin CMS API, secure admin frontend with Announcement management, and architecture/learning documentation.
- Phase 6A working branch: `feature/cicd-deployment-readiness`.
- Phase 6A scope: CI and future Azure deployment preparation only. No Azure account, subscription, credentials, resource provisioning, application deployment, database deployment, DNS change, or Phase 6B action has occurred.
- CI now validates backend, frontend, documentation, and secret safety for pull requests/main and creates short-lived API, standalone web, and EF migration artifacts.
- API has safe liveness `/health` and database readiness `/health/ready` endpoints plus fail-fast Production configuration checks.
- `infra` defines an inert, parameterized Bicep target for two Linux Web Apps, one App Service plan, Azure SQL, and API managed identity.
- `.github/workflows/deploy-azure.yml` is manual-only, Phase 6B-gated, and designed for future GitHub OIDC.
- Smoke tests and artifact scripts live under `scripts`.
- Local verification completed for Phase 6A: NuGet restore and npm clean install, Release API build with zero warnings, 59 backend tests, frontend lint/typecheck, 10 frontend tests, production Next.js build, EF model-drift check, API publish, standalone web artifact, migration SQL fallback artifact, documentation validation, workflow YAML parsing, and end-to-end local API/web smoke tests.
- A Linux EF migration bundle is built by CI. The equivalent local Windows bundle could not be generated inside the managed sandbox, so the checked script provides an idempotent migration SQL fallback. Bicep compilation also remains unverified locally because Azure CLI/Bicep is not installed; Phase 6B must compile and validate it before provisioning.
- Remaining product features are still deferred: other CMS admin screens, parent/athlete portals, registrations, payments, documents, media, iOS, and production deployment.

## Paste This Into the Prompt-Writing Conversation

```text
Repository reconciliation update for El1te Spr1nt Athlet1cs:

Treat the repository, its docs, and passing tests as the source of truth. Do not rely on assumptions from earlier chat messages when they conflict with this handoff.

The project is a monorepo with a .NET 10 ASP.NET Core API, EF Core/SQL Server, and a Next.js 15 npm frontend. Main is complete through Phase 5.5: CMS domain, public/admin APIs, JWT authentication and CmsAdmin authorization, secure HttpOnly admin web session, Announcement management, tests, and learning documentation.

Phase 6A is Cloud-Ready CI/CD and Azure Deployment Preparation. It adds CI for PR/main, documentation and secret validation, Release/deployment artifacts, EF migration bundles, safe /health and database-aware /health/ready endpoints, Production configuration validation, standalone Next.js output, smoke scripts, Dependabot, parameterized Azure Bicep, a manual-only OIDC deployment template, branch-protection guidance, and CI/Azure learning docs.

Verified Phase 6A results: the Release API build has zero warnings; 32 unit and 27 integration tests pass; frontend lint, typecheck, 10 tests, and production build pass; EF model drift is clean; local API/web smoke tests pass. Azure CLI/Bicep is not installed locally, so Bicep compilation and all real Azure validation remain explicit Phase 6B prerequisites.

No Azure account/subscription/credentials exist. No Azure resources, application, database, DNS, GitHub deployment secrets, or Phase 6B deployment have been created or run. Bicep and deployment workflows are preparation only. Managed identity SQL authorization and real Azure validation remain Phase 6B.

Before generating another phase prompt:
1. Preserve all completed behavior and security boundaries.
2. Explicitly distinguish implemented behavior from planned work.
3. Do not repeat Phase 6A or assume Azure is live.
4. Require repository inspection before implementation.
5. Keep future scope limited to the phase requested by the user.
6. Use actual paths and commands from docs/README.md and docs/architecture/cicd-overview.md.
7. Flag any proposed requirement that conflicts with an existing ADR or deferred-work boundary.

Ask for a fresh copy of docs/guides/ai-project-handoff.md if this context may be stale.
```

## How to Reconcile Future Disconnects

1. Compare the proposed prompt with `git log`, `git status`, `docs/README.md`, and this file.
2. Verify named routes, scripts, workflows, and projects exist before treating them as completed.
3. Prefer current ADRs over remembered design discussion.
4. Run the documented validation commands after implementation.
5. Update this handoff with the completed phase, validation outcome, deferred work, and any new ADRs.

Never paste secrets, JWTs, connection strings, local absolute paths, or private account identifiers into an AI conversation.
