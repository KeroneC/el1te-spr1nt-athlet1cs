# Admin CMS API

Phase 4 adds the protected administrative API that will power a future CMS dashboard. It does not include admin frontend pages or media uploads.

## Authorization

All routes under `/api/admin` require the `CmsAdmin` policy. The authenticated database user must:

- be active; and
- have the `Admin` or `SuperAdmin` role.

Missing or invalid authentication returns `401 Unauthorized`. Authenticated users without an allowed active role receive `403 Forbidden`.

`GET /api/auth/me` returns the current user's ID, email, name, role, and active status. It never returns password or token internals.

## Local SuperAdmin

Development can create one SuperAdmin from User Secrets. Set all four values before starting the API:

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.local" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
dotnet user-secrets set "SeedAdmin:Password" "replace-with-a-strong-local-password" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
dotnet user-secrets set "SeedAdmin:FirstName" "Local" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
dotnet user-secrets set "SeedAdmin:LastName" "Admin" --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
```

Seeding runs only in Development, skips incomplete configuration, and never changes an existing account. No password is stored in source control or logged.

Log in through `POST /api/auth/login`, copy `accessToken`, select **Authorize** in Swagger, and enter the token. Swagger supplies the `Bearer` scheme.

## Endpoints

- `GET`, `PUT /api/admin/site-settings`
- `GET`, `POST /api/admin/content-blocks`
- `GET`, `PUT`, `DELETE /api/admin/content-blocks/{id}`
- `GET`, `POST /api/admin/announcements`
- `GET`, `PUT`, `DELETE /api/admin/announcements/{id}`
- `GET`, `POST /api/admin/events`
- `GET`, `PUT`, `DELETE /api/admin/events/{id}`
- `GET`, `POST /api/admin/coaches`
- `GET`, `PUT`, `DELETE /api/admin/coaches/{id}`
- `GET`, `POST /api/admin/sponsors`
- `GET`, `PUT`, `DELETE /api/admin/sponsors/{id}`
- `GET`, `POST /api/admin/faqs`
- `GET`, `PUT`, `DELETE /api/admin/faqs/{id}`
- `GET /api/admin/contact-submissions`
- `GET`, `DELETE /api/admin/contact-submissions/{id}`
- `PUT /api/admin/contact-submissions/{id}/status`

Collection routes support their documented resource filters plus `page` and `pageSize`. Defaults are page 1 and 20 records; page size is capped at 100.

Announcement, event, and sponsor slugs are generated once and remain stable during later title/name changes. Content block keys are immutable after creation. Duplicate keys return `409 Conflict`.

Deleting content blocks, announcements, events, and contact submissions permanently removes them. Deleting coaches, sponsors, or FAQs deactivates them so historical content remains available to administrators while disappearing from public responses.

Public CMS routes remain anonymous and retain their published/active/expiration and coach-email privacy rules. Contact submissions have no public read endpoint.
