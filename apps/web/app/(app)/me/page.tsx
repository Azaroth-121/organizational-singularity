import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import { getMe, getMyMemberships, pingAiGateway, pingDocumentStorage } from "@/lib/api";
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

  // AI gateway diagnostics (see docs/adr/0003) -- real, end-to-end proof that ModelGateway
  // actually reaches a model in this environment, using the same server-side token this page
  // already resolves. Only attempted if we know which tenant to ask on behalf of.
  const primaryTenantId = membershipsResult.ok ? membershipsResult.data.memberships[0]?.tenantId : undefined;
  const aiPingResult = primaryTenantId ? await pingAiGateway(accessToken, primaryTenantId) : null;

  // Document storage diagnostics (see docs/adr/0004) -- same reasoning as the AI gateway
  // ping above: no upload UI exists yet, so this is the only way to prove real blob storage
  // connectivity against the live Azure deployment.
  const documentPingResult = primaryTenantId ? await pingDocumentStorage(accessToken, primaryTenantId) : null;

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

      <Card className="max-w-xl">
        <CardHeader>
          <CardTitle>AI gateway</CardTitle>
          <CardDescription>Returned by POST /ai/diagnostics/ping (see docs/adr/0003)</CardDescription>
        </CardHeader>
        <CardContent>
          {!primaryTenantId ? (
            <p className="text-sm text-muted-foreground">No tenant to diagnose against yet.</p>
          ) : !aiPingResult?.ok ? (
            <p className="text-sm text-destructive">
              POST /ai/diagnostics/ping returned {aiPingResult?.status ?? "a network error"}
              {aiPingResult && !aiPingResult.ok && aiPingResult.message ? `: ${aiPingResult.message}` : ""}
            </p>
          ) : (
            <dl className="grid grid-cols-2 gap-y-3 text-sm">
              <dt className="text-muted-foreground">Outcome</dt>
              <dd className="font-medium">{aiPingResult.data.outcome}</dd>

              <dt className="text-muted-foreground">Model</dt>
              <dd className="font-medium">{aiPingResult.data.modelDeployment}</dd>

              <dt className="text-muted-foreground">Output</dt>
              <dd className="font-medium">{aiPingResult.data.outputText ?? "—"}</dd>

              <dt className="text-muted-foreground">Tokens (in / out)</dt>
              <dd className="font-medium">
                {aiPingResult.data.inputTokens ?? "—"} / {aiPingResult.data.outputTokens ?? "—"}
              </dd>

              <dt className="text-muted-foreground">Latency</dt>
              <dd className="font-medium">
                {aiPingResult.data.latencyMs !== null ? `${aiPingResult.data.latencyMs} ms` : "—"}
              </dd>

              {aiPingResult.data.errorMessage && (
                <>
                  <dt className="text-muted-foreground">Error</dt>
                  <dd className="font-medium text-destructive">{aiPingResult.data.errorMessage}</dd>
                </>
              )}
            </dl>
          )}
        </CardContent>
      </Card>

      <Card className="max-w-xl">
        <CardHeader>
          <CardTitle>Document storage</CardTitle>
          <CardDescription>Returned by POST /documents/diagnostics/ping (see docs/adr/0004)</CardDescription>
        </CardHeader>
        <CardContent>
          {!primaryTenantId ? (
            <p className="text-sm text-muted-foreground">No tenant to diagnose against yet.</p>
          ) : !documentPingResult?.ok ? (
            <p className="text-sm text-destructive">
              POST /documents/diagnostics/ping returned {documentPingResult?.status ?? "a network error"}
              {documentPingResult && !documentPingResult.ok && documentPingResult.message ? `: ${documentPingResult.message}` : ""}
            </p>
          ) : !documentPingResult.data.success ? (
            <p className="text-sm text-destructive">
              Upload/download round trip failed{documentPingResult.data.error ? `: ${documentPingResult.data.error}` : ""}
            </p>
          ) : (
            <dl className="grid grid-cols-2 gap-y-3 text-sm">
              <dt className="text-muted-foreground">Round trip</dt>
              <dd className="font-medium">Succeeded</dd>

              <dt className="text-muted-foreground">Document ID</dt>
              <dd className="font-medium">{documentPingResult.data.documentId}</dd>

              <dt className="text-muted-foreground">Blob name</dt>
              <dd className="font-medium">{documentPingResult.data.blobName ?? "—"}</dd>
            </dl>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
