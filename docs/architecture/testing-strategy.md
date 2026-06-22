# Testing Strategy

The test suite uses xUnit for .NET and Vitest for the Next.js TypeScript logic. Test scope follows the boundary being protected.

## Backend Unit Tests

`apps/api/tests/El1teSpr1ntTrack.UnitTests` isolates application services with test doubles. It covers registration/login behavior, CMS validation, public visibility and privacy, admin lifecycle behavior, unique slug generation, and clock behavior. These tests are fast and explain business decisions without requiring SQL Server or an HTTP host.

Run:

```powershell
dotnet test apps/api/tests/El1teSpr1ntTrack.UnitTests/El1teSpr1ntTrack.UnitTests.csproj --configuration Release
```

## Backend Integration Tests

`apps/api/tests/El1teSpr1ntTrack.IntegrationTests` hosts the real API pipeline for authorization tests and inspects the EF Core model. `AdminAuthorizationTests` verifies anonymous `401`, Parent `403`, privileged success, and inactive-user rejection. This belongs at the integration level because authentication middleware, claims, policy handlers, routing, and database-backed active checks must cooperate.

`CmsModelTests` verifies important EF configuration such as keys, indexes, lengths, and enum storage. It does not replace applying migrations to a real local SQL Server.

Run all backend tests:

```powershell
dotnet test apps/api/El1teSpr1ntTrack.sln --configuration Release
```

## Frontend Tests

`apps/web/tests` uses Vitest. `admin-validation.test.ts` covers login rules, admin-role selection, announcement filters, form validation, and lifecycle labels. `api-error.test.ts` verifies that safe field errors survive while sensitive backend detail does not. The current tests target deterministic boundary logic rather than rendering components or starting browsers.

Run:

```powershell
cd apps/web
npm.cmd run test
```

The same package also provides lint, strict TypeScript, and production build checks:

```powershell
npm.cmd run lint
npm.cmd run typecheck
npm.cmd run build
```

## Isolation and Manual Coverage

Unit tests use in-memory fakes scoped to each test. API authorization tests use their own hosted configuration and test data. Manual verification should create clearly disposable records and remove them afterward; never rely on production credentials or data.

Public visibility rules deserve tests because one missing predicate can expose drafts, expired content, coach email, or private submissions. Authorization deserves hosted tests because a correct service cannot compensate for a missing controller policy.

Not yet covered: automated browser end-to-end tests, accessibility automation, visual regression, load testing, production SQL migration smoke tests, token refresh/revocation, and admin screens beyond Announcements. Test counts can change frequently, so the test runner is the source of truth rather than a number in documentation.
