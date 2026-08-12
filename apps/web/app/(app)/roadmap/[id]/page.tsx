import Link from "next/link";
import { revalidatePath } from "next/cache";
import { verifySession } from "@/lib/dal";
import { getApiAccessToken } from "@/lib/auth-token";
import {
  getMyMemberships,
  listTenantMembers,
  listInitiatives,
  getInitiative,
  updateInitiative,
  transitionInitiative,
  addInitiativeMilestone,
  updateInitiativeMilestone,
  addInitiativeDependency,
  removeInitiativeDependency,
} from "@/lib/api";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { ADMIN_TIER_ROLES } from "../../members/roles";
import { STATUS_LABELS, PRIORITY_TONE, ALLOWED_TRANSITIONS } from "../values";
import { TransitionActions, type TransitionState } from "./transition-actions";
import { MilestonePanel, type MilestoneState } from "./milestone-panel";
import { AddDependencyForm, RemoveDependencyButton, type DependencyFormState } from "./dependency-form";
import { EditInitiativeForm, type EditInitiativeState } from "./edit-initiative-form";

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

export default async function InitiativeDetailPage({ params }: { params: Promise<{ id: string }> }) {
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
  const isAdminTier = ADMIN_TIER_ROLES.has(myMembership.role);

  const initiativeResult = await getInitiative(accessToken, tenantId, id);
  if (!initiativeResult.ok) {
    return (
      <Placeholder title="Initiative not found">
        GET .../initiatives/{id} returned {initiativeResult.status ?? "a network error"}.
      </Placeholder>
    );
  }
  const initiative = initiativeResult.data;

  const membersResult = await listTenantMembers(accessToken, tenantId);
  const members = membersResult.ok ? membersResult.data : [];

  async function transitionAction(
    toStatus: string,
    _prevState: TransitionState,
    _formData: FormData
  ): Promise<TransitionState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const current = await getInitiative(token, tenantId, id);
    if (!current.ok) return { error: "Could not load the current initiative version." };

    const result = await transitionInitiative(token, tenantId, id, {
      expectedVersion: current.data.version,
      toStatus,
    });
    if (!result.ok) {
      return { error: result.message ?? `Transition failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/roadmap/${id}`);
    revalidatePath("/roadmap");
    return { error: null };
  }

  async function addMilestoneAction(
    _prevState: MilestoneState,
    formData: FormData
  ): Promise<MilestoneState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const title = String(formData.get("title") ?? "").trim();
    if (!title) return { error: "Title is required." };
    const dueDate = String(formData.get("dueDate") ?? "");

    const result = await addInitiativeMilestone(token, tenantId, id, {
      title,
      dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
    });
    if (!result.ok) {
      return { error: result.message ?? `Add milestone failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/roadmap/${id}`);
    return { error: null };
  }

  async function toggleMilestoneAction(
    milestoneId: string,
    isDone: boolean,
    _prevState: MilestoneState,
    _formData: FormData
  ): Promise<MilestoneState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in." };

    const current = await getInitiative(token, tenantId, id);
    if (!current.ok) return { error: "Could not load the current initiative." };
    const milestone = current.data.milestones.find((m) => m.id === milestoneId);
    if (!milestone) return { error: "Milestone not found." };

    const result = await updateInitiativeMilestone(token, tenantId, id, milestoneId, {
      title: milestone.title,
      dueDate: milestone.dueDate,
      isDone,
    });
    if (!result.ok) {
      return { error: result.message ?? `Update failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/roadmap/${id}`);
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
    if (!code) return { error: "Enter an initiative code." };

    const all = await listInitiatives(token, tenantId);
    if (!all.ok) return { error: "Could not resolve initiative codes." };
    const target = all.data.find((i) => i.code.toUpperCase() === code);
    if (!target) return { error: `No initiative with code ${code} in this tenant.` };

    const result = await addInitiativeDependency(token, tenantId, id, target.id);
    if (!result.ok) {
      return { error: result.message ?? `Add dependency failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/roadmap/${id}`);
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

    const result = await removeInitiativeDependency(token, tenantId, id, dependencyId);
    if (!result.ok) {
      return { error: result.message ?? `Remove failed: API returned ${result.status ?? "a network error"}.` };
    }

    revalidatePath(`/roadmap/${id}`);
    return { error: null };
  }

  async function updateInitiativeAction(
    _prevState: EditInitiativeState,
    formData: FormData
  ): Promise<EditInitiativeState> {
    "use server";
    const token = await getApiAccessToken();
    if (!token) return { error: "No API access token available. Sign out and back in.", success: null };

    const startDate = String(formData.get("targetStartDate") ?? "");
    const completionDate = String(formData.get("targetCompletionDate") ?? "");
    const result = await updateInitiative(token, tenantId, id, {
      expectedVersion: Number(formData.get("expectedVersion")),
      title: String(formData.get("title") ?? "").trim(),
      description: String(formData.get("description") ?? ""),
      priority: String(formData.get("priority") ?? ""),
      expectedOutcome: String(formData.get("expectedOutcome") ?? "") || undefined,
      ownerUserId: String(formData.get("ownerUserId") ?? "") || null,
      targetStartDate: startDate ? new Date(startDate).toISOString() : null,
      targetCompletionDate: completionDate ? new Date(completionDate).toISOString() : null,
    });
    if (!result.ok) {
      return {
        error: result.message ?? `Save failed: API returned ${result.status ?? "a network error"}.`,
        success: null,
      };
    }

    revalidatePath(`/roadmap/${id}`);
    return { error: null, success: "Saved." };
  }

  const allowedTransitions = ALLOWED_TRANSITIONS[initiative.status] ?? [];

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div>
        <Link href="/roadmap" className="text-xs text-muted-foreground hover:underline">
          ← Roadmap
        </Link>
        <div className="mt-1 flex flex-wrap items-center gap-2">
          <span className="font-mono text-sm text-muted-foreground">{initiative.code}</span>
          <h1 className="text-2xl font-semibold">{initiative.title}</h1>
        </div>
        <div className="mt-2 flex flex-wrap items-center gap-2">
          <Badge variant={PRIORITY_TONE[initiative.priority] ?? "outline"}>{initiative.priority}</Badge>
          <Badge variant="outline">{STATUS_LABELS[initiative.status] ?? initiative.status}</Badge>
          {initiative.sourceFindingCode && (
            <Link href={`/intelligence-debt/${initiative.sourceFindingId}`} className="text-xs text-muted-foreground hover:underline">
              from {initiative.sourceFindingCode}
            </Link>
          )}
        </div>
      </div>

      {isAdminTier && allowedTransitions.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Status</CardTitle>
          </CardHeader>
          <CardContent>
            <TransitionActions allowedTransitions={allowedTransitions} action={transitionAction} />
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Initiative</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <Field label="Description" value={initiative.description} />
            <Field label="Owner" value={initiative.ownerName} />
            <Field label="Organization" value={initiative.organizationName} />
            <Field label="Expected outcome" value={initiative.expectedOutcome} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Timeline</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <Field label="Target start" value={initiative.targetStartDate ? new Date(initiative.targetStartDate).toLocaleDateString() : null} />
            <Field label="Target completion" value={initiative.targetCompletionDate ? new Date(initiative.targetCompletionDate).toLocaleDateString() : null} />
            <Field label="Completed" value={initiative.completedAtUtc ? new Date(initiative.completedAtUtc).toLocaleString() : null} />
            <Field label="Created" value={`${new Date(initiative.createdAtUtc).toLocaleString()} · ${initiative.createdByName ?? ""}`} />
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Milestones</CardTitle>
        </CardHeader>
        <CardContent>
          <MilestonePanel
            milestones={initiative.milestones}
            addAction={addMilestoneAction}
            toggleAction={toggleMilestoneAction}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Dependencies</CardTitle>
          <CardDescription>What this initiative is blocked by, and what it blocks.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div>
            <p className="mb-2 text-xs font-medium text-muted-foreground">Depends on</p>
            {initiative.dependsOn.length === 0 ? (
              <p className="text-sm text-muted-foreground">None.</p>
            ) : (
              <ul className="flex flex-col gap-1">
                {initiative.dependsOn.map((d) => (
                  <li key={d.dependencyId} className="flex items-center justify-between rounded-md bg-muted px-3 py-1.5 text-sm">
                    <Link href={`/roadmap/${d.initiativeId}`} className="hover:underline">
                      <span className="font-mono text-xs text-muted-foreground">{d.code}</span> {d.title}
                    </Link>
                    {isAdminTier && <RemoveDependencyButton action={removeDependencyAction.bind(null, d.dependencyId)} />}
                  </li>
                ))}
              </ul>
            )}
          </div>
          <div>
            <p className="mb-2 text-xs font-medium text-muted-foreground">Blocks</p>
            {initiative.dependedOnBy.length === 0 ? (
              <p className="text-sm text-muted-foreground">None.</p>
            ) : (
              <ul className="flex flex-col gap-1">
                {initiative.dependedOnBy.map((d) => (
                  <li key={d.dependencyId} className="rounded-md bg-muted px-3 py-1.5 text-sm">
                    <Link href={`/roadmap/${d.initiativeId}`} className="hover:underline">
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

      {isAdminTier && (
        <Card className="max-w-2xl">
          <CardHeader>
            <CardTitle className="text-base">Edit details</CardTitle>
          </CardHeader>
          <CardContent>
            <EditInitiativeForm initiative={initiative} members={members} action={updateInitiativeAction} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
