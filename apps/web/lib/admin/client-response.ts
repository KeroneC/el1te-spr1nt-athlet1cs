export const ADMIN_SESSION_EXPIRED_REDIRECT = "/api/admin-session/logout?reason=expired";

export function getAdminResponseRedirect(status: number): string | null {
  if (status === 401) return ADMIN_SESSION_EXPIRED_REDIRECT;
  if (status === 403) return "/admin/access-denied";
  return null;
}

export function redirectForAdminResponse(response: Pick<Response, "status">): boolean {
  const destination = getAdminResponseRedirect(response.status);
  if (!destination) return false;
  window.location.assign(destination);
  return true;
}
