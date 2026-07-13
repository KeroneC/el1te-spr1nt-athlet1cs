# Azure Demo Infrastructure

These Bicep files describe the approved `demo` environment: one shared Linux App Service plan, separate Next.js and ASP.NET Core apps, Azure SQL, private Blob media, Key Vault, and capped Application Insights. The API uses managed identity for SQL, Blob, and Key Vault. East US 2 is preferred, but a nearby region may be used when Azure reports a capacity restriction for one of these SKUs.

`main.bicep` is resource-group scoped and composes modules under `modules`. Names are deterministic within the resource group and parameterized by project prefix and environment. `parameters/demo.example.bicepparam` contains placeholders and reads secure values from environment variables.

B1 App Service, Basic SQL, and Standard LRS storage are conservative grant-funded defaults, not free guarantees. The deployment workflow configures a $125 monthly resource-group budget. SQL public access and its temporary administrator support migration/bootstrap only.

Validate without signing in when Azure CLI with Bicep is installed:

```powershell
az bicep build --file infra/main.bicep
```

Use the manually approved GitHub `demo` Environment workflow rather than deploying from a workstation. See [Azure deployment](../docs/architecture/future-azure-deployment.md) and [Azure setup](../docs/guides/future-azure-setup.md).
