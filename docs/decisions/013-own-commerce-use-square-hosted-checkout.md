# ADR 013: Own Commerce Operations and Use Square-Hosted Checkout

## Status

Accepted

## Context

The existing Square storefront accepts payments but makes product configuration, variant inventory, order production, and practice handoff tedious for staff. El1te needs a tailored customer and Admin experience without taking responsibility for payment-card data or building a second catalog that must remain synchronized with Square.

## Decision

El1te is the source of truth for products, tracked variants, modifiers, inventory, reservations, operational orders, and fulfillment. Square remains the financial provider for hosted payment, receipts, refunds, and reconciliation.

Each checkout creates a unique Square-hosted payment link from an immutable local order snapshot using ad-hoc itemized lines. Staff create and maintain products in the El1te Admin workspace; Square catalog import and synchronization are not part of the supported workflow. Historical pre-cutover orders remain in Square.

Square webhooks are validated against the exact raw request body and configured notification URL, deduplicated by event ID, and processed through a SQL outbox. Raw payment or customer payloads are not retained. Inventory is reserved transactionally before checkout and never permits backorders.

The store remains behind a disabled feature flag through all implementation phases. The existing Square storefront remains available until production-like validation and explicit cutover.

## Consequences

- Customers get an El1te-specific catalog and configurator while card details remain outside the platform.
- Staff get one operational source of truth for stock and fulfillment.
- Order-item snapshots protect historical price and configuration meaning when products later change.
- Webhook, reservation-expiry, email, and reconciliation work must be idempotent and recoverable.
- Manual catalog review and a deliberate physical stocktake are required before cutover.
- Rollback can disable the feature and restore the external Square link without deleting commerce history.
