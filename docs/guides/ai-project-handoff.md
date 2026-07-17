# AI Project Handoff

This is the reconciliation point between the repository and any separate ChatGPT conversation used to draft future phase prompts. The repository, migrations, ADRs, documentation, and passing tests are the source of truth.

## Phase 6B Azure Demo Delivery

- The manually promoted, public-but-noindex `demo` environment is live in Central US under `el1te-demo-central-rg`. Central US was selected after the Basic SQL SKU was unavailable in East US 2 and East US.
- Passing `main` CI runs package immutable API, web, and EF artifacts with a commit manifest and SHA-256 checksums. The deployment workflow accepts a CI run ID and refuses non-main or unsuccessful runs.
- Bicep owns the shared B1 Linux plan, API/web apps, Basic Azure SQL, private Standard LRS Blob media, Key Vault, capped workspace-based Application Insights, and 600-second cold-start limits.
- Production media uses `AzureBlobMediaStorage` through managed identity; Development keeps `LocalMediaStorage`. Public media continues to stream through the API.
- The first SuperAdmin is created only through the idempotent non-HTTP `--bootstrap-admin` command. Development seeding remains disabled in Production.
- GitHub OIDC and the protected `demo` Environment are configured. The API managed identity has scoped Blob/Key Vault roles and a contained SQL user with `db_datareader`/`db_datawriter`; temporary probe resources and firewall rules were removed after launch diagnostics.
- Public demo URLs are `https://el1tesprint-demo-neauu2-web.azurewebsites.net` and `https://el1tesprint-demo-neauu2-api.azurewebsites.net`.

## Current Baseline

- Public-site refinement and Phase 6B delivery work are merged into `main`.
- The reconciled product direction is maintained in `docs/product-roadmap.md`: operational UX, Azure-backed launch readiness, a Winter registration pilot, hybrid store, then full launch.
- The monorepo contains the .NET 10 ASP.NET Core API in `apps/api`, Next.js 15/npm frontend in `apps/web`, documentation in `docs`, deployed Azure/Bicep infrastructure in `infra`, and validation tooling in `scripts`.
- Authentication uses API JWTs and a server-only Next.js HttpOnly session cookie. Admin and SuperAdmin authorization remains API-authoritative.
- The Admin manages announcements, events, coaches, sponsors, FAQs, content blocks, site settings, contact submissions, reusable media, and gallery albums.
- The public website includes home, about, programs, news, events, coaches, sponsors, FAQs, registration information, forms, scholarship, Hall of Fame, team, contact, gallery list, and gallery detail routes.
- The Admin is functionally mature for the current scope. Future Admin work should be targeted UX/UI fine-tuning based on use and board feedback, not a ground-up redesign.

## Active Stage 1 Operational UX

- Bulk media upload queues up to 20 validated images with per-image title, required alt text, optional captions, previews, three concurrent uploads, retryable partial failures, and optional direct album assignment.
- A successful upload is retained when album assignment fails, so retrying the assignment does not create a duplicate asset.
- CMS media fields and gallery album editing use a shared searchable, 24-item paginated media browser instead of a long scrolling list.
- The public header keeps six concise top-level choices while `Club` groups About, Programs, Coaches, Team, and Hall of Fame, and `Resources` groups Forms, Scholarship, and FAQs. Both disclosures support nested active states, Escape/focus restoration, outside-click closing, and the mobile menu closes after navigation.
- Frontend validation for this Stage 1 work: lint passed, strict typecheck passed, 55 Vitest tests passed, and the production build passed. Four Playwright scenarios passed: the real Admin-to-public-gallery workflow, desktop grouped-navigation behavior, mobile navigation closure, and the SuperAdmin invitation-to-Admin authorization boundary.
- Guarded Admin/SuperAdmin management is implemented on `feature/admin-user-management`: active SuperAdmins create email-bound, 72-hour invitations; secrets are hash-only in SQL and returned in URL fragments for trusted manual delivery; recipients set a strong password through the public acceptance route.
- Access changes are database-authoritative. A SuperAdmin cannot change their own role/status or demote/deactivate the final active SuperAdmin. Admins cannot open the access workspace.
- `AdminActivityLogs` is append-only at the application boundary and records safe summaries for invitation and privileged account changes. The activity view is SuperAdmin-only; passwords, raw tokens, JWTs, cookies, and request bodies are excluded.
- Backend Release validation passed with 49 unit tests, 34 integration tests, zero build warnings, and no pending EF model changes. Documentation validation passed for 59 Markdown files, and Bicep compilation passed.
- Stage 1 is complete. Stage 2 begins with observability/support references, privacy-conscious analytics, feedback triage, accessibility, production isolation, and Azure Communication Services invitation delivery.
- Azure demo deployment remains manually paused until a meaningful reviewed release is ready.

