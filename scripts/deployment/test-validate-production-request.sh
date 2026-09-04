#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
validator="$script_dir/validate-production-request.sh"
tests=0

run_validator() {
  env \
    AZURE_CLIENT_ID=client AZURE_TENANT_ID=tenant AZURE_SUBSCRIPTION_ID=subscription \
    AZURE_DEPLOYMENT_PRINCIPAL_OBJECT_ID=principal AZURE_RESOURCE_GROUP=production-rg \
    AZURE_LOCATION=centralus RESOURCE_NAME_PREFIX=el1tesprint SQL_ADMIN_LOGIN=sqladmin \
    SQL_ADMIN_PASSWORD=secret BUDGET_CONTACT_EMAIL=alerts@example.org \
    INFRASTRUCTURE_ONLY=true MANAGED_IDENTITY_SQL_READY=false BOOTSTRAP_ADMIN=false \
    CONFIGURE_API_DOMAIN=false CONFIGURE_PUBLIC_WEB_DOMAINS=false USE_VERIFIED_EMAIL_DOMAIN=false \
    ENFORCE_CSP=false ENABLE_BROWSER_ANALYTICS=false ENABLE_HOME_ALL_AMERICAN_SHOWCASE=false ENABLE_STORE_PREVIEW=false ENABLE_CHECKOUT=false \
    PRELAUNCH_CHECKOUT_TEST=false ENABLE_INDEXING=false RUN_SQL_RESTORE_TEST=false \
    FINAL_LAUNCH_ACKNOWLEDGED=false \
    "$@" bash "$validator"
}

expect_pass() {
  local name="$1"
  shift
  tests=$((tests + 1))
  run_validator "$@" >/dev/null || { echo "FAIL: $name should pass" >&2; exit 1; }
}

expect_fail() {
  local name="$1"
  shift
  tests=$((tests + 1))
  if run_validator "$@" >/dev/null 2>&1; then
    echo "FAIL: $name should fail" >&2
    exit 1
  fi
}

expect_pass "infrastructure only"

expect_pass "private application deployment" \
  INFRASTRUCTURE_ONLY=false MANAGED_IDENTITY_SQL_READY=true \
  DATABASE_MIGRATION_CONNECTION_STRING=connection ENABLE_HOME_ALL_AMERICAN_SHOWCASE=true

expect_pass "prelaunch checkout" \
  INFRASTRUCTURE_ONLY=false MANAGED_IDENTITY_SQL_READY=true DATABASE_MIGRATION_CONNECTION_STRING=connection \
  CONFIGURE_API_DOMAIN=true USE_VERIFIED_EMAIL_DOMAIN=true ENFORCE_CSP=true \
  ENABLE_BROWSER_ANALYTICS=true ENABLE_STORE_PREVIEW=true ENABLE_CHECKOUT=true \
  PRELAUNCH_CHECKOUT_TEST=true SQUARE_LOCATION_ID=location \
  SQUARE_WEBHOOK_NOTIFICATION_URL=https://api.el1tespr1ntathlet1cs.org/api/webhooks/square \
  SQUARE_PRELAUNCH_CHECKOUT_RETURN_URL=https://el1tesprint-production-test-web.azurewebsites.net/shop/order-confirmation \
  EFFECTIVE_SQUARE_CHECKOUT_RETURN_URL=https://el1tesprint-production-test-web.azurewebsites.net/shop/order-confirmation \
  SQUARE_ACCESS_TOKEN_SECRET_URI=https://vault/secrets/token \
  SQUARE_WEBHOOK_SIGNATURE_KEY_SECRET_URI=https://vault/secrets/signature

