import Link from "next/link";
import { revalidatePath } from "next/cache";
import { AlertTriangle, ClipboardCheck, ShieldCheck, Wrench } from "lucide-react";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  listOrganizations,
  listIntelligenceDebtFindings,
  createIntelligenceDebtFinding,
} from "@/lib/api";
import { StatusBadge } from "@/components/ui/status-badge";
import { StatTile } from "@/components/ui/stat-tile";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { ADMIN_TIER_ROLES } from "../members/roles";
import { CreateFindingForm, type CreateFindingState } from "./create-finding-form";
import { CATEGORY_LABELS, STATUS_LABELS, SEVERITY_TONE, FINDING_STATUS_TONE, SEVERITIES } from "./values";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function IntelligenceDebtPage({
  searchParams,
}: {
  searchParams: Promise<{ severity?: string }>;
}) {
  await verifySession();
  const { severity: severityFilter } = await searchParams;

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
        You have no tenant memberships yet, so there&apos;s no register to show.
      </Placeholder>
    );
  }

  const { tenantId, tenantName } = myMembership;
  const isAdminTier = ADMIN_TIER_ROLES.has(myMembership.role);

  const [findingsResult, organizationsResult] = await Promise.all([
    listIntelligenceDebtFindings(accessToken, tenantId),
    listOrganizations(accessToken, tenantId),
  ]);

  async function createFindingAction(
    _prevState: CreateFindingState,
    formData: FormData
  ): Promise<CreateFindingState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const title = String(formData.get("title") ?? "").trim();
    const organizationId = String(formData.get("organizationId") ?? "");
    const category = String(formData.get("category") ?? "");
    const severity = String(formData.get("severity") ?? "");
    const detectionSource = String(formData.get("detectionSource") ?? "");
    const description = String(formData.get("description") ?? "");

    if (!title) return { error: "Title is required." };
    if (!organizationId) return { error: "Organization is required." };
    if (!category) return { error: "Category is required." };

    const result = await createIntelligenceDebtFinding(token, tenantId, {
      organizationId,
      title,
      description,
      category,
      severity,
      detectionSource,
    });
    if (!result.ok) {
      return { error: result.message ?? `Create failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/intelligence-debt");
    return { error: null };
  }

  const allFindings = findingsResult.ok ? findingsResult.data : [];
  const openFindings = allFindings.filter((f) => f.status !== "Validated" && f.status !== "Rejected");
  const detectedCount = allFindings.filter((f) => f.status === "Detected").length;
  const criticalOpen = openFindings.filter((f) => f.severity === "Critical").length;
  const remediationCount = allFindings.filter((f) => f.status === "Remediation").length;
  const validatedCount = allFindings.filter((f) => f.status === "Validated").length;

  const findings = severityFilter ? allFindings.filter((f) => f.severity === severityFilter) : allFindings;

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Intelligence Debt</h1>
        <p className="text-sm text-muted-foreground">
          Identify, prioritize, remediate, and validate organizational fragmentation for {tenantName}.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile
          label="Open findings"
          value={openFindings.length}
          icon={AlertTriangle}
          tone={criticalOpen > 0 ? "critical" : openFindings.length > 0 ? "warning" : "good"}
        />
        <StatTile label="Awaiting review" value={detectedCount} icon={ClipboardCheck} tone={detectedCount > 0 ? "warning" : "good"} />
        <StatTile label="In remediation" value={remediationCount} icon={Wrench} tone={remediationCount > 0 ? "warning" : "neutral"} />
        <StatTile label="Validated" value={validatedCount} icon={ShieldCheck} tone="good" />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Register</CardTitle>
          <CardDescription>
            {isAdminTier
              ? "Anyone can raise a finding; approving, transitioning, and editing requires an admin-tier role."
              : "You can view and raise findings. Only PlatformAdministrator/SoverAIgnArchitect can approve or transition them."}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {!findingsResult.ok ? (
            <p className="text-sm text-destructive">
              GET .../intelligence-debt returned {findingsResult.status ?? "a network error"}.
            </p>
          ) : allFindings.length === 0 ? (
            <p className="text-sm text-muted-foreground">No findings yet — raise one below.</p>
          ) : (
            <>
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="mr-1 text-xs font-medium text-muted-foreground">Filter by severity:</span>
                <Link
                  href="/intelligence-debt"
                  className={cn(
                    "rounded-full border px-2.5 py-1 text-xs transition-colors",
                    !severityFilter ? "border-primary bg-primary/10 text-primary" : "border-border text-muted-foreground hover:bg-muted"
                  )}
                >
                  All ({allFindings.length})
                </Link>
                {SEVERITIES.map((s) => {
                  const count = allFindings.filter((f) => f.severity === s).length;
                  if (count === 0) return null;
                  return (
                    <Link
                      key={s}
                      href={`/intelligence-debt?severity=${s}`}
                      className={cn(
                        "rounded-full border px-2.5 py-1 text-xs transition-colors",
                        severityFilter === s ? "border-primary bg-primary/10 text-primary" : "border-border text-muted-foreground hover:bg-muted"
                      )}
                    >
                      {s} ({count})
                    </Link>
                  );
                })}
              </div>

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Finding</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead>Severity</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Owner</TableHead>
                    <TableHead>Source</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {findings.map((f) => (
                    <TableRow key={f.id} className="cursor-pointer">
                      <TableCell className="max-w-0">
                        <Link href={`/intelligence-debt/${f.id}`} className="block truncate hover:text-primary">
                          <span className="mr-2 font-mono text-xs text-muted-foreground">{f.code}</span>
                          {f.title}
                        </Link>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{CATEGORY_LABELS[f.category] ?? f.category}</TableCell>
                      <TableCell>
                        <StatusBadge tone={SEVERITY_TONE[f.severity] ?? "neutral"}>{f.severity}</StatusBadge>
                      </TableCell>
                      <TableCell>
                        <StatusBadge tone={FINDING_STATUS_TONE[f.status] ?? "neutral"} showIcon={false}>
                          {STATUS_LABELS[f.status] ?? f.status}
                        </StatusBadge>
                      </TableCell>
                      <TableCell className="text-muted-foreground">{f.ownerName ?? "—"}</TableCell>
                      <TableCell className="text-muted-foreground">{f.detectionSource}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </>
          )}

          {organizationsResult.ok && organizationsResult.data.length > 0 ? (
            <CreateFindingForm action={createFindingAction} organizations={organizationsResult.data} />
          ) : (
            <p className="border-t pt-4 text-xs text-muted-foreground">
              Create an organization first (under Organizations) before raising a finding.
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
