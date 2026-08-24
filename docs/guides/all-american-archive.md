# All-American Archive Management

The All-American archive celebrates verified El1te results from the AAU Junior Olympic Games without using private athlete-registration records. The public archive is feature-gated with `ALL_AMERICANS_ARCHIVE_ENABLED`; demo enables it for review and production keeps it disabled until the club approves the content.

## Create an annual story

1. Upload the original approved photographs in **Admin → Media**. Use a meaningful title and alt text. Sources may be JPEG, PNG, or WebP up to 10 MB; the media pipeline keeps the original and generates responsive derivatives.
2. Open **Admin → All-Americans → Create year**. Enter the year, annual title, verified athlete and medal totals, summary, and display order. Save it as a draft.
3. Edit the year, add annual media, arrange it with the arrow controls, and choose one of those assets as the hero image.
4. Publish with **Athlete and result details complete** turned off when only the verified annual summary and photographs are ready. The public page deliberately omits athlete/event sections in this state.
5. Add the annual roster and verified performances later. An individual performance requires one recipient; a relay requires at least two. One athlete may be linked to multiple performances.
6. Turn on **Athlete and result details complete** only when the active roster count equals the verified athlete total and the active performance-recipient count equals the medal total.

Deleting a year from the list is reversible deactivation: it unpublishes the year without removing its history. Athlete and performance removal similarly deactivates the record. Referenced hero, annual, and athlete-photo Media Assets cannot be deleted until the archive reference is changed.

## Public behavior

- `/all-americans` lists published years newest first.
- `/all-americans/{year}` shows the annual hero, verified totals, summary, and editorial media composition.
- Summary-only years never display incomplete names, events, or speculative results.
- Completed years display each athlete once with all linked individual and relay medals.
- Disabled or unpublished archive routes return 404 and are omitted from navigation and the sitemap.

The selective launch-promotion manifest includes annual records and their Media dependencies, but deployment operators must still review `include` values before applying production promotion.
