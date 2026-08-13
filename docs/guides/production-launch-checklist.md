# Production Launch Checklist

This checklist is the release gate for the lean public launch: public CMS, Admin, and Square-hosted payment for physically held club inventory with practice/event handoff. Parent registration, Printify, shipping, feedback, and staff-entered tender are excluded.

## Organization approvals

- [ ] A club decision-maker approves the August 8, 2026 Privacy, Accessibility, Terms, and Store Policy content.
- [ ] The gear lead approves every product, image, option, modifier, SKU, price, and published state.
- [ ] The gear lead records the physical count for every active production size/color variant; promoted quantities begin at zero.
- [ ] The club records its intentional Square merchandise tax configuration of zero. No tax rate is hardcoded in El1te.
- [ ] A launch owner and rollback owner are available for the supervised cutover.

## Production isolation and recovery

- [ ] Production has its own resource group, SQL database, Blob account, Key Vault, App Services, monitoring, identities, secrets, and protected GitHub environment.
- [ ] SQL has explicit 14-day point-in-time retention; Blob/container soft delete is 30 days; Blob versioning and 90-day old-version cleanup are enabled; a disposable restore has succeeded.
- [ ] The selective promotion manifest was reviewed; it excludes users, invitations, submissions, athletes, documents, orders, refunds, webhooks, outbox records, telemetry, and secrets.
- [ ] Every selected media object matches its SHA-256 manifest hash and every promoted variant has zero on-hand/reserved stock before stocktake.
- [ ] A fresh production SuperAdmin is bootstrapped; demo and test users are absent.

## Domain, email, and Square

- [ ] `www.el1tespr1ntathlet1cs.org` and `api.el1tespr1ntathlet1cs.org` have valid TLS; apex redirects to `www`.
- [ ] Squarespace is available read-only at `archive.el1tespr1ntathlet1cs.org` for 30 days.
- [ ] `updates.el1tespr1ntathlet1cs.org` passes Azure ownership, SPF, and DKIM checks; order mail uses `orders@updates.el1tespr1ntathlet1cs.org` with the club Gmail as Reply-To.
- [ ] Email operational logs, delivery-status workbook queries, and failure alerts work without engagement tracking.
- [ ] Production Square credentials are in production Key Vault; production return/webhook URLs and signatures are verified independently from Sandbox.
- [ ] Production Square shows the club-approved `$0.00` merchandise tax result for the supervised test order; El1te contains no hardcoded tax rate.

## Final verification and cutover

- [ ] CI, Bicep, migrations, vulnerability scans, Playwright/axe, keyboard/narrow-screen checks, and performance budgets pass for the immutable release SHA.
- [ ] A private production purchase completes payment, webhook, email, tracking, cancellation/refund, and exact inventory restoration.
- [ ] CSP enforcement, cookie-free public analytics, readiness, 5xx/dependency/latency/email alerts, and the support workbook are verified.
- [ ] Production is initially `noindex`; indexing is enabled only after DNS, HTTPS, content, policy, and commerce checks pass.
- [ ] Cutover sets `STORE_NAVIGATION_MODE=internal` and enables checkout. Rollback disables checkout, sets navigation to `external`, and restores DNS without deleting order history.

Follow the staged procedure in [Production cutover operations](production-cutover-operations.md). Record the immutable CI run ID, release SHA, approver, stocktake sign-off, tax approval, policy approval, test-order reference, restore-test result, DNS change time, and rollback owner in the launch record.
