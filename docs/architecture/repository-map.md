# Repository Map

The map highlights ownership boundaries and useful starting files rather than every file.

## `apps/api`

The .NET solution is `apps/api/El1teSpr1ntTrack.sln`.

| Directory | Responsibility | Open first | Does not belong here |
| --- | --- | --- | --- |
| `src/El1teSpr1ntTrack.Core` | Entities, enums, DTOs, and foundational repository contracts | `Entities/Announcement.cs`, `DTOs/Cms/PublicCmsDtos.cs` | HTTP, EF Core setup, UI code |
| `src/El1teSpr1ntTrack.Application` | Use cases, validation, query options, and interfaces consumed by use cases | `Services/PublicCmsService.cs`, `Services/AdminCmsService.NewsEvents.cs` | Controllers or SQL Server configuration |
| `src/El1teSpr1ntTrack.Infrastructure` | EF Core, SQL Server repositories, migrations, JWT generation, and development seeding | `Data/El1teDbContext.cs`, `Repositories/AdminCmsRepository.cs` | HTTP response decisions or React code |
| `src/El1teSpr1ntTrack.Api` | Composition root, controllers, middleware, authentication, authorization, Swagger, and CORS | `Program.cs`, `Controllers/PublicCmsController.cs`, `Authorization/CmsAdminAuthorization.cs` | Business workflows that belong in Application |
| `tests/El1teSpr1ntTrack.UnitTests` | Fast service, validation, visibility, slug, and auth tests | `AdminCmsServiceTests.cs`, `PublicCmsTests.cs` | Tests requiring the full HTTP host |
| `tests/El1teSpr1ntTrack.IntegrationTests` | Hosted API authorization and EF model checks | `AdminAuthorizationTests.cs`, `CmsModelTests.cs` | Pure algorithm tests |

The dependency direction is Core <- Application <- Infrastructure/API. API is also the composition root and references the projects it wires together.

## `apps/web`

| Directory | Responsibility | Open first | Does not belong here |
| --- | --- | --- | --- |
| `app` | App Router pages, layouts, loading/error states, and Route Handlers | `app/admin/(protected)/layout.tsx`, `app/api/admin-session/login/route.ts` | Reusable domain-independent UI |
| `components/admin` | Client and shared UI for the admin workspace | `admin-shell.tsx`, `announcement-form.tsx` | Secrets or direct database access |
| `lib/admin` | Server API boundary, auth guard, safe errors, types, and validation | `server-api.ts`, `auth.ts`, `validation.ts` | Visual components |
| `tests` | Vitest coverage for frontend validation and safe error mapping | `admin-validation.test.ts`, `api-error.test.ts` | Backend authorization tests |
| `public` | Static browser assets | `images/track-hero.png` | Private uploads or secrets |

Route groups such as `(auth)` and `(protected)` organize layouts without changing public URLs. Dynamic segments such as `[id]` identify a specific announcement.

## `docs`

- `architecture`: current system structure and cross-cutting behavior
- `learning-notes`: phase and workflow explanations
- `decisions`: accepted architecture decision records
- `guides`: procedures for running, testing, migrating, and troubleshooting

Start at [`docs/README.md`](../README.md). Detailed explanations belong under `docs`; the root README remains a concise entry point.
