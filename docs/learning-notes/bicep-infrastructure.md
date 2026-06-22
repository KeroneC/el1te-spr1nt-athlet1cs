# Bicep Infrastructure

## What Was Built

`infra` contains a resource-group-scoped Bicep entry point, modules for App Service and Azure SQL, and a safe example parameter file.

## Why It Exists

Infrastructure as code makes the intended cloud topology reviewable and repeatable before an account exists. Defining resources does not create them; deployment is a separate authenticated action.

## Important Files

- `infra/main.bicep`
- `infra/modules/*.bicep`
- `infra/parameters/demo.example.bicepparam`
- `infra/README.md`

## How It Works

A Bicep **parameter** is deployment input; secure parameters are not written into templates. A **module** composes a focused resource definition. An **output** passes names, URLs, and identity IDs to deployment automation. Resource references create dependencies, so Azure deploys in a valid order. Re-running the same template is intended to converge on the declared state (idempotency), though data and destructive property changes still require review.

## How to Test It

With Azure CLI/Bicep installed, `az bicep build --file infra/main.bicep` compiles syntax without login. Review generated ARM JSON but do not commit it. No Phase 6A command should run `az deployment`.

## Common Problems

- Invalid resource name: adjust the prefix within documented limits.
- Unsupported runtime/API version: confirm current Azure regional support before Phase 6B.
- Secure parameter absent: set the documented environment variable at validation/deployment time.
- API readiness unhealthy after provision: managed identity still needs a contained SQL user and grants.

## Concepts to Study

Declarative infrastructure, scopes, modules, parameters, outputs, dependency graphs, ARM resource IDs, managed identity, and idempotency.

## What Was Intentionally Deferred

No resource group, budget, OIDC trust, SQL identity user, custom domain, network hardening, or Azure resource was created.
