# ADR 008: Use GitHub Actions for CI/CD

## Status

Accepted

## Context

The code is hosted and reviewed in GitHub and needs repeatable PR/main validation plus future Azure integration.

## Decision

Use GitHub Actions for CI and a future OIDC-authenticated manual deployment workflow.

## Reasons

Checks integrate with pull requests and branch protection, runners support .NET/Node/Azure tools, and OIDC avoids stored Azure client secrets.

## Alternatives Considered

Azure DevOps Pipelines, another hosted CI service, or local-only scripts.

## Consequences

The project gains visible automated gates and artifacts but depends on Actions availability, action supply-chain review, runner minutes, and careful secret/environment governance.
