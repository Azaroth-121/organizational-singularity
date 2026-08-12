import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { ApiOrganization, ApiAssessmentSummary, ApiDimensionScore, ApiIntelligenceDebtSummary } from "@/lib/api";
import { STATUS_LABELS as ASSESSMENT_STATUS_LABELS } from "./assessments/values";
import { SEVERITY_TONE } from "./intelligence-debt/values";

const BAND_DOT_COLOR: Record<string, string> = {
  Fragmented: "bg-destructive",
  Emerging: "bg-destructive/50",
  Defined: "bg-muted-foreground/50",
  Integrated: "bg-primary/60",
  Adaptive: "bg-primary",
};

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
  const severityCounts = Object.fromEntries(
    OPEN_FINDING_SEVERITIES.map((s) => [s, openFindings.filter((f) => f.severity === s).length])
  );

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {[
          ["Organizations", organizations.length],
          ["Open findings", openFindings.length],
          ["Awaiting review", detectedCount],
          ["Team members", memberCount],
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
          <CardTitle>Organizations</CardTitle>
          <CardDescription>Latest assessment status and OIQ dimension bands, at a glance.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          {organizations.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No organizations yet — <Link href="/organizations" className="underline">create one</Link> to get started.
            </p>
          ) : (
            organizations.map((org) => {
              const latest = latestAssessmentByOrg.get(org.id);
              const dims = latest ? dimensionScoresByAssessment.get(latest.id) : undefined;
              return (
                <Link
                  key={org.id}
                  href={latest ? `/assessments/${latest.id}` : "/assessments"}
                  className="flex items-center justify-between gap-3 rounded-md border p-3 text-sm hover:bg-muted"
                >
                  <div>
                    <p className="font-medium">{org.name}</p>
                    <p className="text-xs text-muted-foreground">
                      {latest ? ASSESSMENT_STATUS_LABELS[latest.status] ?? latest.status : "No assessment yet"}
                      {latest && latest.status !== "Completed" && latest.status !== "Superseded" && (
                        <span className="tabular-nums"> · {latest.answeredCount}/{latest.totalCount}</span>
                      )}
                    </p>
                  </div>
                  {dims && dims.length > 0 && (
                    <div className="flex gap-1" title="OIQ dimension bands">
                      {dims.map((d) => (
                        <span
                          key={d.dimensionId}
                          title={`${d.name}: ${d.maturityBand ?? "insufficient basis"}`}
                          className={cn(
                            "h-2.5 w-2.5 rounded-full",
                            d.maturityBand ? BAND_DOT_COLOR[d.maturityBand] : "bg-border"
                          )}
                        />
                      ))}
                    </div>
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
              <div key={severity} className="rounded-md border p-2 text-center">
                <p className="text-lg font-semibold tabular-nums">{severityCounts[severity]}</p>
                <Badge variant={SEVERITY_TONE[severity] ?? "outline"} className="mt-1">{severity}</Badge>
              </div>
            ))}
          </div>
          {detectedCount > 0 && (
            <Link href="/intelligence-debt" className="text-sm text-muted-foreground hover:underline">
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
