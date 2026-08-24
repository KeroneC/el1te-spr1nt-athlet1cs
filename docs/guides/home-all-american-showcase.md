# Homepage All-American Showcase

The homepage can display a kinetic photo stack recognizing the club's 2026 AAU Junior Olympic Games results. The first review version intentionally shows only the verified summary: nine All-Americans and eleven All-American performances. Individual names, events, and placements remain absent from visible copy until the roster is fully confirmed.

## Feature control

`HOME_ALL_AMERICAN_SHOWCASE_ENABLED=true` renders the showcase. The homepage is request-rendered so the setting is evaluated by the Next.js server at runtime; cached public CMS fetches keep their existing short revalidation interval. The same immutable application artifact can therefore be configured differently by environment.

- The demo deployment sets the flag to `true` for visual review.
- Production explicitly sets the flag to `false` until the design, athlete details, and publication approval are final.
- An absent, false, or malformed value keeps the established homepage hero unchanged.

## Media preparation

The approved source photographs remain outside the repository. The application contains non-destructive 480-pixel and 960-pixel WebP derivatives at quality 82 under `apps/web/public/images/home/all-americans/`. The component renders only the active image and its two adjacent stack images, supplies responsive `srcset` candidates, and does not give the showcase priority over the primary hero logo.

The relay image uses a landscape finale so every athlete remains visible. Portrait images use per-image focal positions that keep faces, medals, and All-American patches in frame.

## Interaction and accessibility

The showcase advances every five seconds and provides Previous, Pause/Play, and Next controls. Manual navigation pauses the loop. Autoplay also pauses while the showcase is hovered, contains keyboard focus, or the page is hidden.

With reduced motion enabled, autoplay and card transitions are disabled while manual controls remain available. Only the active photograph has meaningful alternative text; the two visible depth cards are decorative and hidden from assistive technology. Automatic changes are not announced.

## Review checklist

- Review at 1440, 1024, 768, 390, and 340 pixels wide.
- Confirm the logo retains its established size and echo treatment.
- Confirm cards do not overlap either call-to-action button.
- Confirm all four relay athletes remain visible.
- Confirm keyboard focus, pause behavior, reduced motion, and no horizontal overflow.
- Keep homepage initial mobile image transfer at or below 1.5 MB.
- Resolve relay surnames and the Claire Jubeck/bib discrepancy before adding individual visible captions.
