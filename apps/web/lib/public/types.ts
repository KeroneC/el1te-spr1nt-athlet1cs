export type EventType =
  | "Other"
  | "Practice"
  | "Meet"
  | "Fundraiser"
  | "TeamEvent"
  | "RegistrationDeadline";

export type SponsorTier =
  | "Platinum"
  | "Gold"
  | "Silver"
  | "Bronze"
  | "Community"
  | "Other";

export type InquiryType =
  | "General"
  | "Parent"
  | "Sponsor"
  | "Volunteer"
  | "Registration"
  | "Other";

export interface SiteSettings {
  clubName: string;
  slogan: string;
  contactEmail: string;
  phoneNumber: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  state: string | null;
  zipCode: string | null;
  facebookUrl: string | null;
  instagramUrl: string | null;
  youTubeUrl: string | null;
  primaryCtaText: string;
  primaryCtaUrl: string;
  secondaryCtaText: string;
  secondaryCtaUrl: string;
  logoUrl: string | null;
}

export interface ContentBlock {
  key: string;
  title: string;
  summary: string | null;
  body: string;
  imageUrl: string | null;
  ctaText: string | null;
  ctaUrl: string | null;
  displayOrder: number;
}

export interface AnnouncementListItem {
  title: string;
  slug: string;
  summary: string;
  imageUrl: string | null;
  isFeatured: boolean;
  publishDateUtc: string | null;
}

export interface AnnouncementDetail extends AnnouncementListItem {
  body: string;
}

export interface EventListItem {
  title: string;
  slug: string;
  eventType: EventType;
  startDateTimeUtc: string;
  endDateTimeUtc: string | null;
  locationName: string;
  imageUrl: string | null;
  isFeatured: boolean;
}

export interface EventDetail extends EventListItem {
  address: string | null;
  description: string;
  registrationUrl: string | null;
}

export interface Coach {
  firstName: string;
  lastName: string;
  role: string;
  bio: string;
  imageUrl: string | null;
  email: string | null;
  displayOrder: number;
}

export interface HallOfFameInductee {
  name: string;
  slug: string;
  affiliation: string;
  summary: string;
  photoUrl: string;
  photoAlt: string;
  inductionYear: number | null;
  displayOrder: number;
}

export interface Sponsor {
  name: string;
  slug: string;
  tier: SponsorTier;
  logoUrl: string | null;
  websiteUrl: string | null;
  description: string | null;
  displayOrder: number;
}

