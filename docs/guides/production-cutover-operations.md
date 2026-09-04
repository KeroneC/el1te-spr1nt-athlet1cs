# Production Cutover Operations

This runbook covers the isolated production environment and the reversible launch of the El1te website. It does not authorize a DNS switch, enable checkout, or approve policy/tax decisions by itself.

## Environment isolation

The GitHub `production` Environment owns production-only Azure OIDC values, database migration access, bootstrap credentials, Square Production identifiers, and Key Vault secret URIs. Do not copy demo tokens, webhook keys, users, orders, or connection strings into it. The **Deploy Azure Production** workflow accepts only a successful immutable artifact from a `main` push and requires Environment approval. The empty production resource group and its scoped OIDC identity must be created once before the first run; the workflow then provisions the isolated resources and a separate `$125` monthly budget with 50%, 75%, 90%, and 100% notifications.

Configure these Environment **variables** before the first infrastructure run: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_DEPLOYMENT_PRINCIPAL_OBJECT_ID`, `AZURE_RESOURCE_GROUP`, `AZURE_LOCATION`, `RESOURCE_NAME_PREFIX`, `SQL_ADMIN_LOGIN`, `BUDGET_CONTACT_EMAIL`, `BOOTSTRAP_ADMIN_FIRST_NAME`, and `BOOTSTRAP_ADMIN_LAST_NAME`. Add `SQUARE_LOCATION_ID`, `SQUARE_WEBHOOK_NOTIFICATION_URL`, `SQUARE_CHECKOUT_RETURN_URL`, `SQUARE_PRELAUNCH_CHECKOUT_RETURN_URL`, `SQUARE_ACCESS_TOKEN_SECRET_URI`, and `SQUARE_WEBHOOK_SIGNATURE_KEY_SECRET_URI` only from the independent Square Production setup; the last two values point to production Key Vault and never contain credentials. The prelaunch return URL must use the production Azure web hostname. The final return URL is `https://www.el1tespr1ntathlet1cs.org/shop/order-confirmation`.

Configure these Environment **secrets**: `SQL_ADMIN_PASSWORD`, `DATABASE_MIGRATION_CONNECTION_STRING`, `BOOTSTRAP_ADMIN_EMAIL`, and `BOOTSTRAP_ADMIN_PASSWORD`. After bootstrap, rotate/remove the bootstrap password according to the launch record. The promotion workflow additionally needs the non-password Azure AD connection/Blob values named `PROMOTION_SOURCE_CONNECTION`, `PROMOTION_DESTINATION_CONNECTION`, `PROMOTION_SOURCE_BLOB_SERVICE_URI`, `PROMOTION_DESTINATION_BLOB_SERVICE_URI`, `PROMOTION_SOURCE_RESOURCE_GROUP`, `PROMOTION_SOURCE_SQL_SERVER`, `PROMOTION_DESTINATION_SQL_SERVER`, and `PRODUCTION_BOOTSTRAP_USER_ID`.

Deploy in stages:

1. Run `infrastructure_only=true`. Leave custom domains, custom email, preview, checkout, and indexing disabled.
2. Create the production API managed-identity SQL user and rerun with `managed_identity_sql_ready=true`.
3. Bootstrap one fresh production SuperAdmin. Do not promote demo users.
4. Verify `updates.el1tespr1ntathlet1cs.org` ownership, SPF, and both DKIM records in Azure before selecting `use_verified_email_domain`. That deployment creates the `orders` sender identity, links the custom domain to Communication Services, and configures `orders@updates.el1tespr1ntathlet1cs.org`; do not create an untracked sender manually in the portal.
5. Create the `api` CNAME and `asuid.api` TXT validation record while `www` and the apex remain on Squarespace. Select `configure_api_domain` to bind only the API hostname and managed TLS.
   Managed-certificate issuance is asynchronous. The deployment polls the named certificate until Azure exposes its thumbprint, then binds SNI TLS; rerunning the same request is safe.
6. Promote reviewed public data, enter physical inventory, and complete private smoke testing. Leave indexing disabled.
7. For the single supervised Square Production test, select `prelaunch_checkout_test`, `configure_api_domain`, verified email, enforced CSP, browser analytics, internal store preview, and checkout. The workflow requires the Azure web return URL, rejects indexing and public web-domain changes, and does not accept the final launch acknowledgement in this mode. Disable checkout again after the refund test.
8. At cutover, move `www` and the apex to Azure and select `configure_public_web_domains`. Public checkout and indexing require both API and web domains, monitoring, verified email, enforced CSP, and the final launch acknowledgement.

## DNS and archive sequence

- `www.el1tespr1ntathlet1cs.org` is canonical.
- `el1tespr1ntathlet1cs.org` binds to the web app and the application returns a permanent `308` redirect to `www`, preserving path and query.
- `api.el1tespr1ntathlet1cs.org` binds only to the API app; Square Production uses its exact `/api/webhooks/square` URL.
- Move the current Squarespace site to `archive.el1tespr1ntathlet1cs.org` before changing `www`. Retain it for 30 days, then remove it after launch acceptance.

App Service must show each hostname as validated before its managed certificate is requested. The API domain is staged independently so Square webhooks can be tested without moving the public website. The Azure web hostname remains the private-test origin until the supervised `www`/apex cutover.

## Data promotion

1. Run **Promote Reviewed Production Content** with `command=export`. For the initial catalog staging pass, also set `include_all_products=true` so all draft and published product graphs are available for review.
2. Download its manifest artifact. Review records, dependencies, status, and Blob SHA-256 values.
3. Set `include` only for approved public records and selected catalog content. The manifest hash intentionally excludes the review flags while protecting all record data and hashes.
4. Commit the reviewed manifest under `launch/manifests/` through a pull request.
5. Run the workflow with `command=apply`, `force_products_draft=true`, and acknowledge the review. It performs a dry run first. Leave the force input off for ordinary later selective promotions unless the same safety behavior is intended.
6. Confirm the report, Blob hashes, rewritten production media URLs, fresh production uploader mapping, Draft/non-featured product state, and zero quantities on every promoted variant.

The importer does not delete unrelated destination records and is idempotent. Users, invitations, private submissions, athletes, documents, orders, refunds, webhooks, outbox records, telemetry, and secrets are prohibited. The shelved All-American archive records must remain excluded from the launch manifest.

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
