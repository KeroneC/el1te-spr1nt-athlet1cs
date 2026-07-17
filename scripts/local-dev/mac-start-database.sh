#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
compose_file="$repo_root/infra/local/compose.yml"
environment_file="$repo_root/infra/local/.env"

if [[ ! -f "$environment_file" ]]; then
  echo "Missing $environment_file. Run ./scripts/local-dev/mac-bootstrap.sh first." >&2
  exit 1
fi

if ! colima status >/dev/null 2>&1; then
  colima start
fi

docker --context colima compose --env-file "$environment_file" -f "$compose_file" up -d --wait
echo "Local SQL Server is healthy on localhost:1433."
