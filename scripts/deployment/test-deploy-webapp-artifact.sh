#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
test_root=$(mktemp -d)
trap 'rm -rf "$test_root"' EXIT

fake_bin="$test_root/bin"
mkdir -p "$fake_bin"

printf '%s\n' \
  '#!/usr/bin/env bash' \
  'set -euo pipefail' \
  'state_dir="${MOCK_AZ_STATE_DIR:?}"' \
  'command_line="$*"' \
  'if [[ "$command_line" == "webapp log deployment list "* ]]; then' \
  '  count_file="$state_dir/list-count"' \
  '  count=0' \
  '  [[ -f "$count_file" ]] && count=$(<"$count_file")' \
  '  count=$((count + 1))' \
  '  printf "%s" "$count" > "$count_file"' \
  '  if [[ -f "$state_dir/published" ]]; then' \
  '    post_count_file="$state_dir/post-list-count"' \
  '    post_count=0' \
  '    [[ -f "$post_count_file" ]] && post_count=$(<"$post_count_file")' \
  '    post_count=$((post_count + 1))' \
  '    printf "%s" "$post_count" > "$post_count_file"' \
  '    if (( post_count <= MOCK_AZ_POST_LIST_FAILURES )); then' \
  '      echo "ERROR: Status lookup failed with status code '\''${MOCK_AZ_POST_LIST_STATUS}'\''." >&2' \
  '      exit 1' \
  '    fi' \
  '    printf "new-deployment\t4\n"' \
  '    exit 0' \
  '  fi' \
  '  if (( count <= MOCK_AZ_LIST_FAILURES )); then' \
  '    echo "ERROR: Status lookup failed with status code '\''${MOCK_AZ_LIST_STATUS}'\''." >&2' \
  '    exit 1' \
  '  fi' \
  '  echo "old-deployment"' \
  '  exit 0' \
  'fi' \
  'if [[ "$command_line" == "webapp deploy "* ]]; then' \
  '  count_file="$state_dir/deploy-count"' \
  '  count=0' \
  '  [[ -f "$count_file" ]] && count=$(<"$count_file")' \
  '  count=$((count + 1))' \
  '  printf "%s" "$count" > "$count_file"' \
  '  if (( count <= MOCK_AZ_DEPLOY_FAILURES )); then' \
  '    if [[ "$MOCK_AZ_DEPLOY_ERROR_STARTS_DEPLOYMENT" == "true" ]]; then touch "$state_dir/published"; fi' \
  '    if [[ -n "$MOCK_AZ_DEPLOY_ERROR" ]]; then echo "$MOCK_AZ_DEPLOY_ERROR" >&2; else echo "ERROR: Publishing failed with status code '\''${MOCK_AZ_STATUS}'\''." >&2; fi' \
  '    exit 1' \
  '  fi' \
  '  touch "$state_dir/published"' \
  '  exit 0' \
  'fi' \
  'echo "Unexpected mock Azure CLI call: $command_line" >&2' \
  'exit 99' \
  > "$fake_bin/az"
chmod +x "$fake_bin/az"

printf '%s\n' \
  '#!/usr/bin/env bash' \
  'set -euo pipefail' \
  'state_dir="${MOCK_AZ_STATE_DIR:?}"' \
  'shift 3' \
  'if [[ "$*" == "az webapp log deployment list "* ]]; then' \
  '  if [[ -f "$state_dir/published" ]]; then' \
  '    count_file="$state_dir/post-status-timeout-count"' \
  '    failure_limit="$MOCK_POST_STATUS_TIMEOUT_FAILURES"' \
  '  else' \
  '    count_file="$state_dir/initial-status-timeout-count"' \
  '    failure_limit="$MOCK_INITIAL_STATUS_TIMEOUT_FAILURES"' \
  '  fi' \
  '  count=0' \
  '  [[ -f "$count_file" ]] && count=$(<"$count_file")' \
  '  count=$((count + 1))' \
  '  printf "%s" "$count" > "$count_file"' \
  '  if (( count <= failure_limit )); then exit 124; fi' \
  'fi' \
  'if [[ "$*" == "az webapp deploy "* ]]; then' \
  '  count_file="$state_dir/timeout-count"' \
  '  count=0' \
  '  [[ -f "$count_file" ]] && count=$(<"$count_file")' \
  '  count=$((count + 1))' \
  '  printf "%s" "$count" > "$count_file"' \
  '  if (( count <= MOCK_TIMEOUT_FAILURES )); then' \
  '    if [[ "$MOCK_TIMEOUT_STARTS_DEPLOYMENT" == "true" ]]; then touch "$state_dir/published"; fi' \
  '    exit 124' \
  '  fi' \
  'fi' \
  'exec "$@"' \
  > "$fake_bin/timeout"
