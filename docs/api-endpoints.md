# API Endpoints

## Phase 9 Media and Gallery

- Admin media: `GET/POST /api/admin/media`, `GET/PUT/DELETE /api/admin/media/{id}`
- Admin albums: `GET/POST /api/admin/gallery-albums`, `GET/PUT/DELETE /api/admin/gallery-albums/{id}`
- Album images: `POST /api/admin/gallery-albums/{id}/media`, `PUT/DELETE /api/admin/gallery-albums/{id}/media/{albumMediaId}`, `PUT /api/admin/gallery-albums/{id}/media/order`
- Public albums: `GET /api/public/gallery-albums`, `GET /api/public/gallery-albums/{slug}`
- Active image bytes: `GET /media/{id}`

All Admin routes use the existing `CmsAdmin` policy. Upload is multipart form data; physical paths and storage keys are never returned.

Liveness is available at `/health`; database readiness is available at `/health/ready`. The legacy descriptive route remains at `/api/v1/health`. Authentication routes are implemented under `/api/auth`. Public CMS routes live under `/api/public`, and protected CMS routes live under `/api/admin`.

## Hall of Fame CMS

- Public inductees: `GET /api/public/hall-of-fame-inductees?page=1&pageSize=8`
- Admin list/create: `GET/POST /api/admin/hall-of-fame-inductees`
- Admin detail/update/deactivate: `GET/PUT/DELETE /api/admin/hall-of-fame-inductees/{id}`

The public route returns only active records, ordered by display order and then name. Admin listing accepts `search`, `isActive`, `inductionYear`, `page`, and `pageSize`. Delete is reversible deactivation. Active records require an accessible photo; names can be edited without changing the generated slug.

## All-American Archive

- Public years: `GET /api/public/all-americans?page=1&pageSize=12`
- Public annual story: `GET /api/public/all-americans/{year}`
- Admin years: `GET/POST /api/admin/all-americans`, `GET/PUT/DELETE /api/admin/all-americans/{id}`
- Annual media: `POST /api/admin/all-americans/{id}/media`, `PUT/DELETE /api/admin/all-americans/{id}/media/{mediaId}`, `PUT /api/admin/all-americans/{id}/media/order`
- Annual athletes: `POST /api/admin/all-americans/{id}/recipients`, `PUT/DELETE /api/admin/all-americans/{id}/recipients/{recipientId}`
- Annual performances: `POST /api/admin/all-americans/{id}/performances`, `PUT/DELETE /api/admin/all-americans/{id}/performances/{performanceId}`

Public APIs return only published years. A summary-only year returns no recipient details until its verified roster and medal-recipient totals reconcile and `DetailsComplete` is enabled. Delete operations are reversible deactivation/unpublishing; no permanent-delete Admin route is exposed.

## Commerce Foundation

- Commerce integration health: `GET /health/commerce`
- Square webhook receiver: `POST /api/webhooks/square`

The webhook route returns `404 Not Found` unless both `Store:Enabled` and `Store:CheckoutEnabled` are true. When enabled, it requires Square's exact HMAC signature, rejects oversized bodies, persists only safe event metadata and a payload hash, and deduplicates the Square event ID. The browser redirect is never treated as payment proof.

Square-only order endpoints are `POST /api/public/store/checkout`, `POST /api/public/store/orders/status`, and `POST /api/public/store/orders/cancel`. Public status accepts a random fragment token and never exposes exact stock. Admin order list, detail, transitions, notes, refunds, tracking rotation, email retry, dashboard, and integration health are under `/api/admin/store` with refund and tracking controls restricted to SuperAdmins.

## Public Store

- `GET /api/public/store/products`
- `GET /api/public/store/products/{slug}`

Both catalog routes return `404 Not Found` while both `Store:Enabled` and `Store:PublicPreviewEnabled` are false. The preview flag exposes only catalog/configurator reads; it does not enable Square, orders, reservations, webhooks, or commerce workers. The list accepts `search`, `category`, `availability`, `page`, and `pageSize`. Public DTOs expose only published products, active options/variants/media, minor-unit prices, and `InStock`, `LowStock`, or `SoldOut`; exact quantities and SKUs remain private. Cart state remains browser-local and non-personal; customer details are sent only when checkout begins.

## Admin Store Catalog and Inventory

The following routes require the `CmsAdmin` policy:

- `GET /api/admin/store/dashboard`
- `GET/POST /api/admin/store/products`
- `GET/PUT/DELETE /api/admin/store/products/{id}` (`DELETE` archives)
- `POST /api/admin/store/products/{id}/duplicate`
- `GET/POST /api/admin/store/categories`
- `PUT /api/admin/store/categories/{id}`
- `GET /api/admin/store/inventory`
- `POST /api/admin/store/inventory/{variantId}/adjustments`
- `POST /api/admin/store/inventory/receipts`
- `GET/POST /api/admin/store/inventory/stocktakes`
- `GET /api/admin/store/inventory/adjustments`

