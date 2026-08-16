import Link from "next/link";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import { getMyMemberships, listOrganizations, listAssessments, createAssessment } from "@/lib/api";
import { StatusBadge } from "@/components/ui/status-badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { STATUS_LABELS, ASSESSMENT_STATUS_TONE } from "./values";
import { CreateAssessmentForm, type CreateAssessmentState } from "./create-assessment-form";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function AssessmentsPage() {
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

  const [assessmentsResult, organizationsResult] = await Promise.all([
    listAssessments(accessToken, tenantId),
    listOrganizations(accessToken, tenantId),
  ]);

  async function createAssessmentAction(
    _prevState: CreateAssessmentState,
    formData: FormData
  ): Promise<CreateAssessmentState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const organizationId = String(formData.get("organizationId") ?? "");
    if (!organizationId) return { error: "Organization is required." };

    const result = await createAssessment(token, tenantId, { organizationId });
    if (!result.ok) {
      return { error: result.message ?? `Create failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/assessments");
    redirect(`/assessments/${result.data.id}`);
  }

  const assessments = assessmentsResult.ok ? assessmentsResult.data : [];

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">Assessments</h1>
        <p className="text-sm text-muted-foreground">
          OIQ assessments for {tenantName}.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All assessments</CardTitle>
          <CardDescription>Start a new one, or resume/review an existing one below.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {!assessmentsResult.ok ? (
            <p className="text-sm text-destructive">
              GET .../assessments returned {assessmentsResult.status ?? "a network error"}.
            </p>
          ) : assessments.length === 0 ? (
            <p className="text-sm text-muted-foreground">No assessments yet — start one below.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Organization</TableHead>
                  <TableHead>Framework</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Progress</TableHead>
                  <TableHead>Started</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {assessments.map((a) => (
                  <TableRow key={a.id}>
                    <TableCell className="max-w-0">
                      <Link href={`/assessments/${a.id}`} className="block truncate hover:text-primary">
                        {a.organizationName ?? "—"}
                      </Link>
                    </TableCell>
                    <TableCell className="truncate text-muted-foreground">{a.frameworkVersionLabel}</TableCell>
                    <TableCell>
                      <StatusBadge tone={ASSESSMENT_STATUS_TONE[a.status] ?? "neutral"} showIcon={false}>
                        {STATUS_LABELS[a.status] ?? a.status}
                      </StatusBadge>
                    </TableCell>
                    <TableCell className="tabular-nums text-muted-foreground">{a.answeredCount}/{a.totalCount}</TableCell>
                    <TableCell className="text-muted-foreground">{new Date(a.createdAtUtc).toLocaleDateString()}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}

          {organizationsResult.ok && organizationsResult.data.length > 0 ? (
            <CreateAssessmentForm action={createAssessmentAction} organizations={organizationsResult.data} />
          ) : (
            <p className="border-t pt-4 text-xs text-muted-foreground">
              Create an organization first (under Organizations) before starting an assessment.
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
