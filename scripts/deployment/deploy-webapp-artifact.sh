#!/usr/bin/env bash
set -euo pipefail

resource_group="${1:-}"
app_name="${2:-}"
artifact_path="${3:-}"

if [[ -z "$resource_group" || -z "$app_name" || -z "$artifact_path" ]]; then
  echo "Usage: deploy-webapp-artifact.sh <resource-group> <app-name> <artifact-path>"
  exit 2
fi

# Linux startup tracking can retain a stale container timeout even after the new
# worker is healthy. Track the immutable Kudu deployment before probing it. The
# ZIP deployment already restarts App Service, so a second restart here would
# create an unnecessary second cold start on the Basic production plan.
publish_retry_base_seconds="${DEPLOYMENT_PUBLISH_RETRY_BASE_SECONDS:-10}"
if [[ ! "$publish_retry_base_seconds" =~ ^[0-9]+$ ]]; then
  echo "DEPLOYMENT_PUBLISH_RETRY_BASE_SECONDS must be a non-negative integer."
  exit 2
fi
publish_retry_max_seconds="${DEPLOYMENT_PUBLISH_RETRY_MAX_SECONDS:-60}"
if [[ ! "$publish_retry_max_seconds" =~ ^[0-9]+$ ]]; then
  echo "DEPLOYMENT_PUBLISH_RETRY_MAX_SECONDS must be a non-negative integer."
  exit 2
fi
publish_attempts="${DEPLOYMENT_PUBLISH_ATTEMPTS:-16}"
if [[ ! "$publish_attempts" =~ ^[1-9][0-9]*$ ]]; then
  echo "DEPLOYMENT_PUBLISH_ATTEMPTS must be a positive integer."
  exit 2
fi
publish_command_timeout_seconds="${DEPLOYMENT_PUBLISH_COMMAND_TIMEOUT_SECONDS:-180}"
if [[ ! "$publish_command_timeout_seconds" =~ ^[1-9][0-9]*$ ]]; then
  echo "DEPLOYMENT_PUBLISH_COMMAND_TIMEOUT_SECONDS must be a positive integer."
  exit 2
fi
status_command_timeout_seconds="${DEPLOYMENT_STATUS_COMMAND_TIMEOUT_SECONDS:-20}"
if [[ ! "$status_command_timeout_seconds" =~ ^[1-9][0-9]*$ ]]; then
  echo "DEPLOYMENT_STATUS_COMMAND_TIMEOUT_SECONDS must be a positive integer."
  exit 2
fi
status_retry_seconds="${DEPLOYMENT_STATUS_RETRY_SECONDS:-5}"
if [[ ! "$status_retry_seconds" =~ ^[0-9]+$ ]]; then
  echo "DEPLOYMENT_STATUS_RETRY_SECONDS must be a non-negative integer."
  exit 2
fi
if ! command -v timeout >/dev/null 2>&1; then
  echo "The coreutils timeout command is required for bounded Azure publishing."
  exit 2
fi

is_transient_azure_deployment_response()
{
  grep -Eiq "status code ['\"]?(502|503|504)|HTTP[^0-9]*(502|503|504)|Bad Gateway|Service Unavailable|Gateway Timeout|Deployment has been stopped due to SCM container restart"
}

is_deployment_in_progress_response()
{
  grep -Eiq "DeploymentInProgress|deployment (is )?currently in progress|Kudu Status[[:space:]]*:[[:space:]]*409"
}

retry_delay_for_attempt()
{
  local attempt="$1"
  local delay=$((publish_retry_base_seconds * (2 ** (attempt - 1))))
  if (( delay > publish_retry_max_seconds )); then
    delay="$publish_retry_max_seconds"
  fi
  printf '%s' "$delay"
}

