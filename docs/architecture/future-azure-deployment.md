# Azure Demo Continuous Delivery

Phase 6B targets a public, search-excluded demo in East US 2. It does not create a production domain or automatic deployment.

```mermaid
flowchart LR
  GH["Approved GitHub release"] --> RG["Azure demo resource group"]
  Browser --> Web["Next.js App Service"] --> API["ASP.NET Core App Service"]
  API --> SQL["Azure SQL"]
  API --> Blob["Private Blob media"]
  API --> KV["Key Vault JWT key"]
  Web --> AI["Capped Application Insights"]
  API --> AI
```

The API system-assigned identity has Blob contributor access only on the media account, secret-read access only on the application vault, and contained-user SQL read/write access. Media remains private and is streamed through `/media/{id}`. Development continues to use `LocalMediaStorage`.

CI creates one immutable release bundle after every successful `main` push: API ZIP, standalone web ZIP, EF bundle, manifest, and SHA-256 checksums. The manual deployment accepts a CI run ID, verifies that it is a successful `main` push, checks out the matching SHA, and promotes those artifacts without rebuilding.

The first run is infrastructure-only. An authorized SQL administrator creates the API identity as a contained user:

```sql
CREATE USER [<api-app-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<api-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<api-app-name>];
```

The full run opens a runner-specific SQL firewall rule, applies the reviewed migration bundle, deploys and checks the API, optionally runs the idempotent SuperAdmin bootstrap command, deploys the web app, and removes temporary SQL access in cleanup. Application rollback redeploys a retained prior artifact. Database changes are forward-only and must remain backward compatible.

Key Vault creates no committed or GitHub-held JWT key. The deployment identity initializes the secret directly in Azure when absent; the API reads it through a Key Vault reference. The workflow also configures a $125 monthly budget with 50/75/90/100 percent alerts.
