export type AdminMutationMethod = "POST" | "PUT" | "DELETE";

export async function readAdminMutationBody(request: Request, method: AdminMutationMethod): Promise<string | undefined> {
  if (method === "DELETE") return undefined;

  const body = await request.text();
  if (!body.trim()) return undefined;

  return JSON.stringify(JSON.parse(body));
}
