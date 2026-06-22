# El1te Spr1nt Athlet1cs Documentation

This is the learning and operating reference for the El1te Spr1nt Athlet1cs platform. The repository currently contains a .NET 10 CMS and authentication API, a Next.js 15 web application, a protected administration workspace, and complete announcement management. It is intentionally still a foundation: registration workflows, payments, documents, additional admin screens, parent and athlete portals, media uploads, and mobile clients are not implemented.

## Suggested Reading Order

1. [System overview](architecture/system-overview.md)
2. [Repository map](architecture/repository-map.md)
3. [Backend architecture](architecture/backend-architecture.md)
4. [Database and EF Core](architecture/database-and-ef-core.md)
5. [Authentication and authorization](architecture/authentication-and-authorization.md)
6. [Frontend architecture](architecture/frontend-architecture.md)
7. [Announcements end to end](learning-notes/announcements-end-to-end.md)
8. [Testing strategy](architecture/testing-strategy.md)
9. [Architecture decisions](#architecture-decisions)
10. [Troubleshooting](guides/troubleshooting.md)

## Architecture

- [System overview](architecture/system-overview.md)
- [Repository map](architecture/repository-map.md)
- [Backend architecture](architecture/backend-architecture.md)
- [Frontend architecture](architecture/frontend-architecture.md)
- [Database and EF Core](architecture/database-and-ef-core.md)
- [Authentication and authorization](architecture/authentication-and-authorization.md)
- [Testing strategy](architecture/testing-strategy.md)

## Learning Notes

- [.NET 10 upgrade](learning-notes/dotnet-10-upgrade.md)
- [CMS domain foundation](learning-notes/cms-domain-foundation.md)
- [Public CMS API](learning-notes/public-cms-api.md)
- [Admin CMS API](learning-notes/admin-cms-api.md)
- [Admin frontend authentication](learning-notes/admin-frontend-authentication.md)
- [Announcements end to end](learning-notes/announcements-end-to-end.md)

## Architecture Decisions

- [ADR 001: Use a monorepo](decisions/001-use-monorepo.md)
- [ADR 002: Use a layered backend architecture](decisions/002-use-layered-backend-architecture.md)
- [ADR 003: Use GUID identifiers](decisions/003-use-guid-identifiers.md)
- [ADR 004: Use JWT authentication](decisions/004-use-jwt-authentication.md)
- [ADR 005: Store the web session in an HttpOnly cookie](decisions/005-use-http-only-web-session-cookie.md)
- [ADR 006: Separate public and admin APIs](decisions/006-separate-public-and-admin-apis.md)
- [ADR 007: Use Next.js with ASP.NET Core](decisions/007-use-nextjs-and-aspnet-core.md)

## Guides

- [Local development](guides/local-development.md)
- [Authentication testing](guides/authentication-testing.md)
- [Announcements testing](guides/announcements-testing.md)
- [EF Core migrations](guides/ef-core-migrations.md)
- [Troubleshooting](guides/troubleshooting.md)
- [Glossary](guides/glossary.md)

The older phase-level files at the root of `docs` are retained as short compatibility links. The documents above are the maintained references.
