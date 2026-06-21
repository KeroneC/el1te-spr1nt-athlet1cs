import Image from "next/image";
import { redirect } from "next/navigation";
import { cookies } from "next/headers";
import { LoginForm } from "@/components/admin/login-form";
import { adminApiFetch, ADMIN_SESSION_COOKIE } from "@/lib/admin/server-api";
import type { CurrentUser } from "@/lib/admin/types";
import { isAdminRole } from "@/lib/admin/validation";

export const dynamic = "force-dynamic";

export default async function AdminLoginPage({ searchParams }: { searchParams: Promise<{ reason?: string }> }) {
  let currentUser: CurrentUser | null = null;
  if ((await cookies()).has(ADMIN_SESSION_COOKIE)) {
    try {
      currentUser = await adminApiFetch<CurrentUser>("/api/auth/me");
    } catch { /* The form can replace an invalid session. */ }
  }
  if (currentUser?.isActive && isAdminRole(currentUser.role)) redirect("/admin");

  const { reason } = await searchParams;
  return (
    <main className="relative min-h-screen bg-track-ink">
      <Image src="/images/track-hero.png" alt="" fill priority sizes="100vw" className="object-cover opacity-35" />
      <div className="absolute inset-0 bg-black/55" />
      <div className="relative mx-auto flex min-h-screen w-full max-w-7xl items-center justify-end px-5 py-10 sm:px-8 lg:px-10">
        <section className="w-full max-w-md border-t-4 border-track-red bg-white p-7 shadow-2xl sm:p-9" aria-labelledby="login-title">
          <p className="text-sm font-black uppercase text-track-red">El1te Spr1nt Athlet1cs</p>
          <h1 id="login-title" className="mt-2 text-3xl font-black text-track-ink">Admin sign in</h1>
          <p className="mt-3 text-sm leading-6 text-slate-600">Manage club announcements through the secure administration workspace.</p>
          {reason === "expired" && <p role="status" className="mt-5 border-l-4 border-amber-500 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900">Your session expired. Please sign in again.</p>}
          <div className="mt-7"><LoginForm /></div>
        </section>
      </div>
    </main>
  );
}
