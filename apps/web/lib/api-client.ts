const fallbackApiBaseUrl = "http://localhost:5000";

export const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? fallbackApiBaseUrl;

export async function apiFetch<TResponse>(
  path: string,
  init?: RequestInit
): Promise<TResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    }
  });

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}.`);
  }

  return response.json() as Promise<TResponse>;
}
