# Store Catalog and Inventory Administration

The private Admin commerce workspace is the source of truth for merchandise catalog and inventory data. Staff create and review products here before enabling public browsing or checkout. Square is used for hosted payment, receipts, and refunds; its catalog is not imported or synchronized.

## What Staff Can Do

All active Admins and SuperAdmins can:

- view draft, published, low-stock, sold-out, variant, and on-hand summaries;
- create and edit categories;
- create products through the guided Basics, Media, Variants, Customizations, and Preview workflow;
- duplicate a product into a new draft with new option identifiers, new SKUs, and zero stock;
- archive products without deleting order or inventory history;
- configure physical size/garment-color options and concrete SKUs;
- configure logo color, logo treatment, name, and number choices without splitting stock;
- position approved transparent visualizer layers with percentage-based coordinates;
- receive stock, record corrections/damage/returns, and complete physical stocktakes.

Catalog and inventory access does not grant refund, Square-configuration, or tracking-link authority. Historical Square source IDs and import-run rows remain mapped for audit and backward compatibility, but no Admin page or API can initiate a catalog import.

## Product Workflow

1. Open **Admin → Merchandise → Catalog**.
2. Create a product or duplicate a similar draft.
3. Enter its name, category, base USD price, descriptions, order, and special-request behavior.
4. Select real product media from the reusable Media library. Assign:
   - `Gallery` to customer-facing photography;
   - `MockupBase` to the garment/base visualizer image;
   - `LogoOverlay` to approved transparent layers.
5. Add only physical inventory options such as Size and Garment color, add their values, and generate the variant matrix. Every active option in this step is tracked.
6. Review every generated SKU and low-stock threshold. Stock is not edited in the product wizard.
7. Add logo color, logo treatment, and optional name/number input under Customizations. These choices never multiply physical stock.
8. Add approved overlay media to the visualizer and set X, Y, width, height, and layer order as percentages.
9. Preview and save.

Drafts may be incomplete and saved for later. Publishing requires at least one image and one active variant. A published record appears in the non-transactional storefront only when `Store:PublicPreviewEnabled` or full commerce is enabled.

Removing an option that participates in existing variants requires confirmation. Those existing rows are omitted and therefore deactivated rather than deleted, preserving inventory, adjustment, reservation, and order history. Newly generated replacement variants start at zero and require a physical stocktake before purchase. Modifier and visualizer configuration can be replaced because paid order items retain immutable configuration snapshots.

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

1. Review every product name, price, category, photograph, and description.
2. Verify each tracked option and SKU matches a physical size/color combination.
3. Complete a physical stocktake for every active variant.
4. Verify low-stock thresholds with staff.
5. Archive obsolete products; do not delete operational history.
6. Approve every base mockup and transparent overlay.
7. Confirm `Store:Enabled=false` and the external Square storefront still handles sales.

## Rollback

Full-commerce rollback is `Store__Enabled=false`; hide the catalog/configurator as well with `Store__PublicPreviewEnabled=false`. Products stay as reversible drafts. Archive an unwanted product; do not delete inventory adjustments, stocktakes, historical import runs, or referenced media.

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

The historical migration `AddStoreCatalogAdministration` retains nullable Square source IDs and import-run records for compatibility. No destructive migration removes those fields or audit records.
