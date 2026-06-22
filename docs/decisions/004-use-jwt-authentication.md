# ADR 004: Use JWT Authentication

## Status

Accepted

## Context

The API must authenticate the Next.js server today and may support mobile clients later without depending on a web framework session store.

## Decision

Issue signed JWT bearer tokens after successful login and validate issuer, audience, lifetime, signing key, and signature in ASP.NET Core.

## Reasons

JWT bearer authentication is client-neutral, integrates with ASP.NET Core policies, and carries identity/role claims needed to begin authorization.

## Alternatives Considered

Opaque server sessions, API keys, or an external identity provider.

## Consequences

The API can serve web and future mobile clients. Tokens remain valid until expiration unless additional revocation infrastructure is added. Sensitive claims and secrets must never be placed in token payloads or logs.
