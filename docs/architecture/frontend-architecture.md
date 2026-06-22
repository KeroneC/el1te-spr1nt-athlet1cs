# Frontend Architecture

`apps/web` uses Next.js 15 App Router, React 19, strict TypeScript, Tailwind CSS, and npm. Pages and layouts are Server Components by default. Components use `"use client"` only for forms, dialogs, navigation state, and other browser interaction.

## Public Website Boundary

The `(public)` route group owns the shared public header, footer, loading, error, and not-found experiences without affecting public URLs. Public pages read anonymous DTOs through `lib/public/client.ts`; they never reuse authenticated Admin DTOs or JWT helpers. Server Components perform CMS reads with a 60-second `revalidate` window. This means a published Admin change may take up to about one minute to appear publicly.

Browser interaction is limited to the mobile menu and contact form. The contact form posts to the same-origin `app/api/public/contact/route.ts` handler, which forwards the exact public request contract with `cache: "no-store"`, preserves safe field errors, and suppresses internal API failures. Arbitrary optional CMS image URLs use normal image elements until a managed media host and upload workflow exist; local hero media continues through Next Image.

Content Block keys are centralized in `lib/public/content.ts`. Pages omit missing unpublished blocks and do not expose raw keys. Public list/detail DTOs preserve API publication, expiration, active-state, ordering, and coach-email privacy rules.

Public routes are `/`, `/about`, `/programs`, `/news`, `/news/[slug]`, `/events`, `/events/[slug]`, `/coaches`, `/sponsors`, `/faqs`, `/registration`, and `/contact`.

## Protected Data Boundary

`app/admin/(protected)/layout.tsx` calls `requireAdminUser`. Server-rendered admin pages load data through `adminApiFetch` with `cache: "no-store"`. That helper reads the HttpOnly `el1te_admin_session` cookie on the server and forwards its JWT to the ASP.NET Core API. Client Components never receive or inspect the token.

Browser mutations call same-origin Next.js Route Handlers. Announcements retain their explicit handlers; Phase 7 resources use the allowlisted `app/api/admin/[...path]/route.ts`. `lib/admin/mutation-policy.ts` permits only supported CMS resource, method, identifier, and contact-status combinations. The ASP.NET Core `CmsAdmin` policy remains authoritative.

## Admin Modules

All list pages are server-rendered and preserve supported search, filter, date, and pagination values in the URL. Module-specific Client Component forms perform immediate usability validation, while backend validation remains authoritative. Safe field errors cross the mutation proxy; internal exception details do not.

Lifecycle behavior follows the API:

- Events, Announcements, Content Blocks, and Contact Submissions support permanent deletion with confirmation.
- Coaches, Sponsors, and FAQs use the API's delete endpoint to deactivate records; their forms can reactivate them.
- Site Settings updates one existing singleton record and never exposes a create route.
- Contact Submissions are private, support New/Read/Resolved/Archived status changes, and are never fetched by a public page.

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
/api/admin-session/*                       login/logout boundary
/api/admin/announcements/*                 announcement mutations
/api/admin/[...path]                       allowlisted Phase 7 mutations
```

The dashboard uses small paged list requests for real upcoming-event, active-coach, active-sponsor, and new-contact counts. It does not invent analytics or require a new backend endpoint.