## Public Refinement Branch

- Branch: `feature/public-site-refinement`
- Source branch: updated `main`
- Scope: public website visual polish, current-site parity, logo/favicon incorporation, Registration Hub, downloadable forms, and docs.
- Backend scope for that historical visual branch was intentionally narrow. Online registration, payments, portals, waiver workflows, private documents, and iOS remain deferred.

## Phase 10 Public Website Work

- Added shared public-site constants in `apps/web/lib/public/site.ts` for brand assets, slogan, shop/social/contact links, footer links, forms, team values, registration steps, and Hall of Fame static content.
- Added or polished parity routes:
  - `/forms`
  - `/scholarship`
  - `/hall-of-fame`
  - `/rgnhof` redirecting to `/hall-of-fame`
  - `/team`
- Polished public navigation, footer, homepage, Registration Hub, events, gallery detail, sponsors, and contact.
- Correction pass: inspected the local Figma-generated project at `C:\Users\Kerone Creary\source\repos\Youth Sports Website Concept` after the first Phase 10 pass did not sufficiently reflect the design reference.
- Figma project findings: Vite 6 + React SPA, Tailwind 4-style CSS, shadcn-style UI files, custom `Navbar`, `Footer`, and page components. It used red `#dc2626`, black `#171717`, muted gray `#f4f4f5`, Oswald display type, Inter body type, a black sticky nav with a thick red rule, skewed red/black buttons, black page heroes with red highlighted words, red skew section marks, event date slabs, document download rows, grayscale-to-color gallery/coach image hovers, and a black footer with red section markers. It did not include local logo/favicon image assets; it used a generated Zap icon mark and remote sports images.
- Adopted Figma design elements in the real app: black/red public header shell, skewed brand/CTA treatment, Oswald/Inter typography via CSS, angular hero badge/buttons, Figma-style public page heroes, red section rules, muted program cards with red bottom borders, dark featured announcement card styling, event date-slab list cards, registration/forms download rows, gallery hover overlays, grayscale coach image hover treatment, sponsor tier panels, contact form panel styling, and footer red skew heading marks.
- Refinement pass after visual review: tightened the homepage closer to the Figma reference by splitting `Greatness` and `Begins Here` into white/red display lines, shortening the first viewport so the Programs section peeks in, moving Programs directly after the hero, adding active red navigation state, removing the extra hero logo, and adding CSS motion patterns for hero reveal, badge snap-in, slow hero-image drift, button shine, card lift, download-row slide, event-row slide, gallery zoom overlays, and coach grayscale-to-color hover.
- Kept the real El1te logo and favicon assets already placed in the app because the generated project did not contain real logo/favicon files.
- Preserved CMS/public API behavior for existing CMS-driven sections. Static parity pages are intentionally simple and easy to move into CMS later.
- Latest brand refinement: the public header uses a typographic home lockup and grouped navigation, with direct Events, Gallery, Sponsors, and Contact links plus Shop and one Registration CTA. The homepage hero now leads with the real black logo on a red field and a white club-name lockup with red numeral `1`s.
- Latest homepage refinement: Upcoming Events uses a dedicated compact card variant, the mission statement is a left-aligned vertical stack, decorative legacy green and yellow-orange accents were removed, text links use the primary red, and the sponsor preview aligns to the main content grid.
- Sponsor policy remains unchanged across Admin, API, and stored data. Public presentation gives Gold, Silver, and Bronze track-medal treatments, Platinum a premium black/red treatment, Community a neutral/red individual-support treatment, and Other a neutral fallback. Homepage previews prioritize Gold and use sponsor logos when available.
- FAQs remain discoverable in the Resources navigation group and the Registration Hub. Native details/summary disclosure behavior and CMS grouping remain intact.
- The RGN El1te Hall of Fame was rebuilt around its family memorial purpose. It restores the dedication to Roland George Newton, uses the existing RGN crest and real inductee photography, and stores profile-ready slugs/image metadata without adding incomplete detail routes.