export interface Faq {
  question: string;
  answer: string;
  category: string;
  displayOrder: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ContactRequest {
  name: string;
  email: string;
  phone: string | null;
  inquiryType: InquiryType;
  message: string;
}

export interface ContactCreatedResponse {
  id: string;
  message: string;
}

export interface ValidationProblem {
  title?: string;
  errors?: Record<string, string[]>;
  referenceId?: string;
}

export type PublicStockStatus = "InStock" | "LowStock" | "SoldOut";
export type ProductModifierType = "Choice" | "Color" | "ShortText" | "Number";
export type ProductMediaRole = "Gallery" | "MockupBase" | "LogoOverlay";

export interface StoreCategory {
  name: string;
  slug: string;
  productCount: number;
}

export interface StoreProductSummary {
  name: string;
  slug: string;
  shortDescription: string | null;
  categoryName: string | null;
  categorySlug: string | null;
  minimumPriceMinor: number;
  maximumPriceMinor: number;
  currency: string;
  isFeatured: boolean;
  primaryImageUrl: string | null;
  primaryImageAltText: string | null;
  availability: PublicStockStatus;
}

export interface StoreCatalog {
  items: StoreProductSummary[];
  categories: StoreCategory[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface StoreProductMedia {
  mediaAssetId: string;
  publicUrl: string;
  altText: string;
  role: ProductMediaRole;
  displayOrder: number;
}

export interface StoreProductOptionValue {
  id: string;
  name: string;
  slug: string;
  colorHex: string | null;
  swatchImageUrl: string | null;
  displayOrder: number;
}

export interface StoreProductOption {
  id: string;
  name: string;
  displayOrder: number;
  values: StoreProductOptionValue[];
}

export interface StoreProductVariant {
  id: string;
  name: string;
  priceMinor: number;
  availability: PublicStockStatus;
  optionValueIds: string[];
}

export interface StoreProductModifierValue {
  id: string;
  name: string;
  priceAdjustmentMinor: number;
  colorHex: string | null;
  overlayImageUrl: string | null;
  displayOrder: number;
}

export interface StoreProductModifierGroup {
  id: string;
  name: string;
  type: ProductModifierType;
  isRequired: boolean;
  minimumSelections: number;
  maximumSelections: number;
  displayOrder: number;
  values: StoreProductModifierValue[];
}

export interface StoreProductVisualizerLayer {
  mediaAssetId: string;
  publicUrl: string;
  productOptionValueId: string | null;
  productModifierValueId: string | null;
  xPercent: number;
  yPercent: number;
  widthPercent: number;
  heightPercent: number;
  zIndex: number;
  blendMode: "normal" | "multiply" | "screen" | "overlay";
}

export interface StoreProduct {
  name: string;
  slug: string;
  shortDescription: string | null;
  description: string | null;
  categoryName: string | null;
  basePriceMinor: number;
  currency: string;
  allowsSpecialRequests: boolean;
  availability: PublicStockStatus;
  media: StoreProductMedia[];
  options: StoreProductOption[];
  variants: StoreProductVariant[];
  modifierGroups: StoreProductModifierGroup[];
  visualizerLayers: StoreProductVisualizerLayer[];
}

export type StoreOrderStatus = "AwaitingPayment" | "Paid" | "NeedsReview" | "ReadyForProduction" |
  "InProduction" | "NeedsCustomerInfo" | "ReadyForHandoff" | "Completed" | "Canceled" | "Refunded";
export type StorePaymentStatus = "Pending" | "Authorized" | "Paid" | "Refunding" | "Refunded" | "PartiallyRefunded" | "Failed" | "Canceled";

export interface StoreCheckoutRequest {
  checkoutAttemptId: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  athleteTeamNote: string | null;
  confirmsAdultBuyer: boolean;
  acceptsStorePolicy: boolean;
  lines: Array<{
    productVariantId: string;
    quantity: number;
    modifierValueIds: string[];
    customInputs: Array<{ modifierGroupId: string; value: string }>;
  }>;
}

export interface StoreCheckoutResult {
  orderReference: string;
  checkoutUrl: string;
  reservationExpiresAtUtc: string;
  subtotalMinor: number;
  taxMinor: number;
  totalMinor: number;
  currency: string;
}

export interface StoreOrderStatusResult {
  orderReference: string;
  status: StoreOrderStatus;
  paymentStatus: StorePaymentStatus;
  subtotalMinor: number;
  taxMinor: number;
  totalMinor: number;
  currency: string;
  hasPersonalization: boolean;
  customerCancellationExpiresAtUtc: string | null;
  canCustomerCancel: boolean;
  items: Array<{
    productName: string;
    variantName: string;
    quantity: number;
    unitPriceMinor: number;
    lineTotalMinor: number;
    configuration: Array<{ label: string; value: string }>;
  }>;
  timeline: Array<{ status: StoreOrderStatus; label: string; createdAtUtc: string }>;
}

export interface StoreCheckoutReturnStatus {
  orderReference: string;
  paymentStatus: StorePaymentStatus;
  status: StoreOrderStatus;
  isFinal: boolean;
  message: string;
}

export interface GalleryAlbumListItem {
  title: string; slug: string; description: string; coverImageUrl: string | null;
  coverAltText: string | null; eventDateUtc: string | null; imageCount: number;
}
export interface GalleryImage {
  publicUrl: string; altText: string; caption: string | null; width: number; height: number; displayOrder: number;
}
export interface GalleryAlbum {
  title: string; slug: string; description: string; eventDateUtc: string | null; images: GalleryImage[];
}

export interface AllAmericanYearListItem {
  year: number; slug: string; title: string; summary: string; athleteCount: number; medalCount: number;
  heroImageUrl: string | null; heroAltText: string | null; imageCount: number;
}
export interface AllAmericanImage {
  publicUrl: string; altText: string; caption: string | null; width: number; height: number; displayOrder: number;
}
export interface AllAmericanResult {
  eventName: string; division: string | null; placement: number | null; isRelay: boolean; displayOrder: number;
}
export interface AllAmericanRecipient {
  firstName: string; lastName: string; photoUrl: string | null; photoAltText: string | null;
  displayOrder: number; results: AllAmericanResult[];
}
export interface AllAmericanYear extends AllAmericanYearListItem {
  detailsComplete: boolean; images: AllAmericanImage[]; recipients: AllAmericanRecipient[];
}
