# El1te Spr1nt Athlet1cs Platform

Initial monorepo foundation for a modern, secure nonprofit youth track club platform.

This first scaffold establishes the repository shape, backend architecture, frontend shell, CI, documentation, and security expectations. It does not implement production registration, payments, documents, messaging, or admin workflows yet.

## Tech Stack

- Backend: ASP.NET Core Web API, Clean Architecture, modular monolith, EF Core, SQL Server
- Frontend: Next.js App Router, TypeScript, Tailwind CSS
- Testing: xUnit for backend, Playwright planned for end-to-end tests
- CI/CD: GitHub Actions
- Deployment targets: Vercel for web, Azure App Service or Azure Container Apps for API, Azure SQL, Azure Blob Storage

## Runtime Note

The backend targets `.NET 10` (`net10.0`). Install the .NET 10 SDK and confirm the active version with `dotnet --version`; the repository-level `global.json` keeps local and CI builds on a compatible .NET 10 SDK.

## Monorepo Structure

```text
apps/
  api/
    El1teSpr1ntTrack.sln
    src/
      El1teSpr1ntTrack.Api/
      El1teSpr1ntTrack.Application/
      El1teSpr1ntTrack.Core/
      El1teSpr1ntTrack.Infrastructure/
    tests/
      El1teSpr1ntTrack.UnitTests/
      El1teSpr1ntTrack.IntegrationTests/
  web/
    app/
    components/
    features/
    lib/
    public/
    src/
    tests/
docs/
.github/workflows/
```

## Backend Commands

```powershell
dotnet restore apps/api/El1teSpr1ntTrack.sln --configfile NuGet.Config
dotnet build apps/api/El1teSpr1ntTrack.sln
dotnet test apps/api/El1teSpr1ntTrack.sln
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

The API exposes health endpoints at `/health` and `/api/v1/health`. Swagger is enabled in development.

The backend also includes the Phase 2 CMS domain and database foundation. See [docs/cms-foundation.md](docs/cms-foundation.md) for its entities, sample data, migration, and scope boundaries.

### Authentication

Implemented backend auth endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`

Registration creates active `Parent` users by default. Parent is the safest public self-registration role because coach/admin access should be granted only through a controlled administrative workflow.

## Frontend Commands

```powershell
cd apps/web
npm install
npm run lint
npm run typecheck
npm run build
npm run dev
```

On Windows PowerShell, if `npm` is blocked by script execution policy, use `npm.cmd` for the same commands.

## Local Configuration

- Copy `apps/web/.env.local.example` to `apps/web/.env.local` for local frontend settings.
- Store backend secrets with user secrets or environment variables, not in `appsettings*.json`.
- Keep JWT signing keys, database credentials, payment keys, storage keys, and private document settings out of source control.

Set the local JWT signing key with user secrets:

```powershell
dotnet user-secrets init --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
dotnet user-secrets set "Jwt:Key" "local-development-key-at-least-32-characters-long" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

The API reads `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpiresMinutes` from `appsettings*.json`; do not store `Jwt:Key` there.

## Deployment Overview

- Web: deploy `apps/web` to Vercel and set `NEXT_PUBLIC_API_BASE_URL`.
- API: deploy the ASP.NET Core API to Azure App Service or Azure Container Apps.
- Database: use Azure SQL for the primary relational store.
- Uploads: use private Azure Blob Storage containers for proof-of-age, medical, and other sensitive files.

## Security Note

This project may eventually process sensitive information about minors, parents, coaches, medical notes, emergency contacts, proof-of-age documents, purchases, donations, and testimonials. Treat privacy, authorization, auditability, logging hygiene, and private uploads as first-class requirements from the start. See [SECURITY.md](SECURITY.md) and [docs/security.md](docs/security.md).
