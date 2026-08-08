export type UserRole = "Parent" | "Athlete" | "Coach" | "Admin" | "SuperAdmin";

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: UserRole;
  };
}

export interface AdminLoginResult {
  requiresMfa: boolean;
  authentication: LoginResponse | null;
  challengeToken: string | null;
  challengeExpiresAtUtc: string | null;
}

export interface AdminAnnouncement {
  id: string;
  title: string;
  slug: string;
  summary: string;
  body: string;
  imageUrl: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  publishDateUtc: string | null;
  expirationDateUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface AnnouncementWriteRequest {
  title: string;
  summary: string;
  body: string;
  imageUrl: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  publishDateUtc: string | null;
  expirationDateUtc: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  referenceId?: string;
}

export interface AnnouncementFilters {
  search?: string;
  isPublished?: string;
  isFeatured?: string;
  includeExpired?: string;
  page?: string;
}

export type AnnouncementState = "Draft" | "Scheduled" | "Published" | "Expired";

export const EVENT_TYPES = ["Other", "Practice", "Meet", "Fundraiser", "TeamEvent", "RegistrationDeadline"] as const;
export type EventType = typeof EVENT_TYPES[number];
export const SPONSOR_TIERS = ["Platinum", "Gold", "Silver", "Bronze", "Community", "Other"] as const;
export type SponsorTier = typeof SPONSOR_TIERS[number];
export const INQUIRY_TYPES = ["General", "Parent", "Sponsor", "Volunteer", "Registration", "Other"] as const;
export type InquiryType = typeof INQUIRY_TYPES[number];
export const CONTACT_STATUSES = ["New", "Read", "Resolved", "Archived"] as const;
export type ContactSubmissionStatus = typeof CONTACT_STATUSES[number];

export interface AdminEvent {
  id: string; title: string; slug: string; eventType: EventType; startDateTimeUtc: string;
  endDateTimeUtc: string | null; locationName: string; address: string | null; description: string;
  registrationUrl: string | null; imageUrl: string | null; isFeatured: boolean; isPublished: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type EventWriteRequest = Omit<AdminEvent, "id" | "slug" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminCoach {
  id: string; firstName: string; lastName: string; role: string; bio: string; imageUrl: string | null;
  email: string | null; isEmailPublic: boolean; displayOrder: number; isActive: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type CoachWriteRequest = Omit<AdminCoach, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminHallOfFameInductee {
  id: string; name: string; slug: string; affiliation: string; summary: string;
  photoUrl: string | null; photoAlt: string | null; inductionYear: number | null;
  displayOrder: number; isActive: boolean; createdAtUtc: string; updatedAtUtc: string | null;
}
export type HallOfFameInducteeWriteRequest = Omit<AdminHallOfFameInductee, "id" | "slug" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminSponsor {
  id: string; name: string; slug: string; tier: SponsorTier; logoUrl: string | null;
  websiteUrl: string | null; description: string | null; displayOrder: number; isActive: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type SponsorWriteRequest = Omit<AdminSponsor, "id" | "slug" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminFaq {
  id: string; question: string; answer: string; category: string; displayOrder: number; isActive: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type FaqWriteRequest = Omit<AdminFaq, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminContentBlock {
  id: string; key: string; title: string; summary: string | null; body: string; imageUrl: string | null;
  ctaText: string | null; ctaUrl: string | null; displayOrder: number; isPublished: boolean;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export type ContentBlockWriteRequest = Omit<AdminContentBlock, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminSiteSettings {
  id: string; clubName: string; slogan: string; contactEmail: string; phoneNumber: string | null;
  addressLine1: string | null; addressLine2: string | null; city: string | null; state: string | null;
  zipCode: string | null; facebookUrl: string | null; instagramUrl: string | null; youtubeUrl: string | null;
  primaryCtaText: string; primaryCtaUrl: string; secondaryCtaText: string; secondaryCtaUrl: string;
  logoUrl: string | null; createdAtUtc: string; updatedAtUtc: string | null;
}
export type SiteSettingsWriteRequest = Omit<AdminSiteSettings, "id" | "createdAtUtc" | "updatedAtUtc">;

export interface AdminContactSubmission {
  id: string; name: string; email: string; phone: string | null; inquiryType: InquiryType;
  message: string; status: ContactSubmissionStatus; createdAtUtc: string; updatedAtUtc: string | null;
}

export interface ListFilters { search?: string; page?: string; }
export interface EventFilters extends ListFilters { eventType?: string; isPublished?: string; isFeatured?: string; fromDate?: string; toDate?: string; }
export interface ActiveFilters extends ListFilters { isActive?: string; }
export interface HallOfFameFilters extends ActiveFilters { inductionYear?: string; }
export interface SponsorFilters extends ActiveFilters { tier?: string; }
export interface FaqFilters extends ActiveFilters { category?: string; }
export interface ContentFilters extends ListFilters { isPublished?: string; }
export interface ContactFilters extends ListFilters { status?: string; inquiryType?: string; fromDate?: string; toDate?: string; }

export interface AdminMediaAsset {
  id: string; originalFileName: string; contentType: string; fileExtension: string; fileSizeBytes: number;
  width: number; height: number; title: string; altText: string; caption: string | null;
  publicUrl: string; isActive: boolean; createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminGalleryAlbumListItem {
  id: string; title: string; slug: string; description: string; coverMediaAssetId: string | null;
  coverImageUrl: string | null; isPublished: boolean; eventDateUtc: string | null;
  displayOrder: number; imageCount: number; createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminGalleryAlbumMedia {
  id: string; mediaAssetId: string; publicUrl: string; title: string; altText: string; caption: string | null;
  altTextOverride: string | null; captionOverride: string | null; displayOrder: number;
  isActive: boolean; width: number; height: number;
}
export interface AdminGalleryAlbum extends Omit<AdminGalleryAlbumListItem, "coverImageUrl" | "imageCount"> {
  media: AdminGalleryAlbumMedia[];
}

export interface AdminUser {
  id: string; firstName: string; lastName: string; email: string; role: "Admin" | "SuperAdmin";
  isActive: boolean; createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminInvitation {
  id: string; firstName: string; lastName: string; email: string; role: "Admin" | "SuperAdmin";
  status: "Pending" | "Accepted" | "Expired" | "Revoked"; expiresAtUtc: string; createdAtUtc: string;
  invitedByDisplayName: string;
}
export interface AdminInvitationCreated {
  invitation: AdminInvitation;
  invitationUrl: string;
}
export interface AdminActivityLog {
  id: string; createdAtUtc: string; actorDisplayName: string; action: string; targetType: string;
  targetId: string | null; summary: string; correlationId: string | null;
}
export interface AdminInvitationDetails {
  firstName: string; lastName: string; email: string; role: "Admin" | "SuperAdmin"; expiresAtUtc: string;
}

export type StoreProductStatus = "Draft" | "Published" | "Archived";
export type ProductMediaRole = "Gallery" | "MockupBase" | "LogoOverlay";
export type ProductModifierType = "Choice" | "Color" | "ShortText" | "Number";
export type InventoryAdjustmentReason = "Receipt" | "Correction" | "Damage" | "ReturnRestock";

export interface AdminStoreDashboard {
  draftProducts: number; publishedProducts: number; activeVariants: number;
  lowStockVariants: number; soldOutVariants: number; totalOnHand: number;
}
export interface AdminProductCategory {
  id: string; name: string; slug: string; displayOrder: number; isActive: boolean;
  productCount: number; squareCatalogObjectId: string | null;
}
export interface AdminStoreProductSummary {
  id: string; name: string; slug: string; categoryName: string | null; basePriceMinor: number;
  currency: string; status: StoreProductStatus; isFeatured: boolean; displayOrder: number;
  variantCount: number; totalOnHand: number; totalAvailable: number; lowStockVariantCount: number;
  primaryImageUrl: string | null; squareCatalogObjectId: string | null;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminProductMedia {
  id: string; mediaAssetId: string; publicUrl: string; title: string; altText: string;
  role: ProductMediaRole; altTextOverride: string | null; displayOrder: number;
}
export interface AdminProductOptionValue {
  id: string; name: string; slug: string; colorHex: string | null; swatchMediaAssetId: string | null;
  displayOrder: number; isActive: boolean; squareCatalogObjectId: string | null;
}
export interface AdminProductOption {
  id: string; name: string; isTracked: boolean; displayOrder: number; isActive: boolean;
  squareCatalogObjectId: string | null; values: AdminProductOptionValue[];
}
export interface AdminProductVariant {
  id: string; name: string; sku: string; priceOverrideMinor: number | null; onHandQuantity: number;
  reservedQuantity: number; availableQuantity: number; lowStockThreshold: number; isActive: boolean;
  squareCatalogObjectId: string | null; squareCatalogVersion: number | null; rowVersion: string;
  optionValueIds: string[];
}
export interface AdminProductModifierValue {
  id: string; name: string; priceAdjustmentMinor: number; colorHex: string | null;
  overlayMediaAssetId: string | null; displayOrder: number; isActive: boolean;
}
export interface AdminProductModifierGroup {
  id: string; name: string; type: ProductModifierType; isRequired: boolean;
  minimumSelections: number; maximumSelections: number; displayOrder: number;
  isActive: boolean; values: AdminProductModifierValue[];
}
export interface AdminProductVisualizerLayer {
  id: string; mediaAssetId: string; productOptionValueId: string | null;
  productModifierValueId: string | null; xPercent: number; yPercent: number;
  widthPercent: number; heightPercent: number; zIndex: number; blendMode: string;
}
export interface AdminStoreProduct {
  id: string; categoryId: string | null; name: string; slug: string; shortDescription: string | null;
  description: string | null; basePriceMinor: number; currency: string; status: StoreProductStatus;
  isFeatured: boolean; displayOrder: number; allowsSpecialRequests: boolean;
  squareCatalogObjectId: string | null; squareCatalogVersion: number | null; importedAtUtc: string | null;
  media: AdminProductMedia[]; options: AdminProductOption[]; variants: AdminProductVariant[];
  modifierGroups: AdminProductModifierGroup[]; visualizerLayers: AdminProductVisualizerLayer[];
  createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminInventoryVariant {
  productId: string; productName: string; variantId: string; variantName: string; sku: string;
  onHandQuantity: number; reservedQuantity: number; availableQuantity: number; lowStockThreshold: number;
  isLowStock: boolean; isSoldOut: boolean; isActive: boolean; rowVersion: string; updatedAtUtc: string | null;
}
export interface SquareCatalogImportPreviewProduct {
  squareCatalogObjectId: string; name: string; variantCount: number; imageCount: number; alreadyImported: boolean;
}
export interface SquareCatalogImportPreview {
  isConfigured: boolean; productCount: number; newProductCount: number; products: SquareCatalogImportPreviewProduct[];
}
export interface SquareCatalogImportResult {
  importRunId: string; productsDiscovered: number; productsCreated: number; productsSkipped: number; imagesImported: number;
}

export type AdminStoreOrderStatus = "AwaitingPayment" | "Paid" | "NeedsReview" | "ReadyForProduction" |
  "InProduction" | "NeedsCustomerInfo" | "ReadyForHandoff" | "Completed" | "Canceled" | "Refunded";
export type AdminPaymentStatus = "Pending" | "Authorized" | "Paid" | "Refunding" | "Refunded" | "PartiallyRefunded" | "Failed" | "Canceled";
export interface AdminStoreOperationsDashboard { awaitingPayment: number; cancellationHold: number; needsReview: number; inProduction: number; readyForHandoff: number; refundFailures: number; emailFailures: number; }
export interface AdminStoreOrderSummary { id: string; orderReference: string; customerName: string; customerEmail: string; status: AdminStoreOrderStatus; paymentStatus: AdminPaymentStatus; totalMinor: number; currency: string; hasPersonalization: boolean; customerCancellationExpiresAtUtc: string | null; createdAtUtc: string; }
export interface AdminStoreOrder {
  id: string; orderReference: string; customerName: string; customerEmail: string; customerPhone: string;
  athleteTeamNote: string | null; fulfillmentNote: string | null; status: AdminStoreOrderStatus; paymentStatus: AdminPaymentStatus;
  subtotalMinor: number; taxMinor: number; totalMinor: number; currency: string; hasPersonalization: boolean;
  customerCancellationExpiresAtUtc: string | null; squareOrderId: string | null; squarePaymentId: string | null;
  items: Array<{ id: string; productVariantId: string | null; productName: string; variantName: string; sku: string; quantity: number; unitPriceMinor: number; lineTotalMinor: number; configuration: Array<{ label: string; value: string }> }>;
  timeline: Array<{ fromStatus: AdminStoreOrderStatus; toStatus: AdminStoreOrderStatus; note: string | null; createdAtUtc: string }>;
  notes: Array<{ id: string; note: string; createdAtUtc: string }>;
  refunds: Array<{ id: string; amountMinor: number; status: string; reason: string; safeFailureCode: string | null; createdAtUtc: string }>;
  emails: Array<{ id: string; templateName: string; status: string; safeFailureCode: string | null; createdAtUtc: string; sentAtUtc: string | null }>;
  createdAtUtc: string; updatedAtUtc: string | null;
}
export interface AdminCommerceIntegrationHealth { checkoutEnabled: boolean; squareConfigured: boolean; squareReachable: boolean; pendingOutboxMessages: number; failedRefunds: number; failedEmails: number; }
