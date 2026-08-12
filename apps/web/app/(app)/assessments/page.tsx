import Link from "next/link";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import { getMyMemberships, listOrganizations, listAssessments, createAssessment } from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { STATUS_LABELS } from "./values";
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
          OIQ assessments for <Badge variant="outline">{tenantName}</Badge>.
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
            <div className="overflow-x-auto">
              <div className="min-w-[640px]">
                <div className="grid grid-cols-[1.4fr_1fr_0.9fr_0.8fr_0.9fr] gap-2 border-b px-3 py-2 text-xs font-medium text-muted-foreground">
                  <span>Organization</span>
                  <span>Framework</span>
                  <span>Status</span>
                  <span>Progress</span>
                  <span>Started</span>
                </div>
                <ul>
                  {assessments.map((a) => (
                    <li key={a.id}>
                      <Link
                        href={`/assessments/${a.id}`}
                        className="grid grid-cols-[1.4fr_1fr_0.9fr_0.8fr_0.9fr] items-center gap-2 border-b px-3 py-2.5 text-sm hover:bg-muted"
                      >
                        <span className="truncate">{a.organizationName ?? "—"}</span>
                        <span className="truncate text-muted-foreground">{a.frameworkVersionLabel}</span>
                        <span className="text-muted-foreground">{STATUS_LABELS[a.status] ?? a.status}</span>
                        <span className="tabular-nums text-muted-foreground">{a.answeredCount}/{a.totalCount}</span>
                        <span className="text-muted-foreground">{new Date(a.createdAtUtc).toLocaleDateString()}</span>
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
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
