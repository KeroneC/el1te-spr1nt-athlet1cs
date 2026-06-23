param([string]$Output = "artifacts/database/migrations.sql")

$ErrorActionPreference = "Stop"
$directory = Split-Path -Parent $Output
if ($directory) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }

dotnet ef migrations has-pending-model-changes `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj `
  --configuration Release `
  --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet ef migrations script `
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj `
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj `
  --configuration Release `
  --no-build `
  --idempotent `
  --output $Output
exit $LASTEXITCODE
