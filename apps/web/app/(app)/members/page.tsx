import { revalidatePath } from "next/cache";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  listTenantMembers,
  addTenantMember,
  updateMemberRole,
  removeMember,
  listTenantInvitations,
  cancelInvitation,
} from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { AddMemberForm, type AddMemberState } from "./member-form";
import { MemberRowActions, type RowActionState } from "./member-row-actions";
import { InvitationRowActions, type CancelInvitationState } from "./invitation-row-actions";
import { ADMIN_TIER_ROLES } from "./roles";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function MembersPage() {
  const session = await verifySession();

  const accessToken = await getApiAccessToken();
  if (!accessToken) {
    return (
      <Placeholder title="No API access token">
        Signed in, but no valid Entra API access token is available. Sign out and back in.
      </Placeholder>
    );
  }

  const membershipsResult = await getMyMemberships(accessToken);
  if (!membershipsResult.ok) {
    return (
      <Placeholder title="Could not resolve tenant">
        GET /api/v1/me/memberships returned {membershipsResult.status ?? "a network error"}.
      </Placeholder>
    );
  }

  const myMembership = membershipsResult.data.memberships[0];
  if (!myMembership) {
    return (
      <Placeholder title="No tenant membership">
        You have no tenant memberships yet, so there&apos;s no member list to show.
      </Placeholder>
    );
  }

  const { tenantId, tenantName } = myMembership;
  const isAdminTier = ADMIN_TIER_ROLES.has(myMembership.role);

  const membersResult = await listTenantMembers(accessToken, tenantId);
  const invitationsResult = isAdminTier ? await listTenantInvitations(accessToken, tenantId) : null;

  async function addMemberAction(
    _prevState: AddMemberState,
    formData: FormData
  ): Promise<AddMemberState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in.", success: null };

    const email = String(formData.get("email") ?? "").trim();
    const role = String(formData.get("role") ?? "");
    if (!email) return { error: "Email is required.", success: null };

    const result = await addTenantMember(token, tenantId, { email, role });
    if (!result.ok) {
      return {
        error: result.message ?? `Add failed: API returned ${result.status ?? "a network error"}.`,
        success: null,
      };
    }

    revalidatePath("/members");
    return {
      error: null,
      success:
        result.data.status === "granted"
          ? `Added ${result.data.email} as ${result.data.role}.`
          : `${result.data.email} has never signed in — created a pending invitation. It'll activate automatically the moment they do.`,
    };
  }

  async function updateRoleAction(
    membershipId: string,
    _prevState: RowActionState,
    formData: FormData
  ): Promise<RowActionState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const role = String(formData.get("role") ?? "");
    const result = await updateMemberRole(token, tenantId, membershipId, role);
    if (!result.ok) {
      return { error: result.message ?? `Update failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/members");
    return { error: null };
  }

  async function removeMemberAction(
    membershipId: string,
    _prevState: RowActionState,
    _formData: FormData
  ): Promise<RowActionState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const result = await removeMember(token, tenantId, membershipId);
    if (!result.ok) {
      return { error: result.message ?? `Remove failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/members");
    return { error: null };
  }

  async function cancelInvitationAction(
    invitationId: string,
    _prevState: CancelInvitationState,
    _formData: FormData
  ): Promise<CancelInvitationState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const result = await cancelInvitation(token, tenantId, invitationId);
    if (!result.ok) {
      return { error: result.message ?? `Cancel failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/members");
    return { error: null };
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Members</h1>
        <p className="text-sm text-muted-foreground">
          Who has access to <Badge variant="outline">{tenantName}</Badge>.
        </p>
      </div>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>{tenantName}</CardTitle>
          <CardDescription>
            {isAdminTier
              ? "You can grant, change, and revoke access below."
              : "You can view members. Only PlatformAdministrator/SoverAIgnArchitect can manage access."}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {!membersResult.ok ? (
            <p className="text-sm text-destructive">
              GET .../memberships returned {membersResult.status ?? "a network error"}.
            </p>
          ) : (
            (() => {
              const adminTierCount = membersResult.data.filter((m) =>
                ADMIN_TIER_ROLES.has(m.role)
              ).length;

              return (
                <ul className="flex flex-col gap-2">
                  {membersResult.data.map((m) => {
                    const isLastAdmin = ADMIN_TIER_ROLES.has(m.role) && adminTierCount <= 1;
                    const isSelf = m.email === session.user.email;

                    return (
                      <li
                        key={m.membershipId}
                        className="flex items-center justify-between gap-4 rounded-md bg-muted px-3 py-2"
                      >
                        <div className="flex flex-col text-sm">
                          <span className="font-medium">
                            {m.name}
                            {isSelf && (
                              <Badge variant="outline" className="ml-2">
                                You
                              </Badge>
                            )}
                          </span>
                          <span className="text-muted-foreground">{m.email}</span>
                        </div>

                        {isAdminTier ? (
                          <MemberRowActions
                            currentRole={m.role}
                            disabled={isLastAdmin}
                            updateRoleAction={updateRoleAction.bind(null, m.membershipId)}
                            removeMemberAction={removeMemberAction.bind(null, m.membershipId)}
                          />
                        ) : (
                          <Badge>{m.role}</Badge>
                        )}
                      </li>
                    );
                  })}
                </ul>
              );
            })()
          )}

          {isAdminTier && <AddMemberForm action={addMemberAction} />}
        </CardContent>
      </Card>

      {isAdminTier && invitationsResult && (
        <Card className="max-w-2xl">
          <CardHeader>
            <CardTitle>Pending invitations</CardTitle>
            <CardDescription>
              Access granted for an email that hasn&apos;t signed in yet — activates
              automatically on their first sign-in.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {!invitationsResult.ok ? (
              <p className="text-sm text-destructive">
                GET .../invitations returned {invitationsResult.status ?? "a network error"}.
              </p>
            ) : invitationsResult.data.length === 0 ? (
              <p className="text-sm text-muted-foreground">No pending invitations.</p>
            ) : (
              <ul className="flex flex-col gap-2">
                {invitationsResult.data.map((i) => (
                  <li
                    key={i.invitationId}
                    className="flex items-center justify-between gap-4 rounded-md bg-muted px-3 py-2"
                  >
                    <div className="flex flex-col text-sm">
                      <span className="font-medium">{i.email}</span>
                      <span className="text-muted-foreground">Invited as {i.role}</span>
                    </div>
                    <InvitationRowActions cancelAction={cancelInvitationAction.bind(null, i.invitationId)} />
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
