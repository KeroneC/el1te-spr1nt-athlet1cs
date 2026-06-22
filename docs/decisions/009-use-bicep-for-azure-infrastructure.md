# ADR 009: Use Bicep for Azure Infrastructure

## Status

Accepted

## Context

The planned deployment is Azure-specific and should be reproducible rather than assembled manually in the portal.

## Decision

Describe Azure resources with modular Bicep templates.

## Reasons

Bicep is Azure-native, compiles to ARM, supports modules/secure parameters/outputs, and provides direct access to Azure resource capabilities.

## Alternatives Considered

Terraform, ARM JSON, Azure portal-only setup, or application-level SDK provisioning.

## Consequences

Templates are concise and Azure-focused but less portable than Terraform. API-version/runtime support and deployment behavior must still be validated against a real subscription in Phase 6B.
