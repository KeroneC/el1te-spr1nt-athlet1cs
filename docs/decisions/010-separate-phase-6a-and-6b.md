# ADR 010: Separate Deployment Preparation from Live Provisioning

## Status

Accepted

## Context

The nonprofit Azure grant is pending and no organization-owned subscription is available. Creating resources under a personal account would introduce ownership, billing, and migration risk.

## Decision

Use Phase 6A for CI, artifacts, templates, scripts, and documentation only. Defer authentication, provisioning, database bootstrap, and deployment to approved Phase 6B.

## Reasons

Preparation can be reviewed and tested without cost or credentials. The organization can establish proper ownership, budgets, OIDC, and security before resources exist.

## Alternatives Considered

Deploy immediately to a personal subscription or postpone all deployment work.

## Consequences

The repository becomes deployment-ready earlier, but real Azure assumptions remain unproven until Phase 6B. Documentation must consistently distinguish planned from completed state.
