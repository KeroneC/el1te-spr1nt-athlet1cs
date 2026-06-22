# Announcements Testing

Start both applications, sign in as a disposable Development SuperAdmin, and use a title that clearly marks local test data.

## Draft and Public Visibility

1. Open `/admin/announcements/new`.
2. Enter title, summary, and body. Leave Published and Featured off.
3. Save and note the displayed slug without copying identifiers into shared material.
4. Confirm the record appears in the admin list with Draft status.
5. Search its title and filter Published = Draft.
6. Request `/api/public/announcements` and its public slug route; confirm the draft is absent and detail returns `404`.

## Publish, Edit, and Feature

1. Edit the draft, enable Published, use a publish time at or before now, and save.
2. Confirm public collection/detail now returns it.
3. Change the title and content. Confirm the public slug remains unchanged.
4. Enable Featured and confirm admin Featured filter finds it.
5. If a public featured filter is used, confirm it appears with `featured=true`.

## Scheduling and Expiration

1. Set Published with a future publish time; confirm admin status Scheduled and public absence.
2. Set publish time in the past and expiration in the future; confirm Published and public visibility.
3. Set expiration in the past; confirm Expired, public absence, and admin visibility only when Include expired is enabled.
4. Remember that the form interprets values in browser local time and sends UTC.

## Search, Filters, and Pagination

Verify search by title/summary, Published and Featured filters, Include expired, clear filters, page navigation, and page reset behavior after changing filters. Pagination requires more than the configured page size; use disposable records only and remove them afterward.

## Validation and Errors

- Submit blank required fields and confirm inline frontend messages.
- Enter a non-HTTP(S), non-root-relative image URL and confirm rejection.
- Put expiration before publication and confirm rejection.
- If bypassing frontend validation with Swagger, confirm backend `400` Problem Details.
- Request a nonexistent admin GUID and confirm `404`.
- Repeat after logout for `401`; use a Parent token in Swagger for `403`.

Ordinary creation does not produce a duplicate-slug conflict: `SlugGenerator` creates `title`, `title-2`, and so on. A `409` remains the API/database response for uniqueness races or conflicting direct data. Do not corrupt the local database merely to force this rare path; `SlugGeneratorTests` and service tests are the repeatable verification.

## Deletion and Cleanup

1. Select delete and confirm the dialog names the record.
2. Cancel once and verify the item remains.
3. Confirm deletion and verify it disappears from admin and public APIs.
4. Remove every disposable record created during the session.

Deletion is permanent; never perform this test against shared or production data.
