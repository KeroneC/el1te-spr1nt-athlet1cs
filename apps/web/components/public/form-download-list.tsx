import { Download, ExternalLink } from "lucide-react";
import type { FORM_DOWNLOADS } from "@/lib/public/site";

export function FormDownloadList({ forms }: { forms: readonly (typeof FORM_DOWNLOADS[number])[] }) {
  return <div className="download-list">
    {forms.map(({ name, description, audience, href, fileType, actionLabel, isDownload, Icon }) => (
      <article className="download-row" key={name}>
        <div className="download-icon"><Icon aria-hidden="true" /></div>
        <div>
          <h3>{name}</h3>
          <p>{description}</p>
          <p className="form-audience"><strong>For:</strong> {audience}</p>
          <span>{fileType}</span>
        </div>
        <a className="button button-secondary" href={href} download={isDownload || undefined}
          target={isDownload ? undefined : "_blank"} rel={isDownload ? undefined : "noreferrer"}
          aria-label={isDownload ? `Download ${name}` : `${actionLabel}: ${name} (opens in a new tab)`}>
          {actionLabel}
          {isDownload ? <Download size={17} aria-hidden="true" /> : <ExternalLink size={17} aria-hidden="true" />}
        </a>
      </article>
    ))}
  </div>;
}
