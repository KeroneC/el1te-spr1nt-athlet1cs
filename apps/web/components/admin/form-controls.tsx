import Link from "next/link";
import { ArrowLeft, LoaderCircle, Save } from "lucide-react";
import type { FieldErrors } from "@/lib/admin/validation";
import { MediaPicker } from "./media-picker";

export function FormNotice({ message, success = false }: { message: string | null; success?: boolean }) {
  return message ? <div role={success ? "status" : "alert"} className={`border-l-4 px-4 py-3 text-sm font-semibold ${success ? "border-track-field bg-emerald-50 text-emerald-900" : "border-track-red bg-red-50 text-red-900"}`}>{message}</div> : null;
}
export function FormSection({ title, description, children }: { title: string; description?: string; children: React.ReactNode }) {
  return <section className="border border-slate-200 bg-white p-5 sm:p-6"><h2 className="text-lg font-black text-track-ink">{title}</h2>{description && <p className="mt-1 text-sm leading-6 text-slate-600">{description}</p>}<div className="mt-5 grid gap-5 sm:grid-cols-2">{children}</div></section>;
}
export function Field({ label, name, error, hint, className = "", ...props }: { label: string; name: string; error?: string; hint?: string; className?: string } & React.InputHTMLAttributes<HTMLInputElement>) {
  if (name === "imageUrl" || name === "logoUrl") {
    return <div className={className}><MediaPicker name={name} label={label.replace(" URL", "")} defaultValue={String(props.defaultValue ?? "")} error={error} /></div>;
  }
  const errorId = `${name}-error`; const hintId = `${name}-hint`;
  return <div className={className}><label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label><input id={name} name={name} {...props} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : hint ? hintId : undefined} className="min-h-11 w-full border border-slate-300 px-3 outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20" />{hint && !error && <p id={hintId} className="mt-1 text-xs leading-5 text-slate-500">{hint}</p>}{error && <p id={errorId} className="mt-1 text-sm font-semibold text-red-700">{error}</p>}</div>;
}
export function TextArea({ label, name, error, className = "", ...props }: { label: string; name: string; error?: string; className?: string } & React.TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const id = `${name}-error`; return <div className={className}><label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label><textarea id={name} name={name} {...props} aria-invalid={Boolean(error)} aria-describedby={error ? id : undefined} className="w-full border border-slate-300 px-3 py-2 outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20" />{error && <p id={id} className="mt-1 text-sm font-semibold text-red-700">{error}</p>}</div>;
}
export function SelectField({ label, name, error, options, ...props }: { label: string; name: string; error?: string; options: readonly (readonly [string, string])[] } & React.SelectHTMLAttributes<HTMLSelectElement>) {
  const id = `${name}-error`; return <div><label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label><select id={name} name={name} {...props} aria-invalid={Boolean(error)} aria-describedby={error ? id : undefined} className="min-h-11 w-full border border-slate-300 bg-white px-3 outline-none focus:border-track-red focus:ring-2 focus:ring-track-red/20">{options.map(([value, text]) => <option key={value} value={value}>{text}</option>)}</select>{error && <p id={id} className="mt-1 text-sm font-semibold text-red-700">{error}</p>}</div>;
}
export function Checkbox({ name, label, defaultChecked, hint }: { name: string; label: string; defaultChecked?: boolean; hint?: string }) {
  return <label className="flex min-h-11 items-start gap-3 text-sm text-track-ink"><input type="checkbox" name={name} defaultChecked={defaultChecked} className="mt-0.5 h-5 w-5 shrink-0 accent-track-red" /><span><strong>{label}</strong>{hint && <span className="mt-1 block font-normal leading-5 text-slate-500">{hint}</span>}</span></label>;
}
export function FormActions({ backHref, backLabel, submitting, editing }: { backHref: string; backLabel: string; submitting: boolean; editing: boolean }) {
  return <div className="flex flex-wrap items-center justify-between gap-3"><Link href={backHref} className="inline-flex min-h-10 items-center gap-2 border border-slate-300 px-4 text-sm font-bold text-track-ink hover:border-track-red"><ArrowLeft size={17} />{backLabel}</Link><button type="submit" disabled={submitting} className="inline-flex min-h-11 items-center gap-2 bg-track-red px-5 text-sm font-bold text-white hover:bg-red-800 disabled:opacity-60">{submitting ? <LoaderCircle size={18} className="animate-spin" /> : <Save size={18} />}{submitting ? "Saving..." : editing ? "Save changes" : "Create"}</button></div>;
}
export function fieldError(errors: FieldErrors, name: string) { return errors[name]?.[0] ?? errors[name[0].toUpperCase() + name.slice(1)]?.[0]; }
export function normalizeErrors(errors: FieldErrors): FieldErrors { return Object.fromEntries(Object.entries(errors).map(([key, value]) => [key[0].toLowerCase() + key.slice(1), value])); }
