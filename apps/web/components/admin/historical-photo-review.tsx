"use client";

import { useId, useState } from "react";
import { useRouter } from "next/navigation";
import { redirectForAdminResponse } from "@/lib/admin/client-response";
import { historicalReviews, type HistoricalReview } from "@/lib/admin/historical-photos";

export function HistoricalPhotoReview({ id, current }: { id: string; current: HistoricalReview }) {
  const labelId = useId();
  const router = useRouter();
  const [review, setReview] = useState(current);
  const [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState("");
  const [saved, setSaved] = useState(false);
  const endpoint = `/api/admin/historical-photos/${id}`;

  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (busy || review === current || (review === "ApprovedForSocial" && !confirmed)) return;
    setBusy(true);
    setNotice("");
    setSaved(false);
    try {
      const response = await fetch(`${endpoint}/review`, {
        method: "PUT", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ review, permissionConfirmed: confirmed })
      });
      if (redirectForAdminResponse(response)) return;
      if (!response.ok) {
        setNotice(response.status === 409 ? "Another reviewer updated this photo. Refresh and try again." : "The review could not be saved. Try again.");
        return;
      }
      setSaved(true);
      setNotice("Review saved.");
      setConfirmed(false);
      router.refresh();
    } catch {
      setNotice("The review could not be saved. Try again.");
    } finally {
      setBusy(false);
    }
  }

  return <form onSubmit={save} className="mt-3 space-y-2">
    <label htmlFor={labelId} className="block text-sm font-bold">Review status</label>
    <select id={labelId} value={review} disabled={busy} onChange={event => {
      setReview(event.target.value as HistoricalReview);
      setConfirmed(false);
      setNotice("");
    }} className="min-h-11 w-full border border-slate-300 bg-white px-2 text-sm">
      {Object.entries(historicalReviews).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
    </select>
    {review === "ApprovedForSocial" && review !== current && <label className="flex items-start gap-2 text-sm">
      <input type="checkbox" className="mt-1" checked={confirmed} required
        onChange={event => setConfirmed(event.target.checked)} />
      <span>I confirmed the club has permission to use this photo on social media.</span>
    </label>}
    <div className="flex flex-wrap items-center gap-2">
      <button className="min-h-11 border border-slate-400 px-3 text-sm font-bold disabled:opacity-50"
        disabled={busy || review === current || (review === "ApprovedForSocial" && !confirmed)}>
        {busy ? "Saving…" : "Save review"}
      </button>
      {current === "ApprovedForSocial" && <a href={`${endpoint}/download`} className="inline-flex min-h-11 items-center px-1 text-sm font-bold text-red-800 underline">Download clean copy</a>}
    </div>
    {notice && <p role={saved ? "status" : "alert"} className="text-sm">{notice}</p>}
  </form>;
}
