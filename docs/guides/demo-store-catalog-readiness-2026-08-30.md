# Demo store catalog readiness — August 30, 2026

This checklist records a read-only review of the 11 products in the noindex demo. Demo quantities are test data and must not be promoted. Every launch product starts at zero in production and requires a signed physical stocktake before checkout is enabled.

## Product review

| Product | Current setup | Readiness action |
| --- | --- | --- |
| Biker Shorts Set | Draft; $30; 5 media assets; Size (4) × Color (2); 8 active variants with unique SKUs; required four-choice logo treatment; no descriptions | Gear lead reviews media, option values, customization wording, price, and SKUs. Add customer-facing copy, then stocktake after promotion. |
| El1te Trucker Hats | Draft; $15; 2 media assets; no physical options or active variants; no descriptions | Keep private. Confirm the physical inventory dimensions and create at least one active variant before publication. |
| El1te T-Shirt | Draft; $20; 4 media assets; Size (5) × Color (4); 20 active variants with unique SKUs; no descriptions | Gear lead reviews media, colors, price, and SKUs. Add customer-facing copy, then stocktake after promotion. |
| Joggers | Draft; $30; 3 media assets; Color (4) × Size (5) configured but no active variants; no descriptions | Keep private. Verify the option values and generate the 20-variant matrix before publication. |
| Long Sleeve Top w/ Leggings | Draft; $40; 5 media assets; Size (3) × Color (2); 6 active variants with unique SKUs; no descriptions | Gear lead reviews size labels, media, price, and SKUs. Add customer-facing copy, then stocktake after promotion. |
| Quarter Zip Sweat Suit Set | Draft; $65; 2 media assets; Size (4) × Color (2); 8 active variants with unique SKUs; no descriptions | Gear lead reviews media, price, and SKUs. Add customer-facing copy, then stocktake after promotion. |
| Racer Back Tank Tops | Draft; $15; 2 media assets; Size (5) × Red; 5 active variants with unique SKUs; no descriptions | Gear lead reviews media, price, naming, and SKUs. Add customer-facing copy, then stocktake after promotion. |
| Track Mom T-Shirt | Draft; $20; 1 media asset; Size (5) configured but no active variants; no descriptions | Repair the copied draft slug to `/track-mom-t-shirt`. Keep private and generate the five-size matrix before publication. |
| Winged Spikes T-Shirt | Draft; $20; 1 media asset; Size (5) configured but no active variants; no descriptions | Repair the copied draft slug to `/winged-spikes-t-shirt`. Keep private and generate the five-size matrix before publication. |
| El1te Hoodie | Published demo baseline; $45; Size (5) × garment color (4); 20 active variants; required logo-color customization; customer-facing short copy present | Retain inactive legacy variants for audit only. Gear lead reapproves the launch configuration and production receives a fresh stocktake. |
| Get Over It T-Shirt | Published demo baseline; $20; 2 media assets; Size (5) × Red; 5 active variants with unique SKUs; customer-facing copy present | Gear lead reapproves the launch configuration and production receives a fresh stocktake. |

## Readiness groups

- **Ready for gear-lead review:** Biker Shorts Set, El1te T-Shirt, Long Sleeve Top w/ Leggings, Quarter Zip Sweat Suit Set, Racer Back Tank Tops, El1te Hoodie, and Get Over It T-Shirt.
- **Missing catalog configuration:** El1te Trucker Hats, Joggers, Track Mom T-Shirt, and Winged Spikes T-Shirt. These remain drafts.
- **Awaiting customer-facing copy:** all newly configured drafts currently have blank short and full descriptions.
- **Awaiting physical stocktake:** every product selected for production launch, without exception.
- **Ready to publish after production promotion:** only products that have completed gear-lead approval, copy review, promotion hash verification, and production stocktake.

## Promotion guardrails

- Promote catalog structure, media, prices, mappings, and stable IDs only after review.
- Reset all promoted on-hand and reserved quantities to zero.
- Do not publish incomplete products or infer missing inventory dimensions.
- Preserve inactive variants and their adjustments as history, but exclude them from operational Admin totals.
