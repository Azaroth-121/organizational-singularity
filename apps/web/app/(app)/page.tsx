import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  listTenantMembers,
  listOrganizations,
  listAssessments,
  listIntelligenceDebtFindings,
  getAssessmentResult,
  type ApiAssessmentSummary,
  type ApiDimensionScore,
} from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { ADMIN_TIER_ROLES } from "./members/roles";
import { AdminDashboard } from "./admin-dashboard";
import { MemberDashboard } from "./member-dashboard";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function DashboardPage() {
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

  const { userId, memberships } = membershipsResult.data;
  const myMembership = memberships[0];
  if (!myMembership) {
    return <Placeholder title="No tenant membership">You have no tenant memberships yet.</Placeholder>;
  }

  const { tenantId, tenantName } = myMembership;
  const isAdminTier = ADMIN_TIER_ROLES.has(myMembership.role);

  const [organizationsResult, assessmentsResult, findingsResult, membersResult] = await Promise.all([
    listOrganizations(accessToken, tenantId),
    listAssessments(accessToken, tenantId),
    listIntelligenceDebtFindings(accessToken, tenantId),
    isAdminTier ? listTenantMembers(accessToken, tenantId) : Promise.resolve(null),
  ]);

  const organizations = organizationsResult.ok ? organizationsResult.data : [];
  const assessments = assessmentsResult.ok ? assessmentsResult.data : [];
  const findings = findingsResult.ok ? findingsResult.data : [];

  const latestAssessmentByOrg = new Map<string, ApiAssessmentSummary>();
  for (const a of assessments) {
    if (!latestAssessmentByOrg.has(a.organizationId)) latestAssessmentByOrg.set(a.organizationId, a);
  }

  const dimensionScoresByAssessment = new Map<string, ApiDimensionScore[]>();
  if (isAdminTier) {
    const completed = [...latestAssessmentByOrg.values()].filter((a) => a.status === "Completed");
    const results = await Promise.all(completed.map((a) => getAssessmentResult(accessToken, tenantId, a.id)));
    results.forEach((r, i) => {
      if (r.ok) dimensionScoresByAssessment.set(completed[i].id, r.data.dimensionScores);
    });
  }

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          Welcome back, {session.user?.name ?? session.user?.email} · <Badge variant="outline">{tenantName}</Badge>
        </p>
      </div>

      {isAdminTier ? (
        <AdminDashboard
          organizations={organizations}
          latestAssessmentByOrg={latestAssessmentByOrg}
          dimensionScoresByAssessment={dimensionScoresByAssessment}
          findings={findings}
          memberCount={membersResult && membersResult.ok ? membersResult.data.length : 0}
        />
      ) : (
        <MemberDashboard
          myFindings={findings.filter((f) => f.ownerUserId === userId)}
          inProgressAssessments={assessments.filter((a) => a.status === "Draft" || a.status === "InProgress" || a.status === "Submitted")}
          organizationCount={organizations.length}
          openFindingCount={findings.filter((f) => f.status !== "Validated" && f.status !== "Rejected").length}
        />
      )}
    </div>
  );
}
