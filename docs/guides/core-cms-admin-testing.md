# Core CMS Admin Testing

Start the API and web app using [Local development](local-development.md), then open `http://localhost:3000/admin`. Use disposable names and restore any seeded value you edit.

## Authentication

1. Confirm logged-out `/admin` redirects to login.
2. Sign in with a Development Admin or SuperAdmin and confirm all CMS navigation links appear.
3. Confirm a Parent account receives the administrative-access error.
4. Log out and confirm `/admin` is protected again.

## Module Checklist

- Events: create a draft, edit dates, publish, filter by type/status, verify the public API, then permanently delete it.
- Coaches: create with a private email, verify public email omission, enable public email, then deactivate and verify public omission.
- Sponsors: create with a tier and URL, edit it, deactivate it, and verify public omission.
- FAQs: create, edit answer/order, deactivate, and verify public omission.
- Content: create a unique key, publish/unpublish it, verify public visibility, test a duplicate key, and permanently delete the disposable block. Treat seeded keys carefully.
- Site Settings: change one safe value, save and reload, restore it, and verify the `SiteSettings` table still contains one row.
- Contact Submissions: create through the public endpoint, filter/open it in admin, move through Read/Resolved/Archived, and permanently delete the disposable submission.

Test `400`, `401`, `403`, `404`, and `409` paths where practical. UI messages must remain safe and must never show tokens, SQL details, connection strings, or raw exceptions.

## Automated Checks

```powershell
cd apps/web
npm.cmd run lint
npm.cmd run typecheck
npm.cmd test
$env:API_BASE_URL = "https://api.example.invalid"
npm.cmd run build
```

No Phase 7 data-model change is expected. `dotnet ef migrations has-pending-model-changes` should remain clean.
