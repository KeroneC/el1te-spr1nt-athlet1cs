#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
test_root="$(mktemp -d)"
trap 'rm -rf "$test_root"' EXIT

fake_az="$test_root/fake-az"
cat > "$fake_az" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

printf '%s\n' "$*" >> "$FAKE_AZ_LOG"

case "$*" in
  *"webapp config hostname list"*".name | [0]"*)
    printf '%s\n' "${FAKE_BOUND_HOST:-}"
    ;;
  *"webapp config hostname add"*)
    ;;
  *"webapp config hostname list"*"sslState=='SniEnabled'"*".thumbprint | [0]"*)
    printf '%s\n' "${FAKE_BOUND_THUMBPRINT:-}"
    ;;
  *"webapp config ssl list"*)
    printf '%s\n' "${FAKE_CERTS:-[]}"
    ;;
  *"webapp config ssl create"*)
    printf '%s\n' "${FAKE_CREATE_THUMBPRINT:-}"
    ;;
  *"webapp config ssl show"*)
    count_file="$FAKE_STATE_DIR/show-count"
    count=0
    [[ -f "$count_file" ]] && count="$(<"$count_file")"
    count=$((count + 1))
    printf '%s' "$count" > "$count_file"
    if (( count >= ${FAKE_READY_AFTER:-1} )); then
      printf '%s\n' "${FAKE_READY_THUMBPRINT:-READY123}"
    fi
    ;;
  *"webapp config ssl bind"*)
    ;;
  *)
    echo "Unexpected fake Azure CLI invocation: $*" >&2
    exit 2
    ;;
esac
EOF
chmod +x "$fake_az"

run_case() {
  local case_name="$1"
  shift
  local state_dir="$test_root/$case_name"
  mkdir -p "$state_dir"
  env \
    AZURE_CLI="$fake_az" \
    FAKE_AZ_LOG="$state_dir/az.log" \
    FAKE_STATE_DIR="$state_dir" \
    APP_SERVICE_CERTIFICATE_POLL_ATTEMPTS=3 \
    APP_SERVICE_CERTIFICATE_POLL_SECONDS=0 \
    "$@" \
    "$script_dir/bind-app-service-managed-certificate.sh" test-rg test-app api.example.org
}

run_case existing env \
  FAKE_BOUND_HOST=api.example.org \
  FAKE_BOUND_THUMBPRINT=EXISTING123

if grep -Eq 'hostname add|ssl (list|create|bind)' "$test_root/existing/az.log"; then
  echo "Existing SNI binding should not enumerate, create, or bind certificates." >&2
  exit 1
fi

run_case nullhosts env FAKE_BOUND_HOST=api.example.org \
  'FAKE_CERTS=[{"hosts":null,"canonical":"unrelated.example.org","thumbprint":"OTHER"},{"hosts":null,"canonical":"api.example.org","thumbprint":"NAMED"}]'
grep -q -- "--certificate-thumbprint NAMED" "$test_root/nullhosts/az.log"
! grep -q 'ssl create' "$test_root/nullhosts/az.log" || exit 1

run_case san env FAKE_BOUND_HOST=api.example.org \
  'FAKE_CERTS=[{"hosts":null},{"hosts":"api.example.org.example.net","thumbprint":"WRONG_TYPE"},{"hosts":["api.example.org.example.net"],"thumbprint":"OTHER"},{"hosts":["api.example.org"],"thumbprint":"SAN"}]'
grep -q -- "--certificate-thumbprint SAN" "$test_root/san/az.log"
! grep -q 'ssl create' "$test_root/san/az.log" || exit 1

run_case asynchronous env \
  FAKE_BOUND_HOST=api.example.org \
  'FAKE_CERTS=[{"hosts":null,"canonical":null,"thumbprint":"UNRELATED"}]' \
  FAKE_READY_AFTER=2 \
  FAKE_READY_THUMBPRINT=ASYNC123

grep -q "webapp config ssl create" "$test_root/asynchronous/az.log"
[[ "$(<"$test_root/asynchronous/show-count")" == "2" ]]
grep -q -- "--certificate-thumbprint ASYNC123" "$test_root/asynchronous/az.log"

set +e
timeout_output="$(run_case timeout env FAKE_BOUND_HOST=api.example.org FAKE_READY_AFTER=99 2>&1)"
timeout_exit=$?
set -e

[[ "$timeout_exit" -ne 0 ]]
grep -q "did not become ready after 3 checks" <<< "$timeout_output"

# A reviewed control-plane hotfix must apply to the previously tested app SHA.
workflow="$script_dir/../../.github/workflows/deploy-production.yml"
stage_line=$(grep -nF 'install -m 0700 .deployment-control/scripts/deployment/bind-app-service-managed-certificate.sh "$RUNNER_TEMP/el1te-bind-managed-tls.sh"' "$workflow" | cut -d: -f1)
checkout_line=$(grep -nF 'name: Checkout exact release commit' "$workflow" | cut -d: -f1)
bind_line=$(grep -nF 'bash "$RUNNER_TEMP/el1te-bind-managed-tls.sh"' "$workflow" | cut -d: -f1)
(( stage_line < checkout_line && checkout_line < bind_line ))

echo "App Service managed-certificate binding tests passed."
