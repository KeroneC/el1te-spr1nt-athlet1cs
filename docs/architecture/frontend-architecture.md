# Frontend Architecture

`apps/web` uses Next.js 15 App Router, React 19, TypeScript, and Tailwind CSS. Pages and layouts are Server Components by default. Components use `"use client"` only when they need browser state or events.

## Server and Client Responsibilities

Server Components load protected data. `app/admin/(protected)/layout.tsx` calls `requireAdminUser`, while the announcements page calls `adminApiFetch`. These requests use `cache: "no-store"`, preventing private admin responses from becoming shared cached output.

Client Components own interaction. `components/admin/login-form.tsx`, `announcement-form.tsx`, `delete-announcement-button.tsx`, and `admin-shell.tsx` handle form state, dialogs, navigation, and responsive controls. They call same-origin Next.js Route Handlers rather than reading the backend JWT.

## Authentication Boundary

- `app/api/admin-session/login/route.ts` calls API login and `/api/auth/me`, accepts only active Admin or SuperAdmin users, then sets `el1te_admin_session`.
- `lib/admin/server-api.ts` is marked `server-only`. It reads the HttpOnly cookie and forwards a Bearer token to `API_BASE_URL`.
- `lib/admin/auth.ts` protects server-rendered routes and converts `401` or `403` into the correct redirect.
- `app/api/admin-session/logout/route.ts` expires the cookie.

The cookie is HttpOnly, SameSite Lax, scoped to `/`, aligned with token expiration, and Secure in production. Browser JavaScript cannot inspect it, which limits token theft through client-side script injection. The API still validates the JWT and authorization policy on every protected request.

## Announcements

The list route is server-rendered and stores search, published, featured, expired, and page filters in the URL. `lib/admin/validation.ts` converts those values into the backend query. Loading, error, empty, and no-results states are explicit.

`components/admin/announcement-form.tsx` performs immediate client validation, converts local date-time inputs to UTC, and posts JSON to same-origin Route Handlers. The handlers in `app/api/admin/announcements` attach the server-held token and preserve safe backend field errors. The backend remains authoritative.

Types in `lib/admin/types.ts` describe the frontend view of API contracts. They improve compile-time checks but are not runtime validation. `lib/admin/api-error.ts` limits which API error details reach the browser.

## Route Layout

```text
/admin/login                         public login page
/admin/access-denied                 public explanatory page
/admin                               protected dashboard
/admin/announcements                 protected list
/admin/announcements/new             protected creation form
/admin/announcements/{id}/edit       protected edit form
/api/admin-session/*                 same-origin session handlers
/api/admin/announcements/*           same-origin protected mutation proxy
```

Only Announcements has an admin screen. Disabled navigation entries indicate future modules without pretending those workflows exist.
