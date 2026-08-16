import Link from "next/link";
import { AlertTriangle, Building2, ClipboardList } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { StatTile } from "@/components/ui/stat-tile";
import { StatusBadge } from "@/components/ui/status-badge";
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
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        <StatTile label="Organizations" value={organizationCount} icon={Building2} href="/organizations" />
        <StatTile
          label="Open findings (tenant-wide)"
          value={openFindingCount}
          icon={AlertTriangle}
          tone={openFindingCount > 0 ? "warning" : "good"}
          href="/intelligence-debt"
        />
        <StatTile label="Assigned to you" value={myFindings.length} icon={ClipboardList} tone={myFindings.length > 0 ? "warning" : "good"} />
      </div>

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
                className="flex items-center justify-between gap-3 rounded-md border p-3 text-sm transition-colors hover:border-primary/40 hover:bg-muted/40"
              >
                <span className="truncate">
                  <span className="mr-2 font-mono text-xs text-muted-foreground">{f.code}</span>
                  {f.title}
                </span>
                <span className="flex shrink-0 items-center gap-2">
                  <StatusBadge tone={SEVERITY_TONE[f.severity] ?? "neutral"}>{f.severity}</StatusBadge>
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
                className="flex items-center justify-between gap-3 rounded-md border p-3 text-sm transition-colors hover:border-primary/40 hover:bg-muted/40"
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
