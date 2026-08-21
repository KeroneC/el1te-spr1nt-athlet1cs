#!/usr/bin/env bash

set -euo pipefail

if [[ "$#" -ne 3 ]]; then
  echo "Usage: $0 <resource-group> <app-name> <hostname>" >&2
  exit 64
fi

resource_group="$1"
app_name="$2"
hostname="$3"
azure_cli="${AZURE_CLI:-az}"
poll_attempts="${APP_SERVICE_CERTIFICATE_POLL_ATTEMPTS:-30}"
poll_seconds="${APP_SERVICE_CERTIFICATE_POLL_SECONDS:-10}"

if [[ ! "$poll_attempts" =~ ^[1-9][0-9]*$ ]]; then
  echo "APP_SERVICE_CERTIFICATE_POLL_ATTEMPTS must be a positive integer." >&2
  exit 64
fi

if [[ ! "$poll_seconds" =~ ^[0-9]+$ ]]; then
  echo "APP_SERVICE_CERTIFICATE_POLL_SECONDS must be a nonnegative integer." >&2
  exit 64
fi

bound="$("$azure_cli" webapp config hostname list \
  --resource-group "$resource_group" \
  --webapp-name "$app_name" \
  --query "[?name=='$hostname'].name | [0]" \
  --output tsv)"

if [[ "$bound" != "$hostname" ]]; then
  "$azure_cli" webapp config hostname add \
    --resource-group "$resource_group" \
    --webapp-name "$app_name" \
    --hostname "$hostname" \
    --output none
fi

thumbprint="$("$azure_cli" webapp config ssl list \
  --resource-group "$resource_group" \
  --query "[?contains(hostNames, '$hostname')].thumbprint | [0]" \
  --output tsv)"

if [[ -z "$thumbprint" ]]; then
  echo "Requesting an App Service managed certificate for $hostname."
  thumbprint="$("$azure_cli" webapp config ssl create \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --hostname "$hostname" \
    --query thumbprint \
    --output tsv)"
fi

# Azure can accept certificate creation before the certificate resource has a
# thumbprint. Poll the named certificate instead of trying to bind an empty
# value. This also makes a rerun safe after an earlier asynchronous request.
if [[ -z "$thumbprint" ]]; then
  for ((attempt = 1; attempt <= poll_attempts; attempt += 1)); do
    thumbprint="$("$azure_cli" webapp config ssl show \
      --resource-group "$resource_group" \
      --certificate-name "$hostname" \
      --query thumbprint \
      --output tsv 2>/dev/null || true)"

    if [[ -n "$thumbprint" ]]; then
      break
    fi

    if (( attempt < poll_attempts )); then
      echo "Managed certificate for $hostname is still provisioning; retrying in ${poll_seconds}s ($attempt/$poll_attempts)."
      sleep "$poll_seconds"
    fi
  done
fi

if [[ -z "$thumbprint" ]]; then
  echo "Managed certificate for $hostname did not become ready after $poll_attempts checks." >&2
  exit 1
fi

ssl_state="$("$azure_cli" webapp config hostname list \
  --resource-group "$resource_group" \
  --webapp-name "$app_name" \
  --query "[?name=='$hostname'].sslState | [0]" \
  --output tsv)"

if [[ "$ssl_state" != "SniEnabled" ]]; then
  "$azure_cli" webapp config ssl bind \
    --resource-group "$resource_group" \
    --name "$app_name" \
    --certificate-thumbprint "$thumbprint" \
    --ssl-type SNI \
    --output none
fi

echo "$hostname is bound to $app_name with SNI TLS."
