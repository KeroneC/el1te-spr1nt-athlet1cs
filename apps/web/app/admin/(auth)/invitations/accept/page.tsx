import Image from "next/image";
import { InvitationAcceptance } from "@/components/admin/invitation-acceptance";

export default function AcceptAdminInvitationPage() {
  return <main className="relative min-h-screen bg-track-ink"><Image src="/images/track-hero.png" alt="" fill priority sizes="100vw" className="object-cover opacity-30" /><div className="absolute inset-0 bg-black/60" /><div className="relative mx-auto flex min-h-screen w-full max-w-7xl items-center justify-end px-5 py-10 sm:px-8 lg:px-10"><section className="w-full max-w-lg border-t-4 border-track-red bg-white p-7 shadow-2xl sm:p-9" aria-labelledby="invite-title"><p className="text-sm font-black uppercase text-track-red">El1te Spr1nt Athlet1cs</p><h1 id="invite-title" className="mt-2 text-3xl font-black text-track-ink">Accept Admin invitation</h1><p className="mt-3 text-sm leading-6 text-slate-600">Confirm your invitation and create the password for your administrative account.</p><div className="mt-7"><InvitationAcceptance /></div></section></div></main>;
}
