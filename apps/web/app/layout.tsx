import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "El1te Spr1nt Athlet1cs",
  description: "A secure platform foundation for a nonprofit youth track club."
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
