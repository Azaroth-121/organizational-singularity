import Link from "next/link";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  getAssessment,
  getAssessmentLineage,
  listIntelligenceDebtFindings,
  type ApiAssessmentLineageEntry,
} from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { formatScore } from "../../values";

const NON_OPEN_FINDING_STATUSES = new Set(["Rejected", "Validated"]);

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

function Delta({ current, previous }: { current: number | null; previous: number | null }) {
  if (current === null || previous === null) return <span className="text-muted-foreground">—</span>;
  const diff = current - previous;
  if (Math.abs(diff) < 0.005) return <span className="text-muted-foreground">flat</span>;
  const up = diff > 0;
  return (
    <span className={up ? "text-emerald-600 dark:text-emerald-400" : "text-destructive"}>
      {up ? "▲" : "▼"} {Math.abs(diff).toFixed(2)}
    </span>
  );
}

/** Hand-rolled inline sparkline -- the web app has no chart dependency, and a single
 * series per dimension doesn't justify adding one. */
function Sparkline({ values }: { values: (number | null)[] }) {
  const width = 160;
  const height = 32;
  const padding = 3;
  const scoreable = values.filter((v): v is number => v !== null);
  if (scoreable.length < 2) return <span className="text-xs text-muted-foreground">Not enough data yet</span>;

  const min = 1;
  const max = 5;
  const step = (width - padding * 2) / (values.length - 1);
  const points = values
    .map((v, i) => {
      if (v === null) return null;
      const x = padding + i * step;
      const y = height - padding - ((v - min) / (max - min)) * (height - padding * 2);
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .filter((p): p is string => p !== null);

  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`} className="text-primary">
      <polyline points={points.join(" ")} fill="none" stroke="currentColor" strokeWidth={1.5} />
      {values.map((v, i) => {
        if (v === null) return null;
        const x = padding + i * step;
        const y = height - padding - ((v - min) / (max - min)) * (height - padding * 2);
        const isLast = i === values.length - 1 || values.slice(i + 1).every((x2) => x2 === null);
        return <circle key={i} cx={x} cy={y} r={isLast ? 2.5 : 1.5} fill="currentColor" />;
      })}
    </svg>
  );
}

export default async function AssessmentTrendPage({ params }: { params: Promise<{ id: string }> }) {
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

  const [lineageResult, findingsResult] = await Promise.all([
    getAssessmentLineage(accessToken, tenantId, id),
    listIntelligenceDebtFindings(accessToken, tenantId),
  ]);

  if (!lineageResult.ok) {
    return (
      <Placeholder title="Could not load history">
        GET .../assessments/{id}/lineage returned {lineageResult.status ?? "a network error"}.
      </Placeholder>
    );
  }

  const chain = lineageResult.data;
  const currentIndex = chain.findIndex((e) => e.isCurrent);
  const current = chain[currentIndex] ?? chain[chain.length - 1];
  const previous: ApiAssessmentLineageEntry | undefined = currentIndex > 0 ? chain[currentIndex - 1] : undefined;

  const findings = findingsResult.ok ? findingsResult.data : [];
  const detectedInCurrent = findings.filter((f) => f.assessmentId === current.id).length;
  const stillOpenFromPrevious = previous
    ? findings.filter((f) => f.assessmentId === previous.id && !NON_OPEN_FINDING_STATUSES.has(f.status)).length
    : null;

  const scoreableChain = chain.filter((e) => e.dimensionScores !== null);
  const dimensionOrder = (scoreableChain[scoreableChain.length - 1] ?? scoreableChain[0])?.dimensionScores ?? [];

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <Link href={`/assessments/${id}`} className="text-xs text-muted-foreground hover:underline">
          ← Assessment
        </Link>
        <h1 className="mt-1 text-2xl font-semibold">Maturity Trend</h1>
        <p className="text-sm text-muted-foreground">
          {assessment.organizationName} · {assessment.frameworkVersionLabel}
        </p>
      </div>

      {!previous ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Current maturity scores</CardTitle>
            <CardDescription>No prior assessment to compare against yet.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
              {(current.dimensionScores ?? []).map((d) => (
                <div key={d.dimensionId} className="rounded-md border p-3">
                  <p className="font-mono text-xs text-muted-foreground">{d.code}</p>
                  <p className="text-sm font-medium">{d.name}</p>
                  <span className="text-xl font-semibold tabular-nums">{formatScore(d.score)}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      ) : (
        <>
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Comparison to prior assessment</CardTitle>
              <CardDescription>
                {previous.completedAtUtc && new Date(previous.completedAtUtc).toLocaleDateString()} →{" "}
                {current.completedAtUtc && new Date(current.completedAtUtc).toLocaleDateString()}
              </CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-2">
                <div className="rounded-md border p-3 text-center">
                  <p className="text-lg font-semibold tabular-nums">{detectedInCurrent}</p>
                  <p className="text-xs text-muted-foreground">Findings detected in this assessment</p>
                </div>
                <div className="rounded-md border p-3 text-center">
                  <p className="text-lg font-semibold tabular-nums">{stillOpenFromPrevious}</p>
                  <p className="text-xs text-muted-foreground">Prior findings still open</p>
                </div>
              </div>
              <ul className="flex flex-col gap-1">
                {(current.dimensionScores ?? []).map((d) => {
                  const prev = previous.dimensionScores?.find((p) => p.dimensionId === d.dimensionId) ?? null;
                  return (
                    <li key={d.dimensionId} className="flex items-center justify-between rounded-md bg-muted px-3 py-1.5 text-sm">
                      <span>
                        <span className="mr-2 font-mono text-xs text-muted-foreground">{d.code}</span>
                        {d.name}
                      </span>
                      <span className="flex items-center gap-3 tabular-nums text-muted-foreground">
                        {formatScore(prev?.score ?? null)} → {formatScore(d.score)}
                        <Delta current={d.score} previous={prev?.score ?? null} />
                      </span>
                    </li>
                  );
                })}
              </ul>
            </CardContent>
          </Card>
        </>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Maturity trend by dimension</CardTitle>
          <CardDescription>Every completed assessment in this chain, oldest to newest.</CardDescription>
        </CardHeader>
        <CardContent>
          {scoreableChain.length < 2 ? (
            <p className="text-sm text-muted-foreground">Not enough completed assessments yet to chart a trend.</p>
          ) : (
            <ul className="flex flex-col gap-2">
              {dimensionOrder.map((dRef) => {
                const values = scoreableChain.map(
                  (e) => e.dimensionScores?.find((d) => d.dimensionId === dRef.dimensionId)?.score ?? null
                );
                return (
                  <li key={dRef.dimensionId} className="flex items-center justify-between gap-4 rounded-md bg-muted px-3 py-2 text-sm">
                    <span className="min-w-0 truncate">
                      <span className="mr-2 font-mono text-xs text-muted-foreground">{dRef.code}</span>
                      {dRef.name}
                    </span>
                    <span className="flex shrink-0 items-center gap-3">
                      <Sparkline values={values} />
                      <Badge variant="outline" className="tabular-nums">
                        {formatScore(values[values.length - 1])}
                      </Badge>
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
