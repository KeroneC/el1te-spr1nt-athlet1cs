# Database and EF Core

Migration `AddMediaLibraryAndGallery` adds `MediaAssets`, `GalleryAlbums`, and `GalleryAlbumMedia`. The explicit join stores album-specific order and metadata overrides. Album deletion cascades only to join rows; shared media is retained, and referenced media deletion is rejected by the application.

The Infrastructure project uses EF Core 10 with SQL Server. `El1teDbContext` exposes both current CMS/authentication tables and several earlier domain scaffolds. This document focuses on the implemented CMS and user workflows.

## Implemented Data Areas

| Entity | Purpose | Important behavior |
| --- | --- | --- |
| `User` | Login identity and role | Unique normalized email, BCrypt password hash, active flag, `UserRole` |
| `SiteSetting` | Club-wide public settings | Single settings record in current services |
| `ContentBlock` | Keyed editable site copy | Unique immutable key, published flag |
| `Announcement` | Time-sensitive club news | Unique slug, draft/published, featured, publish and expiration dates |
| `Event` | Meets, practices, deadlines, and fundraisers | Unique slug, event type, schedule, published/featured flags |
| `Coach` | Public coaching profile | Active flag and explicit email-public consent |
| `Sponsor` | Partner profile | Unique slug, tier, active flag |
| `Faq` | Categorized public answer | Active flag and display order |
| `ContactSubmission` | Private inbound inquiry | Status and inquiry type; no public read route |
| `AdminInvitation` | Single-use privileged-account invitation | Normalized email, role, expiry, lifecycle timestamps, and SHA-256 token hash only |
| `AdminActivityLog` | Append-only identity-management activity | Actor, action, target, safe summary, correlation ID, and creation time |

CMS entities inherit GUID `Id`, `CreatedAtUtc`, and optional `UpdatedAtUtc` from `CmsEntityBase`. The older general entities inherit similar fields from `EntityBase`. GUIDs allow identifiers to be generated before insertion and avoid exposing sequential record counts, at the cost of larger indexes than integer keys.

## Mapping and Integrity

`El1teDbContext.OnModelCreating` applies every `IEntityTypeConfiguration` in the Infrastructure assembly. Files under `Data/Configurations` define table names, required fields, lengths, defaults, indexes, and enum conversions. CMS enums such as sponsor tier and contact status are stored as strings for readable data and stable meaning when enum ordering changes.

Unique indexes protect user email, invitation token hash, content-block key, and announcement/event/sponsor slugs. Application validation provides friendly errors, while constraints protect against races and invalid direct writes. Invitation acceptance and privileged-user guardrails run in serializable transactions so concurrent requests cannot consume one invitation twice or remove the final active SuperAdmin.

## Visibility and Privacy

Published and active are different ideas. Publication controls content readiness; activation controls whether people or partners remain publicly listed. `PublicCmsVisibility.CurrentAnnouncement` requires `IsPublished`, a reached publish date, and no reached expiration date. Admin queries can include drafts and, when requested, expired records.

`PublicCmsVisibility.PublicCoach` includes a coach email only when `IsEmailPublic` is true. Contact submissions can be created anonymously but read only through protected admin endpoints. These rules are applied in query projection, before data leaves the API.

## Repositories and DTOs

`PublicCmsRepository` and `AdminCmsRepository` use `AsNoTracking` for read paths, compose filters in SQL, order results, paginate, and project to DTOs. Services orchestrate writes and validation. EF entities are not returned directly because API contracts, private data, and persistence shape must be able to evolve independently.

## Migrations and Seed Data

Migrations live in `Infrastructure/Data/Migrations`. The current history includes the authentication, CMS, media/gallery, Azure delivery, grouped navigation, and administrative identity foundations. `AddAdminIdentityManagement` introduces the invitation and append-only activity tables. `El1teDbContextFactory` supplies a design-time connection, while the running Development API uses the configured `DefaultConnection`; use an explicit connection when applying migrations locally to avoid updating the tooling database by mistake.

`AddCmsFoundation` includes generic CMS sample rows. Separately, `DevelopmentAdminSeeder` runs only when the API environment is Development and all `SeedAdmin` User Secrets are configured. It never stores credentials in source control and does not modify an existing account.

See [EF Core migrations](../guides/ef-core-migrations.md) for commands and review practice.
