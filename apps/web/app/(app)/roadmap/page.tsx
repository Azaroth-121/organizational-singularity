import Link from "next/link";
import { ListTodo, PlayCircle, PauseCircle, CheckCircle2 } from "lucide-react";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import { getMyMemberships, listInitiatives } from "@/lib/api";
import { StatusBadge } from "@/components/ui/status-badge";
import { StatTile } from "@/components/ui/stat-tile";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { STATUS_LABELS, PRIORITY_TONE, INITIATIVE_STATUS_TONE } from "./values";

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
          Initiatives converted from approved Intelligence Debt findings for {tenantName}.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <StatTile label="Active" value={summary.active} icon={ListTodo} />
        <StatTile label="In progress" value={summary.inProgress} icon={PlayCircle} tone={summary.inProgress > 0 ? "warning" : "neutral"} />
        <StatTile label="On hold" value={summary.onHold} icon={PauseCircle} tone={summary.onHold > 0 ? "serious" : "neutral"} />
        <StatTile label="Completed" value={summary.completed} icon={CheckCircle2} tone="good" />
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
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Initiative</TableHead>
                  <TableHead>Organization</TableHead>
                  <TableHead>Priority</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Owner</TableHead>
                  <TableHead>Target completion</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {initiatives.map((i) => (
                  <TableRow key={i.id}>
                    <TableCell className="max-w-0">
                      <Link href={`/roadmap/${i.id}`} className="block truncate hover:text-primary">
                        <span className="mr-2 font-mono text-xs text-muted-foreground">{i.code}</span>
                        {i.title}
                      </Link>
                    </TableCell>
                    <TableCell className="truncate text-muted-foreground">{i.organizationName ?? "—"}</TableCell>
                    <TableCell>
                      <StatusBadge tone={PRIORITY_TONE[i.priority] ?? "neutral"} showIcon={false}>{i.priority}</StatusBadge>
                    </TableCell>
                    <TableCell>
                      <StatusBadge tone={INITIATIVE_STATUS_TONE[i.status] ?? "neutral"} showIcon={false}>
                        {STATUS_LABELS[i.status] ?? i.status}
                      </StatusBadge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{i.ownerName ?? "—"}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {i.targetCompletionDate ? new Date(i.targetCompletionDate).toLocaleDateString() : "—"}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