Products are created and maintained through the Admin catalog workflow. Square catalog import routes are not exposed. Historical Square source metadata and import-run records remain in the database for compatibility and audit purposes. Exact inventory is private Admin data. Public store routes are feature-gated and `Store:Enabled=false` remains required until cutover.

## Auth

Implemented:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/admin/login`
- `POST /api/auth/admin/mfa/verify`
- `POST /api/auth/admin/password-reset/request`
- `POST /api/auth/admin/password-reset/inspect`
- `POST /api/auth/admin/password-reset/complete`

- `GET /api/auth/me` (authenticated)

Token revocation, API logout, and refresh endpoints are not implemented. The web-only logout Route Handler clears the Next.js HttpOnly cookie.

### Register

`POST /api/auth/register`

Request:

```json
{
  "firstName": "Taylor",
  "lastName": "Parent",
  "email": "parent@example.com",
  "password": "StrongPassword123!",
  "confirmPassword": "StrongPassword123!"
}
```

Successful response: `200 OK`

```json
{
  "accessToken": "jwt",
  "expiresAt": "2026-06-17T19:00:00+00:00",
  "user": {
    "id": "00000000-0000-0000-0000-000000000000",
    "firstName": "Taylor",
    "lastName": "Parent",
    "email": "parent@example.com",
    "role": "Parent"
  }
}
```

Validation failures return `400 Bad Request`. Duplicate email is treated as a validation failure.

Public registration is disabled by default until the Parent portal is implemented. When explicitly enabled in a local environment, new registrations can create only `Parent`. Privileged roles are created through the controlled SuperAdmin invitation workflow described below.

### Login

`POST /api/auth/login`

Request:

```json
{
  "email": "parent@example.com",
  "password": "StrongPassword123!"
}
```

Successful response: `200 OK` with the same auth response shape as registration.

Invalid credentials or inactive users return `401 Unauthorized`. This legacy route cannot authenticate Admin or SuperAdmin accounts; privileged accounts use the dedicated Admin login/MFA flow. Admin password recovery always returns a generic request response, stores only a token hash, and revokes existing sessions when a reset completes.

## Administrative Identity

The following routes require the `SuperAdmin` policy unless marked public:

- `GET /api/admin/users`
- `PUT /api/admin/users/{id}`
- `GET/POST /api/admin/invitations`
- `POST /api/admin/invitations/{id}/reissue`
- `POST /api/admin/invitations/{id}/revoke`
- `POST /api/admin/users/{id}/revoke-sessions`
- `GET /api/admin/activity`
- `POST /api/admin-invitations/inspect` (public)
- `POST /api/admin-invitations/accept` (public)

Invitations are email-address-bound, expire after the configured interval, and can be used once. The generated secret is returned only when an invitation is created or reissued. It is sent in the acceptance page URL fragment and submitted in a JSON request body; SQL stores only its SHA-256 hash. The demo workflow presents a link for an authorized SuperAdmin to copy manually. Automated email delivery is deferred.

SuperAdmins cannot change their own role or active status, and the final active SuperAdmin cannot be demoted or deactivated. Identity-management actions create append-only activity records containing safe summaries and correlation identifiers, not credentials or invitation secrets.

## Athletes

- `GET /api/v1/athletes`
- `GET /api/v1/athletes/{id}`
- `POST /api/v1/athletes`
- `PUT /api/v1/athletes/{id}`
- `POST /api/v1/athletes/{id}/consents`
- `POST /api/v1/athletes/{id}/documents`

## Events

- `GET /api/v1/events`
- `GET /api/v1/events/{id}`
- `POST /api/v1/events`
- `PUT /api/v1/events/{id}`

## Products

- `GET /api/v1/products`
- `GET /api/v1/products/{id}`
- `POST /api/v1/products`
- `PUT /api/v1/products/{id}`

## Orders

- `GET /api/v1/orders`
- `GET /api/v1/orders/{id}`
- `POST /api/v1/orders`
- `POST /api/v1/orders/{id}/payment`

## Donations

- `GET /api/v1/donations`
- `POST /api/v1/donations`
- `GET /api/v1/donations/{id}`

## Testimonials

- `GET /api/v1/testimonials`
- `POST /api/v1/testimonials`
- `PUT /api/v1/testimonials/{id}/status`

## Contact

- `POST /api/v1/contact`

## Feedback

- `POST /api/v1/feedback`
