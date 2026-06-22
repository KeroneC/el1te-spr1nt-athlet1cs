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
3. Verify active sponsors are tier-ordered, missing logos use text, and external links open safely.
4. Verify FAQ category groups and keyboard-operable disclosure controls; deactivate a test FAQ and confirm it disappears.

## Contact and Registration

1. Submit valid contact data and confirm the success state clears the message.
2. Confirm the submission appears only in Admin Contact Submissions.
3. Submit missing/invalid fields and confirm associated field errors appear.
4. Stop the API, retry, and confirm a safe retryable error with no internal details.
5. Edit `registration.intro`, confirm `/registration` updates within the cache window, and confirm there is no athlete registration form, payment, waiver, or account flow.

## Responsive and Accessibility Pass

Check all primary routes around 375, 430, 768, 1280, and 1536 CSS pixels. Verify navigation, hero text, cards, filters, long copy, contact fields, sponsor logos, coach fallbacks, and footer do not overlap or scroll horizontally. Keyboard through the skip link, mobile menu, navigation, FAQ disclosures, filters, form fields, and CTAs. Confirm visible focus and one `h1` per page.
