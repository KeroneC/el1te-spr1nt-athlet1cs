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
publish_retry_base_seconds="${DEPLOYMENT_PUBLISH_RETRY_BASE_SECONDS:-10}"
if [[ ! "$publish_retry_base_seconds" =~ ^[0-9]+$ ]]; then
  echo "DEPLOYMENT_PUBLISH_RETRY_BASE_SECONDS must be a non-negative integer."
  exit 2
fi

publish_attempts=4
publish_succeeded=false
for ((publish_attempt = 1; publish_attempt <= publish_attempts; publish_attempt++)); do
  set +e
  publish_output=$(az webapp deploy \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --src-path "$artifact_path" \
    --type zip \
    --clean true \
    --async true \
    --track-status false \
    --output none 2>&1)
  publish_exit_code=$?
  set -e

  if [[ "$publish_exit_code" -eq 0 ]]; then
    publish_succeeded=true
    break
  fi

  if ! grep -Eiq "status code ['\"]?(502|503|504)|HTTP[^0-9]*(502|503|504)|Bad Gateway|Service Unavailable|Gateway Timeout" <<< "$publish_output"; then
    printf '%s\n' "$publish_output" >&2
    echo "Artifact publishing failed with a non-retriable error for $app_name."
    exit "$publish_exit_code"
  fi

  if [[ "$publish_attempt" -eq "$publish_attempts" ]]; then
    printf '%s\n' "$publish_output" >&2
    echo "Artifact publishing exhausted $publish_attempts attempts for $app_name."
    exit "$publish_exit_code"
  fi

  retry_delay=$((publish_retry_base_seconds * (2 ** (publish_attempt - 1))))
  echo "Azure publishing returned a transient gateway response for $app_name; retrying in ${retry_delay}s (attempt $((publish_attempt + 1))/$publish_attempts)."
  sleep "$retry_delay"
done

if [[ "$publish_succeeded" != "true" ]]; then
  echo "Artifact publishing did not complete for $app_name."
  exit 1
fi

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
