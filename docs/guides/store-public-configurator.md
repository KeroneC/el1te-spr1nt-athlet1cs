# Public Storefront and Configurator

This is the third guarded commerce phase. It adds the custom public catalog, deterministic product configurator, and non-personal cart while keeping full commerce (`Store__Enabled=false`) disabled. The Azure demo uses the separate `Store__PublicPreviewEnabled=true` review flag. The existing Square storefront remains the live sales path until checkout, order operations, and cutover are approved.

## Feature Gate

The ASP.NET Core public store endpoints return `404 Not Found` when both `Store:Enabled` and `Store:PublicPreviewEnabled` are false. Next.js uses that same response to:

- keep `/shop`, `/shop/{slug}`, and `/shop/cart` unavailable;
- retain the external Square storefront link in the header and footer;
- avoid exposing draft storefront work during staged deployments.

For local or demo review, set `Store__PublicPreviewEnabled=true` on the API process. This exposes browsing, configuration, and the non-personal cart without starting Square-dependent workers or accepting payment. Keep `Store__Enabled=false` until the checkout and order release is approved. When either flag exposes the catalog, navigation automatically points to the internal `/shop`.

## Public Routes and Data

- `/shop` provides search, categories, availability filters, featured ordering, and responsive product cards.
- `/shop/{slug}` provides the real image gallery, tracked option selection, live layered preview, modifier pricing, quantity, and accessible configuration summary.
- `/shop/cart` stores only product/configuration identifiers, controlled-choice labels, minor-unit prices, quantity, and the IDs of free-form customization groups in browser storage. Free-form text or number values are deliberately not persisted; the cart shows that personalization will be confirmed securely during checkout.

The public API never returns SKU values or exact on-hand/reserved quantities. Each product and variant exposes only `InStock`, `LowStock`, or `SoldOut`. Available stock is still calculated server-side as on-hand minus reserved. A variant is low stock when its available amount is positive and at or below its Admin threshold.

The cart rechecks every saved variant through the same-origin Next.js boundary. It reports price changes, low stock, sold-out configurations, removed variants, and temporary verification failures. No buyer name, email, phone, athlete information, payment details, or order record is created in this phase.

## Preparing a Product

Before publishing a product in Admin:

1. Add at least one active tracked variant with a unique SKU.
2. Receive physical stock in Merchandise > Inventory.
3. Add a real product image with useful alt text.
4. Assign a `Mockup base` image for the live preview when available.
5. Assign transparent artwork as `Logo overlay`, place it with percentage coordinates, and save the visualizer layers.
6. Add size/garment-color tracked options and any untracked logo treatment, name, or number modifiers.
7. Check every size/color combination and its low-stock threshold.
8. Preview desktop, tablet, mobile, keyboard, and reduced-motion behavior before changing the product to Published.

Visualizer images are layered deterministically at runtime. No AI service is called during customer use. Approved mockup preparation may happen offline, but the El1te mark must remain exact and third-party artwork must not be invented.

## Cart and Checkout Boundary

The cart intentionally ends before payment in this phase. Its Square checkout control stays disabled and clearly states that the guarded payment connection is pending. Phase 4 will replace that preview boundary with server-side cart validation, transactional inventory reservations, customer/order creation, and a unique Square-hosted payment link.

The browser price is presentation data, never final authority. Phase 4 must recalculate product, variant, modifier, quantity, stock, and currency from the database before reserving inventory or creating an order.

## Validation

Run:

```bash
dotnet build apps/api/El1teSpr1ntTrack.sln -c Release
dotnet test apps/api/El1teSpr1ntTrack.sln -c Release --no-build
cd apps/web
npm run lint
npm run typecheck
npm test
npm run build
```

Cross-stack Playwright enables the store only in its isolated E2E API process. It creates a published product, receives low stock, browses the catalog, verifies the configurator, adds the item, rechecks availability in the cart, and cleans up its test records.

## Rollback

Set both `Store__PublicPreviewEnabled=false` and `Store__Enabled=false`. Public endpoints return 404 and the public header/footer return to the external Square link. Catalog, media, variants, visualizer settings, and inventory history remain intact. Do not delete products, inventory adjustments, or media as a rollback mechanism.
