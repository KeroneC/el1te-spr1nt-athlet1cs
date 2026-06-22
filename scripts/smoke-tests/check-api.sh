#!/usr/bin/env bash
set -euo pipefail

base_url="${1:-${API_BASE_URL:-}}"
if [[ -z "$base_url" ]]; then
  echo "Usage: check-api.sh <api-base-url>"
  exit 2
fi
base_url="${base_url%/}"

request() {
  local path="$1"
  local expected="${2:-}"
  local body
  for attempt in {1..12}; do
    if body=$(curl --fail --silent --show-error --max-time 10 "$base_url$path"); then
      if [[ -z "$expected" || "$body" == *"$expected"* ]]; then
        echo "PASS $path"
        return 0
      fi
    fi
    sleep 5
  done
  echo "FAIL $path"
  return 1
}

request "/health" '"status":"healthy"'
request "/health/ready" '"status":"healthy"'
request "/api/public/site-settings"
request "/api/public/announcements?page=1&pageSize=1"
