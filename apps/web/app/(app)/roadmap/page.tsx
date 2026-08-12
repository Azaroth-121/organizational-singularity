import Link from "next/link";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import { getMyMemberships, listInitiatives } from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { STATUS_LABELS, PRIORITY_TONE } from "./values";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function RoadmapPage() {
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

  const myMembership = membershipsResult.data.memberships[0];
  if (!myMembership) {
    return <Placeholder title="No tenant membership">You have no tenant memberships yet.</Placeholder>;
  }

  const { tenantId, tenantName } = myMembership;

  const initiativesResult = await listInitiatives(accessToken, tenantId);
  const initiatives = initiativesResult.ok ? initiativesResult.data : [];

  const active = initiatives.filter((i) => i.status !== "Completed" && i.status !== "Cancelled");
  const summary = {
    active: active.length,
    inProgress: initiatives.filter((i) => i.status === "InProgress").length,
    onHold: initiatives.filter((i) => i.status === "OnHold").length,
    completed: initiatives.filter((i) => i.status === "Completed").length,
  };

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Roadmap</h1>
        <p className="text-sm text-muted-foreground">
          Initiatives converted from approved Intelligence Debt findings for <Badge variant="outline">{tenantName}</Badge>.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[
          ["Active", summary.active],
          ["In progress", summary.inProgress],
          ["On hold", summary.onHold],
          ["Completed", summary.completed],
        ].map(([label, value]) => (
          <Card key={label as string}>
            <CardContent className="py-4">
              <p className="text-2xl font-semibold tabular-nums">{value}</p>
              <p className="text-xs text-muted-foreground">{label}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Initiatives</CardTitle>
          <CardDescription>
            Convert an approved finding into an initiative from that finding&apos;s page in the Intelligence Debt register.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!initiativesResult.ok ? (
            <p className="text-sm text-destructive">
              GET .../initiatives returned {initiativesResult.status ?? "a network error"}.
            </p>
          ) : initiatives.length === 0 ? (
            <p className="text-sm text-muted-foreground">No initiatives yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <div className="min-w-[720px]">
                <div className="grid grid-cols-[1.6fr_1fr_0.7fr_0.9fr_0.9fr_0.9fr] gap-2 border-b px-3 py-2 text-xs font-medium text-muted-foreground">
                  <span>Initiative</span>
                  <span>Organization</span>
                  <span>Priority</span>
                  <span>Status</span>
                  <span>Owner</span>
                  <span>Target completion</span>
                </div>
                <ul>
                  {initiatives.map((i) => (
                    <li key={i.id}>
                      <Link
                        href={`/roadmap/${i.id}`}
                        className="grid grid-cols-[1.6fr_1fr_0.7fr_0.9fr_0.9fr_0.9fr] items-center gap-2 border-b px-3 py-2.5 text-sm hover:bg-muted"
                      >
                        <span className="truncate">
                          <span className="mr-2 font-mono text-xs text-muted-foreground">{i.code}</span>
                          {i.title}
                        </span>
                        <span className="truncate text-muted-foreground">{i.organizationName ?? "—"}</span>
                        <span>
                          <Badge variant={PRIORITY_TONE[i.priority] ?? "outline"}>{i.priority}</Badge>
                        </span>
                        <span className="text-muted-foreground">{STATUS_LABELS[i.status] ?? i.status}</span>
                        <span className="truncate text-muted-foreground">{i.ownerName ?? "—"}</span>
                        <span className="text-muted-foreground">
                          {i.targetCompletionDate ? new Date(i.targetCompletionDate).toLocaleDateString() : "—"}
                        </span>
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
