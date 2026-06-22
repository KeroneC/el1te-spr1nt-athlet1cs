# ADR 002: Use a Layered Backend Architecture

## Status

Accepted

## Context

HTTP, use-case logic, domain data, and SQL Server concerns change for different reasons and need independent tests.

## Decision

Separate the backend into Core, Application, Infrastructure, and API projects. Core owns entities/contracts, Application owns use cases, Infrastructure implements persistence/security details, and API owns HTTP/composition.

## Reasons

The structure makes dependency direction visible, keeps controllers thin, permits service tests without a server, and localizes EF Core.

## Alternatives Considered

A single API project, feature folders without project boundaries, or microservices.

## Consequences

The design adds projects, interfaces, mapping, and navigation overhead. It supports a modular monolith without claiming a textbook Clean Architecture: Core currently includes DTOs and some repository abstractions.
