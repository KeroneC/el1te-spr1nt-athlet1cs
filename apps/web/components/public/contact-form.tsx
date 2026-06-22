"use client";

import { Send } from "lucide-react";
import { FormEvent, useState } from "react";
import type { ContactRequest, ValidationProblem } from "@/lib/public/types";
import { labelEnum } from "@/lib/public/format";
import { INQUIRY_TYPES, validateContact } from "@/lib/public/validation";

const initial: ContactRequest = { name: "", email: "", phone: null, inquiryType: "General", message: "" };

export function ContactForm() {
  const [values, setValues] = useState<ContactRequest>(initial);
  const [errors, setErrors] = useState<Partial<Record<keyof ContactRequest, string>>>({});
  const [status, setStatus] = useState<"idle" | "submitting" | "success" | "error">("idle");
  const [message, setMessage] = useState("");

  function update<K extends keyof ContactRequest>(key: K, value: ContactRequest[K]) {
    setValues((current) => ({ ...current, [key]: value }));
    setErrors((current) => ({ ...current, [key]: undefined }));
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (status === "submitting") return;
    const clientErrors = validateContact(values);
    if (Object.keys(clientErrors).length) {
      setErrors(clientErrors);
      setStatus("error");
      setMessage("Please correct the highlighted fields.");
      return;
    }

    setStatus("submitting");
    setMessage("");
    try {
      const response = await fetch("/api/public/contact", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ...values, phone: values.phone?.trim() || null })
      });
      const payload = (await response.json()) as ValidationProblem & { message?: string };
      if (!response.ok) {
        const fieldErrors: Partial<Record<keyof ContactRequest, string>> = {};
        Object.entries(payload.errors ?? {}).forEach(([key, entries]) => {
          const normalized = `${key.charAt(0).toLowerCase()}${key.slice(1)}` as keyof ContactRequest;
          if (normalized in values) fieldErrors[normalized] = entries[0];
        });
        setErrors(fieldErrors);
        setStatus("error");
        setMessage(payload.message ?? "Your message could not be sent. Please try again.");
        return;
      }
      setValues(initial);
      setErrors({});
      setStatus("success");
      setMessage(payload.message ?? "Your message has been received.");
    } catch {
      setStatus("error");
      setMessage("Your message could not be sent. Please check your connection and try again.");
    }
  }

  return <form className="contact-form" onSubmit={submit} noValidate>
    <div className="form-grid">
      <Field label="Name" name="name" error={errors.name}><input id="name" value={values.name} onChange={(event)=>update("name",event.target.value)} aria-invalid={Boolean(errors.name)} aria-describedby={errors.name?"name-error":undefined} autoComplete="name" /></Field>
      <Field label="Email" name="email" error={errors.email}><input id="email" type="email" value={values.email} onChange={(event)=>update("email",event.target.value)} aria-invalid={Boolean(errors.email)} aria-describedby={errors.email?"email-error":undefined} autoComplete="email" /></Field>
      <Field label="Phone (optional)" name="phone" error={errors.phone}><input id="phone" type="tel" value={values.phone??""} onChange={(event)=>update("phone",event.target.value)} aria-invalid={Boolean(errors.phone)} aria-describedby={errors.phone?"phone-error":undefined} autoComplete="tel" /></Field>
      <Field label="Inquiry type" name="inquiryType" error={errors.inquiryType}><select id="inquiryType" value={values.inquiryType} onChange={(event)=>update("inquiryType",event.target.value as ContactRequest["inquiryType"])}>{INQUIRY_TYPES.map(type=><option key={type} value={type}>{labelEnum(type)}</option>)}</select></Field>
    </div>
    <Field label="Message" name="message" error={errors.message}><textarea id="message" rows={7} maxLength={4000} value={values.message} onChange={(event)=>update("message",event.target.value)} aria-invalid={Boolean(errors.message)} aria-describedby={errors.message?"message-error":undefined} /></Field>
    {message && <p className={`form-status ${status}`} role="status">{message}</p>}
    <button className="button button-primary" type="submit" disabled={status==="submitting"}>{status==="submitting"?"Sending...":<>Send message<Send size={17} aria-hidden="true"/></>}</button>
  </form>;
}

function Field({label,name,error,children}:{label:string;name:keyof ContactRequest;error?:string;children:React.ReactNode}) { return <div className={name==="message"?"form-field full":"form-field"}><label htmlFor={name}>{label}</label>{children}{error&&<p className="field-error" id={`${name}-error`}>{error}</p>}</div>; }
