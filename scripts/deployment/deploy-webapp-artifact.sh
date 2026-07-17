#!/usr/bin/env bash
set -euo pipefail

resource_group="${1:-}"
app_name="${2:-}"
artifact_path="${3:-}"

if [[ -z "$resource_group" || -z "$app_name" || -z "$artifact_path" ]]; then
  echo "Usage: deploy-webapp-artifact.sh <resource-group> <app-name> <artifact-path>"
  exit 2
fi

previous_id=$(az webapp log deployment list \
  --resource-group "$resource_group" \
  --name "$app_name" \
  --query '[?active].id | [0]' \
  --output tsv)

# Linux startup tracking can retain a stale container timeout even after the new
# worker is healthy. Track the immutable Kudu deployment, then restart and probe.
az webapp deploy \
  --resource-group "$resource_group" \
  --name "$app_name" \
  --src-path "$artifact_path" \
  --type zip \
  --clean true \
  --async true \
  --track-status false \
  --output none

deployment_id=""
attempts="${DEPLOYMENT_STATUS_ATTEMPTS:-90}"
for ((attempt = 1; attempt <= attempts; attempt++)); do
  deployment=$(az webapp log deployment list \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --query '[?active] | [0].{id:id,status:status}' \
    --output tsv)
  read -r deployment_id deployment_status <<< "$deployment"

  if [[ -n "$deployment_id" && "$deployment_id" != "$previous_id" && "$deployment_status" == "4" ]]; then
    echo "Deployment $deployment_id is active for $app_name."
    break
  fi
  if [[ -n "$deployment_id" && "$deployment_id" != "$previous_id" && "$deployment_status" == "3" ]]; then
    echo "Deployment $deployment_id failed for $app_name."
    exit 1
  fi

  deployment_id=""
  sleep 5
done

if [[ -z "$deployment_id" ]]; then
  echo "Timed out waiting for a new active deployment for $app_name."
  exit 1
fi

az webapp restart --resource-group "$resource_group" --name "$app_name" --output none
