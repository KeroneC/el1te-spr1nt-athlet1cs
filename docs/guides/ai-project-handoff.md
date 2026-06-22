# AI Project Handoff

This is the reconciliation point between the repository and any separate ChatGPT conversation used to draft future phase prompts. The repository, ADRs, and passing tests are the source of truth.

## Verified Project State

- Monorepo: ASP.NET Core/.NET 10 API in `apps/api`, Next.js 15/npm web app in `apps/web`, documentation in `docs`, inert Azure preparation in `infra`, and validation/artifact scripts in `scripts`.
- `main` is complete through Phase 6A. Phase 7 is implemented on `feature/core-cms-admin-frontend` and is not yet merged at the time of this handoff.
- Authentication remains JWT at the API and an HttpOnly same-origin web session. Active Admin/SuperAdmin authorization is unchanged; JWTs are never exposed to Client Components or browser storage.
- Core CMS administration is complete for Announcements, Events, Coaches, Sponsors, FAQs, Content Blocks, Site Settings, and Contact Submissions.
- Lists use real backend search/filter/pagination contracts. Forms use actual DTOs, safe field errors, accessible controls, and explicit lifecycle language.
- Coach email privacy, public active/published visibility, Content Block key conflicts, Site Settings singleton behavior, Contact Submission status changes, and permanent-deletion confirmations were manually verified locally.
- Phase 7 added no backend code, EF migration, or ADR. No API defect required a fix.
- Validation: frontend lint and strict typecheck pass; 28 frontend tests pass; production Next.js build passes. The unchanged backend suite has 32 unit plus 27 integration tests, and EF model drift remains clean.
- Phase 6A CI/CD, smoke scripts, Bicep preparation, and manual-only deployment template remain intact.
- No Azure account, credentials, resources, application deployment, database deployment, DNS change, or Phase 6B action exists.

## Deferred Work

- Public website pages still need to consume the existing public CMS API for home, about, programs, events, coaches, sponsors, FAQs, and contact experiences.
- Media/logo/image upload, rich text, contact replies/notifications, gallery, Parent/Athlete portals, registration, payments, waivers, volunteers, documents, iOS, and live Azure deployment remain deferred.
- Board UX/UI review should happen against the usable Phase 7 workflows before a major visual redesign.
- Recommended next phase: public website CMS integration and responsive public content pages, without expanding into portals, payments, media upload, or Azure provisioning.

## Paste Into the Prompt-Writing Conversation

```text
Repository reconciliation update for El1te Spr1nt Athlet1cs:

Treat the repository, docs, ADRs, and passing tests as source of truth. Main is complete through Phase 6A. Phase 7 is implemented on feature/core-cms-admin-frontend and completes the core CMS admin frontend.

The protected Next.js admin now manages Announcements, Events, Coaches, Sponsors, FAQs, Content Blocks, singleton Site Settings, and private Contact Submissions using the existing ASP.NET Core Admin CMS API. It preserves the HttpOnly session/JWT server boundary, active Admin/SuperAdmin policy, no-store protected reads, URL filters/pagination, backend-authoritative validation, safe 400/401/403/404/409 handling, UTC event dates, coach email privacy, correct deactivate-vs-delete semantics, content-key conflict handling, and contact status transitions.

Phase 7 introduced no backend changes, migrations, API fixes, or ADRs. Frontend lint/typecheck/build pass and 28 frontend tests pass. The unchanged backend suite is 59 tests. Manual local verification covered authentication, non-admin denial, all module lifecycle types, public visibility/privacy, singleton persistence, duplicate keys, contact statuses, deletion, logout, and disposable-data cleanup.

No Azure resource or credential exists and Phase 6B has not run. Public website CMS pages, media uploads, portals, registration, payments, documents, iOS, and production deployment remain unimplemented.

For the next prompt, inspect current repository paths and preserve all security and lifecycle boundaries. The recommended next phase is public website CMS integration. Do not repeat admin CRUD, invent backend contracts, begin Phase 6B, or combine public pages with portals/payments/media upload unless the user explicitly changes scope.
```

## Reconciliation Procedure

1. Compare a proposed prompt with `git log`, `git status`, `docs/README.md`, this file, and current ADRs.
2. Verify every named route, DTO, script, and workflow exists before calling it complete.
3. Distinguish implemented behavior from deferred work.
4. Run documented validation after implementation and update this file with exact results.

Never paste secrets, JWTs, connection strings, local absolute paths, or private account identifiers into an AI conversation.
