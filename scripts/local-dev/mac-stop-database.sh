#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

docker --context colima compose \
  --env-file "$repo_root/infra/local/.env" \
  -f "$repo_root/infra/local/compose.yml" \
  down
