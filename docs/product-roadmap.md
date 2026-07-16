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

### Feedback and accessibility

- Add a public feedback route for problems, feature ideas, accessibility issues, content corrections, and general feedback.
- Add rate limiting, spam protection, optional contact information, safe validation, and an Admin triage queue. Do not accept attachments or sensitive athlete/medical information.
- Adopt WCAG 2.2 AA as the acceptance target.
- Add automated axe checks to Playwright plus keyboard, NVDA/Chrome, 200% zoom, narrow-screen, reduced-motion, and high-contrast manual checks.
- Publish accessibility and privacy statements with an accessible issue-reporting path. Do not use an accessibility overlay widget.

### Production launch foundation

- Provision production separately from demo with explicit cost review, custom domain and TLS, backup/restore checks, secure email through Azure Communication Services, and release/rollback runbooks.
- Import only reviewed public CMS and media. Exclude users, contact/feedback records, credentials, and other private or test data.
- Keep the previous Squarespace site as a temporary read-only archive for 60 to 90 days after cutover.

## Stage 3: Winter Registration Pilot

- Parent accounts own structured seasonal athlete applications.
- Applications move through Draft, Submitted, Needs Information, Approved, Declined, or Withdrawn states.
- Collect registration details plus registration form, waiver, photo consent, and proof-of-age documents.
- Keep medical and insurance information offline. Do not add registration payment or waiver e-signature in this pilot.
- Store documents privately, scan uploads, quarantine failures, and allow full-record access only to Admin and SuperAdmin users.
- Provide parent status/document views and an Admin review queue while preserving the paper fallback.

## Stage 4: Hybrid Store

- Manage products, variants, SKU values, inventory adjustments, and availability inside the platform.
- Use Square-hosted checkout and payment processing after the catalog and inventory workflows are stable.
- Reconcile Square webhooks idempotently into internal order and inventory records.
- Defer fulfillment automation, promotions, and advanced commerce reporting until real usage establishes the need.

## Stage 5: Full Launch

Full launch follows a successful registration pilot and requires:

- Accepted accessibility review and critical-flow browser tests.
- Approved public content and production data import.
- Tested monitoring, support, backup, email, incident, release, and rollback procedures.
- Board/staff readiness for registration review, feedback triage, content updates, and inventory operations.
- A scheduled cutover communicated at least four weeks before the registration window.

## Deferred Until Prior Stages Prove The Need

- Registration payments and online medical/insurance collection.
- Parent messaging, attendance, meet entry, volunteer management, and athlete portals beyond registration status.
- Custom Hall of Fame profile routes.
- Advanced store fulfillment, discounting, and reporting.
- Automatic production deployment, deployment slots, and mobile applications.
