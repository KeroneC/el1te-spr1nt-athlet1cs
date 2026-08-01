# Security

## Data Sensitivity

The platform may handle sensitive information about minors, parents, coaches, medical notes, emergency contacts, proof-of-age documents, orders, donations, testimonials, contact messages, and feedback. Security decisions should assume this data is private unless explicitly approved for publication.

## Secrets

No real secrets belong in the repository. Use user secrets locally, protected GitHub Environment secrets for deployment-only values, and Azure Key Vault or managed identity for the demo API. The JWT signing key is generated directly in Key Vault and is not stored as a GitHub secret.

The JWT signing key is `Jwt:Key`. It must be supplied through user secrets or environment-specific secret storage and must be at least 32 characters long. `appsettings*.json` may contain safe `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpiresMinutes` values, but never a real signing key.

## Authentication

Passwords are stored only as BCrypt hashes. Auth endpoints return DTOs and never return `PasswordHash` or EF entities directly.

Anonymous Parent registration is disabled by default until the Parent portal is implemented. When explicitly enabled for local testing, it can create only the `Parent` role. Admin and SuperAdmin accounts use one-time, email-bound invitations created by an active SuperAdmin. Invitation secrets are stored only as SHA-256 hashes, expire after 72 hours, and are placed in a browser URL fragment so routine HTTP request logging does not receive them. The demo returns each invitation link once for trusted manual delivery.

Admin login has distributed IP/account throttles and a 15-minute lock after five failed passwords in 15 minutes. SuperAdmins complete a one-time email-code challenge. Password recovery uses one-time, hash-at-rest URL-fragment tokens and increments the account security version, as do role changes, deactivation, and explicit session revocation. Every authenticated API request compares the JWT security version with the current active user record.

Current roles are:

- `Parent`
- `Athlete`
- `Coach`
- `Admin`
- `SuperAdmin`

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

Unexpected server failures expose only a safe support reference. API request telemetry uses route templates and clears user/session context; Next.js error instrumentation records a safe route template and production digest rather than the raw request. Demo browser analytics are cookie-free, public-route-only, and exclude Admin activity, identifiers, queries, slugs, form values, cart customization, automatic dependencies, and raw exceptions. Raw telemetry and the support workbook stay behind Azure RBAC. See [Observability and support references](guides/observability-support.md).

## Administrative Audit

Invitation creation, reissue, revocation, acceptance, and privileged role/active-status changes write append-only activity records. Only SuperAdmins may read the activity view. The application provides no update or delete endpoint for activity records.

The initial activity scope intentionally excludes CMS edits and private future registration records. Expand it through reviewed, safe summaries rather than full entity or request snapshots.

## Future Audit Goals

Before production use, extend audit coverage for authentication events, CMS changes, athlete profile changes, consent records, document access, payments, refunds, and donation updates.
