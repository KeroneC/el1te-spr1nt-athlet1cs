# Store Catalog and Inventory Administration

This is the second guarded delivery phase of the El1te merchandise replacement. It adds a private Admin commerce workspace and a one-time Square catalog copy while keeping full commerce (`Store:Enabled=false`) disabled. A later demo-only preview may expose catalog reads, but the existing Square storefront remains the only active sales path.

## What Staff Can Do

All active Admins and SuperAdmins can:

- view draft, published, low-stock, sold-out, variant, and on-hand summaries;
- create and edit categories;
- create products through the guided Basics, Media, Variants, Customizations, and Preview workflow;
- duplicate a product into a new draft with new option identifiers, new SKUs, and zero stock;
- archive products without deleting order or inventory history;
- configure tracked size/color options and concrete SKUs;
- configure untracked logo, name, and number choices;
- position approved transparent visualizer layers with percentage-based coordinates;
- receive stock, record corrections/damage/returns, and complete physical stocktakes.

Only a SuperAdmin can preview or run the one-time Square import because it calls a credentialed financial-provider integration. Catalog and inventory access does not grant refund, Square-configuration, or tracking-link authority.

## Safe Square Import

The import reads Square Catalog items, categories, item options, variations, fixed prices, image references, and the selected location's `IN_STOCK` quantities. It creates unpublished El1te drafts and copies trusted Square-hosted images into the existing El1te Media library.

The import:

- never publishes a product;
- never imports historical orders or customer/payment data;
- skips a product whose Square catalog object ID was imported previously;
- gives generated local SKUs to variations that do not have one;
- records source object IDs and versions for audit/idempotency, not ongoing synchronization;
- creates an initial Receipt adjustment for imported positive inventory;
- records a safe import-run result without provider bodies or credentials;
- runs catalog persistence transactionally so a failure does not expose a partial local catalog.

After the first successful import, El1te is the editing source of truth. Do not use this action as a recurring synchronization job.

The import needs `Square__AccessToken` and `Square__LocationId` even while `Store__Enabled=false`. Keep the token in Azure Key Vault and supply its secret URI through the existing deployment input. The Admin portal never accepts or displays the token.

## Product Workflow

1. Open **Admin → Merchandise → Catalog**.
2. Create a product or duplicate a similar draft.
3. Enter its name, category, base USD price, descriptions, order, and special-request behavior.
4. Select real product media from the reusable Media library. Assign:
   - `Gallery` to customer-facing photography;
   - `MockupBase` to the garment/base visualizer image;
   - `LogoOverlay` to approved transparent layers.
5. Add tracked options such as Size and Garment color, add their values, and generate the variant matrix.
6. Review every generated SKU and low-stock threshold. Stock is not edited in the product wizard.
7. Add untracked choices such as logo treatment plus optional name/number input.
8. Add approved overlay media to the visualizer and set X, Y, width, height, and layer order as percentages.
9. Preview and save.

Drafts may be incomplete and saved for later. Publishing requires at least one image and one active variant. A published record appears in the non-transactional storefront only when `Store:PublicPreviewEnabled` or full commerce is enabled.

Omitting an existing variant while editing deactivates it rather than deleting it. This preserves inventory, reservation, and order references. Modifier and visualizer configuration can be replaced because paid order items will retain immutable configuration snapshots.

## Product Image Preparation

Use real, approved product photography or club-owned mockups. Do not invent, redraw, or alter third-party marks.

Recommended source:

- PNG or WebP for transparency; high-quality JPEG for photography;
- at least 1600 px on the longest side, preferably 2400 px for reusable mockups;
- under the Media library's 10 MB limit;
- tightly framed with predictable transparent padding;
- one consistent camera angle and garment scale across color choices;
- an accurate title and useful alt text;
- transparent logo-treatment layers on the same canvas/proportions as their base mockup.

AI image tools may help clean or standardize an approved garment reference during asset preparation, but runtime previews are deterministic and the El1te logo artwork must remain exact. Staff must approve every edited mockup before public cutover.

## Inventory Rules

Available stock is `on hand - reserved`. Public APIs will eventually expose only In stock, Low stock, or Sold out.

- **Receive** uses a positive quantity and creates a Receipt adjustment.
- **Physical count** records expected and counted on-hand values. Only changed rows create Correction adjustments.
- **Adjust** accepts a signed whole-number change. Correction may increase or decrease; Damage must decrease; ReturnRestock must increase.
- Manual Admin calls cannot use Sale, ReservationRelease, or ReturnWithoutRestock because later order/refund workflows own those reasons.
- No operation can make on-hand inventory negative or lower than its reserved quantity.
- Row-version concurrency detects stale screens. Refresh rather than overriding a conflict.
- Adjustments and stocktake lines are append-only operational history.

Do not change physical quantity in the product wizard. Use the Inventory workspace so the reason, actor, result, and timestamp remain auditable.

## Pre-Public Review

Before the configurator or checkout phases use this data:

1. Review every imported product name, price, category, photograph, and description.
2. Verify each tracked option and SKU matches a physical size/color combination.
3. Complete a physical stocktake for every active variant.
4. Verify low-stock thresholds with staff.
5. Archive obsolete imports; do not delete history.
6. Approve every base mockup and transparent overlay.
7. Confirm `Store:Enabled=false` and the external Square storefront still handles sales.

## Rollback

The phase does not enable checkout. Full-commerce rollback is `Store__Enabled=false`; hide the catalog/configurator as well with `Store__PublicPreviewEnabled=false`. Imported products stay as reversible drafts and source IDs make a repeated import safe. Archive an unwanted product; do not delete inventory adjustments, stocktakes, import runs, or media that may be referenced.

## Validation

Run:

```bash
dotnet test apps/api/El1teSpr1ntTrack.sln --no-restore
dotnet ef migrations has-pending-model-changes \
  --project apps/api/src/El1teSpr1ntTrack.Infrastructure/El1teSpr1ntTrack.Infrastructure.csproj \
  --startup-project apps/api/src/El1teSpr1ntTrack.Api/El1teSpr1ntTrack.Api.csproj
npm --prefix apps/web test
npm --prefix apps/web run lint
npm --prefix apps/web run typecheck
npm --prefix apps/web run build
```

The migration `AddStoreCatalogAdministration` preserves existing option/modifier rows as active, adds unique nullable Square source IDs, and adds audited stocktake and import-run records.

## Official Square References

- [Search Catalog objects](https://developer.squareup.com/reference/square/catalog-api/search-catalog-objects)
- [Catalog item options](https://developer.squareup.com/docs/catalog-api/item-options)
- [Batch retrieve inventory counts](https://developer.squareup.com/reference/square/inventory-api/batch-retrieve-inventory-counts)
