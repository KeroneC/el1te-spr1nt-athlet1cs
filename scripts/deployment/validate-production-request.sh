#!/usr/bin/env bash
set -euo pipefail

require_value() {
  local name="$1"
  [[ -n "${!name:-}" ]] || { echo "Missing production configuration: $name" >&2; exit 1; }
}

require_true() {
  local value="$1" message="$2"
  [[ "$value" == "true" ]] || { echo "$message" >&2; exit 1; }
}

require_false() {
  local value="$1" message="$2"
  [[ "$value" == "false" ]] || { echo "$message" >&2; exit 1; }
}

for name in \
  AZURE_CLIENT_ID AZURE_TENANT_ID AZURE_SUBSCRIPTION_ID AZURE_DEPLOYMENT_PRINCIPAL_OBJECT_ID \
  AZURE_RESOURCE_GROUP AZURE_LOCATION RESOURCE_NAME_PREFIX SQL_ADMIN_LOGIN SQL_ADMIN_PASSWORD \
  BUDGET_CONTACT_EMAIL INFRASTRUCTURE_ONLY MANAGED_IDENTITY_SQL_READY BOOTSTRAP_ADMIN \
  CONFIGURE_API_DOMAIN CONFIGURE_PUBLIC_WEB_DOMAINS USE_VERIFIED_EMAIL_DOMAIN ENFORCE_CSP \
  ENABLE_BROWSER_ANALYTICS ENABLE_STORE_PREVIEW ENABLE_CHECKOUT PRELAUNCH_CHECKOUT_TEST \
  ENABLE_INDEXING RUN_SQL_RESTORE_TEST FINAL_LAUNCH_ACKNOWLEDGED; do
  require_value "$name"
done

for name in \
  INFRASTRUCTURE_ONLY MANAGED_IDENTITY_SQL_READY BOOTSTRAP_ADMIN CONFIGURE_API_DOMAIN \
  CONFIGURE_PUBLIC_WEB_DOMAINS USE_VERIFIED_EMAIL_DOMAIN ENFORCE_CSP ENABLE_BROWSER_ANALYTICS \
  ENABLE_STORE_PREVIEW ENABLE_CHECKOUT PRELAUNCH_CHECKOUT_TEST ENABLE_INDEXING \
  RUN_SQL_RESTORE_TEST FINAL_LAUNCH_ACKNOWLEDGED; do
  [[ "${!name}" == "true" || "${!name}" == "false" ]] || {
    echo "$name must be true or false." >&2
    exit 1
  }
done

if [[ "$INFRASTRUCTURE_ONLY" == "false" ]]; then
  require_true "$MANAGED_IDENTITY_SQL_READY" "Full deployment requires production SQL managed-identity readiness."
  require_value DATABASE_MIGRATION_CONNECTION_STRING
else
  require_false "$BOOTSTRAP_ADMIN" "SuperAdmin bootstrap requires an application deployment."
  require_false "$RUN_SQL_RESTORE_TEST" "The SQL restore test requires an application deployment."
  require_false "$ENABLE_CHECKOUT" "Checkout cannot be enabled by an infrastructure-only run."
fi

if [[ "$BOOTSTRAP_ADMIN" == "true" ]]; then
  for name in BOOTSTRAP_ADMIN_EMAIL BOOTSTRAP_ADMIN_PASSWORD BOOTSTRAP_ADMIN_FIRST_NAME BOOTSTRAP_ADMIN_LAST_NAME; do
    require_value "$name"
  done
fi

if [[ "$CONFIGURE_PUBLIC_WEB_DOMAINS" == "true" ]]; then
  require_true "$CONFIGURE_API_DOMAIN" "Public web domains require the canonical API domain."
fi

if [[ "$PRELAUNCH_CHECKOUT_TEST" == "true" ]]; then
  require_true "$ENABLE_CHECKOUT" "Prelaunch checkout mode requires checkout to be enabled."
  require_true "$CONFIGURE_API_DOMAIN" "Prelaunch checkout requires the canonical API domain."
  require_false "$CONFIGURE_PUBLIC_WEB_DOMAINS" "Prelaunch checkout must not move the public web domains."
  require_false "$ENABLE_INDEXING" "Prelaunch checkout must remain noindex."
  require_false "$FINAL_LAUNCH_ACKNOWLEDGED" "Prelaunch checkout cannot use the final launch acknowledgement."
fi

if [[ "$ENABLE_INDEXING" == "true" ]]; then
  require_true "$FINAL_LAUNCH_ACKNOWLEDGED" "Indexing requires the final launch acknowledgement."
  require_true "$CONFIGURE_PUBLIC_WEB_DOMAINS" "Indexing requires canonical public web domains and TLS."
  require_false "$PRELAUNCH_CHECKOUT_TEST" "Indexing cannot be enabled during a prelaunch checkout test."
fi

if [[ "$ENABLE_CHECKOUT" == "true" ]]; then
  require_false "$INFRASTRUCTURE_ONLY" "Checkout cannot be enabled by an infrastructure-only run."
  require_true "$ENABLE_STORE_PREVIEW" "Checkout requires the internal store navigation and catalog."
  require_true "$CONFIGURE_API_DOMAIN" "Checkout requires the canonical API domain and TLS."
  require_true "$USE_VERIFIED_EMAIL_DOMAIN" "Checkout requires the verified production email domain."
  require_true "$ENFORCE_CSP" "Checkout requires enforced CSP."
  require_true "$ENABLE_BROWSER_ANALYTICS" "Checkout requires production monitoring to be enabled."
  for name in \
    SQUARE_LOCATION_ID SQUARE_WEBHOOK_NOTIFICATION_URL EFFECTIVE_SQUARE_CHECKOUT_RETURN_URL \
    SQUARE_ACCESS_TOKEN_SECRET_URI SQUARE_WEBHOOK_SIGNATURE_KEY_SECRET_URI; do
    require_value "$name"
  done
  [[ "$SQUARE_WEBHOOK_NOTIFICATION_URL" == "https://api.el1tespr1ntathlet1cs.org/api/webhooks/square" ]] || {
    echo "Unexpected production Square webhook URL." >&2
    exit 1
  }

  if [[ "$PRELAUNCH_CHECKOUT_TEST" == "true" ]]; then
    require_value SQUARE_PRELAUNCH_CHECKOUT_RETURN_URL
    [[ "$EFFECTIVE_SQUARE_CHECKOUT_RETURN_URL" == "$SQUARE_PRELAUNCH_CHECKOUT_RETURN_URL" ]] || {
      echo "Prelaunch checkout must use the protected prelaunch return URL." >&2
      exit 1
    }
    [[ "$EFFECTIVE_SQUARE_CHECKOUT_RETURN_URL" =~ ^https://[a-z0-9-]+\.azurewebsites\.net/shop/order-confirmation$ ]] || {
      echo "Prelaunch checkout return URL must use the production Azure web hostname." >&2
      exit 1
    }
  else
    require_true "$FINAL_LAUNCH_ACKNOWLEDGED" "Public checkout requires the final launch acknowledgement."
    require_true "$CONFIGURE_PUBLIC_WEB_DOMAINS" "Public checkout requires canonical public web domains and TLS."
    [[ "$EFFECTIVE_SQUARE_CHECKOUT_RETURN_URL" == "https://www.el1tespr1ntathlet1cs.org/shop/order-confirmation" ]] || {
      echo "Unexpected production Square return URL." >&2
      exit 1
    }
  fi
fi

echo "Production deployment request is valid."