chmod +x "$fake_bin/timeout"

run_deployment()
{
  local state_dir="$1"
  local failures="$2"
  local status="$3"
  local list_failures="${4:-0}"
  local list_status="${5:-502}"
  local deploy_error="${6:-}"
  local post_list_failures="${7:-0}"
  local post_list_status="${8:-503}"
  local timeout_failures="${9:-0}"
  local timeout_starts_deployment="${10:-true}"
  local deploy_error_starts_deployment="${11:-false}"
  local initial_status_timeout_failures="${12:-0}"
  local post_status_timeout_failures="${13:-0}"

  mkdir -p "$state_dir"
  PATH="$fake_bin:$PATH" \
    MOCK_AZ_STATE_DIR="$state_dir" \
    MOCK_AZ_DEPLOY_FAILURES="$failures" \
    MOCK_AZ_STATUS="$status" \
    MOCK_AZ_DEPLOY_ERROR="$deploy_error" \
    MOCK_AZ_LIST_FAILURES="$list_failures" \
    MOCK_AZ_LIST_STATUS="$list_status" \
    MOCK_AZ_POST_LIST_FAILURES="$post_list_failures" \
    MOCK_AZ_POST_LIST_STATUS="$post_list_status" \
    MOCK_TIMEOUT_FAILURES="$timeout_failures" \
    MOCK_TIMEOUT_STARTS_DEPLOYMENT="$timeout_starts_deployment" \
    MOCK_AZ_DEPLOY_ERROR_STARTS_DEPLOYMENT="$deploy_error_starts_deployment" \
    MOCK_INITIAL_STATUS_TIMEOUT_FAILURES="$initial_status_timeout_failures" \
    MOCK_POST_STATUS_TIMEOUT_FAILURES="$post_status_timeout_failures" \
    DEPLOYMENT_PUBLISH_RETRY_BASE_SECONDS=0 \
    DEPLOYMENT_PUBLISH_RETRY_MAX_SECONDS=0 \
    DEPLOYMENT_PUBLISH_COMMAND_TIMEOUT_SECONDS=1 \
    DEPLOYMENT_STATUS_COMMAND_TIMEOUT_SECONDS=1 \
    DEPLOYMENT_STATUS_RETRY_SECONDS=0 \
    bash "$script_dir/deploy-webapp-artifact.sh" test-resource-group test-app test-artifact.zip
}

status_retry_state="$test_root/status-retry"
status_retry_output=$(run_deployment "$status_retry_state" 0 502 2 504)
[[ "$(<"$status_retry_state/list-count")" == "4" ]]
grep -q "deployment status returned a transient gateway response" <<< "$status_retry_output"

long_status_retry_state="$test_root/long-status-retry"
long_status_retry_output=$(run_deployment "$long_status_retry_state" 0 502 8 503)
[[ "$(<"$long_status_retry_state/list-count")" == "10" ]]
grep -q "attempt 9/16" <<< "$long_status_retry_output"

