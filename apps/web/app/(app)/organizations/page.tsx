import { revalidatePath } from "next/cache";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import { getMyMemberships, listOrganizations, createOrganization } from "@/lib/api";
import { CreateOrganizationForm, type CreateOrganizationState } from "./create-organization-form";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function OrganizationsPage() {
  await verifySession();

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

  const membership = membershipsResult.data.memberships[0];
  if (!membership) {
    return (
      <Placeholder title="No tenant membership">
        You have no tenant memberships yet, so there&apos;s no organization list to show.
      </Placeholder>
    );
  }

  const orgsResult = await listOrganizations(accessToken, membership.tenantId);

  async function createOrganizationAction(
    _prevState: CreateOrganizationState,
    formData: FormData
  ): Promise<CreateOrganizationState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) {
      return { error: "No API access token available. Sign out and back in." };
    }

    const name = String(formData.get("name") ?? "").trim();
    if (!name) {
      return { error: "Name is required." };
    }

    const result = await createOrganization(token, membership.tenantId, { name });
    if (!result.ok) {
      return { error: `Create failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/organizations");
    return { error: null };
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Organizations</h1>
        <p className="text-sm text-muted-foreground">
          Acting as <span className="font-medium text-foreground">{membership.role}</span> in{" "}
          <Badge variant="outline">{membership.tenantSlug}</Badge>
        </p>
      </div>

      <Card className="max-w-xl">
        <CardHeader>
          <CardTitle>{membership.tenantName}</CardTitle>
          <CardDescription>Organizations in this tenant.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {!orgsResult.ok ? (
            <p className="text-sm text-destructive">
              GET .../organizations returned {orgsResult.status ?? "a network error"}.
            </p>
          ) : orgsResult.data.length === 0 ? (
            <p className="text-sm text-muted-foreground">No organizations yet — create one below.</p>
          ) : (
            <ul className="flex flex-col gap-2 text-sm">
              {orgsResult.data.map((o) => (
                <li key={o.id} className="rounded-md bg-muted px-3 py-2 font-medium">
                  {o.name}
                  {o.industry && (
                    <span className="ml-2 text-muted-foreground">({o.industry})</span>
                  )}
                </li>
              ))}
            </ul>
          )}

          <CreateOrganizationForm action={createOrganizationAction} />
        </CardContent>
      </Card>
    </div>
  );
}