## Phase 10 Assets

- Logo assets:
  - `apps/web/public/brand/el1te-logo-white.png`
  - `apps/web/public/brand/el1te-mark-black.png`
- Favicon:
  - `apps/web/public/favicon.png`
- Hall of Fame assets:
  - `apps/web/public/images/hall-of-fame/rgn-hall-of-fame-crest.png`
  - `apps/web/public/images/hall-of-fame/dani-prunzik.jpeg`
  - `apps/web/public/images/hall-of-fame/kaitlyn-eger.jpg`
- Downloadable forms:
  - `apps/web/public/forms/athlete-registration-form.pdf`
  - `apps/web/public/forms/liability-waiver.pdf`
  - `apps/web/public/forms/photo-consent-form.pdf`
  - `apps/web/public/forms/scholarship-form.pdf`

## Phase 10 Validation

Run from `apps/web` using `npm.cmd` on Windows PowerShell if `npm.ps1` is blocked by execution policy.

- `npm.cmd install`: passed; dependencies were already up to date. npm reported existing moderate audit warnings and pending allow-scripts review for install-script packages.
- `npm.cmd run lint`: passed after the correction pass.
- `npm.cmd run typecheck`: passed after the correction pass.
- `npm.cmd test`: passed after the correction pass, 5 files and 40 tests.
- `npm.cmd run build`: passed after the correction pass. The API was not running, so build output logged expected `ECONNREFUSED` fetch messages while public fallbacks rendered successfully.
- `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/validation/validate-docs.ps1`: passed for 56 Markdown files.
- Browser smoke check on a clean `next dev -p 3001`: passed after the correction pass for `/`, `/registration`, `/forms`, `/scholarship`, `/hall-of-fame`, `/team`, `/coaches`, `/gallery`, `/events`, `/sponsors`, and `/contact` at 390px and 1440px widths. Each route returned 200, rendered one `h1`, and had no horizontal overflow.
- Backend validation: not required because Phase 10 did not change backend code.
- Latest refinement validation: `npm.cmd run lint` passed; `npm.cmd run typecheck` passed; `npm.cmd test` passed with 5 files and 43 tests; `npm.cmd run build` passed. Build logged expected API `ECONNREFUSED` fallback messages because the backend was not running.
- Latest browser matrix: 14 public routes passed at 390, 768, 1280, and 1536 CSS pixels (56 route/viewport combinations). Every page rendered one visible `h1`, no horizontal overflow, no broken images, and the expected desktop/mobile navigation state. Mobile FAQ navigation and menu closing, FAQ disclosures, sponsor tier treatments, homepage sponsor ordering, local Hall of Fame images, and `/rgnhof` redirection were checked separately.

## Phase 9 Delivered

- Added `MediaAsset`, `GalleryAlbum`, and `GalleryAlbumMedia`, EF configurations, and migration `AddMediaLibraryAndGallery`.
- Added `IMediaStorage`; Development uses path-safe local storage under an ignored directory, while the Azure demo uses the private `AzureBlobMediaStorage` provider.
- Upload accepts JPEG, PNG, and WebP up to a configurable 10 MB. SkiaSharp validates the declared type, extension, encoded format, dimensions, corruption, and full decodability.
- Protected API routes are `/api/admin/media` and `/api/admin/gallery-albums`; active image bytes are `/media/{id}`; public albums are `/api/public/gallery-albums`.
- Admin routes are `/admin/media`, `/admin/media/[id]/edit`, `/admin/gallery`, `/admin/gallery/new`, and `/admin/gallery/[id]/edit`.
- Public routes are `/gallery` and `/gallery/[slug]`.
- The reusable Media Picker is connected to announcements, events, coaches, sponsors, content blocks, and the site logo while preserving manual URL entry.
- Deletion rejects referenced assets, album deletion preserves shared assets, public queries hide drafts and inactive images, and album-specific metadata overrides asset defaults.
- The multipart upload crosses the existing same-origin Next.js server boundary; JWTs remain unavailable to client JavaScript.

## Manual-Test Reconciliation

Manual end-to-end testing after implementation found and fixed three integration issues:

