import type { ReactNode } from "react";

type PolicyBlock =
  | { kind: "heading"; text: string }
  | { kind: "paragraph"; text: string }
  | { kind: "list"; items: string[] };

const linkPattern = /\[([^\]]+)]\(([^)]+)\)/g;

export function parsePolicyBody(body: string): PolicyBlock[] {
  const lines = body.replace(/\r\n/g, "\n").split("\n");
  const blocks: PolicyBlock[] = [];
  let paragraph: string[] = [];
  let list: string[] = [];

  const flushParagraph = () => {
    if (paragraph.length) blocks.push({ kind: "paragraph", text: paragraph.join(" ").trim() });
    paragraph = [];
  };
  const flushList = () => {
    if (list.length) blocks.push({ kind: "list", items: list });
    list = [];
  };

  for (const sourceLine of lines) {
    const line = sourceLine.trim();
    if (!line) {
      flushParagraph();
      flushList();
    } else if (line.startsWith("## ")) {
      flushParagraph();
      flushList();
      blocks.push({ kind: "heading", text: line.slice(3).trim() });
    } else if (line.startsWith("- ")) {
      flushParagraph();
      list.push(line.slice(2).trim());
    } else {
      flushList();
      paragraph.push(line);
    }
  }
  flushParagraph();
  flushList();
  return blocks;
}

export function isApprovedPolicyHref(href: string): boolean {
  return href.startsWith("https://") || href.startsWith("mailto:") || href.startsWith("/");
}

function inlineContent(text: string): ReactNode[] {
  const content: ReactNode[] = [];
  let cursor = 0;
  for (const match of text.matchAll(linkPattern)) {
    const index = match.index ?? 0;
    if (index > cursor) content.push(text.slice(cursor, index));
    const label = match[1];
    const href = match[2];
    content.push(isApprovedPolicyHref(href)
      ? <a href={href} key={`${index}-${href}`}>{label}</a>
      : match[0]);
    cursor = index + match[0].length;
  }
  if (cursor < text.length) content.push(text.slice(cursor));
  return content;
}

export function PolicyContent({ body }: { body: string }) {
  return <div className="site-container narrow article-body policy-content">
    {parsePolicyBody(body).map((block, index) => {
      if (block.kind === "heading") return <h2 key={`${block.kind}-${index}`}>{block.text}</h2>;
      if (block.kind === "list") return <ul key={`${block.kind}-${index}`}>{block.items.map((item, itemIndex) => <li key={itemIndex}>{inlineContent(item)}</li>)}</ul>;
      return <p key={`${block.kind}-${index}`}>{inlineContent(block.text)}</p>;
    })}
  </div>;
}
