"use client";
import { AlertTriangle, RotateCw } from "lucide-react";
import { SupportReference } from "@/components/shared/support-reference";
import { supportReferenceFromDigest } from "@/lib/observability/support-reference";
export default function AnnouncementsError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) { return <div className="border-l-4 border-track-red bg-white p-8"><AlertTriangle size={28} className="text-track-red" /><h1 className="mt-3 text-2xl font-black text-track-ink">Announcements unavailable</h1><p className="mt-2 text-sm text-slate-600">The service could not load announcement data. No private error details were exposed.</p><SupportReference referenceId={supportReferenceFromDigest(error.digest)}/><button onClick={reset} className="mt-5 inline-flex min-h-10 items-center gap-2 bg-track-ink px-4 text-sm font-bold text-white"><RotateCw size={17} />Try again</button></div>; }
