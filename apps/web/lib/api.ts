const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

export interface ApiHealth {
  status: string;
  environment: string;
  version: string;
}

export async function getApiHealth(): Promise<ApiHealth | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/api/v1/health`, { cache: "no-store" });
    if (!res.ok) return null;
    return (await res.json()) as ApiHealth;
  } catch {
    return null;
  }
}

export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; status: number | null; message?: string };

async function apiFetch<T>(
  path: string,
  accessToken: string,
  init?: RequestInit
): Promise<ApiResult<T>> {
  try {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      cache: "no-store",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });

    if (!res.ok) {
      const message = await res
        .json()
        .then((body: unknown) =>
          typeof body === "object" && body !== null && "detail" in body && typeof body.detail === "string"
            ? body.detail
            : undefined
        )
        .catch(() => undefined);
      return { ok: false, status: res.status, message };
    }

    if (res.status === 204) {
      return { ok: true, data: undefined as T };
    }
    return { ok: true, data: (await res.json()) as T };
  } catch {
    return { ok: false, status: null };
  }
}

export interface ApiMe {
  name: string | null;
  email: string | null;
  oid: string | null;
  tenantId: string | null;
}

export function getMe(accessToken: string): Promise<ApiResult<ApiMe>> {
  return apiFetch<ApiMe>("/api/v1/me", accessToken);
}

export interface ApiMembership {
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  role: string;
}

export function getMyMemberships(
  accessToken: string
): Promise<ApiResult<{ userId: string; memberships: ApiMembership[] }>> {
  return apiFetch("/api/v1/me/memberships", accessToken);
}

export interface ApiOrganization {
  id: string;
  name: string;
  industry: string | null;
  employeeCount: number | null;
}

export function listOrganizations(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiOrganization[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/organizations`, accessToken);
}

export function createOrganization(
  accessToken: string,
  tenantId: string,
  body: { name: string; industry?: string; employeeCount?: number }
): Promise<ApiResult<ApiOrganization>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/organizations`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export interface ApiTenantMember {
  membershipId: string;
  userId: string;
  name: string;
  email: string;
  role: string;
  invitedAtUtc: string;
  acceptedAtUtc: string | null;
}

export function listTenantMembers(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiTenantMember[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships`, accessToken);
}

export interface ApiInvitation {
  invitationId: string;
  email: string;
  role: string;
  invitedAtUtc: string;
}

export type AddMemberResult =
  | ({ status: "granted" } & ApiTenantMember)
  | ({ status: "invited" } & ApiInvitation);

export function addTenantMember(
  accessToken: string,
  tenantId: string,
  body: { email: string; role: string }
): Promise<ApiResult<AddMemberResult>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function listTenantInvitations(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiInvitation[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/invitations`, accessToken);
}

export function cancelInvitation(
  accessToken: string,
  tenantId: string,
  invitationId: string
): Promise<ApiResult<undefined>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/invitations/${invitationId}`, accessToken, {
    method: "DELETE",
  });
}

export function updateMemberRole(
  accessToken: string,
  tenantId: string,
  membershipId: string,
  role: string
): Promise<ApiResult<{ membershipId: string; role: string; acceptedAtUtc: string | null }>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships/${membershipId}`, accessToken, {
    method: "PUT",
    body: JSON.stringify({ role }),
  });
}

export function removeMember(
  accessToken: string,
  tenantId: string,
  membershipId: string
): Promise<ApiResult<undefined>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships/${membershipId}`, accessToken, {
    method: "DELETE",
  });
}
