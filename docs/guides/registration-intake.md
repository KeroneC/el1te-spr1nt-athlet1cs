# Seasonal registration and returning check-in

The Registration page has two parent/guardian paths, separate from Contact, athlete accounts, the roster and Square. The application shows a reference immediately after SQL saves it. No receipt or decision email is sent by the application. Staff send follow-up, waitlists and offers separately from the club inbox.

## Before opening

1. Deploy the reviewed immutable artifact and additive migration through the standard release workflow. Keep intake closed. Verify the canonical `/registration` route, noindex in demo, and the active Admin review workspace.
2. In **Admin → Registration → Season Settings**, enter the approved season title, November 27 opening date (Eastern), end date, eligible age groups, practice site/schedule, fees, realistic reply window, designated club paperwork inbox, in-person handoff instructions, and review and retention owners. Saving a draft does not expose those details publicly. After a closed season is edited, staff must approve it again by opening.
3. Have the club approve the blank forms and privacy notice for the short intake (guardian contact, athlete first name, eligibility and prior-club information, staff access and retention). Check Registration, Forms and FAQ wording, the review queue, and the paper fallback. Do not use real families in demo. No seed seasons are shipped.
4. On Friday, November 27, confirm launch checks and manually select **Open intake**. The API rejects new submissions before the configured Eastern date or while closed. Verify the live flow before starting ads. **Close intake** stops new submissions without removing saved records.

Ads can use `/registration?src=facebook` or `/registration?src=instagram`. The season totals count visits (not unique visitors), new applications, check-ins and enrollment by the three fixed channels, including Direct / other. Unknown tags become Direct. No ad pixel, tracking cookies or family data are sent to ad platforms.

## Review and paperwork

- Use **New Applications** or **Returning Check-Ins**. Search stays in the private Admin form, not the URL. Follow up, waitlist, offer, close or reopen as appropriate. Status changes never contact a family; send the decision manually.
- An offer allows paperwork while a prior-club case is unresolved. Verify any applicable AAU and USATF steps outside the app. Do not mark an athlete competition-ready until they are enrolled and transfer review is cleared or not needed.
- Send the approved inbox or in-person instructions only after an offer. Ask families to put their receipt reference in the email subject and use PDFs or clear photos, or hand in the packet in person. Do not advertise texting or upload completed forms to the app. Photo consent is optional and tracked separately from enrollment.
- At least 90 days after the season ends, the retention owner verifies the season is closed and asks an authorized SuperAdmin to confirm closeout in Season Settings. This permanently removes SQL submission records and linked review history while retaining channel totals. It does not delete club email, paper files, or any copies in existing backups; follow the club’s separate retention procedure for those. Before demo sign-off, use only synthetic submissions and a clearly labeled season.
