# Private historical photos: demo pilot

Historical Photos is a separate SuperAdmin workspace under **Admin → Media**. It is not the public Media Library, gallery, or All-American archive. Nothing imported here is published or added to the public-content promotion manifest.

## Access and deployment

1. Use the reviewed immutable main release in **Deploy Azure Demo**, with the normal demo settings and `enable_private_historical_photos=true`. Keep the existing Square Sandbox and internal Shop settings. Production ignores the flag and does not provision the container.
2. Verify the demo SQL migration and the private `historical-photos` container. The demo App Service identity already has account-level Blob access. Before the import, verify the demo SQL server, database, and storage account match the approved deployment. Never reuse the production identity.
3. Use an approved demo Entra operator. Grant time-limited **Storage Blob Data Contributor** on the archive container only. Have the demo SQL administrator create the operator as a contained Entra user if needed and grant only `SELECT, INSERT` on `dbo.LegacyPhotos`, `dbo.LegacyPhotoSources` and `dbo.LegacyPhotoPages`. Do not grant migration, update, delete, or public-media rights to the importer. Use a temporary single-IP SQL firewall rule if necessary; revoke the grants and remove the rule when done.

## Verify, import, and reconcile

Keep the backed-up originals and manifest together on approved local storage. Do not put them in Git, GitHub Actions artifacts, or logs. The verified archive manifest SHA-256 is `b235eb91ed1df47a99de3c777bfa0f685696d1409a0c5877f2234550ee05a10d`.

```bash
dotnet run --project apps/api/tools/El1teSpr1ntTrack.LegacyPhotoImport -- \
  --manifest "/path/to/squarespace-original-archive/manifest.json" \
  --manifest-sha256 b235eb91ed1df47a99de3c777bfa0f685696d1409a0c5877f2234550ee05a10d
```

- The default verifies a deterministic 24-photo pilot with representative collections, duplicate sources, multi-page provenance, both file types, and both orientations. It does not contact Azure.
- After dry-run succeeds, sign in with the demo operator using Azure CLI, then add `--apply --confirm-demo el1tesprint-demo-neauu2`. This tool can write only to the named demo environment.
- If interrupted, rerun the same command. The importer never overwrites stored bytes or staff decisions. Checksum conflicts stop that photo and are reported by hash; investigate before attempting manual repair. A successful pilot should verify 24 photos with no failures.
- After launch **and** pilot sign-off, run the full dry-run with `--all`. To apply the full import, additionally pass `--apply --confirm-demo el1tesprint-demo-neauu2 --launch-complete`. Verify 595 unique photos, 602 source URLs, all page associations, and zero failures. Do not run a production import.

## Review and rollback

Verify a SuperAdmin can filter by collection and review state, approve a photo only after confirming club permission, and download a metadata-stripped JPEG. Set it to Restricted and verify direct download stops working. Also verify ordinary Admin and anonymous denial, public `/media` not found for the archive photo ID, and the usual public gallery still works.

If the pilot threatens launch or demo health, redeploy demo with the archive flag off. This hides the UI and disables the archive endpoints without deleting the database records or blobs. Keep the verified local backup independently of any deployment.
