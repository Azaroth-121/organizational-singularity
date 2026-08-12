import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { ApiAssessmentSummary, ApiIntelligenceDebtSummary } from "@/lib/api";
import { STATUS_LABELS as ASSESSMENT_STATUS_LABELS } from "./assessments/values";
import { STATUS_LABELS as FINDING_STATUS_LABELS, SEVERITY_TONE } from "./intelligence-debt/values";

export function MemberDashboard({
  myFindings,
  inProgressAssessments,
  organizationCount,
  openFindingCount,
}: {
  myFindings: ApiIntelligenceDebtSummary[];
  inProgressAssessments: ApiAssessmentSummary[];
  organizationCount: number;
  openFindingCount: number;
}) {
  return (
    <div className="flex flex-col gap-6">
      <p className="text-sm text-muted-foreground">
        {organizationCount} organization{organizationCount === 1 ? "" : "s"} · {openFindingCount} open finding{openFindingCount === 1 ? "" : "s"} tenant-wide
      </p>

      <Card>
        <CardHeader>
          <CardTitle>Your findings</CardTitle>
          <CardDescription>Intelligence Debt findings assigned to you.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          {myFindings.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nothing assigned to you right now.</p>
          ) : (
            myFindings.map((f) => (
              <Link
                key={f.id}
                href={`/intelligence-debt/${f.id}`}
                className="flex items-center justify-between gap-3 rounded-md border p-3 text-sm hover:bg-muted"
              >
                <span className="truncate">
                  <span className="mr-2 font-mono text-xs text-muted-foreground">{f.code}</span>
                  {f.title}
                </span>
                <span className="flex shrink-0 items-center gap-2">
                  <Badge variant={SEVERITY_TONE[f.severity] ?? "outline"}>{f.severity}</Badge>
                  <span className="text-xs text-muted-foreground">{FINDING_STATUS_LABELS[f.status] ?? f.status}</span>
                </span>
              </Link>
            ))
          )}
          <Link href="/intelligence-debt" className="self-start text-xs text-muted-foreground underline-offset-2 hover:underline">
            View full register
          </Link>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Assessments in progress</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          {inProgressAssessments.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nothing in progress right now.</p>
          ) : (
            inProgressAssessments.map((a) => (
              <Link
                key={a.id}
                href={`/assessments/${a.id}`}
                className="flex items-center justify-between gap-3 rounded-md border p-3 text-sm hover:bg-muted"
              >
                <span>{a.organizationName ?? "Assessment"}</span>
                <span className="flex shrink-0 items-center gap-2 text-xs text-muted-foreground">
                  {ASSESSMENT_STATUS_LABELS[a.status] ?? a.status}
                  <span className="tabular-nums">{a.answeredCount}/{a.totalCount}</span>
                </span>
              </Link>
            ))
          )}
          <Link href="/assessments" className="self-start text-xs text-muted-foreground underline-offset-2 hover:underline">
            View all assessments
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
