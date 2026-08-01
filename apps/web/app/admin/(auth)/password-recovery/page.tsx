import { PasswordRecoveryForm } from "@/components/admin/password-recovery-form";

export default function PasswordRecoveryPage() {
  return <main className="flex min-h-screen items-center justify-center bg-track-ink px-5 py-10"><section className="w-full max-w-md border-t-4 border-track-red bg-white p-7 shadow-2xl sm:p-9"><p className="text-sm font-black uppercase text-track-red">El1te Admin</p><h1 className="mt-2 text-3xl font-black">Recover access</h1><p className="mt-3 mb-7 text-sm leading-6 text-slate-600">Request a one-time link to securely choose a new Admin password.</p><PasswordRecoveryForm /></section></main>;
}
