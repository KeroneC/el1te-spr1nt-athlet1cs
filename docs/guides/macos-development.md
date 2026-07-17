# macOS Development

This is the maintained setup for an Apple Silicon Mac. It keeps source code in GitHub, local credentials outside Git, and the SQL-compatible local-development database in a persistent Docker volume.

## What Is Different From Windows

- SQL Server Express LocalDB is Windows-only. macOS uses the repository's pinned compatibility container on `localhost:1433`.
- The API uses its HTTP launch profile at `http://localhost:5126`, avoiding local certificate-store differences.
- Shell commands use `npm` rather than the Windows-specific `npm.cmd` form.
- The bootstrap stores the generated Development admin login in macOS Keychain and all application secrets in .NET User Secrets.

Microsoft's supported SQL Server Linux image requires x86-64 Linux, while full x86 emulation is not practical on this 8 GB Apple Silicon laptop. The Mac compose file therefore pins the last native ARM Azure SQL Edge image by digest and uses it strictly as an isolated **local-development compatibility database**. [Azure SQL Edge retired on September 30, 2025](https://learn.microsoft.com/en-us/lifecycle/products/azure-sql-edge), receives no security or product updates, and must never be exposed to a network or used for production. Windows development, CI, and Azure deployment continue to use supported SQL Server.

## Prerequisites

- Git and [GitHub CLI](https://cli.github.com/)
- [Visual Studio Code](https://code.visualstudio.com/) with the `code` shell command
- .NET 10 SDK compatible with `global.json`
- Node.js 22 or newer and npm
- Homebrew `colima`, `docker`, and `docker-compose`

Install the container prerequisites together:

```bash
brew install colima docker docker-compose
```

Confirm the toolchain:

```bash
git --version
gh --version
code --version
dotnet --version
node --version
npm --version
colima version
docker version
```

The repository recommends C# Dev Kit, C#, ESLint, Prettier, and Containers when opened in VS Code.

## First-Time Setup

Clone the project once on the Mac:

```bash
git clone https://github.com/KeroneC/el1te-spr1nt-athlet1cs.git
cd el1te-spr1nt-athlet1cs
```

Then run the idempotent bootstrap:

```bash
./scripts/local-dev/mac-bootstrap.sh
```

The script:

1. verifies .NET and Node.js versions;
2. creates ignored local database and frontend environment files;
3. installs EF Core CLI 10.0.9 and locked npm dependencies;
4. restores the .NET solution;
5. generates a JWT key and a disposable Development SuperAdmin;
6. stores the admin login in macOS Keychain;
7. starts SQL Server and applies all EF Core migrations.

The generated files and secrets are deliberately machine-local. Run the bootstrap separately on each development computer; never copy them through Git.

## Open and Run in VS Code

```bash
code .
```

In VS Code, open **Terminal > Run Task** and choose **Dev: Start full stack**. This starts:

- the SQL-compatible development database on `localhost:1433`;
- the API on `http://localhost:5126`;
- Next.js on `http://localhost:3000`.

Useful URLs:

- Swagger: `http://localhost:5126/swagger`
- API health: `http://localhost:5126/health`
- API readiness: `http://localhost:5126/health/ready`
- Public website: `http://localhost:3000`
- Admin: `http://localhost:3000/admin`

Open **Keychain Access**, search for **El1te Spr1nt Athlet1cs Local Admin**, and reveal the saved password when you need the local Admin login. The default generated email is `local-admin@el1tespr1nt.local`.

The **API: Debug (HTTP)** Run and Debug profile starts the backend with breakpoints. Start the database first. The **Check: Web** and **Build: API** tasks provide quick validation from the editor.

## Terminal Workflow

Start the database:

```bash
./scripts/local-dev/mac-start-database.sh
```

Start the API:

```bash
dotnet run --project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj --launch-profile http
```

In a second terminal, start the web app:

```bash
cd apps/web
npm run dev
```

Stop the apps with `Ctrl+C`. Stop the database without deleting its data:

```bash
./scripts/local-dev/mac-stop-database.sh
```

The named Docker volume preserves the database between container restarts. `docker compose down -v` deletes that local database and should only be used for an intentional reset.

## Move Between Computers Safely

GitHub synchronizes source code and documentation; it does not synchronize secrets, dependencies, build output, uploads, or the local database.

Before beginning work on either computer:

```bash
git status
git pull --rebase
```

Do work on a branch, then commit and push before changing computers:

```bash
git switch -c feature/short-description
git add <files-you-intend-to-commit>
git commit -m "Describe the change"
git push -u origin HEAD
```

On the other computer, fetch and switch to the same branch:

```bash
git fetch --prune
git switch feature/short-description
git pull --rebase
```

Authenticate each computer once with `gh auth login`, and use the same Git author identity. Never begin a machine switch with uncommitted work that exists on only one computer.

## Validate the Setup

```bash
dotnet build apps/api/El1teSpr1ntTrack.sln --configuration Release
dotnet test apps/api/El1teSpr1ntTrack.sln --configuration Release

cd apps/web
npm run lint
npm run typecheck
npm run test
npm run build
```

The Playwright cross-stack workflow still targets Windows LocalDB and is run by GitHub Actions on Windows. See [testing strategy](../architecture/testing-strategy.md).

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| Docker cannot connect | Re-run the database script; it starts Colima automatically. |
| SQL Server container is unhealthy | Run `docker --context colima logs el1te-sqlserver`; confirm port 1433 is free. |
| API still tries `(localdb)` | Re-run the bootstrap so `ConnectionStrings:DefaultConnection` is stored in User Secrets. |
| EF reports version 8 | Run `dotnet tool update --global dotnet-ef --version 10.0.9`. |
| Next.js cannot reach the API | Confirm `apps/web/.env.local` uses `http://localhost:5126` and restart Next.js. |
| Git push asks for credentials | Run `gh auth login`, then `gh auth setup-git`. |

See the general [troubleshooting guide](troubleshooting.md) for application-specific failures.
