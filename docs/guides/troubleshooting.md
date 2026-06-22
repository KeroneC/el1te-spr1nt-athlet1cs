# Troubleshooting

Work from the symptom to the boundary that failed. Do not disable security checks to make local setup pass.

| Symptom | Likely cause | Verify | Fix |
| --- | --- | --- | --- |
| SDK projects cannot load | .NET 10 SDK or IDE workload missing | `dotnet --version`, `dotnet --list-sdks` | Install compatible .NET 10/Visual Studio support; restart IDE |
| Database connection/login failure | LocalDB stopped, database absent, or connection mismatch | `sqllocaldb info MSSQLLocalDB`; compare Development connection | Start LocalDB and apply migrations with explicit `El1teSpr1ntTrack_Dev` connection |
| Table/column missing | Migration not applied to runtime database | `dotnet ef migrations list`; inspect `__EFMigrationsHistory` | Apply migrations to the same connection the API uses |
| `Jwt:Key is required` or too short | User Secret missing/invalid | `dotnet user-secrets list --project <api-project>` without sharing output | Set a private key of at least 32 characters; restart API |
| Login says invalid credentials | Wrong password/email, inactive account, or seed not created | Normalize email; check local user and API startup log | Correct local credentials; configure all seed fields before startup |
| Admin endpoint returns `401` | Missing/expired/invalid token | Check Authorization scheme and token expiration without exposing value | Log in again; align issuer/audience/key and clocks |
| Admin endpoint returns `403` | Parent/Coach role, inactive user, or stale privileged claim | Call `/api/auth/me`; inspect local user role/active state | Use an active Admin/SuperAdmin; do not weaken the policy |
| Seeded SuperAdmin missing | Incomplete secrets, non-Development environment, or email already exists | Check all four `SeedAdmin` names and environment | Set all values and restart; existing users are intentionally not modified |
| Next.js cannot reach API | API stopped, wrong `API_BASE_URL`, or TLS trust failure | Open API health/Swagger; inspect web server error | Start API; copy `.env.local.example`; restart Next.js after env changes |
| HTTPS certificate error | Development certificate untrusted by OS/Node | Open Swagger and inspect Node error | `dotnet dev-certs https --trust`; use `NODE_OPTIONS=--use-system-ca` locally |
| Browser CORS error | Browser called API origin directly and origin is not allowed | Compare request origin and `ApiCorsExtensions` configuration | Use intended Next.js server boundary or configure approved Development origin; avoid wildcard credentials |
| Cookie not set | Login rejected, using wrong host, or production Secure cookie over HTTP | Inspect login status and cookie metadata, not value | Use correct admin account/host; production must use HTTPS |
| Session immediately expires | Expired token, clock skew, changed JWT config, stale cookie | Compare expiration and system clocks without copying token | Clear cookie, restart with consistent config, sign in again |
| Frontend production build fails | Type, lint, environment-independent rendering, or dependency problem | Run lint, typecheck, test, then build separately | Fix the first failing check; reinstall from lockfile if dependency tree is damaged |
| EF reports pending model changes | Entity/configuration differs from snapshot | Run pending-model command and inspect Git diff | Create/review a migration; do not hand-edit snapshot to hide it |
| Duplicate slug conflict | Unique index race or direct conflicting data | Search admin data and inspect generated slug | Keep unique generation through service; resolve duplicate local data safely |
| Admin form saves but list looks unchanged | Active filters exclude the updated record or navigation refresh is pending | Clear URL filters; reload the list; inspect the safe response status | Use the module's clear-filter control; correct backend validation rather than bypassing it |
| Duplicate content-block key | Another block already owns the exact page-section key | Search Content by key; do not rename seeded records casually | Choose a unique key or deliberately update the existing block |
| Coach email unexpectedly public/private | `IsEmailPublic` and Email do not match the intended state | Inspect both fields and the public coaches response | Provide a valid email and enable public visibility explicitly, or keep the toggle off |
| Draft unexpectedly public | Publication/date data or public predicate changed | Compare record flags/dates and `PublicCmsVisibility` | Restore published/date rules and add a regression test |
| Published item missing publicly | Future publish date, expired date, inactive state, or wrong slug | Compare UTC dates and public collection filters | Correct lifecycle values; admin visibility alone does not imply public visibility |

Additional clues:

- A `404` at `https://localhost:7171/` is expected; use `/swagger` or `/health`.
- PowerShell script policy may block `npm`; use `npm.cmd`.
- Environment and `.env.local` changes require process restarts.
- Do not solve certificate, cookie, CORS, or authorization problems by turning those protections off.
