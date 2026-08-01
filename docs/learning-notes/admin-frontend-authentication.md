# Admin Frontend Authentication

## What Was Built

The Next.js application adds admin login, a server-managed JWT session, protected layouts, current-user display, access-denied handling, expiration cleanup, and logout.

## Why It Exists

A browser admin experience needs convenient navigation without exposing a bearer token to browser JavaScript. The Next.js server acts as a web-specific session boundary while ASP.NET Core remains the source of truth.

## Important Files

- `apps/web/app/admin/(auth)/login/page.tsx`
- `apps/web/components/admin/login-form.tsx`
- `apps/web/app/api/admin-session/login/route.ts`
- `apps/web/app/api/admin-session/logout/route.ts`
- `apps/web/app/admin/(protected)/layout.tsx`
- `apps/web/lib/admin/server-api.ts`
- `apps/web/lib/admin/auth.ts`

## How It Works

The Client Component submits credentials to the same-origin login Route Handler. That handler calls `/api/auth/admin/login`; SuperAdmins then complete `/api/auth/admin/mfa/verify`. The handler calls `/api/auth/me`, accepts only active Admin/SuperAdmin users, and stores the JWT only in `el1te_admin_session`, an HttpOnly cookie. The short-lived MFA challenge token is held only in a separate HttpOnly cookie scoped to the MFA route.

The protected layout calls `requireAdminUser` on every dynamic request. `adminApiFetch` reads the cookie server-side, adds a Bearer header, and disables caching. A `401` routes through logout to clear the cookie; a `403` routes to access denied.

## Request or Data Flow

```text
Browser form -> Next.js login Route Handler -> API login -> API /auth/me
  -> role/active check -> HttpOnly cookie -> protected Server Component
```

## How to Test It

Use [authentication testing](../guides/authentication-testing.md). In browser developer tools, inspect cookie flags without copying its value. Confirm no token appears in Local Storage, Session Storage, the login JSON response, or client logs.

## Common Problems

- Login reports service unavailable: Next.js cannot reach `API_BASE_URL` or does not trust local HTTPS.
- Immediate expiration: the cookie/token expired, clocks differ, or API JWT configuration changed.
- Correct credentials but access denied: the account is not currently an active Admin/SuperAdmin.
- Cookie missing in production: HTTPS is required for a Secure cookie.

## Concepts to Study

Backend-for-frontend, HttpOnly/Secure/SameSite cookies, Server Components, Route Handlers, bearer authentication, XSS, CSRF, and cache control.

## What Was Intentionally Deferred

This original learning note predates launch hardening. Refresh tokens, remember-me, authenticator-app TOTP, and granular frontend permissions remain deferred; email MFA, one-time password recovery, and security-version session revocation are now implemented.
