# AI Project Handoff

This is the reconciliation point between the repository and any separate ChatGPT conversation used to draft future phase prompts. The repository, ADRs, documentation, and passing tests are the source of truth.

## Verified Project State

- `main` includes Phase 7 through merge commit `8ef25ae`. Phase 8 is implemented on `feature/public-cms-website`.
- The monorepo contains the .NET 10 API in `apps/api`, Next.js 15/npm frontend in `apps/web`, documentation in `docs`, inert Bicep preparation in `infra`, and validation/smoke tooling in `scripts`.
- The complete public website now integrates Site Settings, Content Blocks, announcements, events, coaches, sponsors, FAQs, and contact submissions.
- Public routes are home, about, programs, news list/detail, events list/detail, coaches, sponsors, FAQs, registration information, and contact.
- Public reads use anonymous Server Component requests with a 60-second revalidation window. Admin reads remain `no-store`. Contact POST requests are uncached and use a same-origin Next.js handler.
- The public layout includes CMS branding, responsive navigation, primary CTA, footer contact/social data, skip navigation, focus styles, mobile behavior, safe loading/error/empty/not-found states, and basic metadata.
- API visibility remains authoritative: drafts, expired announcements, draft events, inactive records, and private coach email remain hidden. Contact submissions remain publicly write-only.
- Phase 8 added no backend code, database entity, EF migration, authentication change, or ADR. No backend defect required a fix.
- Frontend validation passes: lint, strict typecheck, 37 tests, and the standalone production build. Backend Release build passes with zero warnings; 32 unit and 27 integration tests pass; EF reports no pending model changes.
- Public smoke scripts now check home, news, events, registration, and Admin login. Documentation validation and secret safety validation remain part of Phase 6A verification.
- No Azure account, credentials, resources, application deployment, database deployment, DNS change, or Phase 6B action exists.

## Deferred Work

- Media/logo/image upload, managed image hosting, galleries, rich text, contact replies/notifications, and advanced page building remain deferred.
- Parent/Athlete portals, online athlete registration, payments, waivers, volunteer workflows, attendance, meet entry, messaging, documents, and iOS remain deferred.
- Final visual/UX redesign should follow board review of the usable Admin and public content loop.
- Recommended next phase: structured board feedback and scoped UX refinement, without combining it with portals, payments, media upload, or Azure provisioning.

## Paste Into the Prompt-Writing Conversation

```text
Repository reconciliation update for El1te Spr1nt Athlet1cs:

Treat the repository, docs, ADRs, and passing tests as source of truth. Main contains Phase 7. Phase 8 is implemented on feature/public-cms-website and completes the public website CMS integration.

The Next.js public website now uses the existing anonymous ASP.NET Core contracts for CMS-driven Site Settings, Content Blocks, announcements, events, coaches, sponsors, FAQs, and contact submissions. Routes include home, about, programs, news list/detail, events list/detail, coaches, sponsors, FAQs, informational registration, and contact. Server Components use 60-second revalidation; the interactive contact form posts through an uncached same-origin route. Draft/expired/inactive visibility, coach email privacy, and write-only public contact behavior remain API-authoritative.

Phase 8 introduced no backend changes, migration, auth changes, or ADR. Frontend lint/typecheck/build pass with 37 tests. Backend Release build passes with 32 unit and 27 integration tests, and EF model drift is clean. Public smoke coverage includes home, news, events, and registration. No Azure resources exist and Phase 6B has not run.

Media uploads, galleries, rich text, portals, online athlete registration, payments, waivers, documents, iOS, Azure deployment, and final board-driven visual polish remain deferred. Do not invent these workflows or repeat the completed CMS integrations in the next phase.
```

## Reconciliation Procedure

1. Compare a proposed prompt with `git log`, `git status`, `docs/README.md`, this file, and current ADRs.
2. Verify every named route, DTO, script, and workflow exists before calling it complete.
3. Distinguish implemented behavior from deferred work.
4. Run documented validation after implementation and update this file with exact results.

Never paste secrets, JWTs, connection strings, local absolute paths, or private account identifiers into an AI conversation.
