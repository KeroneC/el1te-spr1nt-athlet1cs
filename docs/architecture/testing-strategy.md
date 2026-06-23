# Testing Strategy

The test suite uses xUnit for .NET, Vitest for deterministic Next.js TypeScript logic, and Playwright for the critical cross-stack browser workflow. Test scope follows the boundary being protected.

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

## Browser End-to-End Test

`apps/web/e2e` uses Playwright with the real Next.js application, ASP.NET Core API, EF Core migrations, SQL Server LocalDB, authentication cookie boundary, and local media storage. The critical test signs in, uploads an image, creates a published album, adds the image, verifies the public album, and removes its records afterward.

The harness uses dedicated ports (`3100` and `5127`), the dedicated `El1teSpr1ntTrack_E2E` database, fixed test-only credentials, and ignored storage under `artifacts/e2e`. It does not use Development User Secrets or the normal Development database.

From `apps/web` on Windows:

```powershell
npx.cmd playwright install chromium
npm.cmd run test:e2e
```

Use `npm.cmd run test:e2e:headed` when watching the browser is useful. LocalDB and EF Core CLI 10.0.9 must be available. CI runs the same workflow on `windows-2022` and retains Playwright traces, screenshots, and video on failure.

## Isolation and Manual Coverage

Unit tests use in-memory fakes scoped to each test. API authorization tests use their own hosted configuration and test data. Manual verification should create clearly disposable records and remove them afterward; never rely on production credentials or data.

Public visibility rules deserve tests because one missing predicate can expose drafts, expired content, coach email, or private submissions. Authorization deserves hosted tests because a correct service cannot compensate for a missing controller policy.

Not yet covered: broad browser coverage for every Admin resource, accessibility automation, visual regression, load testing, production SQL migration smoke tests, and token refresh/revocation. Test counts can change frequently, so the test runner is the source of truth rather than a number in documentation.
