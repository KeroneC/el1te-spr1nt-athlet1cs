"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { CheckCircle2, LoaderCircle } from "lucide-react";
import type { StoreCheckoutReturnStatus } from "@/lib/public/types";
import { writeStoreCart } from "@/lib/store/cart";

export function StoreOrderConfirmation() {
  const [result, setResult] = useState<StoreCheckoutReturnStatus | null>(null);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    let active = true;
    let timer: ReturnType<typeof setTimeout>;
    const check = async () => {
      const response = await fetch("/api/public/store/orders/return-status", { method: "POST" });
      if (!active) return;
      if (!response.ok) { setFailed(true); return; }
      const status = await response.json() as StoreCheckoutReturnStatus;
      setResult(status);
      if (status.paymentStatus === "Paid") writeStoreCart([]);
      if (!status.isFinal) timer = setTimeout(check, 2500);
    };
    void check();
    return () => { active = false; clearTimeout(timer); };
  }, []);
  return <main className="store-order-result"><div className="site-container"><div className="store-order-result-card">
    {result?.paymentStatus === "Paid" ? <CheckCircle2 className="store-result-success" aria-hidden="true"/> : <LoaderCircle className={result?.isFinal ? "store-result-failed" : "store-result-spinner"} aria-hidden="true"/>}
    <p className="eyebrow">Square checkout return</p><h1>{result?.paymentStatus === "Paid" ? "Payment confirmed" : failed ? "Confirmation unavailable" : "Confirming your payment"}</h1>
    <p>{failed ? "We could not find this checkout return. If Square charged you, check your email for the secure order link or contact the club." : result?.message ?? "Please keep this page open while Square confirms the payment."}</p>
    {result && <p className="store-order-reference">Order reference: <strong>{result.orderReference}</strong></p>}
    <div className="store-result-actions"><Link className="button button-primary" href="/shop">Return to shop</Link><Link className="button button-secondary" href="/contact">Contact the club</Link></div>
  </div></div></main>;
}
