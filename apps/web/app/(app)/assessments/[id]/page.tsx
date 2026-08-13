import Link from "next/link";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  getAssessment,
  getAssessmentResult,
  getAssessmentLineage,
  saveAssessmentResponse,
  submitAssessment,
  cancelAssessment,
  createAssessment,
} from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { STATUS_LABELS, BAND_TONE, formatScore } from "../values";
import { AssessmentWizard, type FlatQuestion } from "./assessment-wizard";
import { CancelAssessmentButton, type CancelAssessmentState } from "./cancel-assessment-button";
import { ReassessButton, type ReassessState } from "./reassess-button";

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

export default async function AssessmentDetailPage({ params }: { params: Promise<{ id: string }> }) {
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

  const isFinal = assessment.status === "Completed" || assessment.status === "Superseded";
  const isEditable = assessment.status === "Draft" || assessment.status === "InProgress";

  async function saveResponseAction(
    questionId: string,
    payload: {
      answerState: string;
      selectedMaturityLevelId: string | null;
      respondentComment: string;
      confidence?: string;
      evidenceReferences: string[];
    }
  ): Promise<{ error: string | null }> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const result = await saveAssessmentResponse(token, tenantId, id, questionId, payload);
    if (!result.ok) {
      return { error: result.message ?? `Save failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/assessments/${id}`);
    return { error: null };
  }

  async function submitAction(): Promise<{ error: string | null }> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const result = await submitAssessment(token, tenantId, id);
    if (!result.ok) {
      return { error: result.message ?? `Submit failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/assessments/${id}`);
    revalidatePath("/assessments");
    return { error: null };
  }

  async function cancelAction(_prevState: CancelAssessmentState, _formData: FormData): Promise<CancelAssessmentState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const result = await cancelAssessment(token, tenantId, id);
    if (!result.ok) {
      return { error: result.message ?? `Cancel failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/assessments");
    redirect("/assessments");
  }

  async function reassessAction(_prevState: ReassessState, _formData: FormData): Promise<ReassessState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const result = await createAssessment(token, tenantId, {
      organizationId: assessment.organizationId,
      supersedesAssessmentId: id,
    });
    if (!result.ok) {
      return { error: result.message ?? `Could not start reassessment: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath("/assessments");
    redirect(`/assessments/${result.data.id}`);
  }

  const flatQuestions: FlatQuestion[] = assessment.dimensions.flatMap((dimension) =>
    dimension.capabilities.flatMap((capability) =>
      capability.questions.map((question) => ({
        id: question.id,
        code: question.code,
        text: question.text,
        dimensionCode: dimension.code,
        dimensionName: dimension.name,
        fundamentalQuestion: dimension.fundamentalQuestion,
        capabilityCode: capability.code,
        capabilityName: capability.name,
        evidenceGuidance: capability.evidenceGuidance,
        response: question.response,
      }))
    )
  );

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <Link href="/assessments" className="text-xs text-muted-foreground hover:underline">
          ← Assessments
        </Link>
        <div className="mt-1 flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-semibold">{assessment.organizationName ?? "Assessment"}</h1>
          <Badge variant="outline">{STATUS_LABELS[assessment.status] ?? assessment.status}</Badge>
        </div>
        <p className="text-sm text-muted-foreground">{assessment.frameworkVersionLabel}</p>
        {(assessment.supersedesAssessmentId || assessment.supersededByAssessmentId) && (
          <p className="mt-1 flex flex-wrap gap-3 text-xs text-muted-foreground">
            {assessment.supersedesAssessmentId && (
              <Link href={`/assessments/${assessment.supersedesAssessmentId}`} className="hover:underline">
                ← Reassessment of prior assessment
              </Link>
            )}
            {assessment.supersededByAssessmentId && (
              <Link href={`/assessments/${assessment.supersededByAssessmentId}`} className="hover:underline">
                Superseded by newer assessment →
              </Link>
            )}
          </p>
        )}
      </div>

      <LineageSection accessToken={accessToken} tenantId={tenantId} assessmentId={id} />

      {isFinal ? (
        <>
          <div className="flex flex-wrap items-center gap-3">
            <Link href={`/assessments/${id}/report`} className="text-sm underline-offset-2 hover:underline">
              View executive report →
            </Link>
            <Link href={`/assessments/${id}/trend`} className="text-sm underline-offset-2 hover:underline">
              View maturity trend →
            </Link>
          </div>
          {assessment.status === "Completed" && !assessment.supersededByAssessmentId && (
            <ReassessButton action={reassessAction} />
          )}
          <AssessmentResultView accessToken={accessToken} tenantId={tenantId} assessmentId={id} />
        </>
      ) : (
        <>
          {!isEditable && (
            <p className="rounded-md border bg-muted px-3 py-2 text-sm text-muted-foreground">
              This assessment has been submitted and is no longer editable.
            </p>
          )}
          {isEditable && (
            <CancelAssessmentButton
              action={cancelAction}
              supersedesLabel={assessment.supersedesAssessmentId ? "the prior assessment" : null}
            />
          )}
          <AssessmentWizard
            questions={flatQuestions}
            maturityLevels={assessment.maturityLevels}
            saveAction={saveResponseAction}
            submitAction={submitAction}
            readOnly={!isEditable}
          />
        </>
      )}
    </div>
  );
}

