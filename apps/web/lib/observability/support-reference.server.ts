import { randomBytes } from "node:crypto";

export function createSupportReference(): string {
  return `ESA-${randomBytes(8).toString("hex").toUpperCase()}`;
}

export function logUnexpectedWebFailure(input: {
  referenceId: string;
  category: string;
  status?: number;
}) {
  console.error(JSON.stringify({
    eventName: "UnexpectedWebFailure",
    referenceId: input.referenceId,
    category: input.category,
    status: input.status ?? 500,
    releaseSha: (process.env.RELEASE_SHA ?? "local").slice(0, 40)
  }));
}