post_status_retry_state="$test_root/post-status-retry"
post_status_retry_output=$(run_deployment "$post_status_retry_state" 0 502 0 502 "" 3 504)
[[ "$(<"$post_status_retry_state/list-count")" == "5" ]]
[[ "$(<"$post_status_retry_state/post-list-count")" == "4" ]]
grep -q "status remained transient" <<< "$post_status_retry_output"

initial_status_timeout_state="$test_root/initial-status-timeout"
initial_status_timeout_output=$(run_deployment "$initial_status_timeout_state" 0 502 0 502 "" 0 503 0 true false 2 0)
[[ "$(<"$initial_status_timeout_state/initial-status-timeout-count")" == "3" ]]
grep -q "status lookup timed out" <<< "$initial_status_timeout_output"

post_status_timeout_state="$test_root/post-status-timeout"
post_status_timeout_output=$(run_deployment "$post_status_timeout_state" 0 502 0 502 "" 0 503 0 true false 0 2)
[[ "$(<"$post_status_timeout_state/post-status-timeout-count")" == "3" ]]
grep -q "status polling timed out" <<< "$post_status_timeout_output"

retry_state="$test_root/retry"
run_deployment "$retry_state" 2 502
[[ "$(<"$retry_state/deploy-count")" == "3" ]]

scm_restart_state="$test_root/scm-restart"
scm_restart_output=$(run_deployment "$scm_restart_state" 1 500 0 502 "Deployment has been stopped due to SCM container restart.")
[[ "$(<"$scm_restart_state/deploy-count")" == "2" ]]
grep -q "Azure publishing returned a transient response" <<< "$scm_restart_output"

timeout_retry_state="$test_root/timeout-retry"
timeout_retry_output=$(run_deployment "$timeout_retry_state" 0 502 0 502 "" 0 503 1)
[[ "$(<"$timeout_retry_state/timeout-count")" == "1" ]]
[[ ! -f "$timeout_retry_state/deploy-count" ]]
[[ ! -f "$timeout_retry_state/restart-count" ]]
grep -q "without restarting or resubmitting" <<< "$timeout_retry_output"

timeout_without_deployment_state="$test_root/timeout-without-deployment"
set +e
timeout_without_deployment_output=$(DEPLOYMENT_STATUS_ATTEMPTS=1 run_deployment "$timeout_without_deployment_state" 0 502 0 502 "" 0 503 1 false 2>&1)
timeout_without_deployment_exit=$?
set -e
[[ "$timeout_without_deployment_exit" -ne 0 ]]
[[ "$(<"$timeout_without_deployment_state/timeout-count")" == "1" ]]
[[ ! -f "$timeout_without_deployment_state/restart-count" ]]
grep -q "Timed out waiting for a new active deployment" <<< "$timeout_without_deployment_output"

deployment_in_progress_state="$test_root/deployment-in-progress"
deployment_in_progress_output=$(run_deployment "$deployment_in_progress_state" 1 409 0 502 "ERROR: DeploymentInProgress: There is a deployment currently in progress." 0 503 0 true true)
[[ "$(<"$deployment_in_progress_state/deploy-count")" == "1" ]]
[[ ! -f "$deployment_in_progress_state/restart-count" ]]
grep -q "waiting for its Kudu status instead of resubmitting" <<< "$deployment_in_progress_output"

non_retry_state="$test_root/non-retry"
set +e
non_retry_output=$(run_deployment "$non_retry_state" 1 401 2>&1)
non_retry_exit=$?
set -e
[[ "$non_retry_exit" -ne 0 ]]
[[ "$(<"$non_retry_state/deploy-count")" == "1" ]]
grep -q "non-retriable" <<< "$non_retry_output"

exhausted_state="$test_root/exhausted"
set +e
exhausted_output=$(run_deployment "$exhausted_state" 20 503 2>&1)
exhausted_exit=$?
set -e
[[ "$exhausted_exit" -ne 0 ]]
[[ "$(<"$exhausted_state/deploy-count")" == "16" ]]
grep -q "exhausted 16 attempts" <<< "$exhausted_output"

echo "Deployment publishing retry tests passed."
