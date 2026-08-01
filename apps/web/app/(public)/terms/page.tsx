import type { Metadata } from "next";
import { PolicyPage } from "@/components/public/policy-page";
import { CONTENT_KEYS } from "@/lib/public/content";
export const metadata: Metadata = { title: "Website Terms" };
export default function Page() { return <PolicyPage contentKey={CONTENT_KEYS.termsPolicy} fallbackTitle="Website Terms" />; }
