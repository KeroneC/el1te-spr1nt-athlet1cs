# ADR 007: Use Next.js with ASP.NET Core

## Status

Accepted

## Context

The project needs a modern web experience and a durable API that owns authentication, authorization, business rules, and persistence for more than one possible client.

## Decision

Use Next.js for web rendering and interaction, and ASP.NET Core as the backend source of truth.

## Reasons

Next.js provides App Router server rendering and a server-side web session boundary. ASP.NET Core provides typed controllers, middleware, policy authorization, dependency injection, and EF Core integration. The API remains reusable by future mobile clients.

## Alternatives Considered

A Next.js-only full stack, an ASP.NET-rendered UI, or separate client-specific backends.

## Consequences

The system gains clear client/server boundaries and technology strengths, but local development runs two servers and must manage HTTPS trust, CORS, environment configuration, and synchronized contracts.
