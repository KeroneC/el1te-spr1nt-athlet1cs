import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Admin | El1te Spr1nt Athlet1cs",
  robots: { index: false, follow: false, nocache: true }
};
export default function AdminRootLayout({ children }: { children: React.ReactNode }) { return children; }
