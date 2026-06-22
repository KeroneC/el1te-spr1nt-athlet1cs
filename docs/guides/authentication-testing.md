# Authentication Testing

Use only disposable local accounts and avoid displaying token values in screenshots, logs, shell history, or documentation.

## Preparation

Complete [local development](local-development.md), start API and web, and configure a Development SuperAdmin. Swagger is at `https://localhost:7171/swagger`; admin login is at `http://localhost:3000/admin`.

## Registration and Login

1. In Swagger, call `POST /api/auth/register` with a new local email, matching password confirmation, and a password of at least eight characters.
2. Confirm `200` and role `Parent`. The request cannot choose Admin/SuperAdmin.
3. Call `POST /api/auth/login` with wrong credentials; confirm `401` without learning whether the email exists.
4. Call it with the registered credentials; confirm a token and expiration are returned by the API.
5. Select Swagger **Authorize** and enter the bearer token. Do not paste the value elsewhere.
6. Call `GET /api/auth/me`; confirm identity, role, and active state, with no password hash or token internals.

## Authorization Matrix

| Test | Setup | Expected |
| --- | --- | --- |
| Anonymous | Remove Swagger authorization and call `/api/admin/announcements` | `401` |
| Parent | Authorize with the registered Parent token | `403` |
| SuperAdmin | Authorize with the Development SuperAdmin token | `200` |
| Inactive | Mark a disposable local admin inactive, then reuse its token | `403` on admin API; login itself rejects inactive credentials |

The inactive test has no product UI. Use a disposable user in a local database, change `IsActive` with a trusted database tool, run the check, then restore or remove the test record. The automated `AdminAuthorizationTests` is the safer repeatable proof.

## Admin Frontend Session

1. Open `/admin` in a private browser window; confirm redirect to login.
2. Try invalid credentials; confirm a generic error.
3. Sign in as the Development SuperAdmin; confirm dashboard name and role.
4. In developer tools, open Application/Storage -> Cookies and select `http://localhost:3000`.
5. Confirm `el1te_admin_session` is HttpOnly, SameSite Lax, scoped to `/`, and has an expiration. Secure is enabled in production; local HTTP development does not set it.
6. Do not reveal or copy the cookie value. Confirm Local Storage and Session Storage contain no JWT.
7. Inspect the login request response: it returns user data, not `accessToken`.
8. Log out; confirm the cookie disappears and `/admin` requires login again.

## Invalid or Expired Session

Use developer tools to delete the cookie, then reload a protected page. The app should route through logout and show login. To test a naturally expired token, temporarily use a short `Jwt:ExpiresMinutes` override in User Secrets, restart both apps, sign in, wait beyond expiration plus configured clock skew, and reload. Remove the override afterward.

Never edit or paste a real JWT to simulate expiration. See [troubleshooting](troubleshooting.md) for unexpected `401` and `403` results.
