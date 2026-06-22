# Future Azure Setup (Phase 6B)

These are future steps. None has been completed by Phase 6A.

1. Activate the Microsoft nonprofit Azure grant under the organization, confirm subscription ownership, and avoid personal billing ownership.
2. Review current prices and grant terms; create conservative budgets and alerts before resources.
3. Register required Azure resource providers if the subscription requires it.
4. Create the target resource group and an Entra application/federated credential for GitHub OIDC with least-privilege resource-group scope.
5. Create the GitHub `demo` Environment. Add required reviewers, `main` deployment rules, and environment history controls.
6. Add documented non-secret variables and encrypted secrets from [future Azure deployment](../architecture/future-azure-deployment.md). Never use a publish profile as the preferred identity.
7. Install/verify Azure CLI Bicep and run `az bicep build --file infra/main.bicep` plus a what-if deployment.
8. Review names, region availability, SKUs, tags, SQL networking, TLS, health paths, and projected cost.
9. Run the manual workflow with explicit Phase 6B acknowledgment and its default infrastructure-only mode to provision reviewed resources.
10. Configure temporary, narrow firewall access for migration/bootstrap and remove it afterward.
11. Connect as an authorized Entra SQL administrator, create the API managed identity as a contained database user, and grant only required permissions. Confirm the passwordless API connection, then rerun the workflow with infrastructure-only disabled and managed-identity readiness acknowledged.
12. Decide how the first production SuperAdmin is created through a secure one-time administrative process; Development seeding must not run in Production.
13. Run the migration bundle with an execution-time connection string. Verify migration history before deploying code.
14. Deploy the API artifact, wait for `/health/ready`, and run API smoke tests.
15. Deploy the standalone web artifact and run frontend smoke tests.
16. Review logs without secret values, lock down temporary SQL access, and record deployment results.
17. Add custom domains/certificates later after the demo deployment is stable.

Do not begin these steps until grant activation, organizational ownership, security review, and Phase 6B approval are explicit.
