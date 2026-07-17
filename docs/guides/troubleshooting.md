# Troubleshooting

## Uploaded Image Does Not Load

Confirm `MediaStorage__PublicBaseUrl` matches the API profile currently running and restart the API after changing it. Apply `AddMediaLibraryAndGallery` if media requests fail because tables are missing. A `404` from `/media/{id}` also means the asset is inactive, missing, or its local file was removed.

## Upload Is Rejected

The file must be a decodable JPEG, PNG, or WebP no larger than 10 MB by default. Renaming an extension does not change the encoded format. Check the safe validation response in Admin; parser details and physical paths are intentionally suppressed.

Work from the symptom to the boundary that failed. Do not disable security checks to make local setup pass.

| Symptom | Likely cause | Verify | Fix |
| --- | --- | --- | --- |
| SDK projects cannot load | .NET 10 SDK or IDE workload missing | `dotnet --version`, `dotnet --list-sdks` | Install compatible .NET 10/Visual Studio support; restart IDE |
| Database connection/login failure | LocalDB stopped, database absent, or connection mismatch | `sqllocaldb info MSSQLLocalDB`; compare Development connection | Start LocalDB and apply migrations with explicit `El1teSpr1ntTrack_Dev` connection |
| Database connection fails on macOS | LocalDB is Windows-only, Colima is stopped, or the SQL container is unhealthy | `colima status`; `docker ps`; `docker logs el1te-sqlserver` | Follow the [macOS guide](macos-development.md), start the container, and re-run the bootstrap if the User Secret is missing |
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
| Admin change is not visible immediately | Public page still has a valid 60-second cache entry | Wait one minute and refresh; compare the public API response | Allow the cache window; do not disable public caching or Admin `no-store` behavior |
| Public site shows a safe unavailable state | API stopped, wrong `API_BASE_URL`, or a malformed response | Check API health and the Next.js server log | Start the API, correct the server-only URL, and restart Next.js after environment changes |
| Contact form cannot submit | API unavailable or backend validation rejected a field | Check the safe form status and API health; do not expose exception details | Correct highlighted fields or restore API connectivity; contact POSTs are intentionally uncached |
| Homepage photo works on desktop but is broken on iPhone Safari | A device-specific Next.js runtime image-optimizer request failed or was cached behind Azure App Service affinity | Inspect the rendered `src`/`srcset`, request the exact `/_next/image` URL, and compare it with the original static asset | For a critical fixed image, publish a reasonably sized static JPEG under a new cache-safe filename and reference it directly; verify the live HTML no longer uses the optimizer for that image |
| Deleted static asset still returns `200` after Azure deployment | ZIP deployment retained an orphaned file from an earlier release | Request the retired path directly after deployment and compare the App Service files with the immutable artifact | Deploy API and web ZIPs with `az webapp deploy --clean true`; verify retired paths return `404` after the release |
| Phone still shows an old or broken image after a successful release | Browser/CDN cache, an unchanged asset URL, or a genuinely failing optimized request | Test the exact asset URL with a cache-busting query and inspect the page source before assuming it is only cache | Prefer a new fingerprint-like filename for emergency asset replacements, then verify both the new `200` response and old-path `404` responses |
| Media upload says to check highlighted fields but none is highlighted | Client validation summary is not mapped to the specific queued file field | Submit one image with required alt text blank; inspect field error state, focus, and accessible error association | Mark the exact field invalid, render its message beside the field, and move focus to the first invalid control |
| Azure deployment says the site failed to start, but health checks pass | Azure CLI Linux startup tracking retained stale container-timeout state after Kudu activated the new artifact | Compare `az webapp log deployment list`, `az webapp log startup show`, and the live readiness endpoint | Track the new active Kudu deployment ID, restart after activation, then run application smoke tests; do not accept an old-version health response as proof of promotion |

Additional clues:

- A `404` at `https://localhost:7171/` is expected; use `/swagger` or `/health`.
- PowerShell script policy may block `npm`; use `npm.cmd`.
- Environment and `.env.local` changes require process restarts.
- Do not solve certificate, cookie, CORS, or authorization problems by turning those protections off.

## Production Incident Notes

### 2026-07-14: iPhone Homepage Photo Failure

- **Symptom:** the replacement homepage achievement photo rendered on desktop but appeared as a broken-image icon in iPhone Safari.
- **Finding:** the page layout and original file were valid, but the browser depended on a responsive `/_next/image` request. Direct optimizer probes were inconsistent, and a phone cache initially made the problem look like a stale-page issue.
- **Resolution:** generated a 1920 x 1280, approximately 369 KB JPEG; published it as `/images/team/meet-community-static.jpg`; and used a plain eager-loaded image URL for this fixed homepage asset.
- **Deployment hardening:** Azure ZIP deployments now use clean mode. The previous `meet-community.jpg` and `medalists.jpg` paths were independently verified as `404`, while the new JPEG and homepage returned `200`.
- **Regression check:** inspect compiled and live homepage markup to ensure this image does not route through `/_next/image` unless cross-device optimizer behavior is deliberately reintroduced and tested.

### 2026-07-17: Azure CLI Reported a False API Startup Failure

- **Symptom:** `az webapp deploy` reported that the API worker failed to start within 10 minutes, so the workflow skipped the web artifact.
- **Finding:** Kudu marked the new API ZIP deployment active and successful, the new invitation endpoint was live, and App Service logs showed the application listening on port 8080 with a successful platform startup probe. The CLI continued polling stale container-timeout state and eventually returned exit code 1.
- **Resolution:** deployment now disables the unreliable Linux startup tracker, waits for a new active Kudu deployment ID with success status, explicitly restarts the app, and then runs the existing API or web smoke tests.
- **Safety:** the temporary SQL firewall rule was removed by the workflow's unconditional cleanup even though the deployment step failed.
