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
  *"webapp config ssl list"*)
    printf '%s\n' "${FAKE_EXISTING_THUMBPRINT:-}"
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
  *"webapp config hostname list"*".sslState | [0]"*)
    printf '%s\n' "${FAKE_SSL_STATE:-}"
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
  FAKE_EXISTING_THUMBPRINT=EXISTING123 \
  FAKE_SSL_STATE=SniEnabled

if grep -q "ssl create\|ssl bind" "$test_root/existing/az.log"; then
  echo "Existing binding should not create or bind a certificate." >&2
  exit 1
fi

run_case asynchronous env \
  FAKE_BOUND_HOST=api.example.org \
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

echo "App Service managed-certificate binding tests passed."
