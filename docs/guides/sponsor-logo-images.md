# Sponsor Logo Image Guide

Use this standard when preparing a sponsor logo for the public Sponsors page and homepage preview. Consistent source canvases keep marks with very different proportions visually balanced while the website preserves each logo's natural shape.

## Preferred delivery from sponsors

Ask for an official, high-resolution logo in SVG, EPS, or transparent PNG format. Do not use screenshots, photographs, low-resolution website thumbnails, or images with an unintended white background.

Preserve the sponsor's approved artwork, proportions, and brand colors. Do not redraw the mark, add effects, or invent a replacement. Obtain approval before publishing a third-party logo.

## Web-ready standard

- Export a transparent PNG on a 2400 × 1350 pixel canvas (16:9).
- Center the artwork horizontally and vertically.
- Scale the artwork to occupy approximately 80–88% of at least one canvas dimension.
- Keep clear, visually even padding on every side; do not crop or stretch the mark.
- Keep the file below the Admin Media limit of 10 MB.
- Use a descriptive lowercase filename such as `pittsburgh-regional-transit-logo.png`.
- Use a descriptive media title and alt text such as `Pittsburgh Regional Transit logo`.

SVG or EPS is the preferred source because it can be exported cleanly at the standard canvas size. A high-resolution transparent PNG is an acceptable source when vector artwork is unavailable.

## Admin workflow

1. Prepare and inspect the normalized PNG before upload. Confirm the canvas is transparent, the complete mark is visible, and small text remains readable.
2. Upload the normalized file in **Admin → Media** with an accurate title and alt text.
3. Open **Admin → Sponsors**, select the uploaded media item, and confirm the sponsor tier, display order, and official website URL.
4. Keep the previous media asset active until the new logo has been approved publicly. Record the old and new media URLs when replacing an existing mark.
5. Review `/sponsors` and the homepage preview at desktop, tablet, and mobile widths. Check legibility, padding, keyboard focus, external-link behavior, and horizontal overflow.

The public component renders logos with a 16:9 canvas and `object-fit: contain`, so a nonstandard image will not be intentionally cropped or distorted. Normalizing the uploaded file is still important because transparent padding inside the source image determines how large the visible artwork appears.

For the July 2026 asset replacement mapping, see [2026 sponsor media rollback](sponsor-media-rollback-2026.md).
