import Link from "next/link";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  getAssessment,
  getAssessmentResult,
  listIntelligenceDebtFindings,
  listInitiatives,
} from "@/lib/api";
import { StatusBadge } from "@/components/ui/status-badge";
import { OiqProfileBars } from "@/components/ui/oiq-profile-bars";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { BAND_TONE, formatScore } from "../../values";
import { SEVERITY_TONE, STATUS_LABELS as FINDING_STATUS_LABELS } from "../../../intelligence-debt/values";
import { PRIORITY_TONE, STATUS_LABELS as INITIATIVE_STATUS_LABELS } from "../../../roadmap/values";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

const SEVERITY_ORDER = ["Critical", "High", "Moderate", "Low", "Informational"];
const PRIORITY_ORDER = ["Critical", "High", "Medium", "Low"];
const AUTHORITATIVE_FINDING_STATUSES = ["ApprovedFinding", "Remediation", "Validation", "Validated"];
const CONFIDENCE_LABELS: Record<string, string> = {
  CorroboratedEvidence: "Corroborated evidence",
  SupportingEvidence: "Supporting evidence",
  AssertionOnly: "Assertion only",
};

export default async function ExecutiveReportPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
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

  const { tenantId } = myMembership;

  const assessmentResult = await getAssessment(accessToken, tenantId, id);
  if (!assessmentResult.ok) {
    return (
      <Placeholder title="Assessment not found">
        GET .../assessments/{id} returned {assessmentResult.status ?? "a network error"}.
      </Placeholder>
    );
  }
  const assessment = assessmentResult.data;

  if (assessment.status !== "Completed" && assessment.status !== "Superseded") {
    return (
      <Placeholder title="Report not available yet">
        This assessment must be completed before an executive report can be generated.
      </Placeholder>
    );
  }

  const [resultData, findingsResult, initiativesResult] = await Promise.all([
    getAssessmentResult(accessToken, tenantId, id),
    listIntelligenceDebtFindings(accessToken, tenantId),
    listInitiatives(accessToken, tenantId),
  ]);

  if (!resultData.ok) {
    return (
      <Placeholder title="Result not available">
        GET .../assessments/{id}/result returned {resultData.status ?? "a network error"}.
      </Placeholder>
    );
  }
  const result = resultData.data;

  const questions = assessment.dimensions.flatMap((d) => d.capabilities.flatMap((c) => c.questions));
  const answered = questions.filter((q) => q.response?.answerState === "Answered");
  const confidenceCounts = {
    CorroboratedEvidence: answered.filter((q) => q.response?.confidence === "CorroboratedEvidence").length,
    SupportingEvidence: answered.filter((q) => q.response?.confidence === "SupportingEvidence").length,
    AssertionOnly: answered.filter((q) => q.response?.confidence === "AssertionOnly").length,
    none: answered.filter((q) => !q.response?.confidence).length,
  };

  const topFindings = (findingsResult.ok ? findingsResult.data : [])
    .filter((f) => f.organizationId === assessment.organizationId && AUTHORITATIVE_FINDING_STATUSES.includes(f.status))
    .sort((a, b) => SEVERITY_ORDER.indexOf(a.severity) - SEVERITY_ORDER.indexOf(b.severity))
    .slice(0, 10);

  const roadmap = (initiativesResult.ok ? initiativesResult.data : [])
    .filter((i) => i.organizationId === assessment.organizationId)
    .sort((a, b) => PRIORITY_ORDER.indexOf(a.priority) - PRIORITY_ORDER.indexOf(b.priority));

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <Link href={`/assessments/${id}`} className="text-xs text-muted-foreground hover:underline">
          ← Assessment
        </Link>
        <h1 className="mt-1 text-2xl font-semibold">Executive Report</h1>
        <p className="text-sm text-muted-foreground">
          {assessment.organizationName} · {assessment.frameworkVersionLabel} ·{" "}
          {assessment.completedAtUtc && new Date(assessment.completedAtUtc).toLocaleDateString()}
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>OIQ Profile</CardTitle>
          <CardDescription>
            The 11-dimension profile is the primary result. Composite average ({formatScore(result.compositeAverage)}) is
            secondary/internal reference only.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <OiqProfileBars
            bandTone={BAND_TONE}
            rows={result.dimensionScores.map((d) => ({ key: d.dimensionId, code: d.code, name: d.name, score: d.score, band: d.maturityBand }))}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Evidence summary</CardTitle>
          <CardDescription>How much of this assessment&apos;s {answered.length} answered questions rest on real evidence.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
            {(["CorroboratedEvidence", "SupportingEvidence", "AssertionOnly"] as const).map((key) => (
              <div key={key} className="rounded-md border p-3 text-center">
                <p className="text-lg font-semibold tabular-nums">{confidenceCounts[key]}</p>
                <p className="text-xs text-muted-foreground">{CONFIDENCE_LABELS[key]}</p>
              </div>
            ))}
            <div className="rounded-md border p-3 text-center">
              <p className="text-lg font-semibold tabular-nums">{confidenceCounts.none}</p>
              <p className="text-xs text-muted-foreground">No confidence recorded</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Top Intelligence Debt</CardTitle>
          <CardDescription>Approved findings for this organization, most severe first.</CardDescription>
        </CardHeader>
        <CardContent>
          {topFindings.length === 0 ? (
            <p className="text-sm text-muted-foreground">No approved findings yet.</p>
          ) : (
            <ul className="flex flex-col gap-1">
              {topFindings.map((f) => (
                <li key={f.id}>
                  <Link
                    href={`/intelligence-debt/${f.id}`}
                    className="flex items-center justify-between gap-3 rounded-md bg-muted px-3 py-2 text-sm hover:bg-muted/70"
                  >
                    <span className="truncate">
                      <span className="mr-2 font-mono text-xs text-muted-foreground">{f.code}</span>
                      {f.title}
                      {f.detection && (
                        <span className="ml-2 text-xs text-muted-foreground">
                          ({f.detection.observedScore.toFixed(1)} vs {f.detection.thresholdUsed.toFixed(1)} threshold)
                        </span>
                      )}
                    </span>
                    <span className="flex shrink-0 items-center gap-2">
                      <StatusBadge tone={SEVERITY_TONE[f.severity] ?? "neutral"}>{f.severity}</StatusBadge>
                      <span className="text-xs text-muted-foreground">{FINDING_STATUS_LABELS[f.status] ?? f.status}</span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Prioritized roadmap</CardTitle>
          <CardDescription>Initiatives for this organization, highest priority first.</CardDescription>
        </CardHeader>
        <CardContent>
          {roadmap.length === 0 ? (
            <p className="text-sm text-muted-foreground">No initiatives yet.</p>
          ) : (
            <ul className="flex flex-col gap-1">
              {roadmap.map((i) => (
                <li key={i.id}>
                  <Link
                    href={`/roadmap/${i.id}`}
                    className="flex items-center justify-between gap-3 rounded-md bg-muted px-3 py-2 text-sm hover:bg-muted/70"
                  >
                    <span className="truncate">
                      <span className="mr-2 font-mono text-xs text-muted-foreground">{i.code}</span>
                      {i.title}
                    </span>
                    <span className="flex shrink-0 items-center gap-2">
                      <StatusBadge tone={PRIORITY_TONE[i.priority] ?? "neutral"} showIcon={false}>{i.priority}</StatusBadge>
                      <span className="text-xs text-muted-foreground">{INITIATIVE_STATUS_LABELS[i.status] ?? i.status}</span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
