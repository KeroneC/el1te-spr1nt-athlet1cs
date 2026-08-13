# Production Cutover Operations

This runbook covers the isolated production environment and the reversible launch of the El1te website. It does not authorize a DNS switch, enable checkout, or approve policy/tax decisions by itself.

## Environment isolation

The GitHub `production` Environment owns production-only Azure OIDC values, database migration access, bootstrap credentials, Square Production identifiers, and Key Vault secret URIs. Do not copy demo tokens, webhook keys, users, orders, or connection strings into it. The **Deploy Azure Production** workflow accepts only a successful immutable artifact from a `main` push and requires Environment approval. The empty production resource group and its scoped OIDC identity must be created once before the first run; the workflow then provisions the isolated resources and a separate `$125` monthly budget with 50%, 75%, 90%, and 100% notifications.

Deploy in stages:

1. Run `infrastructure_only=true`. Leave custom domains, custom email, preview, checkout, and indexing disabled.
2. Create the production API managed-identity SQL user and rerun with `managed_identity_sql_ready=true`.
3. Bootstrap one fresh production SuperAdmin. Do not promote demo users.
4. Verify `updates.el1tespr1ntathlet1cs.org` ownership, SPF, and both DKIM records in Azure before selecting `use_verified_email_domain`.
5. Create DNS validation records for `www`, apex, and `api`; then select `use_custom_domains` to add App Service hostnames and managed TLS.
6. Promote reviewed public data, enter physical inventory, and complete private smoke testing. Leave CSP report-only and indexing disabled until demo violations are cleared.
7. Enable enforced CSP, cookie-free browser analytics, checkout, and indexing only at the supervised final cutover. The workflow rejects checkout unless the internal catalog, verified email domain, custom domains, monitoring, and final acknowledgement are all enabled.

## DNS and archive sequence

- `www.el1tespr1ntathlet1cs.org` is canonical.
- `el1tespr1ntathlet1cs.org` binds to the web app and the application returns a permanent `308` redirect to `www`, preserving path and query.
- `api.el1tespr1ntathlet1cs.org` binds only to the API app; Square Production uses its exact `/api/webhooks/square` URL.
- Move the current Squarespace site to `archive.el1tespr1ntathlet1cs.org` before changing `www`. Retain it for 30 days, then remove it after launch acceptance.

App Service must show every hostname as validated before managed certificates are requested. Do not enable canonical URLs early: the Azure hostnames are intentionally retained for private pre-DNS verification.

## Data promotion

1. Run **Promote Reviewed Production Content** with `command=export`.
2. Download its manifest artifact. Review records, dependencies, status, and Blob SHA-256 values.
3. Set `include` only for approved public records and selected catalog content. The manifest hash intentionally excludes the review flags while protecting all record data and hashes.
4. Commit the reviewed manifest under `launch/manifests/` through a pull request.
5. Run the workflow with `command=apply` and acknowledge the review. It performs a dry run first.
6. Confirm the report, Blob hashes, rewritten production media URLs, fresh production uploader mapping, and zero quantities on every promoted variant.

The importer does not delete unrelated destination records and is idempotent. Users, invitations, private submissions, athletes, documents, orders, refunds, webhooks, outbox records, telemetry, and secrets are prohibited.

## Restore and rollback

### Media

Production media has 30-day Blob/container soft delete and versioning. Previous versions expire after 90 days. To undo an overwrite, locate the prior version and promote it as the current Blob. To undo deletion, undelete the Blob/container and then promote the desired version. Verify the SHA-256 value and public media response before closing the incident.

### SQL

Azure SQL point-in-time retention is explicit at 14 days. The production workflow can create a disposable restored database and delete it after verification. A restore is never switched into service automatically. Compare row counts and critical order/inventory records before any planned connection change.

### Application and checkout

- Immediate commerce stop: redeploy with `enable_checkout=false`.
- Navigation rollback: deploy with preview disabled so `STORE_NAVIGATION_MODE=external` restores the legacy Square storefront link.
- Artifact rollback: redeploy a previously successful immutable `main` CI run. Do not roll back database migrations destructively.
- DNS rollback: restore the prior `www`/apex records only if application rollback is insufficient. Keep `api` available for already-paid order/webhook reconciliation.

Never delete completed orders, refunds, inventory adjustments, or Square records during rollback.

## Final observation window

During the DNS switch and first live orders, monitor readiness, web/API 5xx, dependency failures, p95 latency, Square operations, transactional email outcomes, release SHA, and support-reference lookup. Keep a launch owner and rollback owner available and record every intervention with time, release SHA, and outcome.
