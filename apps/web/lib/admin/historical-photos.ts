export const historicalReviews = {
  NeedsReview: "Needs review",
  ApprovedForSocial: "Approved for social",
  Restricted: "Restricted"
} as const;

export type HistoricalReview = keyof typeof historicalReviews;

export type HistoricalPhoto = {
  id: string;
  fileNames: string[];
  collections: string[];
  review: HistoricalReview;
  reviewedAtUtc: string | null;
};

export type HistoricalPhotoList = {
  items: HistoricalPhoto[];
  collections: string[];
  page: number;
  totalPages: number;
  totalCount: number;
};

export function historicalPhotosEnabled() {
  return process.env.LEGACY_MEDIA_ARCHIVE_ENABLED === "true";
}
