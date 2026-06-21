# El1te Spr1nt Athlet1cs Platform

Initial monorepo foundation for a modern, secure nonprofit youth track club platform.

This first scaffold establishes the repository shape, backend architecture, frontend shell, CI, documentation, and security expectations. It does not implement production registration, payments, documents, messaging, or admin workflows yet.

## Tech Stack

- Backend: ASP.NET Core Web API, Clean Architecture, modular monolith, EF Core, SQL Server
- Frontend: Next.js App Router, TypeScript, Tailwind CSS
- Testing: xUnit for backend, Playwright planned for end-to-end tests
- CI/CD: GitHub Actions
- Deployment targets: Vercel for web, Azure App Service or Azure Container Apps for API, Azure SQL, Azure Blob Storage

## Prerequisites

- .NET 10 SDK (`global.json` pins `10.0.301` and permits newer .NET 10 feature bands)
- Visual Studio with .NET 10 support and the ASP.NET workload, or another editor plus the .NET CLI
- SQL Server Express LocalDB for the default Windows development setup
- Node.js 22 or newer for frontend development

Confirm the .NET SDK before continuing:

```powershell
dotnet --version
```

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

## Backend Quick Start

Run these commands from the repository root.

1. Restore and build the solution:

```powershell
dotnet restore apps/api/El1teSpr1ntTrack.sln --configfile NuGet.Config
dotnet build apps/api/El1teSpr1ntTrack.sln
```

2. Set a private local JWT signing key of at least 32 characters:

```powershell
dotnet user-secrets set "Jwt:Key" "replace-with-your-private-local-key-of-32-or-more-characters" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

The API project already has a `UserSecretsId`, so `dotnet user-secrets init` is not required. The secret is stored outside the repository and is not tracked by Git.

In Visual Studio, right-click `El1teSpr1ntTrack.Api`, select **Manage User Secrets**, and use this equivalent JSON:

```json
{
  "Jwt": {
    "Key": "replace-with-your-private-local-key-of-32-or-more-characters"
  }
}
```

3. Start LocalDB and install the matching EF CLI tool if needed:

```powershell
sqllocaldb start MSSQLLocalDB
dotnet tool install --global dotnet-ef --version 10.0.9
```

If `dotnet ef --version` already reports `10.0.9`, skip the installation command.

4. Apply migrations to the database used by the Development API:

```powershell
dotnet ef database update `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj `
  --connection "Server=(localdb)\mssqllocaldb;Database=El1teSpr1ntTrack_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

The explicit connection is important: EF's design-time context uses a separate `El1teSpr1ntTrack_DesignTime` database, while the running Development API uses `El1teSpr1ntTrack_Dev`.

5. Run and verify the API:

```powershell
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --launch-profile https
```

Open [https://localhost:7171/swagger](https://localhost:7171/swagger). A `404` at `https://localhost:7171/` is expected because the API does not define a root endpoint. Health checks are available at `/health` and `/api/v1/health`.

Run backend tests with:

```powershell
dotnet test apps/api/El1teSpr1ntTrack.sln
```

For alternative SQL Server configurations and troubleshooting, see [docs/local-development.md](docs/local-development.md).

The backend also includes the Phase 2 CMS domain and database foundation. See [docs/cms-foundation.md](docs/cms-foundation.md) for its entities, sample data, migration, and scope boundaries.

Published CMS content and contact submissions are available through the Phase 3 public API. See [docs/public-cms-api.md](docs/public-cms-api.md) for endpoints, filters, visibility rules, and examples.

Protected CMS management is available to active `Admin` and `SuperAdmin` users through the Phase 4 admin API. See [docs/admin-cms-api.md](docs/admin-cms-api.md) for endpoints, authorization, and local SuperAdmin setup.

The Phase 5 web admin foundation provides secure server-managed login and complete Announcements management. See [docs/admin-frontend.md](docs/admin-frontend.md) for routes, session architecture, setup, and limitations.

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

The API reads `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpiresMinutes` from `appsettings*.json`; do not store `Jwt:Key` there.

## Deployment Overview

- Web: deploy `apps/web` to Vercel and set `NEXT_PUBLIC_API_BASE_URL`.
- API: deploy the ASP.NET Core API to Azure App Service or Azure Container Apps.
- Database: use Azure SQL for the primary relational store.
- Uploads: use private Azure Blob Storage containers for proof-of-age, medical, and other sensitive files.

## Security Note

This project may eventually process sensitive information about minors, parents, coaches, medical notes, emergency contacts, proof-of-age documents, purchases, donations, and testimonials. Treat privacy, authorization, auditability, logging hygiene, and private uploads as first-class requirements from the start. See [SECURITY.md](SECURITY.md) and [docs/security.md](docs/security.md).
