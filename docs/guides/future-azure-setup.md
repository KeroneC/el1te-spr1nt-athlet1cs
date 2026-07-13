# Azure Demo Setup and Release

## One-time organization setup

1. Confirm the nonprofit sponsorship owner, renewal date, East US 2 availability, and billing contacts.
2. Create the demo resource group and GitHub OIDC Entra application. Grant resource-group Contributor plus User Access Administrator so Bicep can create scoped role assignments.
3. Add a federated credential restricted to this repository and the GitHub `demo` Environment.
4. Protect that Environment with required reviewers and `main` deployment rules.
5. Configure Environment variables: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_DEPLOYMENT_PRINCIPAL_OBJECT_ID`, `AZURE_RESOURCE_GROUP`, `AZURE_LOCATION=eastus2`, `RESOURCE_NAME_PREFIX`, `SQL_ADMIN_LOGIN`, `BUDGET_CONTACT_EMAIL`, `BOOTSTRAP_ADMIN_FIRST_NAME`, and `BOOTSTRAP_ADMIN_LAST_NAME`.
6. Configure secrets: `SQL_ADMIN_PASSWORD`, `DATABASE_MIGRATION_CONNECTION_STRING`, `BOOTSTRAP_ADMIN_EMAIL`, and `BOOTSTRAP_ADMIN_PASSWORD`. The JWT key is generated directly in Key Vault.

## First deployment

1. Merge only after every CI check passes on `main`; record the successful CI run ID.
2. Run **Deploy Azure Demo** with approval acknowledged and `infrastructure_only=true`.
3. Review Bicep what-if, resource names, tags, budget, SQL networking, and workflow summary.
4. Connect as the authorized SQL administrator and execute the contained-user grants documented in [Azure Demo Continuous Delivery](../architecture/future-azure-deployment.md).
5. Rerun the same CI run with `infrastructure_only=false`, `managed_identity_sql_ready=true`, and `bootstrap_admin=true` for the first release only.
6. Sign in, change the bootstrap password, upload a disposable image, publish an album, verify public retrieval, and remove disposable records.
7. Confirm `/health/ready`, `/rgnhof`, secure cookies, CORS, Blob persistence, Application Insights, migration history, and firewall cleanup.

## Later releases and rollback

Promote a selected successful `main` CI run with `bootstrap_admin=false`. Never deploy an unverified branch artifact. To roll back code, rerun a retained earlier successful release bundle. Do not downgrade the database; deploy corrective forward migrations. Record URLs, commit SHA, CI/deployment run IDs, observed monthly cost, and incidents in the release notes.

Custom domains, slots, automatic deployment, and production promotion remain deferred until the demo proves stable.
