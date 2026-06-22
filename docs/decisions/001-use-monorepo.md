# ADR 001: Use a Monorepo

## Status

Accepted

## Context

The web app, API, tests, and documentation evolve together and frequently change the same contract.

## Decision

Keep them in one Git repository under `apps`, `docs`, and shared repository configuration.

## Reasons

One change can update API behavior, web types, tests, and explanation atomically. Local setup, review, and discovery use one history.

## Alternatives Considered

Separate repositories for frontend/backend or a documentation-only repository.

## Consequences

Cross-layer changes are easier to coordinate and learn. The repository is larger and requires clear ownership boundaries so unrelated modules do not become tightly coupled.
