import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  metadataBase: new URL(process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000"),
  title: {
    default: "El1te Spr1nt Athlet1cs",
    template: "%s | El1te Spr1nt Athlet1cs"
  },
  description: "Youth track and field training, competition, and community.",
  icons: {
    icon: "/favicon.png",
    apple: "/favicon.png"
  },
  openGraph: {
    title: "El1te Spr1nt Athlet1cs",
    description: "Greatness begins here; hang on for the ride!",
    siteName: "El1te Spr1nt Athlet1cs",
    images: [{ url: "/brand/el1te-logo-white.png", width: 1024, height: 1024 }]
  }
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="antialiased">{children}</body>
    </html>
  );
}
