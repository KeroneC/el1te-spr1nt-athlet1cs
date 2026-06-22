# Admin CMS API

## What Was Built

Protected `/api/admin` endpoints manage site settings, content blocks, announcements, events, coaches, sponsors, FAQs, and contact submissions. They support resource filters, pagination, validation, conflict handling, and appropriate hard-delete or deactivate behavior.

## Why It Exists

Administrators need a complete view, including drafts and inactive records, but that power must be isolated from anonymous and ordinary authenticated users.

## Important Files

- `apps/api/src/El1teSpr1ntTrack.Api/Controllers/Admin`
- `apps/api/src/El1teSpr1ntTrack.Api/Authorization/CmsAdminAuthorization.cs`
- `apps/api/src/El1teSpr1ntTrack.Application/Services/AdminCmsService*.cs`
- `apps/api/src/El1teSpr1ntTrack.Infrastructure/Repositories/AdminCmsRepository.cs`
- `apps/api/src/El1teSpr1ntTrack.Infrastructure/Data/DevelopmentAdminSeeder.cs`

## How It Works

Every admin controller uses `[Authorize(Policy = "CmsAdmin")]`. The policy checks authentication, Admin/SuperAdmin role, and current database state. List routes default to 20 rows and cap page size at 100. Services validate writes, generate unique slugs on creation, preserve those slugs on update, and map expected errors to `400`, `404`, or `409`.

Content blocks, announcements, events, and contact submissions are hard-deleted. Coaches, sponsors, and FAQs are deactivated so history remains administratively visible while public queries hide them.

## Request or Data Flow

```text
PUT /api/admin/announcements/{id}
  -> JWT authentication
  -> CmsAdmin policy and active database user check
  -> AdminAnnouncementsController.Update
  -> AdminCmsService.UpdateAnnouncementAsync
  -> validation and entity update
  -> AdminCmsRepository.SaveChangesAsync
  -> AdminAnnouncementDto
```

## How to Test It

Configure a disposable Development SuperAdmin through User Secrets, log in through Swagger, select Authorize, and exercise protected routes. Verify anonymous `401`, Parent `403`, privileged `200`, and inactive rejection using [authentication testing](../guides/authentication-testing.md).

## Common Problems

- `401`: token missing, malformed, expired, or signed for different issuer/audience/key.
- `403`: current account is not an active Admin/SuperAdmin.
- Seed admin absent: all four `SeedAdmin` values are required before API startup.
- `409`: key or slug conflicts with an existing unique record.

## Concepts to Study

Policy-based authorization, claims, resource lifecycle, optimistic concurrency limits, Problem Details, soft deactivation versus deletion, and privileged data boundaries.

## What Was Intentionally Deferred

The API has no audit trail, role-management UI, token revocation, or granular CMS permissions. Only Announcements currently has an admin frontend.
