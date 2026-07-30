# Store and Square Foundation

The commerce foundation is the first delivery phase of the El1te merchandise replacement. Later phases add the [catalog and inventory workspace](store-catalog-inventory.md) and a separately gated storefront preview, but they still do not accept payments or replace Square as the active sales path. `Store:Enabled` remains `false` until the final cutover.

## Ownership Boundary

El1te owns the catalog, tracked variants, inventory, reservations, order fulfillment, and immutable order-item snapshots. Square owns hosted card entry, payment receipts, refunds, and financial reconciliation. Checkout will create ad-hoc, itemized Square orders from an El1te order snapshot instead of maintaining a second synchronized Square catalog.

All monetary values in the new commerce tables use integer minor units and `USD`. A value of `2500` means $25.00. Inventory availability will be calculated as on-hand minus active reservations.

Hosted orders request Square's automatic catalog-tax calculation. Before launch, the organization must review the Square tax objects and ensure applicable taxes allow custom amounts; the application does not contain a tax rate or legal assumption.

## Foundation Components

- Expanded `Product`, `Order`, and `OrderItem` entities plus catalog, option, variant, modifier, visualizer, inventory, reservation, refund, webhook, and outbox records.
- Migrations `AddCommerceFoundation` and `AddCommerceCommunicationHistory`, including safe conversion of any legacy decimal prices and totals into minor units, draft legacy products, internal notes, and email-delivery history.
- A typed Square client for connection checks, hosted payment-link creation, payment retrieval, and refunds.
- `POST /api/webhooks/square`, which is hidden with `404 Not Found` while the store is disabled.
- Exact raw-body webhook signature validation using the configured notification URL, HMAC-SHA-256, and constant-time comparison.
- Unique Square event IDs and a SQL outbox. Raw webhook bodies, buyer details, payment details, and provider error bodies are never persisted or logged.
- An always-on, idempotent outbox worker. This foundation marks safely accepted events processed; later checkout phases add payment, refund, reservation, email, and reconciliation handlers.
- `/health/commerce`, which reports healthy while the integration is intentionally disabled and performs a safe Square connection check when enabled.

The existing `/health/ready` endpoint remains a database readiness check. A disabled or temporarily unavailable commerce integration cannot incorrectly remove the public website from service.

## Configuration

Safe non-secret defaults are version controlled:

```text
Store__Enabled=false
Store__PublicPreviewEnabled=false
Store__Currency=USD
Store__ReservationMinutes=30
Store__DefaultLowStockThreshold=3
Store__OutboxPollSeconds=5
Square__Environment=Sandbox
Square__ApiVersion=2026-07-15
Square__RequestTimeoutSeconds=15
```

The following values are required only when `Store__Enabled=true`:

```text
Square__AccessToken
Square__LocationId
Square__WebhookSignatureKey
Square__WebhookNotificationUrl
Square__CheckoutReturnUrl
```

Production validation rejects a store-enabled deployment when these values are absent or when either URL is not an absolute, non-loopback HTTPS URL.

Use a Square Sandbox token during development and production-like testing. The single production personal access token and webhook signature key must be stored as separate Azure Key Vault secrets. Bicep accepts their Key Vault secret URIs and emits App Service Key Vault references; it never accepts or stores the secret values in source control. The notification URL must exactly match Square's webhook subscription URL, including scheme, host, and path, because it is part of signature validation.

Never put tokens, signature keys, complete webhook bodies, customer details, card information, authorization headers, or provider response bodies in documentation, source code, issue comments, or telemetry.

## Operational Checks

With the feature disabled:

1. Confirm `/health/commerce` returns `{"status":"healthy"}`.
2. Confirm `POST /api/webhooks/square` returns `404`.
3. Confirm the existing external Square storefront remains the only customer sales path.

Before enabling Sandbox:

1. Add the Sandbox token and signature key to Key Vault.
2. Supply the location ID, exact notification URL, checkout return URL, and both secret URIs to the deployment.
3. Keep the public Next.js shop feature disabled.
4. Confirm `/health/commerce` is healthy.
5. Send a signed Square test event and confirm one `SquareWebhookEvents` row and one processed `CommerceOutboxMessages` row.
6. Replay the same event and confirm no second event or outbox record is created.
7. Send an invalid signature and confirm `403` with no persisted event.

## Failure and Rollback

Provider failures retain only a stable safe code. Unexpected application failures continue to use the privacy-safe `ESA-` support reference system.

The immediate full-commerce rollback is `Store__Enabled=false`. That stops the worker, hides webhook intake, and prevents Square calls without deleting catalog, inventory, event, or order history. Set `Store__PublicPreviewEnabled=false` separately when the catalog/configurator preview must also be hidden. Never delete outbox or webhook rows as a rollback mechanism.

## Delivery Sequence

1. Foundation: schema, client, webhook security, outbox, health, and disabled flag.
2. Admin catalog and inventory: one-time draft import, product wizard, variants, stocktake, and media. Implemented behind the disabled public flag.
3. Public configurator: shop pages, deterministic preview, cart, and availability.
4. Checkout and orders: reservations, hosted checkout, fulfillment, email, tracking, staff sales, refunds, and reconciliation.
5. Cutover: production credentials, verified email domain, reviewed inventory, navigation switch, and rollback controls.

Each phase branches from the newly updated `origin/main`, passes its own pull request checks, and is deployed with transactional commerce disabled until cutover. The demo may expose the non-transactional catalog/configurator through its separate preview flag.

## Official References

- [Square Checkout API](https://developer.squareup.com/docs/checkout-api)
- [Create payment link](https://developer.squareup.com/reference/square/checkout-api/create-payment-link)
- [Apply catalog taxes to ad-hoc orders](https://developer.squareup.com/docs/orders-api/apply-taxes-and-discounts/auto-apply-taxes)
- [Validate Square webhooks](https://developer.squareup.com/docs/webhooks/step3validate)
- [Refund payment](https://developer.squareup.com/reference/square/payments-api/refundpayment)
