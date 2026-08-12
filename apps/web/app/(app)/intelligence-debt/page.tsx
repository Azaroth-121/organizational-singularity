import Link from "next/link";
import { revalidatePath } from "next/cache";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  listOrganizations,
  listIntelligenceDebtFindings,
  createIntelligenceDebtFinding,
} from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { ADMIN_TIER_ROLES } from "../members/roles";
import { CreateFindingForm, type CreateFindingState } from "./create-finding-form";
import { CATEGORY_LABELS, STATUS_LABELS, SEVERITY_TONE } from "./values";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function IntelligenceDebtPage() {
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

  const findings = findingsResult.ok ? findingsResult.data : [];
  const openFindings = findings.filter((f) => f.status !== "Validated" && f.status !== "Rejected");
  const summary = {
    open: openFindings.length,
    critical: openFindings.filter((f) => f.severity === "Critical").length,
    high: openFindings.filter((f) => f.severity === "High").length,
    remediation: findings.filter((f) => f.status === "Remediation").length,
    awaitingValidation: findings.filter((f) => f.status === "Validation").length,
    validated: findings.filter((f) => f.status === "Validated").length,
  };

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Intelligence Debt</h1>
        <p className="text-sm text-muted-foreground">
          Identify, prioritize, remediate, and validate organizational fragmentation in{" "}
          <Badge variant="outline">{tenantName}</Badge>.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        {[
          ["Open findings", summary.open],
          ["Critical", summary.critical],
          ["High", summary.high],
          ["In remediation", summary.remediation],
          ["Awaiting validation", summary.awaitingValidation],
          ["Validated", summary.validated],
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
          ) : findings.length === 0 ? (
            <p className="text-sm text-muted-foreground">No findings yet — raise one below.</p>
          ) : (
            <div className="overflow-x-auto">
              <div className="min-w-[720px]">
                <div className="grid grid-cols-[1.6fr_1.2fr_0.7fr_0.9fr_0.9fr_0.8fr] gap-2 border-b px-3 py-2 text-xs font-medium text-muted-foreground">
                  <span>Finding</span>
                  <span>Category</span>
                  <span>Severity</span>
                  <span>Status</span>
                  <span>Owner</span>
                  <span>Source</span>
                </div>
                <ul>
                  {findings.map((f) => (
                    <li key={f.id}>
                      <Link
                        href={`/intelligence-debt/${f.id}`}
                        className="grid grid-cols-[1.6fr_1.2fr_0.7fr_0.9fr_0.9fr_0.8fr] items-center gap-2 border-b px-3 py-2.5 text-sm hover:bg-muted"
                      >
                        <span className="truncate">
                          <span className="mr-2 font-mono text-xs text-muted-foreground">{f.code}</span>
                          {f.title}
                        </span>
                        <span className="truncate text-muted-foreground">{CATEGORY_LABELS[f.category] ?? f.category}</span>
                        <span>
                          <Badge variant={SEVERITY_TONE[f.severity] ?? "outline"}>{f.severity}</Badge>
                        </span>
                        <span className="text-muted-foreground">{STATUS_LABELS[f.status] ?? f.status}</span>
                        <span className="truncate text-muted-foreground">{f.ownerName ?? "—"}</span>
                        <span className="text-muted-foreground">{f.detectionSource}</span>
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
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
