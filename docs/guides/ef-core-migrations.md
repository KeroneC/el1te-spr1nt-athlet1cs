# EF Core Migrations

A migration is a reviewed, versioned description of how to move a database schema between application model versions. The EF model is built from entities plus `El1teDbContext` configuration; the database changes only after a migration is applied.

## Projects and Databases

- Migration project: `apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj`
- Startup project: `apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj`
- Migrations folder: `Infrastructure/Data/Migrations`
- Development runtime database: `El1teSpr1ntTrack_Dev`
- Design-time factory default: `El1teSpr1ntTrack_DesignTime`

## Detect Model Changes

```powershell
dotnet ef migrations has-pending-model-changes `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

Run this after entity/configuration changes. A clean result does not prove an existing database has applied every migration.

## Create and Review

```powershell
dotnet ef migrations add <DescriptiveMigrationName> `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj `
  --output-dir Data/Migrations
```

Review the generated `Up`, `Down`, designer, and model snapshot. Check destructive operations, nullability, defaults, indexes, enum conversions, foreign keys, data transformations, and whether rollback is realistic. Build and test before applying.

## Apply Locally

```powershell
dotnet ef database update `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj `
  --connection "Server=(localdb)\mssqllocaldb;Database=El1teSpr1ntTrack_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Use the explicit local connection because the design-time factory otherwise selects a different database. For non-local environments, use their secure configuration and deployment process; never put credentials in the command committed to Git.

## Migration Discipline

Commit migration files with the model change. Do not normally rewrite or delete a migration that teammates or environments may have applied; create a corrective migration. Never manually edit the model snapshot to silence a pending-change warning.

The CMS sample data is defined in `CmsSeedData` and represented through the CMS migration. Development SuperAdmin seeding is different: `DevelopmentAdminSeeder` runs at API startup only in Development and reads User Secrets.

## Common Errors

- `dotnet-ef` missing/version mismatch: install 10.0.9 and reopen the terminal.
- SDK project not found: confirm .NET 10 and paths from repository root.
- Login/database open failure: start LocalDB and use the explicit connection.
- Wrong database updated: compare runtime connection with the command and check EF migration history.
- Pending changes immediately after migration: rebuild, inspect all configurations, and verify the generated snapshot is included.
- Destructive migration: stop and review; do not apply until data preservation is understood.