expect_pass "final public checkout" \
  INFRASTRUCTURE_ONLY=false MANAGED_IDENTITY_SQL_READY=true DATABASE_MIGRATION_CONNECTION_STRING=connection \
  CONFIGURE_API_DOMAIN=true CONFIGURE_PUBLIC_WEB_DOMAINS=true USE_VERIFIED_EMAIL_DOMAIN=true \
  ENFORCE_CSP=true ENABLE_BROWSER_ANALYTICS=true ENABLE_STORE_PREVIEW=true ENABLE_CHECKOUT=true \
  ENABLE_INDEXING=true FINAL_LAUNCH_ACKNOWLEDGED=true SQUARE_LOCATION_ID=location \
  SQUARE_WEBHOOK_NOTIFICATION_URL=https://api.el1tespr1ntathlet1cs.org/api/webhooks/square \
  EFFECTIVE_SQUARE_CHECKOUT_RETURN_URL=https://www.el1tespr1ntathlet1cs.org/shop/order-confirmation \
  SQUARE_ACCESS_TOKEN_SECRET_URI=https://vault/secrets/token \
  SQUARE_WEBHOOK_SIGNATURE_KEY_SECRET_URI=https://vault/secrets/signature

expect_fail "bootstrap during infrastructure only" BOOTSTRAP_ADMIN=true
expect_fail "full deploy without SQL readiness" INFRASTRUCTURE_ONLY=false DATABASE_MIGRATION_CONNECTION_STRING=connection
expect_fail "prelaunch without API domain" \
  INFRASTRUCTURE_ONLY=false MANAGED_IDENTITY_SQL_READY=true DATABASE_MIGRATION_CONNECTION_STRING=connection \
  USE_VERIFIED_EMAIL_DOMAIN=true ENFORCE_CSP=true ENABLE_BROWSER_ANALYTICS=true ENABLE_STORE_PREVIEW=true \
  ENABLE_CHECKOUT=true PRELAUNCH_CHECKOUT_TEST=true
expect_fail "prelaunch with public domains" \
  INFRASTRUCTURE_ONLY=false MANAGED_IDENTITY_SQL_READY=true DATABASE_MIGRATION_CONNECTION_STRING=connection \
  CONFIGURE_API_DOMAIN=true CONFIGURE_PUBLIC_WEB_DOMAINS=true USE_VERIFIED_EMAIL_DOMAIN=true \
  ENFORCE_CSP=true ENABLE_BROWSER_ANALYTICS=true ENABLE_STORE_PREVIEW=true ENABLE_CHECKOUT=true \
  PRELAUNCH_CHECKOUT_TEST=true
expect_fail "public checkout without acknowledgement" \
  INFRASTRUCTURE_ONLY=false MANAGED_IDENTITY_SQL_READY=true DATABASE_MIGRATION_CONNECTION_STRING=connection \
  CONFIGURE_API_DOMAIN=true CONFIGURE_PUBLIC_WEB_DOMAINS=true USE_VERIFIED_EMAIL_DOMAIN=true \
  ENFORCE_CSP=true ENABLE_BROWSER_ANALYTICS=true ENABLE_STORE_PREVIEW=true ENABLE_CHECKOUT=true
expect_fail "prelaunch with non-Azure return URL" \
  INFRASTRUCTURE_ONLY=false MANAGED_IDENTITY_SQL_READY=true DATABASE_MIGRATION_CONNECTION_STRING=connection \
  CONFIGURE_API_DOMAIN=true USE_VERIFIED_EMAIL_DOMAIN=true ENFORCE_CSP=true \
  ENABLE_BROWSER_ANALYTICS=true ENABLE_STORE_PREVIEW=true ENABLE_CHECKOUT=true \
  PRELAUNCH_CHECKOUT_TEST=true SQUARE_LOCATION_ID=location \
  SQUARE_WEBHOOK_NOTIFICATION_URL=https://api.el1tespr1ntathlet1cs.org/api/webhooks/square \
  SQUARE_PRELAUNCH_CHECKOUT_RETURN_URL=https://example.org/shop/order-confirmation \
  EFFECTIVE_SQUARE_CHECKOUT_RETURN_URL=https://example.org/shop/order-confirmation \
  SQUARE_ACCESS_TOKEN_SECRET_URI=https://vault/secrets/token \
  SQUARE_WEBHOOK_SIGNATURE_KEY_SECRET_URI=https://vault/secrets/signature

echo "Passed $tests production request validation tests."
