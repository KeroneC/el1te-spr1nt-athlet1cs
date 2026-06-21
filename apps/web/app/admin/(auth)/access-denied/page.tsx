import Link from "next/link";
import { ShieldX } from "lucide-react";

export default function AccessDeniedPage() {
  return <main className="flex min-h-screen items-center justify-center bg-slate-100 px-5"><section className="w-full max-w-lg border-t-4 border-track-red bg-white p-8 text-center shadow-sm"><ShieldX size={38} className="mx-auto text-track-red" /><h1 className="mt-4 text-3xl font-black text-track-ink">Access denied</h1><p className="mt-3 text-slate-600">This account does not have active Admin or SuperAdmin access.</p><div className="mt-6 flex justify-center gap-3"><Link href="/" className="border border-slate-300 px-4 py-2 text-sm font-bold text-track-ink hover:border-track-red">Public site</Link><a href="/api/admin-session/logout" className="bg-track-red px-4 py-2 text-sm font-bold text-white hover:bg-red-800">Use another account</a></div></section></main>;
}
