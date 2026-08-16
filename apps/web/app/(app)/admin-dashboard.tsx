import Link from "next/link";
import { AlertTriangle, Building2, ClipboardCheck, Users } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { StatTile } from "@/components/ui/stat-tile";
import { StatusBadge } from "@/components/ui/status-badge";
import { OiqProfileBars } from "@/components/ui/oiq-profile-bars";
import type { ApiOrganization, ApiAssessmentSummary, ApiDimensionScore, ApiIntelligenceDebtSummary } from "@/lib/api";
import { STATUS_LABELS as ASSESSMENT_STATUS_LABELS, BAND_TONE } from "./assessments/values";
import { SEVERITY_TONE } from "./intelligence-debt/values";

const OPEN_FINDING_SEVERITIES = ["Critical", "High", "Moderate", "Low", "Informational"] as const;

export function AdminDashboard({
  organizations,
  latestAssessmentByOrg,
  dimensionScoresByAssessment,
  findings,
  memberCount,
}: {
  organizations: ApiOrganization[];
  latestAssessmentByOrg: Map<string, ApiAssessmentSummary>;
  dimensionScoresByAssessment: Map<string, ApiDimensionScore[]>;
  findings: ApiIntelligenceDebtSummary[];
  memberCount: number;
}) {
  const openFindings = findings.filter((f) => f.status !== "Validated" && f.status !== "Rejected");
  const detectedCount = findings.filter((f) => f.status === "Detected").length;
  const criticalOpen = openFindings.filter((f) => f.severity === "Critical").length;
  const severityCounts = Object.fromEntries(
    OPEN_FINDING_SEVERITIES.map((s) => [s, openFindings.filter((f) => f.severity === s).length])
  );

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile label="Organizations" value={organizations.length} icon={Building2} href="/organizations" />
        <StatTile
          label="Open findings"
          value={openFindings.length}
          icon={AlertTriangle}
          tone={criticalOpen > 0 ? "critical" : openFindings.length > 0 ? "warning" : "good"}
          href="/intelligence-debt"
        />
        <StatTile
          label="Awaiting review"
          value={detectedCount}
          icon={ClipboardCheck}
          tone={detectedCount > 0 ? "warning" : "good"}
          href="/intelligence-debt"
        />
        <StatTile label="Team members" value={memberCount} icon={Users} href="/members" />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Organizations</CardTitle>
          <CardDescription>Latest assessment status and OIQ dimension profile, at a glance.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {organizations.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No organizations yet — <Link href="/organizations" className="text-primary underline underline-offset-2">create one</Link> to get started.
            </p>
          ) : (
            organizations.map((org) => {
              const latest = latestAssessmentByOrg.get(org.id);
              const dims = latest ? dimensionScoresByAssessment.get(latest.id) : undefined;
              return (
                <Link
                  key={org.id}
                  href={latest ? `/assessments/${latest.id}` : "/assessments"}
                  className="flex flex-col gap-2 rounded-lg border p-4 text-sm transition-colors hover:border-primary/40 hover:bg-muted/40"
                >
                  <div className="flex items-center justify-between gap-3">
                    <p className="font-medium">{org.name}</p>
                    <p className="text-xs text-muted-foreground">
                      {latest ? ASSESSMENT_STATUS_LABELS[latest.status] ?? latest.status : "No assessment yet"}
                      {latest && latest.status !== "Completed" && latest.status !== "Superseded" && (
                        <span className="tabular-nums"> · {latest.answeredCount}/{latest.totalCount}</span>
                      )}
                    </p>
                  </div>
                  {dims && dims.length > 0 && (
                    <OiqProfileBars
                      bandTone={BAND_TONE}
                      rows={dims.slice(0, 4).map((d) => ({ key: d.dimensionId, code: d.code, name: d.name, score: d.score, band: d.maturityBand }))}
                    />
                  )}
                </Link>
              );
            })
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Intelligence Debt</CardTitle>
          <CardDescription>Open findings across all organizations.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <div className="grid grid-cols-5 gap-2">
            {OPEN_FINDING_SEVERITIES.map((severity) => (
              <div key={severity} className="flex flex-col items-center gap-1.5 rounded-md border p-2 text-center">
                <p className="text-lg font-semibold tabular-nums">{severityCounts[severity]}</p>
                <StatusBadge tone={SEVERITY_TONE[severity] ?? "neutral"} showIcon={false}>
                  {severity}
                </StatusBadge>
              </div>
            ))}
          </div>
          {detectedCount > 0 && (
            <Link href="/intelligence-debt" className="text-sm text-primary hover:underline">
              {detectedCount} candidate{detectedCount === 1 ? "" : "s"} awaiting review →
            </Link>
          )}
          <Link href="/intelligence-debt" className="self-start text-xs text-muted-foreground underline-offset-2 hover:underline">
            View full register
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
