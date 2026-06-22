# Azure Infrastructure Preparation

These Bicep files describe a future single `demo` environment: one Linux App Service plan, separate Next.js and ASP.NET Core Web Apps, an Azure SQL logical server/database, and a system-assigned identity on the API. Nothing in this directory creates resources by itself.

`main.bicep` is resource-group scoped and composes modules under `modules`. Names are deterministic within the resource group and parameterized by project prefix and environment. `parameters/demo.example.bicepparam` contains placeholders and reads secure values from environment variables.

Phase 6B must review pricing and SKUs before deployment. B1 App Service and Basic SQL are low-cost starting defaults, not free guarantees. SQL public access and the temporary SQL administrator support initial migration/bootstrap only and should be tightened after managed identity database access is established.

Validate without signing in when Azure CLI with Bicep is installed:

```powershell
az bicep build --file infra/main.bicep
```

Do not run an Azure deployment during Phase 6A. See [future Azure deployment](../docs/architecture/future-azure-deployment.md) and [future Azure setup](../docs/guides/future-azure-setup.md).
