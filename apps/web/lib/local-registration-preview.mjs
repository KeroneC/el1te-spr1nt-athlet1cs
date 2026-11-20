/** Local rehearsal only. This gate cannot enable the artwork in a production build. */
export function isLocalRegistrationArchivePreview(env = process.env) {
  return env.NODE_ENV === "development" && env.STARTING_LINE_ARCHIVE_PREVIEW === "true" &&
    env.SITE_URL === "http://127.0.0.1:3013" && env.API_BASE_URL === "http://127.0.0.1:5130";
}

export const localArchiveArtwork = ["starting-blocks", "team-huddle"].flatMap(photo =>
  [480, 960, 1440].map(width => `${photo}-${width}.webp`));
