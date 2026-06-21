# Admin Frontend

Phase 5 adds the secure admin frontend foundation and Announcements management at `apps/web`.

## Routes

- `/admin/login`
- `/admin`
- `/admin/announcements`
- `/admin/announcements/new`
- `/admin/announcements/{id}/edit`
- `/admin/access-denied`

Other CMS modules remain intentionally unavailable in this phase.

## Session Architecture

The browser submits credentials to the Next.js login route handler. Next.js calls the .NET login endpoint, verifies the account through `/api/auth/me`, and accepts only active `Admin` or `SuperAdmin` users. The JWT is stored in the `el1te_admin_session` cookie with `HttpOnly`, `SameSite=Lax`, `Path=/`, the JWT expiration, and `Secure` in production.

Client Components never receive or decode the JWT. Protected layouts and announcement requests execute on the Next.js server with `no-store`; the .NET API remains the authorization authority. A `401` clears the web cookie and returns to login. A `403` shows access denied.

Logout clears the web session cookie. It does not revoke the stateless backend JWT because token revocation is not implemented.

## Local Setup

Configure the frontend:

```powershell
Copy-Item apps/web/.env.local.example apps/web/.env.local
```

The default `API_BASE_URL` is `https://localhost:7171`. Trust the ASP.NET development certificate with `dotnet dev-certs https --trust`. If Node does not use the Windows certificate store, set `NODE_OPTIONS=--use-system-ca` locally.

Configure the development SuperAdmin using [admin-cms-api.md](admin-cms-api.md), then run the API and frontend:

```powershell
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --launch-profile https
cd apps/web
npm.cmd run dev
```

Open `http://localhost:3000/admin`. Use the SuperAdmin credentials configured through .NET User Secrets.

## Validation

```powershell
cd apps/web
npm.cmd run lint
npm.cmd run typecheck
npm.cmd run test
npm.cmd run build
```

## Announcements

The module supports private drafts, scheduled/published/expired labels, featured state, URL-backed search and filters, pagination, create/edit forms, backend validation messages, and confirmed hard deletion. Date-time inputs are interpreted in the administrator's local timezone and sent to the API as UTC.

## Known Limitations

- No password reset, remember-me option, or token refresh flow
- Logout does not revoke the backend JWT
- No image upload or rich-text editor
- Only Announcements management is implemented
- Events, coaches, sponsors, FAQs, content blocks, site settings, contact submissions, media, and users remain future work
