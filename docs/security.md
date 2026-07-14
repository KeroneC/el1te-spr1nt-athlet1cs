# Security

## Data Sensitivity

The platform may handle sensitive information about minors, parents, coaches, medical notes, emergency contacts, proof-of-age documents, orders, donations, testimonials, contact messages, and feedback. Security decisions should assume this data is private unless explicitly approved for publication.

## Secrets

No real secrets belong in the repository. Use user secrets locally, protected GitHub Environment secrets for deployment-only values, and Azure Key Vault or managed identity for the demo API. The JWT signing key is generated directly in Key Vault and is not stored as a GitHub secret.

The JWT signing key is `Jwt:Key`. It must be supplied through user secrets or environment-specific secret storage and must be at least 32 characters long. `appsettings*.json` may contain safe `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpiresMinutes` values, but never a real signing key.

## Authentication

Passwords are stored only as BCrypt hashes. Auth endpoints return DTOs and never return `PasswordHash` or EF entities directly.

Public registration defaults to the `Parent` role. This avoids public self-assignment of privileged `Coach` or `Admin` roles; elevated roles should be assigned later through an audited administrative process.

Current roles are:

- `Parent`
- `Athlete`
- `Coach`
- `Admin`

## DTOs and Validation

Do not expose EF entities directly from public endpoints. Add request and response DTOs per use case, validate input at API boundaries, and avoid returning private fields by default.

## Authorization

Authorization must be role-aware and ownership-aware:

- Parents should only access their own athletes and related records.
- Coaches should only access team data needed for their responsibilities.
- Admin access should be audited and limited.
- Public endpoints should never leak private roster, medical, document, payment, or contact data.

## Private Uploads

Proof-of-age documents, medical forms, waivers, and similar uploads should be stored in private blob containers. Access should use short-lived, scoped URLs or streamed API responses after authorization checks.

## CORS

Development CORS allows localhost origins for local Next.js work. Production CORS must be restricted to known frontend domains.

## Logging

Logs should not include passwords, password hashes, JWTs, refresh tokens, payment data, medical notes, emergency contact details, private document URLs, or raw request bodies containing sensitive data.

## Audit Goals

Before production use, add audit trails for authentication events, role changes, athlete profile changes, consent records, document access, payments, refunds, donation updates, and admin actions.
