# Public Website Testing

Start the API and web app using [Local development](local-development.md). Open the public site at `http://localhost:3000` and Admin at `http://localhost:3000/admin`. Use disposable records and restore edited seeded content.

## Content Reflection

1. In Admin Content, edit and publish `home.hero`.
2. Open the homepage and allow up to 60 seconds for the public cache window.
3. Confirm the hero displays the new title/body, then restore the seeded value.
4. Unpublish a noncritical test block and confirm its section is omitted without exposing the key.

## News and Events

1. Create a draft announcement and confirm it is absent from `/news` and its slug returns the normal public not-found page.
2. Publish it, confirm list/detail display, then expire it and confirm both disappear.
3. Create a draft event and confirm it is absent publicly; publish it and verify type, UTC-labeled time, location, filters, and detail data.
4. Check announcement and event pagination with enough disposable records, then delete the records.

## People, Partners, and FAQs

1. Create an active coach with a private email and confirm the email is omitted on `/coaches`.
2. Enable public email and confirm it appears, then deactivate and confirm the coach disappears.
3. Verify active sponsors appear in tier order within white, centered logo sections; missing or failed logos fall back to sponsor names, and external links open safely in a new tab. Confirm tier headings retain their restrained Gold, Silver, Bronze, Community, Platinum, or Other accent without changing the page background.
4. Confirm the homepage preview prioritizes Gold sponsors, reuses the normalized border-free logo treatment, renders configured logos at a useful size, and falls back to sponsor names without broken images. Follow [Sponsor logo image guide](sponsor-logo-images.md) when adding or replacing media.
5. Verify FAQs are linked from the Resources navigation group and the Registration Hub. Check category groups and keyboard-operable disclosure controls; deactivate a test FAQ and confirm it disappears.

## Contact and Registration

1. Submit valid contact data and confirm the success state clears the message.
2. Confirm the submission appears only in Admin Contact Submissions.
3. Submit missing/invalid fields and confirm associated field errors appear.
4. Stop the API, retry, and confirm a safe retryable error with no internal details.
5. Edit `registration.intro`, confirm `/registration` updates within the cache window, and confirm there is no athlete registration form, payment, waiver, or account flow.

## Phase 10 Public Parity Routes

1. Confirm the typographic header lockup, grouped navigation, Registration CTA, Shop external link, footer links, and social links render on desktop and mobile.
2. Open Club and confirm About, Programs, Coaches, Team, and Hall of Fame are present. Open Resources and confirm Forms, Scholarship, and FAQs are present.
3. Verify Escape closes an open group and returns focus to its trigger, outside clicks close disclosures, active child routes highlight their parent group, and mobile navigation closes after selecting a destination.
4. Open `/forms` and confirm all available PDFs download from `apps/web/public/forms`.
5. Open `/scholarship` and confirm the BVN scholarship copy is respectful, links the scholarship form, and does not invent detailed criteria beyond the form.
6. Open `/hall-of-fame` and confirm the local RGN crest, Roland George Newton dedication, Dani Prunzik and Kaitlyn Eger photographs, meaningful alt text, and non-clickable inductee profiles render; open `/rgnhof` and confirm it redirects.
7. Open `/team` and confirm it describes team identity without publishing a private athlete roster.
8. Confirm `/registration` remains a Registration Hub with downloads/contact guidance only, not an online submission workflow.

## Phase 10 Figma Correction Pass

1. Compare the running public site against `C:\Users\Kerone Creary\source\repos\Youth Sports Website Concept`.
2. Confirm the public header uses the Figma-inspired black shell, thick red rule, skewed brand block, uppercase nav, Shop link, and red Registration CTA while keeping the real El1te logo.
3. Confirm the homepage uses the Figma-style dark track hero, red skew badge, huge uppercase display headline, and skewed CTA buttons.
4. Confirm public page headers use the black hero, red highlighted title word, and skewed red underline.
5. Confirm events, forms, gallery, coaches, sponsors, and contact use the adapted Figma patterns: date slabs, document rows, hover overlays, grayscale image cards, a centered sponsor logo wall, and the muted contact form panel.
6. Confirm these visual changes do not alter CMS/API contracts, Admin routes, online registration scope, or deferred payment/Azure/portal work.

## Responsive and Accessibility Pass

Check all primary routes around 375, 430, 768, 1280, and 1536 CSS pixels. Verify navigation, hero text, cards, filters, long copy, contact fields, sponsor logos, coach fallbacks, form cards, Hall of Fame cards, and footer do not overlap or scroll horizontally. Keyboard through the skip link, mobile menu, navigation, FAQ disclosures, filters, form fields, download links, and CTAs. Confirm visible focus and one `h1` per page.

On the homepage, confirm the black-on-red hero mark, white club name, and three red numeral `1`s remain legible at every breakpoint. Confirm the mission is a left-aligned vertical stack, shared text links use brand red, decorative green is absent, and semantic form-success green remains available.
