# Azure Demo Setup and Release

## One-time organization setup

1. Confirm the nonprofit sponsorship owner, renewal date, preferred Azure region availability, and billing contacts. East US 2 is preferred, but the initial July 13, 2026 provisioning attempt found the selected Azure SQL SKU unavailable in East US 2 and East US; Central US accepted it as the nearest validated fallback.
2. Create the demo resource group and GitHub OIDC Entra application. Grant resource-group Contributor plus User Access Administrator so Bicep can create scoped role assignments.
3. Add a federated credential restricted to this repository and the GitHub `demo` Environment.
4. Protect that Environment with required reviewers and `main` deployment rules.
5. Configure Environment variables: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_DEPLOYMENT_PRINCIPAL_OBJECT_ID`, `AZURE_RESOURCE_GROUP=el1te-demo-central-rg`, `AZURE_LOCATION=centralus`, `RESOURCE_NAME_PREFIX`, `SQL_ADMIN_LOGIN`, `BUDGET_CONTACT_EMAIL`, `BOOTSTRAP_ADMIN_FIRST_NAME`, and `BOOTSTRAP_ADMIN_LAST_NAME`.
6. Configure secrets: `SQL_ADMIN_PASSWORD`, `DATABASE_MIGRATION_CONNECTION_STRING`, `BOOTSTRAP_ADMIN_EMAIL`, and `BOOTSTRAP_ADMIN_PASSWORD`. The JWT key is generated directly in Key Vault.

## First deployment

1. Merge only after every CI check passes on `main`; record the successful CI run ID.
2. Run **Deploy Azure Demo** with approval acknowledged and `infrastructure_only=true`.
3. Review Bicep what-if, resource names, tags, budget, SQL networking, and workflow summary.
4. Connect as the authorized SQL administrator and execute the contained-user grants documented in [Azure Demo Continuous Delivery](../architecture/future-azure-deployment.md).
5. Rerun the same CI run with `infrastructure_only=false`, `managed_identity_sql_ready=true`, and `bootstrap_admin=true` for the first release only.
6. Sign in, change the bootstrap password, upload a disposable image, publish an album, verify public retrieval, and remove disposable records.
7. Confirm `/health/ready`, `/rgnhof`, secure cookies, CORS, Blob persistence, Application Insights, migration history, and firewall cleanup.

The first July 2026 launch required two operational hardening fixes now captured in source: preserve hidden `.next` files in the web artifact, and allow 600 seconds for B1 Linux cold starts. Do not remove those controls without testing a fresh App Service instance.

## Later releases and rollback

Promote a selected successful `main` CI run with `bootstrap_admin=false`. Never deploy an unverified branch artifact. To roll back code, rerun a retained earlier successful release bundle. Do not downgrade the database; deploy corrective forward migrations. Record URLs, commit SHA, CI/deployment run IDs, observed monthly cost, and incidents in the release notes.

## Production environment

Create a separate `production` resource group, OIDC federated credential, and protected GitHub Environment. Do not reuse demo identifiers or secrets. Configure the same base Azure variables with production values plus the Square Production URLs/Key Vault secret URIs and the promotion variables documented by the workflow. Require manual approval on the Environment before the first production run.

Run **Deploy Azure Production** in infrastructure-only mode first. The workflow keeps custom URLs, custom email, store preview, checkout, and indexing independently disabled. It can later stage the API domain without moving the public web domains, and its explicit prelaunch checkout mode remains noindex on the Azure web hostname. Follow [Production cutover operations](production-cutover-operations.md) for managed identity, DNS/TLS, email DNS, data promotion, restore testing, stocktake, and final launch gates.

Deployment slots remain deferred. Production content promotion is deliberately reviewed and idempotent rather than automatic.
