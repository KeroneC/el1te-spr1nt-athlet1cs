#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
api_project="$repo_root/apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj"
infrastructure_project="$repo_root/apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj"
environment_file="$repo_root/infra/local/.env"
web_environment_file="$repo_root/apps/web/.env.local"
keychain_service="El1te Spr1nt Athlet1cs Local Admin"

for command_name in colima docker dotnet node npm openssl security; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    echo "Missing required command: $command_name" >&2
    exit 1
  fi
done

dotnet_version="$(dotnet --version)"
node_major="$(node --version | sed -E 's/^v([0-9]+).*/\1/')"

if [[ "$dotnet_version" != 10.* ]]; then
  echo "This repository requires .NET 10; found $dotnet_version." >&2
  exit 1
fi

if (( node_major < 22 )); then
  echo "This repository requires Node.js 22 or newer; found $(node --version)." >&2
  exit 1
fi

umask 077

if [[ ! -f "$environment_file" ]]; then
  database_password="$(openssl rand -base64 36 | tr -d '\n' | tr '/+' 'Aa')"
  printf 'MSSQL_SA_PASSWORD=%s\n' "$database_password" > "$environment_file"
  echo "Created ignored SQL Server credentials at infra/local/.env."
fi

database_password="$(sed -n 's/^MSSQL_SA_PASSWORD=//p' "$environment_file" | head -n 1)"
if [[ -z "$database_password" || "$database_password" == "replace-with-a-strong-local-password" ]]; then
  echo "Set a strong MSSQL_SA_PASSWORD in $environment_file." >&2
  exit 1
fi

if [[ ! -f "$web_environment_file" ]]; then
  printf '%s\n' \
    '# Local macOS development uses the API HTTP launch profile.' \
    'API_BASE_URL=http://localhost:5126' \
    'SITE_URL=http://localhost:3000' \
    'NEXT_PUBLIC_API_BASE_URL=http://localhost:5126' \
    > "$web_environment_file"
  echo "Created ignored frontend configuration at apps/web/.env.local."
fi

dotnet tool update --global dotnet-ef --version 10.0.9 >/dev/null
dotnet restore "$repo_root/apps/api/El1teSpr1ntTrack.sln"
npm ci --prefix "$repo_root/apps/web"

connection_string="Server=localhost,1433;Database=El1teSpr1ntTrack_Dev;User Id=sa;Password=${database_password};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "$connection_string" --project "$api_project" >/dev/null

if ! dotnet user-secrets list --project "$api_project" | grep -q '^Jwt:Key = '; then
  jwt_key="$(openssl rand -base64 48 | tr -d '\n')"
  dotnet user-secrets set "Jwt:Key" "$jwt_key" --project "$api_project" >/dev/null
fi

if ! dotnet user-secrets list --project "$api_project" | grep -q '^SeedAdmin:Email = '; then
  admin_email="${EL1TE_LOCAL_ADMIN_EMAIL:-local-admin@el1tespr1nt.local}"
  admin_password="${EL1TE_LOCAL_ADMIN_PASSWORD:-$(openssl rand -base64 24 | tr -d '\n' | tr '/+' '!A')}"

  dotnet user-secrets set "SeedAdmin:Email" "$admin_email" --project "$api_project" >/dev/null
  dotnet user-secrets set "SeedAdmin:Password" "$admin_password" --project "$api_project" >/dev/null
  dotnet user-secrets set "SeedAdmin:FirstName" "Local" --project "$api_project" >/dev/null
  dotnet user-secrets set "SeedAdmin:LastName" "Admin" --project "$api_project" >/dev/null
  security add-generic-password -a "$admin_email" -s "$keychain_service" -w "$admin_password" -U >/dev/null
  echo "Saved the generated local admin login in macOS Keychain."
fi

"$repo_root/scripts/local-dev/mac-start-database.sh"

dotnet ef database update \
  --project "$infrastructure_project" \
  --startup-project "$api_project" \
  --connection "$connection_string"

echo
echo "Mac bootstrap complete."
echo "Open the repository with: code \"$repo_root\""
echo "In VS Code, run the task: Dev: Start full stack"
echo "The local admin login is stored in Keychain under: $keychain_service"
