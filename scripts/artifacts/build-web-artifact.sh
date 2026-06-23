#!/usr/bin/env bash
set -euo pipefail

web_dir="${1:-apps/web}"
output="${2:-artifacts/web}"

test -f "$web_dir/.next/standalone/apps/web/server.js"
rm -rf "$output"
mkdir -p "$output"
cp -R "$web_dir/.next/standalone/." "$output/"
mkdir -p "$output/apps/web/.next"
cp -R "$web_dir/.next/static" "$output/apps/web/.next/static"
cp -R "$web_dir/public" "$output/apps/web/public"

echo "Web artifact staged at $output; start with: node apps/web/server.js"
