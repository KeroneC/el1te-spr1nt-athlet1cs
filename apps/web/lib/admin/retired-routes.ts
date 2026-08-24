const RETIRED_ADMIN_ROUTES = new Set([
  "/admin/store/import",
]);

export function isRetiredAdminRoute(pathname: string) {
  const normalizedPath = pathname.length > 1
    ? pathname.replace(/\/+$/, "")
    : pathname;

  return RETIRED_ADMIN_ROUTES.has(normalizedPath);
}
