# ADR 003: Use GUID Identifiers

## Status

Accepted

## Context

Entities need identifiers that can be created before database insertion and shared safely across API clients.

## Decision

Use `Guid` primary keys, initialized in entity base classes.

## Reasons

GUIDs avoid a central sequence, work across disconnected future clients, and do not expose simple record counts in URLs.

## Alternatives Considered

Database-generated integers, long integers, or sortable identifier formats.

## Consequences

Identifiers are globally practical but longer for people and larger in database indexes. GUIDs are not authorization controls and must not be treated as secrets.