1. Local frontend/API communication depends on matching `API_BASE_URL` to the Visual Studio launch profile. The HTTP profile is `http://localhost:5126`; HTTPS is `https://localhost:7171`.
2. Media upload now gives an explicit success notice. Its async submit handler captures the form before `await`, preventing React's cleared event target from causing `Cannot read properties of null (reading 'reset')`.
3. Adding media to an existing album now explicitly inserts `GalleryAlbumMedia` through the repository. This fixes the EF Core concurrency exception caused by ambiguous join-entity tracking. A database-backed integration regression test covers the insert.

## Automated Verification

- Backend: 43 unit tests and 30 integration tests pass; Release build, EF model validation, API publish, and migration bundle generation succeed.
- Frontend: 45 Vitest tests, lint, strict typecheck, standalone production build, and immutable web artifact validation pass.
- Playwright now exercises the real critical loop across both applications: Admin sign-in, media upload, published album creation, image assignment, and public gallery verification. It uses dedicated ports, `El1teSpr1ntTrack_E2E`, test-only credentials, isolated ignored media storage, and cleanup.
- GitHub Actions includes the same browser workflow on Windows with LocalDB and uploads failure artifacts.
- CI artifact helper scripts are tracked under `scripts/artifacts`; runtime build outputs remain ignored under root-level `artifacts`.
- Documentation validation and secret scanning remain CI requirements.

## Deferred Work

- Custom domains, deployment slots, automatic production promotion, and private networking hardening beyond the current demo posture.
- Parent and athlete portals, online athlete registration, payments, digital waivers, volunteer workflows, attendance, meet entry, messaging, private documents, and iOS.
- Rich text, advanced image transformations, broad browser coverage, accessibility automation, visual regression, and load testing.
- Hall of Fame inductee detail routes such as `/hall-of-fame/[slug]`; static records already include stable slugs and image metadata for that future work.
- UX/UI polish is expected later, especially on the public experience. Keep Admin changes focused on observed usability issues and consistency.

## Paste Into the Prompt-Writing Conversation

```text
Repository reconciliation update for El1te Spr1nt Athlet1cs, post-Phase 9:

Treat the repository, migrations, ADRs, docs, and passing tests as the source of truth. Phase 9 is merged into main.

The platform has a protected reusable media library and gallery administration plus public gallery list/detail pages. Phase 9 added MediaAsset, GalleryAlbum, GalleryAlbumMedia, EF configurations, migration AddMediaLibraryAndGallery, an IMediaStorage abstraction with ignored local Development storage, validated JPEG/PNG/WebP uploads up to 10 MB, reusable CMS Media Pickers, protected Admin APIs/routes, public gallery APIs/routes, reference-safe deletion, publication filtering, and album-specific image metadata. Phase 6B added the managed-identity Azure Blob provider and deployed private media storage.

Manual end-to-end testing found and fixed: (1) local API_BASE_URL/profile alignment, (2) missing upload confirmation and an async React form-reset crash, and (3) an EF Core concurrency failure when adding media to an album, fixed by explicitly inserting GalleryAlbumMedia and covered by a database-backed regression test.

Current automated validation is 43 backend unit tests, 30 backend integration tests, and 45 frontend Vitest tests, plus a Playwright cross-stack workflow for Admin sign-in -> upload -> album creation/publish -> image assignment -> public gallery verification. Release build, EF model check, API publish, migration bundle, frontend lint/typecheck/build, docs validation, secret scanning, and immutable release packaging pass.

The Admin is functionally mature for the present scope. Future Admin work should be incremental UX/UI fine-tuning informed by use and board feedback, not a rebuild. Public UX/UI refinement can be scoped separately.

The Azure demo is live through manually approved GitHub OIDC delivery. Still deferred: custom domains, slots, automatic production promotion, portals, online registration, payments, waivers, volunteer/attendance/meet-entry/messaging workflows, private documents, iOS, rich text, advanced image transformations, and broad visual/accessibility/load testing. Do not describe media, galleries, Azure Blob, or demo provisioning as deferred.
```

## Reconciliation Procedure

1. Compare each proposed phase prompt with `git log`, `git status`, `docs/README.md`, this file, current migrations, and ADRs.
2. Verify every named route, DTO, script, workflow, and test exists before calling it complete.
3. Keep implemented, manually verified, automated, and deferred work distinct.
4. Run documented validation and update this file with exact results after material changes.

Never paste secrets, JWTs, connection strings, local absolute paths, or private account identifiers into an AI conversation.
