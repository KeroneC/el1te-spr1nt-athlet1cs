# CMS Foundation

Phase 2 adds the backend domain and database foundation for editable public website content.

## Included

- CMS entities for site settings, content blocks, announcements, events, coaches, sponsors, FAQs, and private contact submissions
- GUID identifiers and `CreatedAtUtc` / `UpdatedAtUtc` audit fields
- EF Core table configuration, required fields, lengths, defaults, string-backed enums, indexes, and unique constraints
- Generic CMS repository, validation service, and reusable unique slug generator
- Write DTO foundations for future public and admin APIs
- Migration `AddCmsFoundation`

Unique database constraints protect content block keys and announcement, event, and sponsor slugs. Event validation requires the end time to follow the start time, and contact submissions require a valid email address.

## Sample Data

The CMS migration includes generic sample content for development:

- 1 site settings record
- 7 required content blocks
- 3 announcements
- 4 events covering practice, meet, fundraiser, and registration deadline types
- 3 placeholder coaches
- 4 placeholder sponsors
- 6 FAQs

The sample records contain no private personal information. Review seeded copy and placeholder links before a production launch.

## Apply The Migration

```powershell
dotnet ef database update --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

Configure `Jwt:Key` through user secrets or an environment variable before running the API. Never add a signing key to `appsettings*.json`.

## Not Included

Phase 2 does not add admin CMS endpoints, CMS screens, a full admin dashboard, parent or athlete portal features, media upload, or iOS code. Public read-only CMS endpoints and contact submission creation were added in Phase 3 and are documented in [public-cms-api.md](public-cms-api.md).
