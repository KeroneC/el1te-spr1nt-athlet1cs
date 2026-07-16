import {
  Award,
  ClipboardList,
  FileCheck2,
  FileHeart,
  FileText,
  ShieldCheck
} from "lucide-react";
import type { Sponsor } from "./types";

export const BRAND = {
  name: "El1te Spr1nt Athlet1cs",
  slogan: "Greatness begins here; hang on for the ride!",
  logoWhite: "/brand/el1te-logo-white.png",
  logoMark: "/brand/el1te-mark-black.png",
  favicon: "/favicon.png",
  shopUrl: "https://el1te-spr1nt-athlet1cs.square.site/",
  facebookUrl: "https://www.facebook.com/el1tespr1ntathlet1cs/",
  instagramUrl: "https://www.instagram.com/el1tespr1ntathlet1cs/",
  contactEmail: "el1tespr1nt.athlet1cs@gmail.com",
  contactPhone: "(412) 654-6798"
} as const;

export type PublicNavLink = { href: string; label: string };
export type PublicNavGroup = { label: string; links: readonly PublicNavLink[] };

export const HEADER_NAV_ITEMS = [
  {
    label: "Club",
    links: [
      { href: "/about", label: "About" },
      { href: "/programs", label: "Programs" },
      { href: "/coaches", label: "Coaches" },
      { href: "/team", label: "Team" },
      { href: "/hall-of-fame", label: "Hall of Fame" }
    ]
  },
  { href: "/events", label: "Events" },
  { href: "/gallery", label: "Gallery" },
  { href: "/sponsors", label: "Sponsors" },
  {
    label: "Resources",
    links: [
      { href: "/forms", label: "Forms" },
      { href: "/scholarship", label: "Scholarship" },
      { href: "/faqs", label: "FAQs" }
    ]
  },
  { href: "/contact", label: "Contact" }
] as const satisfies readonly (PublicNavLink | PublicNavGroup)[];

export const PRIMARY_NAV_LINKS: readonly PublicNavLink[] = HEADER_NAV_ITEMS.flatMap<PublicNavLink>((item) =>
  "links" in item ? item.links : [item]
);

export function sponsorTierClass(tier: string) {
  const normalizedTier = tier.toLowerCase();
  return ["platinum", "gold", "silver", "bronze", "community", "other"].includes(normalizedTier)
    ? `sponsor-tier-${normalizedTier}`
    : "sponsor-tier-other";
}

export function prioritizeSponsorPreviews(sponsors: Sponsor[], limit = 5) {
  const bucket = (sponsor: Sponsor) => sponsor.tier === "Gold"
    ? (sponsor.logoUrl ? 0 : 1)
    : (sponsor.logoUrl ? 2 : 3);

  return [...sponsors]
    .sort((a, b) => bucket(a) - bucket(b) || a.displayOrder - b.displayOrder)
    .slice(0, limit);
}

export const FOOTER_LINK_GROUPS = [
  {
    title: "Explore",
    links: [
      { href: "/about", label: "Mission" },
      { href: "/programs", label: "Programs" },
      { href: "/coaches", label: "Coaches" },
      { href: "/team", label: "Team" },
      { href: "/gallery", label: "Gallery" }
    ]
  },
  {
    title: "Families",
    links: [
      { href: "/registration", label: "Registration" },
      { href: "/forms", label: "Forms" },
      { href: "/scholarship", label: "Scholarship" },
      { href: "/hall-of-fame", label: "Hall of Fame" },
      { href: "/faqs", label: "FAQs" }
    ]
  }
] as const;

export const FORM_DOWNLOADS = [
  {
    name: "Athlete Registration Form",
    description: "Core family and athlete paperwork for club review before participation.",
    audience: "Families interested in joining",
    href: "/forms/athlete-registration-form.pdf",
    fileType: "PDF",
    Icon: ClipboardList
  },
  {
    name: "Liability Waiver",
    description: "Required waiver for participation in club activities.",
    audience: "Parent or guardian",
    href: "/forms/liability-waiver.pdf",
    fileType: "PDF",
    Icon: ShieldCheck
  },
  {
    name: "Photo Consent Form",
    description: "Required media consent form for athlete photos and team coverage.",
    audience: "Parent or guardian",
    href: "/forms/photo-consent-form.pdf",
    fileType: "PDF",
    Icon: FileCheck2
  },
  {
    name: "BVN Memorial Scholarship Form",
    description: "Scholarship application and criteria for eligible team members.",
    audience: "Eligible junior or senior athletes",
    href: "/forms/scholarship-form.pdf",
    fileType: "PDF",
    Icon: FileHeart
  }
] as const;

export const TEAM_VALUES = [
  "Competitive growth",
  "Sportsmanship",
  "Discipline",
  "Family support"
] as const;

export const HALL_OF_FAME_INDUCTEES = [
  {
    slug: "dani-prunzik",
    name: "Dani Prunzik",
    affiliation: "Penn State University",
    imageSrc: "/images/hall-of-fame/dani-prunzik.jpeg",
    imageAlt: "Dani Prunzik holding an American flag in her Penn State track uniform",
    summary:
      "Upper St. Clair High School class of 2023 graduate, Penn State student, and talented sprinter with a 60m indoor PR of 7.57."
  },
  {
    slug: "kaitlyn-eger",
    name: "Kaitlyn Eger",
    affiliation: "Youngstown State University",
    imageSrc: "/images/hall-of-fame/kaitlyn-eger.jpg",
    imageAlt: "Kaitlyn Eger posing with a pole vault pole in her Youngstown State uniform",
    summary:
      "Youngstown State University student-athlete studying Exercise Science (Pre-PT). A multi-time top-5 Horizon League finisher and Meet MVP who helped lead back-to-back conference championships in 2024 and 2025."
  }
] as const;

export const QUICK_STEPS = [
  {
    title: "Review the packet",
    body: "Start with the registration form and required waiver and consent documents."
  },
  {
    title: "Ask questions early",
    body: "Contact the club before submitting paperwork if you need help with eligibility, season timing, or form requirements."
  },
  {
    title: "Submit for review",
    body: "Completed paperwork is reviewed by club staff. This site does not collect sensitive athlete information online."
  }
] as const;

export const FileTextIcon = FileText;
export const AwardIcon = Award;
