# API Endpoints

## Phase 9 Media and Gallery

- Admin media: `GET/POST /api/admin/media`, `GET/PUT/DELETE /api/admin/media/{id}`
- Admin albums: `GET/POST /api/admin/gallery-albums`, `GET/PUT/DELETE /api/admin/gallery-albums/{id}`
- Album images: `POST /api/admin/gallery-albums/{id}/media`, `PUT/DELETE /api/admin/gallery-albums/{id}/media/{albumMediaId}`, `PUT /api/admin/gallery-albums/{id}/media/order`
- Public albums: `GET /api/public/gallery-albums`, `GET /api/public/gallery-albums/{slug}`
- Active image bytes: `GET /media/{id}`

All Admin routes use the existing `CmsAdmin` policy. Upload is multipart form data; physical paths and storage keys are never returned.

Liveness is available at `/health`; database readiness is available at `/health/ready`. The legacy descriptive route remains at `/api/v1/health`. Authentication routes are implemented under `/api/auth`. Public CMS routes live under `/api/public`, and protected CMS routes live under `/api/admin`.

## Commerce Foundation

- Commerce integration health: `GET /health/commerce`
- Square webhook receiver: `POST /api/webhooks/square`

The webhook route returns `404 Not Found` while `Store:Enabled` is false. When enabled, it requires Square's exact HMAC signature, rejects oversized bodies, persists only safe event metadata and a payload hash, and deduplicates the Square event ID. Checkout and tracking endpoints remain deferred.

## Public Store

- `GET /api/public/store/products`
- `GET /api/public/store/products/{slug}`

Both routes return `404 Not Found` while `Store:Enabled` is false. The list accepts `search`, `category`, `availability`, `page`, and `pageSize`. Public DTOs expose only published products, active options/variants/media, minor-unit prices, and `InStock`, `LowStock`, or `SoldOut`; exact quantities and SKUs remain private. Cart state is currently browser-local and non-personal, so there is no cart API in this phase.

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

The following one-time import routes require the `SuperAdmin` policy:

- `GET /api/admin/store/square-import/preview`
- `POST /api/admin/store/square-import`

The import creates unpublished local drafts, retains Square source IDs for idempotency, copies trusted Square-hosted images into El1te Media, and never imports customers, payments, or old orders. Exact inventory is private Admin data. Public store routes are feature-gated and `Store:Enabled=false` remains required until cutover.

## Auth

Implemented:

- `POST /api/auth/register`
- `POST /api/auth/login`

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

New public registrations default to `Parent`. Privileged roles are created through the controlled SuperAdmin invitation workflow described below.

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

Invalid credentials or inactive users return `401 Unauthorized`.

## Administrative Identity

The following routes require the `SuperAdmin` policy unless marked public:

- `GET /api/admin/users`
- `PUT /api/admin/users/{id}`
- `GET/POST /api/admin/invitations`
- `POST /api/admin/invitations/{id}/reissue`
- `POST /api/admin/invitations/{id}/revoke`
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