previous_id=""
for ((status_attempt = 1; status_attempt <= publish_attempts; status_attempt++)); do
  set +e
  previous_id_output=$(timeout --signal=TERM --kill-after=5 "${status_command_timeout_seconds}s" az webapp log deployment list \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --query '[?active].id | [0]' \
    --output tsv 2>&1)
  previous_id_exit_code=$?
  set -e

  if [[ "$previous_id_exit_code" -eq 0 ]]; then
    previous_id="$previous_id_output"
    break
  fi

  if [[ "$previous_id_exit_code" -eq 124 || "$previous_id_exit_code" -eq 137 ]]; then
    echo "Azure deployment status lookup timed out after ${status_command_timeout_seconds}s for $app_name; retrying ($status_attempt/$publish_attempts)."
  elif ! is_transient_azure_deployment_response <<< "$previous_id_output"; then
    printf '%s\n' "$previous_id_output" >&2
    echo "Deployment status lookup failed with a non-retriable error for $app_name."
    exit "$previous_id_exit_code"
  fi

  if [[ "$status_attempt" -eq "$publish_attempts" ]]; then
    printf '%s\n' "$previous_id_output" >&2
    echo "Deployment status lookup exhausted $publish_attempts attempts for $app_name."
    exit "$previous_id_exit_code"
  fi

  retry_delay="$(retry_delay_for_attempt "$status_attempt")"
  echo "Azure deployment status returned a transient gateway response for $app_name; retrying in ${retry_delay}s (attempt $((status_attempt + 1))/$publish_attempts)."
  sleep "$retry_delay"
done

publish_succeeded=false
for ((publish_attempt = 1; publish_attempt <= publish_attempts; publish_attempt++)); do
  set +e
  publish_output=$(timeout --signal=TERM --kill-after=15 "${publish_command_timeout_seconds}s" az webapp deploy \
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

  if [[ "$publish_exit_code" -eq 124 || "$publish_exit_code" -eq 137 ]]; then
    # The Azure CLI can time out after Kudu has accepted the asynchronous ZIP
    # deployment. Restarting or resubmitting here races the accepted deployment
    # and produces DeploymentInProgress (409). Observe Kudu instead; the status
    # loop below will fail safely if no new deployment ever appears.
    echo "Azure publishing exceeded ${publish_command_timeout_seconds}s for $app_name; the request may still be running, so deployment status will be observed without restarting or resubmitting."
    publish_succeeded=true
    break
  fi

  if is_deployment_in_progress_response <<< "$publish_output"; then
    echo "Azure reports a deployment already in progress for $app_name; waiting for its Kudu status instead of resubmitting."
    publish_succeeded=true
    break
  fi

  if ! is_transient_azure_deployment_response <<< "$publish_output"; then
    printf '%s\n' "$publish_output" >&2
    echo "Artifact publishing failed with a non-retriable error for $app_name."
    exit "$publish_exit_code"
  fi

  if [[ "$publish_attempt" -eq "$publish_attempts" ]]; then
    printf '%s\n' "$publish_output" >&2
    echo "Artifact publishing exhausted $publish_attempts attempts for $app_name."
    exit "$publish_exit_code"
  fi

  retry_delay="$(retry_delay_for_attempt "$publish_attempt")"
  echo "Azure publishing returned a transient response for $app_name; retrying in ${retry_delay}s (attempt $((publish_attempt + 1))/$publish_attempts)."
  sleep "$retry_delay"
done

if [[ "$publish_succeeded" != "true" ]]; then
  echo "Artifact publishing did not complete for $app_name."
  exit 1
fi

deployment_id=""
attempts="${DEPLOYMENT_STATUS_ATTEMPTS:-90}"
for ((attempt = 1; attempt <= attempts; attempt++)); do
  set +e
  deployment=$(timeout --signal=TERM --kill-after=5 "${status_command_timeout_seconds}s" az webapp log deployment list \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --query '[?active] | [0].{id:id,status:status}' \
    --output tsv 2>&1)
  deployment_exit_code=$?
  set -e

  if [[ "$deployment_exit_code" -ne 0 ]]; then
    if [[ "$deployment_exit_code" -eq 124 || "$deployment_exit_code" -eq 137 ]]; then
      echo "Azure deployment status polling timed out after ${status_command_timeout_seconds}s for $app_name; retrying status check ($attempt/$attempts)."
      sleep "$status_retry_seconds"
      continue
    fi
    if is_transient_azure_deployment_response <<< "$deployment"; then
      echo "Azure deployment status remained transient for $app_name; retrying status check ($attempt/$attempts)."
      sleep "$status_retry_seconds"
      continue
    fi
    printf '%s\n' "$deployment" >&2
    echo "Deployment status polling failed with a non-retriable error for $app_name."
    exit "$deployment_exit_code"
  fi
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
  sleep "$status_retry_seconds"
done

if [[ -z "$deployment_id" ]]; then
  echo "Timed out waiting for a new active deployment for $app_name."
  exit 1
fi

echo "Deployment $deployment_id finished for $app_name; the ZIP deployment-managed restart will be verified by application smoke tests."
