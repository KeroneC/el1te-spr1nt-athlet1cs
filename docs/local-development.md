# Local Development

Run commands from the repository root unless a section says otherwise.

## Prerequisites

- .NET 10 SDK (`global.json` pins `10.0.301` and permits newer .NET 10 feature bands)
- Visual Studio with .NET 10 support and the ASP.NET workload, or another editor plus the .NET CLI
- SQL Server Express LocalDB for the default Windows setup, SQL Server Developer Edition, or a containerized SQL Server instance
- EF Core CLI `10.0.9` for database migrations
- Node.js 22 or newer for frontend development

Confirm the SDK:

```powershell
dotnet --version
dotnet --list-sdks
```

Install EF tooling once if `dotnet ef --version` is unavailable:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.9
```

Reopen the terminal after installing a global tool.

## User Secrets

The API project already contains a `UserSecretsId`. Set a private signing key of at least 32 characters:

```powershell
dotnet user-secrets set "Jwt:Key" "replace-with-your-private-local-key-of-32-or-more-characters" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

In Visual Studio, right-click `El1teSpr1ntTrack.Api`, select **Manage User Secrets**, and use the equivalent JSON:

```json
{
  "Jwt": {
    "Key": "replace-with-your-private-local-key-of-32-or-more-characters"
  }
}
```

The secrets file is stored in the Windows user profile, outside the repository, and is not tracked by Git. Keep `Jwt:Key` out of `appsettings*.json`.

The default Development connection string is already defined in `appsettings.Development.json`. Add a `ConnectionStrings:DefaultConnection` user secret only when overriding it for another SQL Server instance.

## Database

Database migrations currently include:

- `AddAuthenticationFoundation`
- `AddCmsFoundation`

`AddCmsFoundation` creates the CMS tables, indexes, and generic development content described in [cms-foundation.md](cms-foundation.md).

For the default Windows setup, start LocalDB:

```powershell
sqllocaldb start MSSQLLocalDB
```

Apply migrations explicitly to `El1teSpr1ntTrack_Dev`, the database used by the running Development API:

```powershell
dotnet ef database update `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj `
  --connection "Server=(localdb)\mssqllocaldb;Database=El1teSpr1ntTrack_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

The infrastructure project has a design-time context factory that defaults to `El1teSpr1ntTrack_DesignTime`. Running `dotnet ef database update` without `--connection` updates that separate tooling database, not the database used by the Development API.

For another SQL Server instance, set `ConnectionStrings:DefaultConnection` with User Secrets and pass that same connection string to the migration command.

## Backend

Restore, build, test, and run the API:

```powershell
dotnet restore apps/api/El1teSpr1ntTrack.sln --configfile NuGet.Config
dotnet build apps/api/El1teSpr1ntTrack.sln
dotnet test apps/api/El1teSpr1ntTrack.sln
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --launch-profile https
```

The HTTPS launch profile listens on `https://localhost:7171`. Useful URLs:

- Swagger: `https://localhost:7171/swagger`
- Health: `https://localhost:7171/health`
- Public CMS example: `https://localhost:7171/api/public/site-settings`

A `404` at `/` is expected because the API does not define a root endpoint.

Auth endpoints are available at `POST /api/auth/register` and `POST /api/auth/login`. Public CMS endpoints are documented in [public-cms-api.md](public-cms-api.md).

To test protected CMS management, configure the development-only SuperAdmin described in [admin-cms-api.md](admin-cms-api.md), restart the API, log in, and authorize Swagger with the returned access token.

### Visual Studio

1. Open `apps/api/El1teSpr1ntTrack.sln`.
2. Set `El1teSpr1ntTrack.Api` as the startup project.
3. Select its `https` launch profile.
4. Start debugging and open `https://localhost:7171/swagger`.

## Frontend

```powershell
cd apps/web
npm install
npm run dev
```

Set `NEXT_PUBLIC_API_BASE_URL` in `apps/web/.env.local`. Start with `apps/web/.env.local.example`.

If PowerShell blocks `npm`, use `npm.cmd` instead.

## Troubleshooting

| Symptom | Resolution |
| --- | --- |
| `The SDK 'Microsoft.NET.Sdk' specified could not be found` | Install the .NET 10 SDK and use a Visual Studio version that supports .NET 10. Confirm with `dotnet --version`. |
| `dotnet-ef does not exist` | Run `dotnet tool install --global dotnet-ef --version 10.0.9`, reopen the terminal, and verify with `dotnet ef --version`. |
| `sqllocaldb` is not recognized | Install the SQL Server Express LocalDB component through Visual Studio Installer, then reopen the terminal. |
| `Jwt:Key is required` | Set `Jwt:Key` through .NET User Secrets and restart the API. |
| `Cannot open database "El1teSpr1ntTrack_Dev" requested by the login` | Start `MSSQLLocalDB` and rerun the migration command above with the explicit `--connection`. |
| Swagger works but `/` returns `404` | This is expected; open `/swagger` or `/health`. |
| PowerShell cannot run `npm` | Use `npm.cmd` for the frontend commands. |
