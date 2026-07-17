# ADR 012: Use Hashed Admin Invitations and Append-Only Activity Records

## Status

Accepted

## Context

Privileged accounts must not be created through public registration or shared credentials. SuperAdmins also need an attributable history of invitations, role changes, and active-status changes without placing passwords, tokens, or sensitive request bodies in logs.

## Decision

- Only an active database-authoritative SuperAdmin may create, reissue, revoke, or inspect administrative account management data.
- Invitations are bound to one normalized email address and an Admin or SuperAdmin role.
- The API returns a raw invitation secret only when the invitation is created or reissued. SQL stores only its SHA-256 hash.
- Invitation links place the secret after a URL fragment. Browsers do not send fragments in HTTP requests, keeping the secret out of routine server request logs.
- Invitations expire after 72 hours and are single-use. Reissuing replaces the hash and invalidates the previous link.
- The demo uses an explicit copy-link delivery step. Azure Communication Services delivery is deferred to Stage 2 and can be added behind this workflow without changing invitation persistence.
- Privileged account records are deactivated rather than deleted. A SuperAdmin cannot change their own role/status or remove the final active SuperAdmin.
- Successful invitation and privileged-account mutations create append-only `AdminActivityLogs` records. The application exposes no update or delete operation for these records.

## Consequences

The workflow is usable before production email is provisioned and keeps invitation secrets out of SQL and normal URL logs. Manual delivery requires the SuperAdmin to use a trusted channel and confirm the recipient. Audit records contain administrator identity and recipient email because those values are necessary for accountability; access is therefore restricted to SuperAdmins and governed by retention policy.
