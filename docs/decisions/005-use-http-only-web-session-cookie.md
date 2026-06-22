# ADR 005: Store the Web Session in an HttpOnly Cookie

## Status

Accepted

## Context

The admin browser needs a durable session, but exposing a bearer token to JavaScript increases the impact of script injection.

## Decision

Have the Next.js server store the API JWT in an HttpOnly, SameSite Lax cookie, Secure in production, and forward it server-side.

## Reasons

Client code cannot read an HttpOnly cookie. Same-origin Route Handlers provide a narrow browser-to-API boundary and centralized session cleanup.

## Alternatives Considered

`localStorage`, `sessionStorage`, a readable cookie, or returning the JWT to client state.

## Consequences

The token stays outside browser JavaScript and private API work remains server-side. Cookie security, CSRF-aware design, TLS, XSS prevention, and backend authorization are still required. Logout removes the cookie but does not revoke the JWT.
