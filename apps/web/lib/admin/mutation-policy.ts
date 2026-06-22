const resources = new Set(["events", "coaches", "sponsors", "faqs", "content-blocks", "site-settings", "contact-submissions", "media", "gallery-albums"]);
const idPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function isAllowedAdminMutation(path: string[], method: "POST" | "PUT" | "DELETE"): boolean {
  const [resource, id, action] = path;
  if (!resources.has(resource)) return false;
  if (resource === "media") {
    if (method === "POST") return path.length === 1;
    return path.length === 2 && idPattern.test(id ?? "") && (method === "PUT" || method === "DELETE");
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
