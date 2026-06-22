# ADR 006: Separate Public and Admin APIs

## Status

Accepted

## Context

Anonymous consumers should see only current public content, while administrators need drafts, inactive records, private submissions, broader filters, and mutations.

## Decision

Use anonymous `/api/public` routes with public DTOs and visibility predicates, and protected `/api/admin` routes with admin DTOs and the `CmsAdmin` policy.

## Reasons

Separate contracts make privacy and lifecycle rules visible, reduce accidental over-fetching, and permit different pagination/filter semantics.

## Alternatives Considered

One endpoint shape with optional authorization or client-side filtering.

## Consequences

Some mapping and query logic is duplicated intentionally. Public rules remain enforceable in SQL and contact submissions have no public read path. Changes must consider both contracts when appropriate.
