# Public CMS API

## What Was Built

Anonymous routes under `/api/public` expose site settings, published content blocks, current announcements, published events, active coaches/sponsors/FAQs, and write-only contact submission creation. Announcement/event collections paginate; resource-specific filters are supported.

## Why It Exists

Public clients need useful content without receiving drafts, internal state, or private contact information. Keeping these rules in server-side queries gives web and future mobile clients the same safety boundary.

## Important Files

- `apps/api/src/El1teSpr1ntTrack.Api/Controllers/PublicCmsController.cs`
- `apps/api/src/El1teSpr1ntTrack.Application/Services/PublicCmsService.cs`
- `apps/api/src/El1teSpr1ntTrack.Application/Common/PublicCmsVisibility.cs`
- `apps/api/src/El1teSpr1ntTrack.Infrastructure/Repositories/PublicCmsRepository.cs`
- `apps/api/src/El1teSpr1ntTrack.Core/DTOs/Cms/PublicCmsDtos.cs`

## How It Works

Collections default to page 1 and 10 items, capped at 50. Current announcements must be published, past any publish date, and before expiration. Events must be published; optional queries can restrict type, featured state, or future schedule. Coaches, sponsors, and FAQs must be active. Coach projection omits email unless `IsEmailPublic` is true.

Contact submissions accept a validated write DTO, force status to New, return only an identifier and confirmation, and have no public read endpoint.

## Request or Data Flow

```text
GET /api/public/announcements?featured=true
  -> PublicCmsController.GetAnnouncements
  -> PublicCmsService.GetAnnouncementsAsync
  -> PublicCmsRepository.GetAnnouncementsAsync
  -> PublicCmsVisibility.CurrentAnnouncement(now)
  -> EF Core SQL filter/projection
  -> PagedResult<PublicAnnouncementDto>
```

## How to Test It

Start the API and use Swagger or request `GET https://localhost:7171/api/public/announcements`. Compare the result with protected admin data. Follow [announcements testing](../guides/announcements-testing.md) for draft, publish, and expiration checks.

## Common Problems

- Admin can see an item but public cannot: check published, publish date, expiration, and active state.
- Detail returns `404`: hidden records deliberately look unavailable publicly.
- Coach email is null: public display was not explicitly enabled.
- Invalid contact request: inspect field errors for required name, valid email, and message.

## Concepts to Study

Data minimization, query predicates, DTO projection, pagination, idempotent reads, anonymous endpoints, and write-only intake patterns.

## What Was Intentionally Deferred

No public Next.js pages currently consume every CMS endpoint. Contact administration, spam controls, and rate limiting remain future work.
