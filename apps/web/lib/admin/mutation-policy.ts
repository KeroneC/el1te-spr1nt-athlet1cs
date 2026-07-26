const resources = new Set(["events", "coaches", "sponsors", "faqs", "content-blocks", "site-settings", "contact-submissions", "media", "gallery-albums", "users", "invitations", "store"]);
const idPattern = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;

export function isAllowedAdminMutation(path: string[], method: "POST" | "PUT" | "DELETE"): boolean {
  const [resource, id, action] = path;
  if (!resources.has(resource)) return false;
  if (resource === "store") return isAllowedStoreMutation(path.slice(1), method);
  if (resource === "media") {
    if (method === "POST") return path.length === 1;
    return path.length === 2 && idPattern.test(id ?? "") && (method === "PUT" || method === "DELETE");
  }
  if (resource === "users") return method === "PUT" && path.length === 2 && idPattern.test(id ?? "");
  if (resource === "invitations") {
    if (method === "POST" && path.length === 1) return true;
    if (!idPattern.test(id ?? "")) return false;
    if (method === "DELETE") return path.length === 2;
    return method === "POST" && path.length === 3 && action === "reissue";
  }
  if (resource === "gallery-albums" && path.length > 2) {
    if (!idPattern.test(id ?? "") || action !== "media") return false;
    if (path.length === 3) return method === "POST";
    if (path.length === 4 && path[3] === "order") return method === "PUT";
    return path.length === 4 && idPattern.test(path[3]) && (method === "PUT" || method === "DELETE");
  }
  if (resource === "site-settings") return path.length === 1 && method === "PUT";
  if (method === "POST") return path.length === 1 && resource !== "contact-submissions";
  if (!idPattern.test(id ?? "")) return false;
  if (resource === "contact-submissions" && method === "PUT") return path.length === 3 && action === "status";
  if (method === "DELETE") return path.length === 2;
  return method === "PUT" && path.length === 2 && resource !== "contact-submissions";
}

function isAllowedStoreMutation(path: string[], method: "POST" | "PUT" | "DELETE"): boolean {
  const [resource, id, action] = path;
  if (resource === "products") {
    if (method === "POST" && path.length === 1) return true;
    if (!idPattern.test(id ?? "")) return false;
    if (method === "PUT" || method === "DELETE") return path.length === 2;
    return method === "POST" && path.length === 3 && action === "duplicate";
  }
  if (resource === "categories") {
    if (method === "POST") return path.length === 1;
    return method === "PUT" && path.length === 2 && idPattern.test(id ?? "");
  }
  if (resource === "square-import") return method === "POST" && path.length === 1;
  if (resource !== "inventory" || method !== "POST") return false;
  if (path.length === 2) return id === "receipts" || id === "stocktakes";
  return path.length === 3 && idPattern.test(id ?? "") && action === "adjustments";
}

export function isAllowedAdminRead(path: string[]): boolean {
  if (path[0] !== "store") return false;
  const storePath = path.slice(1);
  if (storePath.length === 1) {
    return ["dashboard", "products", "categories", "inventory"].includes(storePath[0]);
  }
  return (storePath[0] === "products" && storePath.length === 2 && idPattern.test(storePath[1])) ||
    (storePath[0] === "inventory" && storePath.length === 2 && ["adjustments", "stocktakes"].includes(storePath[1])) ||
    (storePath[0] === "square-import" && storePath[1] === "preview" && storePath.length === 2);
}
