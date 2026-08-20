#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
workflow="$script_dir/../../.github/workflows/deploy-production.yml"

scale_line=$(grep -n "name: Temporarily scale the publishing worker" "$workflow" | cut -d: -f1)
deploy_line=$(grep -n "name: Deploy and verify immutable applications" "$workflow" | cut -d: -f1)
restore_line=$(grep -n "name: Restore the approved App Service tier" "$workflow" | cut -d: -f1)
cleanup_line=$(grep -n "name: Remove temporary SQL firewall access" "$workflow" | cut -d: -f1)

[[ -n "$scale_line" && -n "$deploy_line" && -n "$restore_line" && -n "$cleanup_line" ]]
(( scale_line < deploy_line ))
(( deploy_line < restore_line ))
(( restore_line < cleanup_line ))

grep -q -- '--sku B2' "$workflow"
grep -q -- '--sku B1' "$workflow"
grep -q "steps.publishing_scale.outcome == 'success'" "$workflow"

echo "Production publishing scale guard tests passed."
