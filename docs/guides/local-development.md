# Local Development

Run commands from the repository root unless stated otherwise.

## Prerequisites

- .NET 10 SDK compatible with `global.json` (`10.0.301`, newer .NET 10 feature bands allowed)
- Visual Studio with .NET 10 and ASP.NET workload, or another editor
- Node.js 22 or newer and npm (`package-lock.json` is committed)
- SQL Server Express LocalDB for the default Windows setup
- EF Core CLI 10.0.9

```powershell
dotnet --version
node --version
dotnet ef --version
```

Install the EF tool once if missing:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.9
```

## API Secrets

The API project already has a `UserSecretsId`. Set a private local JWT signing key of at least 32 characters:

```powershell
$apiProject = "apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj"
dotnet user-secrets set "Jwt:Key" "<private-local-key-at-least-32-characters>" --project $apiProject
```

For admin testing, set all four Development-only seed values:

```powershell
dotnet user-secrets set "SeedAdmin:Email" "<local-admin-email>" --project $apiProject
dotnet user-secrets set "SeedAdmin:Password" "<strong-local-password>" --project $apiProject
dotnet user-secrets set "SeedAdmin:FirstName" "Local" --project $apiProject
dotnet user-secrets set "SeedAdmin:LastName" "Admin" --project $apiProject
```

User Secrets live outside Git. Do not put these values in `appsettings*.json`, `.env.local`, docs, screenshots, or commits. Seeding runs at Development startup, skips incomplete configuration, and does not modify an existing email.

## Database

Start LocalDB and update the database used by the Development API:

```powershell
sqllocaldb start MSSQLLocalDB

dotnet ef database update `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj `
  --connection "Server=(localdb)\mssqllocaldb;Database=El1teSpr1ntTrack_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

The explicit connection matters because `El1teDbContextFactory` defaults tooling to `El1teSpr1ntTrack_DesignTime` while the running Development API uses `El1teSpr1ntTrack_Dev`.

## Frontend Configuration

```powershell
Copy-Item apps/web/.env.local.example apps/web/.env.local
cd apps/web
npm.cmd install
cd ../..
```

`API_BASE_URL` is server-only and defaults to `https://localhost:7171`. `NEXT_PUBLIC_API_BASE_URL` is reserved for public browser API use. Trust the ASP.NET development certificate:

```powershell
dotnet dev-certs https --trust
```

If Node does not use the Windows certificate store, uncomment `NODE_OPTIONS=--use-system-ca` in `.env.local`.

## Start Both Applications

Terminal 1, repository root:

```powershell
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --launch-profile https
```

Terminal 2:

```powershell
cd apps/web
npm.cmd run dev
```

Useful URLs:

- API Swagger: `https://localhost:7171/swagger`
- API health: `https://localhost:7171/health`
- API database readiness: `https://localhost:7171/health/ready`
- Public API example: `https://localhost:7171/api/public/announcements`
- Admin login: `http://localhost:3000/admin`

A `404` at the API root is expected.

## Safe Verification

1. Confirm `/health` and `/health/ready` return healthy and Swagger loads.
2. Sign in with the disposable Development SuperAdmin.
3. Create an announcement clearly named as local test data.
4. Verify draft/public visibility and editing using [announcements testing](announcements-testing.md).
5. Delete the test announcement.
6. Log out and stop both terminals with `Ctrl+C`.

Run all checks with the commands in [testing strategy](../architecture/testing-strategy.md).
