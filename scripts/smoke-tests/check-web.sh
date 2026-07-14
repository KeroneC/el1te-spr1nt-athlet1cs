#!/usr/bin/env bash
set -euo pipefail

base_url="${1:-${WEB_BASE_URL:-}}"
if [[ -z "$base_url" ]]; then
  echo "Usage: check-web.sh <web-base-url>"
  exit 2
fi
base_url="${base_url%/}"

request() {
  local path="$1"
  local marker="$2"
  local body
  local attempts="${SMOKE_TEST_ATTEMPTS:-40}"
  for ((attempt = 1; attempt <= attempts; attempt++)); do
    if body=$(curl --fail --silent --show-error --max-time 10 "$base_url$path") && [[ "$body" == *"$marker"* ]]; then
      echo "PASS $path"
      return 0
    fi
    sleep 5
  done
  echo "FAIL $path"
  return 1
}

request "/" "El1te Spr1nt Athlet1cs"
request "/news" "Club announcements"
request "/events" "Schedule"
request "/gallery" "Gallery"
request "/registration" "Registration information"
request "/admin/login" "Admin sign in"
