# CI Troubleshooting

| Symptom | How to verify | Resolution |
| --- | --- | --- |
| Workflow does not run | Check target branch and changed paths | Include the relevant path or use manual dispatch |
| .NET restore fails | Open Restore log and NuGet source response | Confirm .NET 10, `NuGet.Config`, network, and package availability |
| Backend build/test fails | Reproduce exact Release command locally | Fix the first error; inspect uploaded `.trx` results after test failures |
| EF pending-model check fails | Run the same `dotnet ef` command | Create/review a migration and snapshot; never update a database in CI |
| Local EF bundle reports access denied | Confirm model drift check still passes | Generate `artifacts/database/migrations.sql` with `build-migration-script.ps1`; CI remains responsible for the Linux bundle |
| `npm ci` fails | Compare `package.json` and lockfile | Regenerate and commit the lockfile with the intended npm version |
| Next build says API URL missing | Check job `API_BASE_URL` | Supply a server-only non-secret URL; never convert it to `NEXT_PUBLIC_*` for admin auth |
| Docs validation fails | Run `./scripts/validation/validate-docs.ps1` | Fix the reported relative link, literal path, or fence |
| Secret scan fails | Treat finding as real until disproven | Remove and rotate credentials; use a narrow documented fingerprint allowlist only for verified test data |
| Artifact missing | Check the producing step succeeded | Fix publish/staging first; uploads intentionally do not mask failures |
| Run was cancelled | Compare concurrency group/ref | A newer CI run superseded it; inspect the latest run |

Workflow YAML should be reviewed and parsed locally where practical, but behavior is authoritative only on GitHub Actions. The Azure workflow is manual-only and should not be exercised until [future Azure setup](future-azure-setup.md) is complete.

On a Windows machine with script execution disabled, run repository PowerShell validation with `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/validation/validate-docs.ps1`. This process-scoped bypass does not change the machine policy.

Recommended `main` branch protection in GitHub Settings -> Branches/Rulesets:

1. Require a pull request and conversation resolution.
2. Require Backend, Frontend, Documentation, and Secret Scan checks.
3. Require the branch to be current before merge.
4. Prevent force pushes and deletion.
5. Optionally require at least one approval/code owner as the team grows.

Dependabot runs weekly for NuGet, npm, and GitHub Actions, groups compatible minor/patch updates, and limits open pull requests. It never auto-merges; every update still requires CI and review.
