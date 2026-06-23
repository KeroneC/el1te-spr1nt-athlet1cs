# Media and Gallery Manual Testing

1. Apply migrations, run API and web, and sign in as the Development Admin.
2. In Admin Media upload JPEG, PNG, and WebP samples; confirm dimensions and previews. Confirm a text file, corrupt image, and file over 10 MB receive safe errors.
3. Edit metadata and active status. Use Choose in announcement, event, coach, sponsor, content block, and site logo forms; manual URLs must still work.
4. Create an album, add several existing assets, use arrow controls to reorder, select a cover, and save it as draft. Confirm it is absent from `/gallery`.
5. Publish it and verify list/detail images, captions, alt text, and mobile layout. Deactivate an asset and confirm it disappears publicly.
6. Reuse an asset in two albums. Delete one album and confirm the asset remains. Attempt to delete a referenced asset and expect `409`; remove references and delete a disposable asset.

Do not commit files from the API upload runtime directory.
