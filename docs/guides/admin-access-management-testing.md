# Admin Access Management Testing

## Local setup

1. Apply current EF Core migrations and run the API with a seeded SuperAdmin.
2. Run the Next.js application with `API_BASE_URL` pointing to that API.
3. Set `AdminInvitations:SiteUrl` to the frontend origin. The default is `http://localhost:3000`.
4. Sign in as the seeded SuperAdmin and open `/admin/users`.

## Invitation flow

1. Create an invitation for a unique email address and choose Admin or SuperAdmin.
2. Confirm the invitation appears as Pending and expires 72 hours after creation.
3. Copy the displayed link. The secret must appear only after `#token=` and must not be copied into logs or documentation.
4. Open the link in a private browser session, confirm the recipient identity, and create a password meeting the displayed requirements.
5. Sign in as the invited account.
6. Confirm an Admin cannot see or open Access control. Confirm a SuperAdmin can.
7. Confirm the accepted invitation cannot be reused, revoked, or reissued.

## Guard checks

- Attempt to change the signed-in SuperAdmin's own role or status. The API must return `409` and make no change.
- With one active SuperAdmin remaining, attempt to demote or deactivate that account from another authorized session. The API must return `409`.
- Deactivate an Admin and confirm the next protected API request is denied even if the old JWT has not expired.
- Reissue a pending invitation and confirm the old link stops working while the new link succeeds.
- Revoke a pending invitation and confirm its link stops working.

## Activity history

Open `/admin/users/activity` and confirm successful invitation creation, reissue, revocation, acceptance, role changes, and status changes are attributable and timestamped in UTC. Activity must not contain passwords, raw invitation secrets, JWTs, cookies, request bodies, or private youth data. No activity update/delete UI or API exists.

## Automated validation

```powershell
cd apps/api
dotnet build -c Release
dotnet test -c Release

cd ../web
npm.cmd run lint
npm.cmd run typecheck
npm.cmd test
npm.cmd run build
npm.cmd run test:e2e
```

The identity Playwright scenario creates a real invitation, accepts it as a new Admin, signs in, and confirms the SuperAdmin boundary.
