# Deployment Documentation

Phase 6B operates a manually approved Azure demo in Central US. Read [CI/CD overview](architecture/cicd-overview.md), [Azure demo continuous delivery](architecture/future-azure-deployment.md), [Azure setup and release](guides/future-azure-setup.md), [observability and support references](guides/observability-support.md), and [store and Square foundation](guides/store-square-foundation.md).

Commerce deployments must keep `Store__Enabled=false` until the documented final cutover. Square access tokens and webhook signature keys belong in Azure Key Vault and enter App Service only through Key Vault references.
