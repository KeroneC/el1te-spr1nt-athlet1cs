# CMS Domain Foundation

## What Was Built

The CMS foundation added entities, EF configurations, DTO foundations, validation, slug generation, repositories, migrations, and generic development content for site settings, content blocks, announcements, events, coaches, sponsors, FAQs, and contact submissions.

## Why It Exists

Club content needs lifecycle and privacy rules rather than hard-coded page copy. A shared model lets public and administrative use cases read the same source of truth through different contracts and filters.

## Important Files

- `apps/api/src/El1teSpr1ntTrack.Core/Entities`
- `apps/api/src/El1teSpr1ntTrack.Infrastructure/Data/Configurations`
- `apps/api/src/El1teSpr1ntTrack.Infrastructure/Data/El1teDbContext.cs`
- `apps/api/src/El1teSpr1ntTrack.Application/Services/CmsValidationService.cs`
- `apps/api/src/El1teSpr1ntTrack.Application/Services/SlugGenerator.cs`
- `apps/api/src/El1teSpr1ntTrack.Infrastructure/Data/Migrations/20260619050248_AddCmsFoundation.cs`

## How It Works

CMS entities use GUID identifiers and UTC audit fields. EF configurations enforce lengths, defaults, indexes, string-backed enums, and unique slugs/keys. `CmsValidationService` provides friendly use-case validation. `SlugGenerator` normalizes text and appends numeric suffixes until the repository reports a free slug.

Published flags govern content readiness; active flags govern listed people/partners. Announcement dates add scheduled and expired states. The database remains the final integrity boundary.

## Request or Data Flow

```text
Write DTO -> application service -> entity mapping -> validation
  -> repository -> El1teDbContext -> migration-defined SQL schema
```

## How to Test It

Run `CmsValidationServiceTests`, `SlugGeneratorTests`, and `CmsModelTests`, then apply migrations to a disposable local database and inspect generated sample content through Swagger.

## Common Problems

- Duplicate key/slug: use the application service rather than manually assigning a conflicting value.
- Pending model changes: create and review a migration.
- Dates behave unexpectedly: send explicit UTC offsets and compare against UTC.
- Seed content missing: confirm the CMS migration was applied to the same database the API uses.

## Concepts to Study

Entities versus DTOs, value constraints, unique indexes, migrations, query projection, audit fields, UTC, slug normalization, and defense in depth.

## What Was Intentionally Deferred

This foundation did not add public/admin HTTP routes or web screens. Those arrived in later phases. Media upload and rich-text editing remain deferred.
