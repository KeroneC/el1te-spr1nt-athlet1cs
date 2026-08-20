#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/../.." && pwd)

check_bootstrap_block() {
  local workflow="$1"
  local step_name="$2"
  local block

  block=$(awk -v step="$step_name" '
    index($0, "- name: " step) { capture = 1 }
    capture { print }
    capture && /--bootstrap-admin/ { exit }
  ' "$workflow")

  [[ -n "$block" ]]

  local required
  for required in \
    'AdminInvitations__SiteUrl=' \
    'AdminInvitations__ExpiresHours=72' \
    'AuthFeatures__AllowPublicRegistration=false' \
    'TransactionalEmail__Provider=AzureCommunicationServices' \
    'TransactionalEmail__ConnectionString=' \
    'TransactionalEmail__SenderAddress=' \
    'TransactionalEmail__AdminSiteUrl='; do
    grep -q "$required" <<<"$block"
  done
}

check_bootstrap_block "$repo_root/.github/workflows/deploy-production.yml" "Bootstrap fresh production SuperAdmin"
check_bootstrap_block "$repo_root/.github/workflows/deploy-azure.yml" "Bootstrap first SuperAdmin"

echo "Deployment bootstrap configuration tests passed."
