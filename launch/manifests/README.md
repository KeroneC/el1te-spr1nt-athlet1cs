# Production promotion manifests

Generate a manifest with the protected **Promote Reviewed Production Content** workflow. Download the artifact, review every `include` value and dependency, and commit the approved JSON here through a pull request. The importer verifies the manifest hash, media hashes, environment names, prohibited record types, and dependencies before applying it.

These manifests may contain only already-approved public CMS/catalog content. Never add users, contact submissions, athletes, documents, orders, refunds, webhook data, secrets, or telemetry.
