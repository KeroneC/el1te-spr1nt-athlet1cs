# Local Development

Run commands from the repository root unless stated otherwise.

## Prerequisites

- .NET 10 SDK compatible with `global.json` (`10.0.301`, newer .NET 10 feature bands allowed)
- Visual Studio with .NET 10 and ASP.NET workload, or another editor
- Node.js 22 or newer and npm (`package-lock.json` is committed)
- SQL Server Express LocalDB for the default Windows setup, or Colima and the Docker CLI for Apple Silicon macOS
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

## Apple Silicon macOS Setup

The Windows instructions below use LocalDB. Apple Silicon Macs instead run the
SQL Server Developer container in a dedicated Colima profile named `el1te`.
This profile is unrelated to the Java PostgreSQL environment in the default
Colima profile. Do not delete, reset, or reuse the default profile, its
containers, or its volumes.

SQL Server's Linux container is an x86-64 image. Colima runs it through Rosetta
translation on Apple Silicon, which is suitable only for best-effort local
development and is not an officially supported SQL Server configuration. If it
becomes unreliable, use a dedicated non-production Azure SQL database. Never
fall back to the deployed demo database.

### One-time container setup

Confirm the required tools and that port `1433` is free:

```zsh
uname -m
dotnet --version
node --version
npm --version
dotnet ef --version
colima version
docker version
lsof -nP -iTCP:1433 -sTCP:LISTEN
```

Create and start the isolated virtual machine:

```zsh
colima start el1te \
  --cpus 4 \
  --memory 4 \
  --disk 40 \
  --runtime docker \
  --vm-type vz \
  --vz-rosetta \
  --arch aarch64
```

Some Macs set `DOCKER_HOST` for the default Colima profile. Every El1te Docker
command below removes that override and names the isolated context explicitly:

```zsh
env -u DOCKER_HOST docker --context colima-el1te info
```

Create a private password without printing it, then create the named volume and
container. The pinned image should be deliberately updated when the team adopts
a newer SQL Server 2025 cumulative update.

```zsh
read -s "EL1TE_SQL_SA_PASSWORD?Local SQL sa password: "
echo

env -u DOCKER_HOST docker --context colima-el1te volume create el1te-sql-data

env -u DOCKER_HOST docker --context colima-el1te run -d \
  --name el1te-sql \
  --hostname el1te-sql \
  --platform linux/amd64 \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e MSSQL_PID=Developer \
  -e MSSQL_SA_PASSWORD="$EL1TE_SQL_SA_PASSWORD" \
  -p 1433:1433 \
  -v el1te-sql-data:/var/opt/mssql \
  mcr.microsoft.com/mssql/server:2025-CU7-ubuntu-22.04
```

Wait until SQL Server is ready:

```zsh
env -u DOCKER_HOST docker --context colima-el1te logs -f el1te-sql
```

Stop following the logs with `Ctrl+C` after
`SQL Server is now ready for client connections` appears.

### Private settings and database creation

Run these commands from the repository root. Substitute a disposable local
email address and enter a separate strong local Admin password when prompted.
The values are stored outside Git by .NET User Secrets.

```zsh
api_project="apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=El1teSpr1ntTrack_Dev;User Id=sa;Password=${EL1TE_SQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True" \
  --project "$api_project"

dotnet user-secrets set "Jwt:Key" "$(openssl rand -hex 32)" \
  --project "$api_project"
dotnet user-secrets set "SeedAdmin:Email" "<disposable-local-admin-email>" \
  --project "$api_project"
read -s "EL1TE_ADMIN_PASSWORD?Local Admin password: "
echo
dotnet user-secrets set "SeedAdmin:Password" "$EL1TE_ADMIN_PASSWORD" \
  --project "$api_project"
dotnet user-secrets set "SeedAdmin:FirstName" "Local" \
  --project "$api_project"
dotnet user-secrets set "SeedAdmin:LastName" "Admin" \
  --project "$api_project"

dotnet ef database update \
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj \
  --startup-project "$api_project" \
  --connection "Server=localhost,1433;Database=El1teSpr1ntTrack_Dev;User Id=sa;Password=${EL1TE_SQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

unset EL1TE_SQL_SA_PASSWORD EL1TE_ADMIN_PASSWORD
```

Do not run the migration without the explicit local connection. The database
name must be `El1teSpr1ntTrack_Dev`; deployed demo credentials must never be
used.

