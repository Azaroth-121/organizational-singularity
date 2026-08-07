import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import { getMe, getMyMemberships } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function MePage() {
  await verifySession();

  const accessToken = await getApiAccessToken();
  if (!accessToken) {
    return (
      <Placeholder title="No API access token">
        Signed in, but no valid Entra API access token is available. Sign out and back in.
      </Placeholder>
    );
  }

  const result = await getMe(accessToken);
  if (!result.ok) {
    return (
      <Placeholder title="API call failed">
        GET /api/v1/me returned {result.status ?? "a network error"}.
      </Placeholder>
    );
  }

  const membershipsResult = await getMyMemberships(accessToken);

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Diagnostics</h1>
        <p className="text-sm text-muted-foreground">
          Raw identity claims resolved through the .NET API, for debugging the auth chain.
        </p>
      </div>

      <Card className="max-w-xl">
        <CardHeader>
          <CardTitle>Identity</CardTitle>
          <CardDescription>Returned by GET /api/v1/me</CardDescription>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-y-3 text-sm">
            <dt className="text-muted-foreground">Name</dt>
            <dd className="font-medium">{result.data.name}</dd>

            <dt className="text-muted-foreground">Email / UPN</dt>
            <dd className="font-medium">{result.data.email}</dd>

            <dt className="text-muted-foreground">Object ID (oid)</dt>
            <dd className="font-medium">{result.data.oid}</dd>

            <dt className="text-muted-foreground">Tenant ID (tid)</dt>
            <dd className="font-medium">{result.data.tenantId}</dd>
          </dl>
        </CardContent>
      </Card>

      <Card className="max-w-xl">
        <CardHeader>
          <CardTitle>Tenant memberships</CardTitle>
          <CardDescription>Returned by GET /api/v1/me/memberships</CardDescription>
        </CardHeader>
        <CardContent>
          {!membershipsResult.ok ? (
            <p className="text-sm text-destructive">
              GET /api/v1/me/memberships returned {membershipsResult.status ?? "a network error"}.
            </p>
          ) : membershipsResult.data.memberships.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No tenant memberships yet. An admin needs to assign one.
            </p>
          ) : (
            <ul className="flex flex-col gap-2 text-sm">
              {membershipsResult.data.memberships.map((m) => (
                <li
                  key={m.tenantId}
                  className="flex items-center justify-between rounded-md bg-muted px-3 py-2"
                >
                  <span className="font-medium">
                    {m.tenantName} <span className="text-muted-foreground">({m.tenantSlug})</span>
                  </span>
                  <span className="text-muted-foreground">{m.role}</span>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
