# Media and Gallery Manual Testing

1. Apply migrations, run API and web, and sign in as the Development Admin.
2. In Admin Media queue several JPEG, PNG, and WebP samples. Confirm editable title defaults, required per-image alt text, optional captions, previews, and the 20-file limit. Submit once with one alt-text field blank: the exact field must be visibly marked, its error must be announced, and focus must move to the first invalid control. Confirm a text file and file over 10 MB are skipped safely; the API must still reject corrupt encoded images.
3. Upload with and without optional direct album assignment. Confirm three-at-a-time progress, successful partial results, retry behavior, and that retrying a failed album assignment does not upload a duplicate asset.
4. Edit metadata and active status. Use Choose in announcement, event, coach, sponsor, content block, and site logo forms; search, pagination, manual URLs, clear, and selection must work.
5. Create an album, open Add images, search for several existing assets, add them, use arrow controls to reorder, select a cover, and save it as draft. Confirm it is absent from `/gallery`.
6. Publish it and verify list/detail images, captions, alt text, and mobile layout. Deactivate an asset and confirm it disappears publicly.
7. Reuse an asset in two albums. Delete one album and confirm the asset remains. Attempt to delete a referenced asset and expect `409`; remove references and delete a disposable asset.

Sponsor marks have additional canvas and review requirements. Follow [Sponsor logo image guide](sponsor-logo-images.md) before uploading or replacing a sponsor logo.

Do not commit files from the API upload runtime directory.