Configure the frontend:

```zsh
cp apps/web/.env.local.example apps/web/.env.local
```

Confirm the generated frontend environment file contains only these local
application addresses. The committed template is
`apps/web/.env.local.example`; the generated `.env.local` file remains ignored:

```dotenv
API_BASE_URL=http://localhost:5126
SITE_URL=http://localhost:3000
NEXT_PUBLIC_API_BASE_URL=http://localhost:5126
```

Then install the committed frontend dependencies:

```zsh
cd apps/web
npm install
cd ../..
```

### Daily startup

Terminal 1, repository root:

```zsh
colima start el1te
env -u DOCKER_HOST docker --context colima-el1te start el1te-sql

Store__PublicPreviewEnabled=true Store__Enabled=false \
  dotnet run \
  --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj \
  --launch-profile http
```

Terminal 2:

```zsh
cd apps/web
npm run dev
```

This makes product browsing and configuration visible locally while keeping
payments, orders, webhooks, and commerce workers disabled.

Useful Mac URLs:

- Website: `http://localhost:3000`
- Admin login: `http://localhost:3000/admin/login`
- Shop preview: `http://localhost:3000/shop`
- Swagger: `http://localhost:5126/swagger`
- API health: `http://localhost:5126/health`
- Database readiness: `http://localhost:5126/health/ready`

### Stop, restart, and troubleshoot

Stop Next.js and the API with `Ctrl+C` in their terminals. To stop only the
El1te database environment:

```zsh
env -u DOCKER_HOST docker --context colima-el1te stop el1te-sql
colima stop el1te
```

Restart later with the commands in **Daily startup**. The `el1te-sql-data`
volume preserves the local database when the container and Colima profile stop.

Check status and relevant SQL logs without touching the Java environment:

```zsh
colima status el1te
env -u DOCKER_HOST docker --context colima-el1te ps -a
env -u DOCKER_HOST docker --context colima-el1te logs --tail 100 el1te-sql
```

If readiness fails, confirm the container is running, port `1433` is available,
the password still satisfies SQL Server policy, and the VM has enough memory.
Do not repair this setup by deleting the default Colima profile.

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

## Windows Database

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

`API_BASE_URL` is server-only and defaults to `https://localhost:7171`. Both public Server Components and protected Admin requests use this server boundary; browser JavaScript does not need a public API URL. Trust the ASP.NET development certificate:

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
- Public website: `http://localhost:3000`
- Admin login: `http://localhost:3000/admin`

A `404` at the API root is expected.

Public CMS reads revalidate every 60 seconds. After publishing an Admin change, allow up to about one minute for an already cached public page to refresh. Contact form submissions are never cached.

## Local Media Storage

Phase 9 stores development/demo image bytes beneath the API content root in `uploads`; Git ignores this directory. Apply the latest migration before testing Media or Gallery. The defaults allow JPEG, PNG, and WebP files up to 10 MB and expose active bytes at `/media/{id}`.

Override defaults with `MediaStorage__LocalRoot`, `MediaStorage__PublicBaseUrl`, and `MediaStorage__MaxFileSizeBytes`. Use `http://localhost:5126` when launching the HTTP profile or `https://localhost:7171` for the HTTPS profile. Restart the API after changing configuration. Local storage is not durable production storage; a future Azure Blob provider will replace it through `IMediaStorage`.

## Safe Verification

1. Confirm `/health` and `/health/ready` return healthy and Swagger loads.
2. Sign in with the disposable Development SuperAdmin.
3. Create an announcement clearly named as local test data.
4. Verify draft/public visibility and editing using [announcements testing](announcements-testing.md).
5. Delete the test announcement.
6. Log out and stop both terminals with `Ctrl+C`.

Run all checks with the commands in [testing strategy](../architecture/testing-strategy.md).

## Run the End-to-End Workflow

Stop any applications using ports `3100` or `5127`, then run from the web project:

```powershell
cd apps/web
npx.cmd playwright install chromium
npm.cmd run test:e2e
```

Playwright starts and stops both applications. It migrates only `El1teSpr1ntTrack_E2E`, seeds a test-only SuperAdmin, and stores temporary images under ignored `artifacts/e2e`. Your User Secrets, `El1teSpr1ntTrack_Dev` database, and normal ports are not used.
