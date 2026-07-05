import {
  Award,
  ClipboardList,
  FileCheck2,
  FileHeart,
  FileText,
  ShieldCheck
} from "lucide-react";

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

export const PRIMARY_NAV_LINKS = [
  { href: "/", label: "Home" },
  { href: "/about", label: "About" },
  { href: "/programs", label: "Programs" },
  { href: "/events", label: "Events" },
  { href: "/gallery", label: "Gallery" },
  { href: "/registration", label: "Registration" },
  { href: "/sponsors", label: "Sponsors" },
  { href: "/contact", label: "Contact" }
] as const;

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
    name: "Dani Prunzik",
    summary:
      "Upper St. Clair High School class of 2023 graduate, Penn State student, and talented sprinter with a 60m indoor PR of 7.57."
  },
  {
    name: "Kaitlyn Eger",
    summary:
      "Youngstown State University student-athlete studying Exercise Science. A multi-time top-5 Horizon League finisher and Meet MVP who helped lead back-to-back conference championships."
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
