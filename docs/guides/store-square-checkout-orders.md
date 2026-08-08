# Square Checkout and Order Operations

This release completes the Square-only launch workflow for physically counted club inventory. Printify development remains paused on `feature/store-printify-foundation` and is not part of the launch application.

## Runtime gates

The catalog and payments have separate controls:

- `Store__Enabled=true` exposes the full internal store.
- `Store__CheckoutEnabled=true` enables order creation, Square webhooks, reconciliation, refunds, and order email.
- Both values must be true for transactional operations. Disable `Store__CheckoutEnabled` first during an incident; public catalog browsing can remain available.
- Demo uses `Square__Environment=Sandbox`. Never use production Square credentials in demo.

Required Square secrets stay in Azure Key Vault: access token and webhook signature key. The public webhook URL and checkout-return URL must exactly match the corresponding Square Sandbox configuration. Card data never passes through El1te.

The demo deployment workflow has an `enable_square_sandbox_checkout` switch. It stays off by default. Before switching it on, configure these GitHub `demo` environment variables (the credential values themselves remain in Key Vault):

- `SQUARE_LOCATION_ID`
- `SQUARE_WEBHOOK_NOTIFICATION_URL`
- `SQUARE_CHECKOUT_RETURN_URL`
- `SQUARE_ACCESS_TOKEN_SECRET_URI`
- `SQUARE_WEBHOOK_SIGNATURE_KEY_SECRET_URI`

The workflow rejects an enabled deployment when any setting is missing or when the URLs are not the expected HTTPS webhook, confirmation, and Key Vault URI forms.

## Customer workflow

1. The browser stores only non-personal cart configuration.
2. Checkout revalidates every product, variant, modifier, personalization value, price, and available unit in SQL.
3. The API reserves club inventory for 30 minutes and creates one idempotent Square payment link.
4. A short-lived HttpOnly cookie connects the Square return to a processing page; the redirect is never accepted as payment proof.
5. Signed Square events trigger a fresh payment lookup. Currency, amount, Square order ID, and completed status must match before inventory is sold.
6. The confirmation email carries a random order-status secret in the URL fragment. Only its SHA-256 hash is stored.
7. For 30 minutes after verified payment, the customer can cancel the complete order. Production is locked, stock is restored once, and a full Square refund is queued.
8. Standard orders become ready for production after the hold. Name/number orders remain in review until staff approves them.

## Staff workflow

Use **Admin → Merchandise → Orders** for payment review, production instructions, internal notes, customer-information requests, ready-for-handoff updates, and completion. Only SuperAdmins may issue refunds or rotate secure tracking links. Every refund requires a reason and line-by-line restock quantities.

Expired unpaid orders are reconciled with Square before their payment link is deleted and stock is released. Ambiguous provider responses keep inventory reserved and create visible operational work instead of risking a late paid order or overselling.

## Demo test checklist

- Successful and declined Sandbox cards.
- Abandoned checkout and expired-link stock release.
- Duplicate and delayed payment webhooks.
- Final-unit concurrency and sold-out cart changes.
- Standard and personalized order status paths.
- Customer cancellation just before and after the 30-minute boundary.
- Full and partial SuperAdmin refunds with zero, partial, and full restocking.
- Failed email retry and tracking-link rotation.
- Keyboard-only checkout, narrow screens, reduced motion, and policy links.

Rollback sets `Store__CheckoutEnabled=false`, then restores the external Square-store navigation if required. Do not delete local orders, inventory adjustments, refunds, or reconciliation history.
