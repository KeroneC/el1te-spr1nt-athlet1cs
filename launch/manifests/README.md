# Production promotion manifests

Generate a manifest with the protected **Promote Reviewed Production Content** workflow. Download the artifact, review every `include` value and dependency, and commit the approved JSON here through a pull request. The importer verifies the manifest hash, media hashes, environment names, prohibited record types, and dependencies before applying it.

These manifests may contain only already-approved public CMS/catalog content. Never add users, contact submissions, athletes, documents, orders, refunds, webhook data, secrets, or telemetry.

For the initial full-catalog staging pass, run export with `include_all_products=true`. This selects every product graph, including drafts, while preserving the normal selective behavior when the input is false. Review the resulting JSON and exclude all shelved All-American archive records before committing it. Apply that launch manifest with `force_products_draft=true`; the importer then forces every selected product to Draft and non-featured and resets all variant quantities to zero.
