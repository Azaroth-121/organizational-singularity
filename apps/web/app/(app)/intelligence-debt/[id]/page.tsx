import Link from "next/link";
import { revalidatePath } from "next/cache";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  listTenantMembers,
  listIntelligenceDebtFindings,
  getIntelligenceDebtFinding,
  updateIntelligenceDebtFinding,
  transitionIntelligenceDebtFinding,
  reviewIntelligenceDebtFinding,
  getIntelligenceDebtHistory,
  addIntelligenceDebtEvidence,
  addIntelligenceDebtDependency,
  removeIntelligenceDebtDependency,
  listInitiatives,
  createInitiative,
} from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { StatusBadge } from "@/components/ui/status-badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ADMIN_TIER_ROLES } from "../../members/roles";
import { CATEGORY_LABELS, STATUS_LABELS, SEVERITY_TONE, FINDING_STATUS_TONE, ALLOWED_TRANSITIONS } from "../values";
import { TransitionActions, type TransitionState } from "./transition-actions";
import { ReviewPanel, type ReviewState } from "./review-panel";
import { EvidenceForm, type EvidenceFormState } from "./evidence-form";
import { AddDependencyForm, RemoveDependencyButton, type DependencyFormState } from "./dependency-form";
import { EditFindingForm, type EditFindingState } from "./edit-finding-form";
import { ConvertToInitiativeForm, type ConvertState } from "./convert-to-initiative-form";

const CONVERTIBLE_STATUSES = ["ApprovedFinding", "Remediation", "Validation", "Validated"];

function Placeholder({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">{children}</p>
    </div>
  );
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium text-muted-foreground">{label}</dt>
      <dd className="text-sm">{value || <span className="text-muted-foreground">—</span>}</dd>
    </div>
  );
}

