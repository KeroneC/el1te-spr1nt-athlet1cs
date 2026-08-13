# Azure Infrastructure

These Bicep files describe isolated `demo` and `production` environments: one shared Linux App Service plan per environment, separate Next.js and ASP.NET Core apps, Azure SQL, private Blob media, Key Vault, Azure Communication Services Email, and capped Application Insights. The API uses managed identity for SQL, Blob, and Key Vault. East US 2 is preferred, but a nearby region may be used when Azure reports a capacity restriction for one of these SKUs.

`main.bicep` is resource-group scoped and composes modules under `modules`. Names are deterministic within the resource group and parameterized by project prefix and environment. The example parameter files document safe demo and production defaults; secrets are supplied only by protected GitHub Environments and Key Vault.

Production protection includes Blob/container soft delete, Blob versioning with old-version lifecycle cleanup, explicit SQL point-in-time retention, deletion locks, a prepared custom email domain, and indexing disabled. CSP starts report-only and browser telemetry starts disabled; enforcement, telemetry, custom URLs, verified email sending, internal store navigation, checkout, and indexing remain independent release gates.

B1 App Service, Basic SQL, and Standard LRS storage are conservative grant-funded defaults, not free guarantees. The deployment workflow configures a $125 monthly resource-group budget. SQL public access and its temporary administrator support migration/bootstrap only.

Validate without signing in when Azure CLI with Bicep is installed:

```powershell
az bicep build --file infra/main.bicep
```

Use the manually approved GitHub `demo` or `production` Environment workflow rather than deploying from a workstation. Production values and secrets must never be copied from demo. See [Azure deployment](../docs/architecture/future-azure-deployment.md), [Azure setup](../docs/guides/future-azure-setup.md), and the [production launch checklist](../docs/guides/production-launch-checklist.md).
