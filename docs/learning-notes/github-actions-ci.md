# GitHub Actions CI

## What Was Built

`.github/workflows/ci.yml` validates backend, frontend, documentation, and secret safety for pull requests and `main`. It also creates short-lived API, web, test-result, and migration artifacts.

## Why It Exists

Review cannot reliably detect compile errors, migration drift, broken links, dependency installation failures, or secrets. CI makes the same checks repeatable before and after merge.

## Important Files

- `.github/workflows/ci.yml`
- `.github/dependabot.yml`
- `scripts/validation/validate-docs.ps1`
- `scripts/artifacts`

## How It Works

A **workflow** is a YAML automation definition. A **trigger** starts it. A **job** is an isolated runner assignment; a **step** is one command/action inside it. A **runner** is the temporary Ubuntu machine. An **action** is reusable automation such as checkout or setup-dotnet. An **artifact** is retained output, unlike a dependency **cache**, which only speeds later runs.

**Concurrency** cancels obsolete CI for the same ref. A GitHub **environment** groups future deployment protection/configuration. A **secret** is encrypted sensitive configuration; a repository **variable** stores non-secret configuration. Every command communicates success/failure through its **exit code**.

## Request or Data Flow

```text
Git change -> trigger -> four parallel jobs -> commands/actions
  -> nonzero exit fails check -> branch protection blocks merge
  -> successful build outputs become seven-day artifacts
```

## How to Test It

Run the same commands locally, validate YAML with an available parser, then open a pull request and inspect each job. `workflow_dispatch` supports a manual rehearsal. Do not test deployment from Phase 6A.

## Common Problems

- Restore fails: inspect NuGet source/network and lockfiles.
- EF drift fails: create and review a migration; never apply one in CI.
- Frontend build lacks `API_BASE_URL`: CI supplies a non-routable validation URL.
- Gitleaks false positive: first remove/rotate real secrets; if truly synthetic, add a narrowly documented fingerprint suppression rather than a broad path exclusion.
- Workflow skipped: confirm the changed path is in the trigger list.

## Concepts to Study

Immutable runners, least-privilege permissions, artifacts versus caches, status checks, supply-chain pinning, OIDC, concurrency, and exit codes.

## What Was Intentionally Deferred

No automatic deployment, GitHub environment configuration, Azure credentials, or auto-merge is included.
