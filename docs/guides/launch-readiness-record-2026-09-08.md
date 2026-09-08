# Launch Readiness Record — September 8, 2026

This record captures the private-production verification completed before the inventory-dependent public cutover. Production remained `noindex`, public checkout remained disabled, and `www` remained on Squarespace throughout this work.

## Immutable release

- Release SHA: `625dea90c85a266ae9eef8be0f3444901fcd45fa`
- Main CI run: `34268032230` — passed
- Demo deployment: `34268721277` — passed
- Production deployment: `34271036301` — passed
- Reviewed changes: pull requests `#142`, `#143`, and `#144`
- Production release SHA was verified from the deployed App Service settings.

## Launch safeguards

- Production uses the B2 App Service tier with one instance.
- `Store:PublicPreviewEnabled=true` provides the internal read-only catalog preview.
- `Store:Enabled=false` and `Store:CheckoutEnabled=false` prevent commerce operations.
- `PUBLIC_INDEXING_ENABLED=false` emits `noindex, nofollow, nocache` metadata.
- `CSP_MODE=enforce` emits the enforced `Content-Security-Policy` header at runtime.
- Cookie-free public browser analytics and the approved homepage All-American showcase are enabled.
- The production API readiness endpoint returned `healthy` after deployment.

## Homepage image and public performance

- Added the 1600×1067 quality-90 community photograph derivative at 291,146 bytes while retaining the 480px and 960px candidates.
- Responsive source selection was verified at 390px/2×, 768px, 1440px/1×, and 1440px/2×. Phones selected the 960px candidate; only the high-density desktop case selected the 1600px candidate.
- The September 8 private-production audit covered `/`, `/registration`, `/forms`, `/gallery`, `/hall-of-fame`, and `/shop` at a 390×844 viewport with a 2× device scale factor.
- All defined budgets passed. Warm LCP ranged from 156–1,668 ms, CLS was 0 on every route, homepage image transfer was 550,072 bytes, gallery image transfer stayed below 2 MB, and unchanged warm images retransferred 0 bytes.
- Static and managed media returned `public, max-age=86400, stale-while-revalidate=604800`.
- The cold Hall of Fame visit took 8,656 ms LCP because of an Azure/API cold path; its warm LCP was 196 ms. Cold-start variance remains an operational observation rather than a warm-budget failure.

## Square production cleanup

- The supervised `$1 Square Production Checkout Test` product remains preserved as a Draft with its active variant at zero on-hand, zero reserved, and zero available.
- Its successful payment, webhook, email, tracking, cancellation, refund, inventory adjustment, and audit history were retained.
- The Square Production receipt Facebook destination was corrected to `https://www.facebook.com/el1tespr1ntathlet1cs`.
- The existing receipt website, address, and phone were verified without modification.
- The club-approved `$0.00` merchandise-tax decision remains unchanged and is not hardcoded in El1te.

## Recovery and monitoring

- SQL point-in-time retention remains 14 days. A disposable restore reached `Online` and was deleted in production deployment run `34264085534`; the live database was never switched or modified by the test.
- Production deployment run `34271036301` repeated the disposable restore test for the final release. The copy reached `Online` at `2026-09-08T20:28:11Z`, was deleted by the workflow, and a direct database listing confirmed that only `master` and the live production database remained.
- Email send/status operational diagnostics are enabled; email engagement diagnostics remain disabled.
- The production workbook and readiness, latency, server/dependency, and email-failure alerts are enabled.
- The production action group is enabled with the approved email receiver.
- The `$125` monthly production budget and notifications remain configured.

## Remaining public-cutover gates

- Obtain the gear lead's signed physical size/color counts, enter them in production, and publish only products that pass the catalog review.
- Complete the final private catalog and checkout smoke checks with the approved inventory.
- Obtain final documented policy approval and name the launch and rollback owners.
- Bind `www` and the apex to Azure, issue and verify TLS, confirm the apex redirect, and update Square's return URL to the canonical website.
- Enable checkout and complete final smoke testing before removing `noindex`.
- Retain `archive.el1tespr1ntathlet1cs.org` for 30 days after cutover.
