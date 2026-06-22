# EF Core Deployment Artifacts

## What Was Built

CI and `scripts/artifacts/build-migration-bundle.*` generate an EF Core migration bundle from the actual Infrastructure and API projects after checking for pending model changes.

## Why It Exists

Application startup should not silently mutate production schema. A reviewed, versioned artifact separates database change from application process startup and can be promoted with the matching release.

## Important Files

- `apps/api/src/El1teSpr1ntTrack.Infrastructure/Data/Migrations`
- `scripts/artifacts/build-migration-bundle.sh`
- `scripts/artifacts/build-migration-bundle.ps1`
- `.github/workflows/ci.yml`

## How It Works

`dotnet ef migrations has-pending-model-changes` compares the current model with the committed snapshot. The bundle command compiles all committed migrations into one platform-specific executable. No connection string is embedded; `--connection` is supplied only when an authorized Phase 6B process executes it.

Normal CI generates and uploads the bundle but never runs it. The manual deployment template shows its future position before application deployment.

## How to Test It

Install `dotnet-ef` 10.0.9, build Release, then run the platform script. The script deliberately uses that verified build rather than rebuilding implicitly. Inspect `artifacts/database`; do not commit it. Test execution only against a disposable database with an explicitly supplied connection.

CI generates the Linux bundle with `build-migration-bundle.sh`. If a managed local environment blocks EF's bundle packager from traversing its runtime/NuGet paths, `build-migration-script.ps1` generates an idempotent SQL artifact from the same committed migrations and drift check. This fallback proves migration generation without weakening CI's bundle requirement.

## Common Problems

- Drift check fails: create/review/commit the missing migration.
- Bundle build restores unexpectedly: verify SDK, EF tool, NuGet access, and Release build.
- Wrong platform: generate the bundle on the same OS/runtime as its future runner or specify a runtime deliberately.
- Authentication fails at execution: configure short-lived deployment access; do not place credentials in the bundle.

## Concepts to Study

Schema versioning, migration snapshots, idempotency, deployment sequencing, least privilege, and artifact provenance.

## What Was Intentionally Deferred

No shared database is updated, no rollback automation is claimed, and managed identity bootstrap remains Phase 6B.
