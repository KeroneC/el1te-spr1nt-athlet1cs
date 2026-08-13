# Product Rebase and Staged Delivery Roadmap

This roadmap reconciles the original platform phases, the implemented repository, manual testing, Azure demo delivery, and the club's current priorities. The repository, migrations, ADRs, and passing validation remain the source of truth.

Deployment is intentionally batched. A feature is merged only after its own validation passes, and the manually approved Azure demo is updated only when a meaningful release is ready for review.

## Product Principles

- Protect youth and family information by default.
- Keep public CMS, media, gallery, and Admin behavior that already works.
- Improve observed workflows incrementally instead of rebuilding the Admin.
- Use Azure services covered by the nonprofit grant when they provide a suitable operational capability.
- Keep demo and production isolated. Production receives reviewed content, not test users, private submissions, or local data.
- Preserve a paper/offline path while new registration workflows are piloted.

## Stage 1: Operational UX

The first stage removes friction from workflows already in use.

- Bulk media queue for up to 20 images, with three concurrent uploads, per-image title and required alt text, retryable partial failures, and optional direct album assignment.
- Searchable, paginated media selection for CMS image fields and gallery albums.
- Grouped public navigation so Scholarship, Hall of Fame, Forms, Programs, Coaches, and Team are discoverable without crowding the primary header.
- Email-invited Admin and guarded SuperAdmin management, including active-state controls, audit records, and protection against removing the final SuperAdmin or accidentally disabling the current user.

Delivery status: Stage 1 is implemented. The platform includes the media workflow, searchable media selection, grouped public navigation, email-bound Admin/SuperAdmin invitations, guarded access controls, and append-only privileged identity activity history. Email MFA, password recovery, and security-version session revocation were subsequently delivered in the Stage 2 launch-hardening foundation; broader CMS audit coverage remains pending.

## Stage 2: Launch Readiness

This stage prepares a separate production environment and the support practices needed for public use.

### Azure operations

- Add cookie-free Application Insights browser monitoring for anonymous page views, performance, JavaScript failures, and a minimal approved event taxonomy.
- Never send names, email addresses, athlete data, form contents, document names, authenticated user IDs, or Admin content to analytics.
- Correlate browser, Next.js, and API requests and show users a safe reference ID for unexpected failures.
- Add an Application Insights workbook for readiness, failures, latency, dependencies, route usage, and release markers.
- Add Azure Monitor alerts for sustained 5xx responses, readiness failures, dependency failures, and latency.
- Keep raw logs in Azure behind RBAC and document the support path from reported time, route, and reference ID to the relevant trace and deployment SHA.
- Retain the current cost controls unless observed traffic justifies a reviewed change: 30-day logs and the configured daily ingestion cap.

Delivery status: the server-side support foundation is implemented. Next.js and API failures are correlated with safe, copyable references; Bicep owns the readiness check, workbook, alerts, release markers, and RBAC-protected support workflow. The launch-hardening release adds runtime-configurable, cookie-free public route-template and performance telemetry while continuing to exclude Admin activity, identity, queries, slugs, form values, cart customization, and raw exceptions. Demo validation and organization privacy approval remain required before production.

### Feedback and accessibility

- Add a public feedback route for problems, feature ideas, accessibility issues, content corrections, and general feedback.
- Add rate limiting, spam protection, optional contact information, safe validation, and an Admin triage queue. Do not accept attachments or sensitive athlete/medical information.
- Adopt WCAG 2.2 AA as the acceptance target.
- Add automated axe checks to Playwright plus keyboard, NVDA/Chrome, 200% zoom, narrow-screen, reduced-motion, and high-contrast manual checks.
- Publish accessibility and privacy statements with an accessible issue-reporting path. Do not use an accessibility overlay widget.

### Production launch foundation

- Provision production separately from demo with explicit cost review, custom domain and TLS, backup/restore checks, secure email through Azure Communication Services, and release/rollback runbooks.
- Import only reviewed public CMS and media. Exclude users, contact/feedback records, credentials, and other private or test data.
- Keep the previous Squarespace site as a temporary read-only archive for 30 days after cutover.

Delivery status: the launch-hardening foundation and Square-only checkout are implemented on demo. Admin lockout, email MFA, password recovery, session revocation, responsive media, policies, security headers, accessibility automation, selective promotion, Square payment, cancellation, refund, tracking, and inventory restoration are available. Production provisioning, custom-domain email, final content selection, Blob-copy rehearsal, physical inventory, and public cutover remain pending.

## Stage 3: Winter Registration Pilot

- Parent accounts own structured seasonal athlete applications.
- Applications move through Draft, Submitted, Needs Information, Approved, Declined, or Withdrawn states.
- Collect registration details plus registration form, waiver, photo consent, and proof-of-age documents.
- Keep medical and insurance information offline. Do not add registration payment or waiver e-signature in this pilot.
- Store documents privately, scan uploads, quarantine failures, and allow full-record access only to Admin and SuperAdmin users.
- Provide parent status/document views and an Admin review queue while preserving the paper fallback.

## Stage 4: Hybrid Store

- Manage products, categories, media, tracked size/color variants, untracked customizations, SKU values, inventory adjustments, reservations, and availability inside the platform.
- Use a custom public catalog and deterministic garment configurator while Square-hosted Checkout keeps payment-card data outside El1te.
- Keep payment and fulfillment states separate; process webhooks, reservation expiry, email, and reconciliation idempotently through a SQL outbox.
- Provide an Admin product wizard, inventory matrix, order workboard, production sheets, staff-entered sales, audited refunds, and integration health.
- Defer shipping, customer accounts, discounts, loyalty, custom-artwork uploads, advanced reporting, and other unproven complexity.

Delivery status: the Square-only club-inventory workflow is implemented through checkout and order operations. It includes the Admin catalog and inventory workspace, public configurator/cart, inventory reservations, hosted Square payment, verified webhooks, customer tracking and cancellation, audited refunds/restocking, order emails, and fulfillment workboard. Printify is paused on its unmerged WIP branch; shipping and staff-entered tender remain deferred. The existing Square storefront remains the rollback path until public cutover.

## Stage 5: Lean Public Launch

The club has chosen to launch the public CMS and Square-only merchandise workflow before the Parent registration pilot. Launch requires:

- Accepted accessibility review and critical-flow browser tests.
- Approved public content and production data import.
- Tested monitoring, support, backup, email, incident, release, and rollback procedures.
- Club/staff readiness for content updates, inventory operations, handoff, cancellation, and refunds.
- Parent registration, feedback triage, Printify, shipping, and staff-entered sales remain disabled.

## Deferred Until Prior Stages Prove The Need

- Registration payments and online medical/insurance collection.
- Parent messaging, attendance, meet entry, volunteer management, and athlete portals beyond registration status.
- Custom Hall of Fame profile routes.
- Advanced store fulfillment, discounting, and reporting.
- Automatic production deployment, deployment slots, and mobile applications.