export default async function IntelligenceDebtDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id: findingId } = await params;
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
  const isAdminTier = ADMIN_TIER_ROLES.has(myMembership.role);

  const findingResult = await getIntelligenceDebtFinding(accessToken, tenantId, findingId);
  if (!findingResult.ok) {
    return (
      <Placeholder title="Finding not found">
        GET .../intelligence-debt/{findingId} returned {findingResult.status ?? "a network error"}.
      </Placeholder>
    );
  }
  const finding = findingResult.data;

  const membersResult = await listTenantMembers(accessToken, tenantId);
  const members = membersResult.ok ? membersResult.data : [];

  const historyResult = await getIntelligenceDebtHistory(accessToken, tenantId, findingId);
  const history = historyResult.ok ? historyResult.data : [];

  const isUnderReview = finding.status === "Detected" || finding.status === "EvidenceReviewed";

  const initiativesResult = await listInitiatives(accessToken, tenantId);
  const existingInitiative = initiativesResult.ok
    ? initiativesResult.data.find((i) => i.sourceFindingId === findingId)
    : undefined;
  const isConvertible = CONVERTIBLE_STATUSES.includes(finding.status);

  async function transitionAction(
    toStatus: string,
    _prevState: TransitionState,
    formData: FormData
  ): Promise<TransitionState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const current = await getIntelligenceDebtFinding(token, tenantId, findingId);
    if (!current.ok) return { error: "Could not load the current finding version." };

    const outcome = String(formData.get("outcome") ?? "").trim();
    const result = await transitionIntelligenceDebtFinding(token, tenantId, findingId, {
      expectedVersion: current.data.version,
      toStatus,
      outcome: outcome || undefined,
    });
    if (!result.ok) {
      return { error: result.message ?? `Transition failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/intelligence-debt/${findingId}`);
    return { error: null };
  }

  async function reviewAction(
    outcome: string,
    _prevState: ReviewState,
    formData: FormData
  ): Promise<ReviewState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const current = await getIntelligenceDebtFinding(token, tenantId, findingId);
    if (!current.ok) return { error: "Could not load the current finding version." };

    const rationale = String(formData.get("rationale") ?? "").trim();
    if (!rationale) return { error: "Rationale is required." };

    const result = await reviewIntelligenceDebtFinding(token, tenantId, findingId, {
      expectedVersion: current.data.version,
      outcome,
      rationale,
    });
    if (!result.ok) {
      return { error: result.message ?? `Review failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/intelligence-debt/${findingId}`);
    return { error: null };
  }

  async function convertToInitiativeAction(
    _prevState: ConvertState,
    formData: FormData
  ): Promise<ConvertState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const title = String(formData.get("title") ?? "").trim();
    if (!title) return { error: "Title is required." };
    const targetCompletionDate = String(formData.get("targetCompletionDate") ?? "");

    const result = await createInitiative(token, tenantId, {
      sourceFindingId: findingId,
      title,
      description: String(formData.get("description") ?? ""),
      priority: String(formData.get("priority") ?? "Medium"),
      targetCompletionDate: targetCompletionDate ? new Date(targetCompletionDate).toISOString() : undefined,
    });
    if (!result.ok) {
      return { error: result.message ?? `Convert failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/intelligence-debt/${findingId}`);
    revalidatePath("/roadmap");
    return { error: null };
  }

  async function addEvidenceAction(
    _prevState: EvidenceFormState,
    formData: FormData
  ): Promise<EvidenceFormState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const evidenceType = String(formData.get("evidenceType") ?? "");
    const description = String(formData.get("description") ?? "").trim();
    const sourceReference = String(formData.get("sourceReference") ?? "").trim();
    if (!description) return { error: "Description is required." };

    const result = await addIntelligenceDebtEvidence(token, tenantId, findingId, {
      evidenceType,
      description,
      sourceReference: sourceReference || undefined,
    });
    if (!result.ok) {
      return { error: result.message ?? `Add evidence failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/intelligence-debt/${findingId}`);
    return { error: null };
  }

  async function addDependencyAction(
    _prevState: DependencyFormState,
    formData: FormData
  ): Promise<DependencyFormState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const code = String(formData.get("dependsOnCode") ?? "").trim().toUpperCase();
    if (!code) return { error: "Enter a finding code." };

    const all = await listIntelligenceDebtFindings(token, tenantId);
    if (!all.ok) return { error: "Could not resolve finding codes." };
    const target = all.data.find((f) => f.code.toUpperCase() === code);
    if (!target) return { error: `No finding with code ${code} in this tenant.` };

    const result = await addIntelligenceDebtDependency(token, tenantId, findingId, target.id);
    if (!result.ok) {
      return { error: result.message ?? `Add dependency failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/intelligence-debt/${findingId}`);
    return { error: null };
  }

  async function removeDependencyAction(
    dependencyId: string,
    _prevState: DependencyFormState,
    _formData: FormData
  ): Promise<DependencyFormState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const result = await removeIntelligenceDebtDependency(token, tenantId, findingId, dependencyId);
    if (!result.ok) {
      return { error: result.message ?? `Remove failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/intelligence-debt/${findingId}`);
    return { error: null };
  }

  async function updateFindingAction(
    _prevState: EditFindingState,
    formData: FormData
  ): Promise<EditFindingState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in.", success: null };

    const targetDate = String(formData.get("targetResolutionDate") ?? "");
    const result = await updateIntelligenceDebtFinding(token, tenantId, findingId, {
      expectedVersion: Number(formData.get("expectedVersion")),
      title: String(formData.get("title") ?? "").trim(),
      description: String(formData.get("description") ?? ""),
      category: String(formData.get("category") ?? ""),
      severity: String(formData.get("severity") ?? ""),
      businessImpact: String(formData.get("businessImpact") ?? "") || undefined,
      affectedScope: String(formData.get("affectedScope") ?? "") || undefined,
      ownerUserId: String(formData.get("ownerUserId") ?? "") || null,
      targetResolutionDate: targetDate ? new Date(targetDate).toISOString() : null,
      recommendedAction: String(formData.get("recommendedAction") ?? "") || undefined,
      remediationPlan: String(formData.get("remediationPlan") ?? "") || undefined,
      validationCriteria: String(formData.get("validationCriteria") ?? "") || undefined,
    });
    if (!result.ok) {
      return {
        error: result.message ?? `Save failed: API returned ${result.status ?? "a network error"}.`,
        success: null,
      };
    }

    revalidatePath(`/intelligence-debt/${findingId}`);
    return { error: null, success: "Saved." };
  }

  const allowedTransitions = ALLOWED_TRANSITIONS[finding.status] ?? [];

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <Link href="/intelligence-debt" className="text-xs text-muted-foreground hover:underline">
          ← Register
        </Link>
        <div className="mt-1 flex flex-wrap items-center gap-2">
          <span className="font-mono text-sm text-muted-foreground">{finding.code}</span>
          <h1 className="text-2xl font-semibold">{finding.title}</h1>
        </div>
        <div className="mt-2 flex flex-wrap items-center gap-2">
          <StatusBadge tone={SEVERITY_TONE[finding.severity] ?? "neutral"}>{finding.severity}</StatusBadge>
          <StatusBadge tone={FINDING_STATUS_TONE[finding.status] ?? "neutral"} showIcon={false}>
            {STATUS_LABELS[finding.status] ?? finding.status}
          </StatusBadge>
          <Badge variant="outline">{CATEGORY_LABELS[finding.category] ?? finding.category}</Badge>
          <span className="text-xs text-muted-foreground">via {finding.detectionSource}</span>
        </div>
      </div>

      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="evidence">Evidence &amp; Roadmap</TabsTrigger>
          <TabsTrigger value="history">History</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="flex flex-col gap-6 pt-4">
      {isAdminTier && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{isUnderReview ? "Review" : "Status"}</CardTitle>
            <CardDescription>
              {isUnderReview
                ? "Only admin-tier roles can review candidate findings."
                : allowedTransitions.length === 0
                  ? "This finding is in a terminal state."
                  : "Only admin-tier roles can change status."}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {isUnderReview ? (
              <ReviewPanel action={reviewAction} />
            ) : (
              <TransitionActions allowedTransitions={allowedTransitions} action={transitionAction} />
            )}
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Finding</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <Field label="Description" value={finding.description} />
            <Field label="Owner" value={finding.ownerName} />
            <Field
              label="Target resolution"
              value={finding.targetResolutionDate ? new Date(finding.targetResolutionDate).toLocaleDateString() : null}
            />
            <Field label="Organization" value={finding.organizationName} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Impact</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <Field label="Business impact" value={finding.businessImpact} />
            <Field label="Affected scope" value={finding.affectedScope} />
            <Field label="Related OIQ dimension" value={finding.dimensionName} />
            <Field label="Related capability" value={finding.capabilityName} />
            <Field label="Originating assessment" value={finding.assessmentId} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Remediation &amp; validation</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <Field label="Recommended action" value={finding.recommendedAction} />
            <Field label="Remediation plan" value={finding.remediationPlan} />
            <Field label="Validation criteria" value={finding.validationCriteria} />
            <Field label="Outcome" value={finding.outcome} />
          </CardContent>
        </Card>

        {finding.detection && (
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Why this was detected</CardTitle>
              <CardDescription>The exact rule and observed score that produced this finding.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <Field
                label="Observed score vs. threshold"
                value={`${finding.detection.observedScore.toFixed(2)} (threshold ${finding.detection.thresholdUsed.toFixed(2)})`}
              />
              <Field label="Maturity band" value={finding.detection.maturityBand} />
              <Field label="Detected" value={new Date(finding.detection.detectedAtUtc).toLocaleString()} />
            </CardContent>
          </Card>
        )}

        <Card>
          <CardHeader>
            <CardTitle className="text-base">History</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <Field label="Created" value={`${new Date(finding.createdAtUtc).toLocaleString()} · ${finding.createdByName ?? ""}`} />
            <Field
              label="Approved"
              value={finding.approvedAtUtc ? `${new Date(finding.approvedAtUtc).toLocaleString()} · ${finding.approvedByName ?? ""}` : null}
            />
            <Field
              label="Remediation started"
              value={finding.remediationStartedAtUtc ? new Date(finding.remediationStartedAtUtc).toLocaleString() : null}
            />
            <Field label="Resolved (entered validation)" value={finding.resolvedAtUtc ? new Date(finding.resolvedAtUtc).toLocaleString() : null} />
            <Field
              label="Validated"
              value={finding.validatedAtUtc ? `${new Date(finding.validatedAtUtc).toLocaleString()} · ${finding.validatedByName ?? ""}` : null}
            />
          </CardContent>
        </Card>
      </div>
        </TabsContent>

        <TabsContent value="evidence" className="flex flex-col gap-6 pt-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Evidence</CardTitle>
          <CardDescription>What proves this finding is real.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          {finding.evidence.length === 0 ? (
            <p className="text-sm text-muted-foreground">No evidence recorded yet.</p>
          ) : (
            <ul className="flex flex-col gap-2">
              {finding.evidence.map((e) => (
                <li key={e.id} className="rounded-md bg-muted px-3 py-2 text-sm">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline">{e.evidenceType}</Badge>
                    <span className="text-xs text-muted-foreground">
                      {e.addedByName} · {new Date(e.addedAtUtc).toLocaleDateString()}
                    </span>
                  </div>
                  <p className="mt-1">{e.description}</p>
                  {e.sourceReference && <p className="text-xs text-muted-foreground">{e.sourceReference}</p>}
                </li>
              ))}
            </ul>
          )}
          <EvidenceForm action={addEvidenceAction} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Dependencies</CardTitle>
          <CardDescription>What this finding is blocked by, and what it blocks.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div>
            <p className="mb-2 text-xs font-medium text-muted-foreground">Depends on</p>
            {finding.dependsOn.length === 0 ? (
              <p className="text-sm text-muted-foreground">None.</p>
            ) : (
              <ul className="flex flex-col gap-1">
                {finding.dependsOn.map((d) => (
                  <li key={d.dependencyId} className="flex items-center justify-between rounded-md bg-muted px-3 py-1.5 text-sm">
                    <Link href={`/intelligence-debt/${d.findingId}`} className="hover:underline">
                      <span className="font-mono text-xs text-muted-foreground">{d.code}</span> {d.title}
                    </Link>
                    {isAdminTier && (
                      <RemoveDependencyButton action={removeDependencyAction.bind(null, d.dependencyId)} />
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>
          <div>
            <p className="mb-2 text-xs font-medium text-muted-foreground">Blocks</p>
            {finding.dependedOnBy.length === 0 ? (
              <p className="text-sm text-muted-foreground">None.</p>
            ) : (
              <ul className="flex flex-col gap-1">
                {finding.dependedOnBy.map((d) => (
                  <li key={d.dependencyId} className="rounded-md bg-muted px-3 py-1.5 text-sm">
                    <Link href={`/intelligence-debt/${d.findingId}`} className="hover:underline">
                      <span className="font-mono text-xs text-muted-foreground">{d.code}</span> {d.title}
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </div>
          {isAdminTier && <AddDependencyForm action={addDependencyAction} />}
        </CardContent>
      </Card>

      {isAdminTier && isConvertible && (
        <Card className="max-w-2xl">
          <CardHeader>
            <CardTitle className="text-base">Roadmap</CardTitle>
            <CardDescription>
              {existingInitiative
                ? "This finding already has an initiative."
                : "Convert this approved finding into a roadmap initiative."}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {existingInitiative ? (
              <Link href={`/roadmap/${existingInitiative.id}`} className="text-sm hover:underline">
                <span className="mr-2 font-mono text-xs text-muted-foreground">{existingInitiative.code}</span>
                {existingInitiative.title} →
              </Link>
            ) : (
              <ConvertToInitiativeForm defaultTitle={finding.title} action={convertToInitiativeAction} />
            )}
          </CardContent>
        </Card>
      )}

      {isAdminTier && (
        <Card className="max-w-2xl">
          <CardHeader>
            <CardTitle className="text-base">Edit details</CardTitle>
          </CardHeader>
          <CardContent>
            <EditFindingForm finding={finding} members={members} action={updateFindingAction} />
          </CardContent>
        </Card>
      )}
        </TabsContent>

        <TabsContent value="history" className="pt-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Audit history</CardTitle>
        </CardHeader>
        <CardContent>
          {history.length === 0 ? (
            <p className="text-sm text-muted-foreground">No events recorded yet.</p>
          ) : (
            <ul className="flex flex-col gap-2">
              {history.map((event) => (
                <li key={event.id} className="rounded-md bg-muted px-3 py-2 text-sm">
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant="outline">{event.eventType}</Badge>
                    <span className="text-xs text-muted-foreground">
                      {event.actorName ?? "system"} · {new Date(event.occurredAtUtc).toLocaleString()}
                    </span>
                  </div>
                  {event.payload && "rationale" in event.payload && typeof event.payload.rationale === "string" && (
                    <p className="mt-1 text-xs text-muted-foreground">
                      {"outcome" in event.payload ? `${String(event.payload.outcome)}: ` : ""}
                      {event.payload.rationale}
                    </p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
