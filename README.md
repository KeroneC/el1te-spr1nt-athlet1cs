# El1te Spr1nt Athlet1cs Platform

A learning-oriented full-stack platform for a nonprofit youth track club. The monorepo combines a .NET 10 ASP.NET Core API, EF Core and SQL Server persistence, and a Next.js 15 web application.

## Current Status

Implemented:

- User registration, JWT login, and current-user lookup
- CMS domain and EF Core migration for site content
- Anonymous public CMS endpoints with visibility and privacy rules
- Protected Admin/SuperAdmin CMS endpoints
- Development-only SuperAdmin seeding through User Secrets
- Secure admin web login with an HttpOnly session cookie
- Admin dashboard shell and complete announcement management
- Backend unit/integration tests and frontend validation/error tests

Intentionally not implemented yet: additional CMS admin screens, media uploads, password reset/refresh/revocation, parent or athlete portals, registration, payments, private documents, production infrastructure, and iOS code.

## Documentation

Start at the [documentation home](docs/README.md). Useful entry points:

- [System architecture](docs/architecture/system-overview.md)
- [Announcements end to end](docs/learning-notes/announcements-end-to-end.md)
- [Local development](docs/guides/local-development.md)
- [Troubleshooting](docs/guides/troubleshooting.md)
- [Glossary](docs/guides/glossary.md)
- [CI/CD and future Azure preparation](docs/architecture/cicd-overview.md)
- [AI project handoff](docs/guides/ai-project-handoff.md)

## Prerequisites

- .NET 10 SDK compatible with `global.json`
- Node.js 22 or newer and npm
- SQL Server Express LocalDB for the default Windows setup
- EF Core CLI 10.0.9

## Local Quick Start

The complete first-time setup, User Secrets, migration command, and certificate guidance are in [local development](docs/guides/local-development.md).

After setup, start the API from the repository root:

```powershell
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --launch-profile https
```

In another terminal:

```powershell
cd apps/web
npm.cmd run dev
```

Open:

- Swagger: `https://localhost:7171/swagger`
- API health: `https://localhost:7171/health`
- API readiness: `https://localhost:7171/health/ready`
- Admin: `http://localhost:3000/admin`

## Validation

Backend:

```powershell
dotnet build apps/api/El1teSpr1ntTrack.sln --configuration Release
dotnet test apps/api/El1teSpr1ntTrack.sln --configuration Release
```

Frontend:

```powershell
cd apps/web
npm.cmd run lint
npm.cmd run typecheck
npm.cmd run test
npm.cmd run build
```

Keep JWT keys, seed passwords, connection credentials, and token values out of Git. See [authentication and authorization](docs/architecture/authentication-and-authorization.md) and [security](docs/security.md).