async function LineageSection({
  accessToken,
  tenantId,
  assessmentId,
}: {
  accessToken: string;
  tenantId: string;
  assessmentId: string;
}) {
  const lineageResult = await getAssessmentLineage(accessToken, tenantId, assessmentId);
  if (!lineageResult.ok || lineageResult.data.length < 2) return null;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Assessment history</CardTitle>
        <CardDescription>This assessment&apos;s full reassessment chain, oldest first.</CardDescription>
      </CardHeader>
      <CardContent>
        <ul className="flex flex-col gap-1">
          {lineageResult.data.map((entry) => (
            <li key={entry.id}>
              <Link
                href={`/assessments/${entry.id}`}
                className={`flex items-center justify-between gap-3 rounded-md px-3 py-2 text-sm hover:bg-muted/70 ${
                  entry.isCurrent ? "bg-muted ring-1 ring-primary/40" : "bg-muted/40"
                }`}
              >
                <span>
                  {entry.completedAtUtc
                    ? new Date(entry.completedAtUtc).toLocaleDateString()
                    : entry.createdAtUtc
                      ? `Started ${new Date(entry.createdAtUtc).toLocaleDateString()}`
                      : "—"}
                  {entry.isCurrent && " (this assessment)"}
                </span>
                <span className="flex items-center gap-2 text-xs text-muted-foreground">
                  {formatScore(entry.compositeAverage)}
                  <Badge variant="outline">{STATUS_LABELS[entry.status] ?? entry.status}</Badge>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
}

async function AssessmentResultView({
  accessToken,
  tenantId,
  assessmentId,
}: {
  accessToken: string;
  tenantId: string;
  assessmentId: string;
}) {
  const resultData = await getAssessmentResult(accessToken, tenantId, assessmentId);
  if (!resultData.ok) {
    return (
      <p className="text-sm text-destructive">
        GET .../assessments/{assessmentId}/result returned {resultData.status ?? "a network error"}.
      </p>
    );
  }
  const result = resultData.data;
  const capabilitiesByDimension = new Map<string, typeof result.capabilityScores>();
  for (const c of result.capabilityScores) {
    const list = capabilitiesByDimension.get(c.dimensionId) ?? [];
    list.push(c);
    capabilitiesByDimension.set(c.dimensionId, list);
  }

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>OIQ Profile</CardTitle>
          <CardDescription>
            The 11-dimension profile below is the primary result. Composite average ({formatScore(result.compositeAverage)}) is
            secondary/internal reference only.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4">
            {result.dimensionScores.map((d) => (
              <div key={d.dimensionId} className="rounded-md border p-3">
                <p className="font-mono text-xs text-muted-foreground">{d.code}</p>
                <p className="text-sm font-medium">{d.name}</p>
                <div className="mt-2 flex items-center gap-2">
                  <span className="text-xl font-semibold tabular-nums">{formatScore(d.score)}</span>
                  {d.maturityBand && <Badge variant={BAND_TONE[d.maturityBand] ?? "outline"}>{d.maturityBand}</Badge>}
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Capability breakdown</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {result.dimensionScores.map((d) => (
            <div key={d.dimensionId}>
              <p className="mb-1 text-xs font-medium text-muted-foreground">{d.name}</p>
              <ul className="flex flex-col gap-1">
                {(capabilitiesByDimension.get(d.dimensionId) ?? []).map((c) => (
                  <li key={c.capabilityId} className="flex items-center justify-between rounded-md bg-muted px-3 py-1.5 text-sm">
                    <span>
                      <span className="mr-2 font-mono text-xs text-muted-foreground">{c.code}</span>
                      {c.name}
                    </span>
                    <span className="tabular-nums text-muted-foreground">
                      {formatScore(c.score)}
                      {c.answeredQuestionCount === 0 && " (insufficient basis)"}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}
