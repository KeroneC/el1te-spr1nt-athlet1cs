import type { ApiProblem } from "./types";

export class AdminApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly fieldErrors: Record<string, string[]> = {}
  ) {
    super(message);
  }
}

export function safeProblem(status: number, value: unknown): AdminApiError {
  const problem = isProblem(value) ? value : {};
  const messages: Record<number, string> = {
    400: "Please correct the highlighted fields.",
    401: "Your session has expired. Please sign in again.",
    403: "You do not have permission to perform this action.",
    404: "The requested record was not found.",
    409: "This change conflicts with existing content."
  };
  return new AdminApiError(status, messages[status] ?? "The service could not complete the request.", problem.errors ?? {});
}

function isProblem(value: unknown): value is ApiProblem {
  return typeof value === "object" && value !== null;
}
