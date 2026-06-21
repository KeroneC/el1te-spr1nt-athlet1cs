"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, EyeOff, LoaderCircle, LogIn } from "lucide-react";
import { validateLoginInput, type FieldErrors } from "@/lib/admin/validation";

export function LoginForm() {
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState<string | null>(null);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting) return;
    const data = new FormData(event.currentTarget);
    const input = { email: String(data.get("email") ?? ""), password: String(data.get("password") ?? "") };
    const clientErrors = validateLoginInput(input);
    setErrors(clientErrors);
    setMessage(null);
    if (Object.keys(clientErrors).length) return;

    setSubmitting(true);
    try {
      const response = await fetch("/api/admin-session/login", {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(input)
      });
      const result = await response.json() as { message?: string; errors?: FieldErrors };
      if (!response.ok) {
        setErrors(result.errors ?? {});
        setMessage(result.message ?? "Sign in could not be completed.");
        return;
      }
      router.replace("/admin");
      router.refresh();
    } catch {
      setMessage("The admin service is unavailable. Please try again.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={submit} noValidate className="space-y-5" aria-describedby={message ? "login-message" : undefined}>
      {message && <div id="login-message" role="alert" className="border-l-4 border-track-red bg-red-50 px-4 py-3 text-sm font-semibold text-red-900">{message}</div>}
      <Field label="Email" name="email" type="email" autoComplete="email" error={errors.email?.[0]} />
      <div>
        <label htmlFor="password" className="mb-2 block text-sm font-bold text-track-ink">Password</label>
        <div className="relative">
          <input id="password" name="password" type={showPassword ? "text" : "password"} autoComplete="current-password" aria-invalid={Boolean(errors.password)} aria-describedby={errors.password ? "password-error" : undefined} className="min-h-11 w-full border border-slate-300 bg-white px-3 pr-12 text-base outline-none transition focus:border-track-red focus:ring-2 focus:ring-track-red/20" />
          <button type="button" onClick={() => setShowPassword((value) => !value)} className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-slate-600 hover:text-track-red focus:outline-none focus:ring-2 focus:ring-inset focus:ring-track-red" aria-label={showPassword ? "Hide password" : "Show password"}>
            {showPassword ? <EyeOff size={19} /> : <Eye size={19} />}
          </button>
        </div>
        {errors.password && <p id="password-error" className="mt-1 text-sm font-semibold text-red-700">{errors.password[0]}</p>}
      </div>
      <button type="submit" disabled={submitting} className="inline-flex min-h-11 w-full items-center justify-center gap-2 bg-track-red px-5 text-sm font-bold text-white transition hover:bg-red-800 focus:outline-none focus:ring-2 focus:ring-track-red focus:ring-offset-2 disabled:cursor-wait disabled:opacity-65">
        {submitting ? <LoaderCircle size={18} className="animate-spin" /> : <LogIn size={18} />}
        {submitting ? "Signing in..." : "Sign in"}
      </button>
    </form>
  );
}

function Field({ label, name, error, ...props }: { label: string; name: string; error?: string } & React.InputHTMLAttributes<HTMLInputElement>) {
  const errorId = `${name}-error`;
  return <div><label htmlFor={name} className="mb-2 block text-sm font-bold text-track-ink">{label}</label><input id={name} name={name} {...props} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} className="min-h-11 w-full border border-slate-300 bg-white px-3 text-base outline-none transition focus:border-track-red focus:ring-2 focus:ring-track-red/20" />{error && <p id={errorId} className="mt-1 text-sm font-semibold text-red-700">{error}</p>}</div>;
}
