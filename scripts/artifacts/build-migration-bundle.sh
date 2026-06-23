#!/usr/bin/env bash
set -euo pipefail

output="${1:-artifacts/database/efbundle}"
mkdir -p "$(dirname "$output")"

dotnet ef migrations has-pending-model-changes \
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj \
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj \
  --configuration Release \
  --no-build

dotnet ef migrations bundle \
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj \
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj \
  --configuration Release \
  --no-build \
  --output "$output" \
  --force

echo "Migration bundle created at $output"
