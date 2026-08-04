# Launch Hardening, Performance, and Content Promotion

This release prepares the demo for launch review. It does not enable Square checkout, create production infrastructure, or move data into production.

## Admin access baseline

- Admin and SuperAdmin accounts use `POST /api/auth/admin/login`; the older login endpoint no longer accepts privileged accounts.
- Five failed passwords inside 15 minutes lock the account for 15 minutes. Database-backed account partitions and ASP.NET rate limits provide additional account and resolved-IP protection.
- SuperAdmins must enter a six-digit email code after a correct password. Codes expire after ten minutes, allow five attempts, and are represented in the browser only by a short-lived HttpOnly challenge cookie.
- Password recovery uses a 256-bit token in the URL fragment. Only its SHA-256 hash is stored. Links expire after 30 minutes, are one-time use, and revoke existing sessions on completion.
- Admin JWTs carry a security version. Password resets, role or active-state changes, and the session-revocation action increment the database version, invalidating older tokens.
- Local email is written to the API process's `.dev-mail/` directory. The directory is gitignored and each file is owner-readable only. Demo uses an Azure-managed Communication Services email domain. A custom sender domain remains a production cutover gate.
- Anonymous parent registration is disabled by default through `AuthFeatures__AllowPublicRegistration=false`.

## Responsive media

Original uploads remain unchanged and retain the 10 MB source limit. New eligible images synchronously receive transparent/photo-safe WebP derivatives at 480, 960, and 1600 pixels wide without upscaling. Public managed media accepts only `?width=480`, `960`, or `1600`; an absent derivative safely falls back to the original.

Backfill an environment after its migration is applied:

```bash
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api -- --backfill-media-derivatives
```

The JSON report includes processed, skipped, and failed counts, byte totals, and original SHA-256 hashes. Re-running is safe. CMS references remain unchanged. Confirm representative photographs, transparent logos, portraits, sponsor marks, and shop images before relying on derivatives.

Treat any nonzero `failed` count as an incomplete backfill even though original-image fallback keeps the site available. A completed environment check must confirm representative `?width=480` responses return `image/webp` with the versioned derivative cache policy; an original JPEG or PNG response means that asset still needs processing.

Demo additionally enables an idempotent startup backfill. It quickly skips assets whose expected derivatives already exist, never blocks application readiness, and leaves the original available if an individual conversion fails. Production keeps startup backfill disabled unless it is explicitly enabled for a controlled promotion.

The public `ResponsiveMediaImage` component adds `srcset`, `sizes`, dimensions, lazy loading, and async decoding for managed `/media/{guid}` URLs. A page may explicitly prioritize only its primary above-the-fold image.

## Browser performance telemetry

Demo sets `BROWSER_ANALYTICS_ENABLED=true`; it can be disabled at runtime without rebuilding. The SDK disables cookies, browser storage, automatic user identity, automatic exception capture, and Admin tracking. It records only sanitized public route templates, Core Web Vitals, navigation duration, release SHA, and safe public error references. Queries, fragments, slugs, form values, cart configuration, custom text, record IDs, and authenticated identifiers are not recorded.

Use `/api/runtime-config` to confirm the runtime flag. Inspect Application Insights only through authorized Azure access. The existing 30-day retention applies.

## Security headers and policy drafts

All web responses receive HSTS in production plus content-type, frame, referrer, permissions, and CSP headers. Demo uses `CSP_MODE=report-only`. Production must use `CSP_MODE=enforce` only after report-only violations have been reviewed.

The reserved CMS keys `policy.privacy`, `policy.accessibility`, `policy.terms`, and `policy.store` feed `/privacy`, `/accessibility`, `/terms`, and `/store-policy`. Their approved text-only format supports `##` headings, `-` list items, paragraphs separated by blank lines, and links using `[label](https://...)`, `[label](mailto:...)`, or root-relative destinations. Raw HTML and other URL schemes are always rendered as text. The policies describe actual website, analytics, payment, Printify fulfillment, cancellation, return, and accessibility behavior; they remain operational wording rather than legal advice.

## Selective content promotion

`El1teSpr1ntTrack.LaunchPromotion` is a standalone CLI and is never exposed through HTTP. Export creates a reviewable manifest where active/published public records default to `include: true` and drafts/inactive records default to false. The allowlist covers site settings, content blocks, public CMS records, galleries/media, and catalog configuration. The tool explicitly rejects user, invitation, audit, submission, athlete, document, consent, order, refund, webhook, outbox, import-run, secret, and telemetry types.

Example export:

```bash
PROMOTION_SOURCE_CONNECTION='…' dotnet run --project apps/api/tools/El1teSpr1ntTrack.LaunchPromotion -- export \
  --source-environment demo --destination-environment production \
  --source-api-base https://demo-api.example --destination-api-base https://api.example \
  --source-blob-service-uri https://demostorage.blob.core.windows.net --media-container media \
  --manifest ./artifacts/launch-promotion.json
```

Review and edit only each record's `include` value. Import is dry-run by default:

```bash
PROMOTION_DESTINATION_CONNECTION='…' dotnet run --project apps/api/tools/El1teSpr1ntTrack.LaunchPromotion -- import \
  --source-environment demo --destination-environment production \
  --source-api-base https://demo-api.example --destination-api-base https://api.example \
  --source-blob-service-uri https://demostorage.blob.core.windows.net \
  --destination-blob-service-uri https://productionstorage.blob.core.windows.net --media-container media \
  --manifest ./artifacts/launch-promotion.json --bootstrap-user-id 00000000-0000-0000-0000-000000000000
```

Applying additionally requires `--apply --confirm production`. Import preserves stable IDs/slugs, copies only selected media objects under their stable storage keys, verifies source and destination SHA-256 hashes, rewrites managed API URLs, remaps media ownership to the bootstrap SuperAdmin, resets variant on-hand and reserved stock to zero, upserts transactionally, and does not delete unrelated destination records. Local fake storage can be selected with `--source-media-root` and `--destination-media-root` for CI/rehearsal. Production use remains prohibited until a disposable source/destination rehearsal is complete.

## Manual accessibility and performance review

For the demo approval pass, test public pages, Admin login/recovery/MFA, and shop preview with:

- Keyboard-only navigation and visible focus.
- 200% browser zoom.
- 390px and 340px viewports with no horizontal scrolling.
- Reduced-motion and forced/high-contrast settings.
- VoiceOver/Safari or an equivalent screen-reader smoke test.
- Warm mobile Lighthouse: LCP at or below 2.5 seconds, homepage initial images at or below 1.5 MB, and gallery initial images at or below 2 MB.
- A gallery network recording proving below-the-fold images wait until they approach the viewport.

Record Azure cold-start variance separately; it must not be confused with a warm performance regression.
