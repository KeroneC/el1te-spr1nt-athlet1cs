# Frontend Architecture

The Phase 9 Admin media upload travels through a same-origin Next.js route, which reads the HttpOnly Admin session server-side and forwards multipart data to ASP.NET Core. Client JavaScript never receives the JWT. The reusable Media Picker keeps existing CMS URL contracts migration-safe while selecting library URLs. Public gallery pages remain Server Components.

`apps/web` uses Next.js 15 App Router, React 19, strict TypeScript, Tailwind CSS, and npm. Pages and layouts are Server Components by default. Components use `"use client"` only for forms, dialogs, navigation state, and other browser interaction.

## Public Website Boundary

The `(public)` route group owns the shared public header, footer, loading, error, and not-found experiences without affecting public URLs. Public pages read anonymous DTOs through `lib/public/client.ts`; they never reuse authenticated Admin DTOs or JWT helpers. Server Components perform CMS reads with a 60-second `revalidate` window. This means a published Admin change may take up to about one minute to appear publicly.

Browser interaction includes the mobile menu, contact form, feature-gated store configurator, and non-personal cart. The contact form posts to the same-origin `app/api/public/contact/route.ts` handler. The cart refreshes product availability through a narrow same-origin store product handler, while server-rendered store pages use anonymous public DTOs. Browser storage never contains buyer contact or payment information.

Content Block keys are centralized in `lib/public/content.ts`. Pages omit missing unpublished blocks and do not expose raw keys. Public list/detail DTOs preserve API publication, expiration, active-state, ordering, and coach-email privacy rules.

Public routes are `/`, `/about`, `/programs`, `/news`, `/news/[slug]`, `/events`, `/events/[slug]`, `/coaches`, `/sponsors`, `/faqs`, `/registration`, `/forms`, `/scholarship`, `/hall-of-fame`, `/rgnhof` redirect, `/team`, `/contact`, `/gallery`, `/gallery/[slug]`, and feature-gated `/shop`, `/shop/[slug]`, and `/shop/cart`.

Phase 10 keeps the Registration Hub frontend-only. The public site links downloadable PDFs under `apps/web/public/forms` and asks families to contact the club instead of collecting athlete details, proof of age, medical information, waivers, payments, or private documents online. Current-site parity pages that do not yet have CMS modules use structured static content in Server Components so they can be converted later without changing the public route shape.

## Protected Data Boundary

`app/admin/(protected)/layout.tsx` calls `requireAdminUser`. Server-rendered admin pages load data through `adminApiFetch` with `cache: "no-store"`. That helper reads the HttpOnly `el1te_admin_session` cookie on the server and forwards its JWT to the ASP.NET Core API. Client Components never receive or inspect the token.

Browser mutations call same-origin Next.js Route Handlers. Announcements retain their explicit handlers; CMS and Store resources use the allowlisted `app/api/admin/[...path]/route.ts`. `lib/admin/mutation-policy.ts` permits only supported resource, method, identifier, inventory action, and Square-import combinations. A small read allowlist supports browser refreshes inside the inventory workspace. The ASP.NET Core `CmsAdmin` and `SuperAdmin` policies remain authoritative.

## Admin Modules

All list pages are server-rendered and preserve supported search, filter, date, and pagination values in the URL. Module-specific Client Component forms perform immediate usability validation, while backend validation remains authoritative. Safe field errors cross the mutation proxy; internal exception details do not.

Lifecycle behavior follows the API:

- Events, Announcements, Content Blocks, and Contact Submissions support permanent deletion with confirmation.
- Coaches, Sponsors, and FAQs use the API's delete endpoint to deactivate records; their forms can reactivate them.
- Site Settings updates one existing singleton record and never exposes a create route.
- Contact Submissions are private, support New/Read/Resolved/Archived status changes, and are never fetched by a public page.
- Merchandise adds a dashboard, categories, product list, five-step product wizard, visualizer placement, inventory matrix, and SuperAdmin-only Square import. Published products feed the feature-gated public catalog and deterministic configurator; the live Azure flag remains off.

Shared primitives in `components/admin` cover form controls, notices, list filters, badges, pagination, empty states, and accessible confirmation dialogs. Forms remain module-specific rather than using a generic CRUD framework.

## Route Layout

```text
/admin                                      dashboard
/admin/announcements                       list/new/{id}/edit
/admin/events                              list/new/{id}/edit
/admin/coaches                             list/new/{id}/edit
/admin/sponsors                            list/new/{id}/edit
/admin/faqs                                list/new/{id}/edit
/admin/content                             list/new/{id}/edit
/admin/site-settings                       singleton editor
/admin/contact-submissions                 list/{id}
/admin/store                               private commerce dashboard
/admin/store/categories                    category manager
/admin/store/products                      list/new/{id}/edit
/admin/store/inventory                     receipt/count/adjust matrix
/admin/store/import                        SuperAdmin one-time Square import
/api/admin-session/*                       login/logout boundary
/api/admin/announcements/*                 announcement mutations
/api/admin/[...path]                       allowlisted CMS/store reads and mutations
```

The dashboard uses small paged list requests for real upcoming-event, active-coach, active-sponsor, and new-contact counts. It does not invent analytics or require a new backend endpoint.
