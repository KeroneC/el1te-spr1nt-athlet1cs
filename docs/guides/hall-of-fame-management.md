# Hall of Fame Management

The RGN El1te Hall of Fame keeps its memorial presentation while inductee profiles are managed from the protected Admin workspace.

## Admin workflow

1. Sign in as an active Admin or SuperAdmin and open **Hall of Fame**.
2. Use search, status, or induction-year filters to find an existing profile.
3. Choose **Create inductee** to add a record. Name, affiliation, and summary are always required.
4. Select an image from Media or enter a root-relative or HTTP(S) URL. Write alt text that identifies the person and useful context in the image.
5. Leave **Active** off to save an incomplete draft. An active record requires a valid photo URL and meaningful alt text.
6. Set a nonnegative display order. Lower values appear first; equal values are ordered by name.
7. Use deactivate to hide a profile without deleting its history. Edit an inactive profile and enable **Active** to restore it.

Names may change, but the generated slug stays stable. The public site does not expose Admin identifiers, timestamps, or inactive records. An image referenced by any Hall of Fame record, including an inactive draft, cannot be deleted from Media until that reference is changed.

## Canonical photo rollout

The migration initially uses the two existing root-relative photographs as deployment-safe fallbacks. After a new environment is deployed:

1. Upload the original Dani Prunzik and Kaitlyn Eger JPEG files through that environment's Media Library.
2. Preserve the original bytes; neither file needs resizing because both are well below the 10 MB upload limit.
3. Use descriptive Media titles and alt text, then select each uploaded asset on the matching Hall of Fame record.
4. Confirm the public page is serving both photographs from `/media/{id}` and that their crop and card presentation match the static fallbacks.
5. Keep the static files in the frontend as a rollback path. Each environment must use its own Media records instead of copying environment-specific Media URLs into the EF migration.

Development stores uploaded bytes through `LocalMediaStorage`; the Azure demo and production configuration use private Blob storage through `AzureBlobMediaStorage`. Both providers serve active assets through the same public Media endpoint, so the Hall of Fame rendering does not depend on the storage provider.

The current Azure demo Media records prepared for Hall of Fame rollout are:

| Inductee | Demo Media URL |
|---|---|
| Dani Prunzik | `https://el1tesprint-demo-neauu2-api.azurewebsites.net/media/781d023e-1f2d-489e-8e08-4dc84e63f7ac` |
| Kaitlyn Eger | `https://el1tesprint-demo-neauu2-api.azurewebsites.net/media/07651fec-f945-409e-95cd-d6bb42f36b82` |

Both Azure-served files were verified as exact SHA-256 byte matches with the repository originals on July 31, 2026. The Media upload queue processes files one at a time because simultaneous writes stalled during Azure verification; queued-file limits and per-file failure recovery remain unchanged.

## Public behavior

`/hall-of-fame` requests eight active inductees per page from `GET /api/public/hall-of-fame-inductees`. The memorial hero, crest, dedication, cards, and contact call-to-action remain unchanged. A configured induction year appears as `Class of YYYY · Affiliation`; otherwise only the affiliation is shown. Public CMS reads may remain cached for approximately 60 seconds.

## Verification checklist

- Confirm Dani Prunzik and Kaitlyn Eger appear with their existing wording and photographs after the migration.
- Upload the two canonical photographs to Media, switch their records to the managed URLs, and confirm the downloaded bytes match the originals.
- Create an inactive draft without a photo and confirm it saves but does not appear publicly.
- Attempt to activate it without a photo or alt text and confirm friendly field errors appear.
- Add Media, activate the record, and allow up to 60 seconds for public visibility.
- Rename the record and confirm its slug does not change.
- Verify year, active-state, and name/affiliation filters.
- Deactivate the record and confirm it disappears publicly while remaining editable in Admin.
- With more than eight active local records, verify Previous/Next navigation and consistent one-column mobile/two-column desktop cards.
- Confirm keyboard focus, photo alt text, mobile widths, and no horizontal overflow.

Use disposable records for local testing. Do not deploy pagination test records.
