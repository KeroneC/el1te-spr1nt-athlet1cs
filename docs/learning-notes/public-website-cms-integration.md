# Public Website CMS Integration

Phase 8 closes the public content loop: an administrator edits CMS data, the ASP.NET Core public API applies visibility and privacy rules, and Next.js presents the published result to visitors.

## Architecture

The `app/(public)` route group supplies one public layout without changing URL paths or wrapping the Admin workspace. Server Components fetch CMS data through `lib/public/client.ts`. The dedicated public client uses only anonymous public DTOs, converts non-success responses into safe errors, and revalidates GET data every 60 seconds. Admin reads remain `no-store`.

Only interactive behavior is client-rendered: `site-header.tsx` controls the mobile menu and `contact-form.tsx` handles validation and submission state. The form posts to a same-origin Next.js Route Handler, which forwards the exact public contact request to ASP.NET Core without caching. The JWT and Admin session boundary are not involved.

## Content Blocks

`lib/public/content.ts` centralizes the supported keys:

- `home.hero`, `home.mission`, and `home.programs`
- `about.story` and `about.values`
- `registration.intro`
- `sponsors.intro`

Pages request the published block collection once, build an exact-key map, and omit missing blocks. This is intentionally smaller than a general page builder.

## Public Flows

Announcements and events use paged list DTOs and slug-based detail DTOs. Missing, draft, expired, or unpublished details all produce the same public not-found experience. Event filters pass the API's string enum values and `upcomingOnly` option. Coaches display email only when the public DTO includes it. Sponsors and FAQs preserve backend ordering.

The contact page combines Site Settings with the write-only contact endpoint. Client validation improves usability, but backend validation remains authoritative. Field errors are returned safely; connection and internal failures become retryable generic messages. Duplicate clicks are blocked while a request is in progress, and successful submissions clear the form.

## Images and SEO

The local track hero uses Next Image. CMS image/logo fields may contain arbitrary remote URLs, so optional CMS media uses normal image elements with layout fallbacks until a managed media host exists. No upload or Azure storage behavior was added.

The root layout defines title templates and a base description. Content pages have page metadata, while announcement and event details generate title, description, canonical path, and basic Open Graph data from public DTOs.

## Testing and Operation

Run the automated commands in the root README. Public behavior tests cover content lookup, paging/filter query generation, the revalidation policy, contact validation, exact POST forwarding, backend field errors, and API unavailability. Production build and smoke scripts cover rendered routes.

Use [Public website testing](../guides/public-website-testing.md) for the manual Admin-to-public workflow. After an Admin publish, allow up to 60 seconds for cached public content to refresh.

## Deferred Work

Media upload, galleries, rich text, online athlete registration, portals, payments, waivers, documents, advanced motion, and final visual polish remain deferred. The visual system is intentionally usable and responsive now; board feedback should guide the later redesign rather than embedding assumptions before review.
