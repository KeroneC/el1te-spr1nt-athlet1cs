# API Endpoints

Liveness is available at `/health`; database readiness is available at `/health/ready`. The legacy descriptive route remains at `/api/v1/health`. Authentication routes are implemented under `/api/auth`. Public CMS routes live under `/api/public`, and protected CMS routes live under `/api/admin`.

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

New public registrations default to `Parent` because coach and admin roles must be granted through a controlled future workflow.

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
