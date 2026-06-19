# Local Development

## Prerequisites

- .NET 10 SDK (the repository pins SDK `10.0.301` and permits newer .NET 10 feature bands)
- Node.js 22 or newer recommended for frontend development
- SQL Server LocalDB, SQL Server Developer Edition, or a containerized SQL Server instance

## Backend

Confirm the SDK before restoring packages:

```powershell
dotnet --version
dotnet --list-sdks
```

```powershell
dotnet restore apps/api/El1teSpr1ntTrack.sln --configfile NuGet.Config
dotnet build apps/api/El1teSpr1ntTrack.sln
dotnet test apps/api/El1teSpr1ntTrack.sln
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

Swagger is available in development. Health checks are available at `/health` and `/api/v1/health`.

Auth endpoints are available at `POST /api/auth/register` and `POST /api/auth/login`.

## User Secrets

Initialize user secrets for local backend development:

```powershell
dotnet user-secrets init --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
dotnet user-secrets set "Jwt:Key" "local-development-key-at-least-32-characters-long" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=El1teSpr1ntTrack_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

Equivalent JSON shape:

```json
{
  "Jwt": {
    "Key": "local-development-key-at-least-32-characters-long"
  }
}
```

`Jwt:Key` must be at least 32 characters long. Keep it out of `appsettings*.json`.

## Database

Database migrations currently include:

- `AddAuthenticationFoundation`
- `AddCmsFoundation`

`AddCmsFoundation` creates the CMS tables and indexes and inserts the generic development content described in [cms-foundation.md](cms-foundation.md).

```powershell
dotnet ef database update --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

If `dotnet ef` is not installed, install EF tooling locally or globally:

```powershell
dotnet tool install dotnet-ef --tool-path .dotnet-tools --version 10.0.9
.\.dotnet-tools\dotnet-ef.exe database update --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

## Frontend

```powershell
cd apps/web
npm install
npm run dev
```

Set `NEXT_PUBLIC_API_BASE_URL` in `apps/web/.env.local`. Start with `apps/web/.env.local.example`.

If PowerShell blocks `npm`, use `npm.cmd` instead.

## Playwright

Playwright is planned for end-to-end coverage after the first user-facing workflows exist. It is intentionally not installed in this initial scaffold to keep setup light.
