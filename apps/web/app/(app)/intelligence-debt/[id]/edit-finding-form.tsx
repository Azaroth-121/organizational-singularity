"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { CATEGORIES, CATEGORY_LABELS, SEVERITIES } from "../values";
import type { ApiIntelligenceDebtDetail, ApiTenantMember } from "@/lib/api";

export interface EditFindingState {
  error: string | null;
  success: string | null;
}

export function EditFindingForm({
  finding,
  members,
  action,
}: {
  finding: ApiIntelligenceDebtDetail;
  members: ApiTenantMember[];
  action: (state: EditFindingState, formData: FormData) => Promise<EditFindingState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null, success: null });

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <input type="hidden" name="expectedVersion" value={finding.version} />

      <div>
        <label className="text-xs font-medium text-muted-foreground">Title</label>
        <Input name="title" defaultValue={finding.title} required />
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Description</label>
        <textarea
          name="description"
          defaultValue={finding.description}
          rows={3}
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
        />
      </div>

      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="text-xs font-medium text-muted-foreground">Category</label>
          <select name="category" defaultValue={finding.category} className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm">
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>{CATEGORY_LABELS[c]}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="text-xs font-medium text-muted-foreground">Severity</label>
          <select name="severity" defaultValue={finding.severity} className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm">
            {SEVERITIES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Owner</label>
        <select name="ownerUserId" defaultValue={finding.ownerUserId ?? ""} className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm">
          <option value="">Unassigned</option>
          {members.map((m) => (
            <option key={m.userId} value={m.userId}>{m.name}</option>
          ))}
        </select>
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Target resolution date</label>
        <Input
          type="date"
          name="targetResolutionDate"
          defaultValue={finding.targetResolutionDate?.slice(0, 10) ?? ""}
        />
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Business impact</label>
        <textarea
          name="businessImpact"
          defaultValue={finding.businessImpact ?? ""}
          rows={2}
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
        />
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Affected scope</label>
        <Input name="affectedScope" defaultValue={finding.affectedScope ?? ""} />
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Recommended action</label>
        <textarea
          name="recommendedAction"
          defaultValue={finding.recommendedAction ?? ""}
          rows={2}
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
        />
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Remediation plan</label>
        <textarea
          name="remediationPlan"
          defaultValue={finding.remediationPlan ?? ""}
          rows={2}
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
        />
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Validation criteria</label>
        <textarea
          name="validationCriteria"
          defaultValue={finding.validationCriteria ?? ""}
          rows={2}
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
        />
      </div>

      <div>
        <Button type="submit" disabled={pending}>
          {pending ? "Saving…" : "Save changes"}
        </Button>
      </div>
      {state.success && <p className="text-sm text-green-600 dark:text-green-400">{state.success}</p>}
      {state.error && <p className="text-sm text-destructive">{state.error}</p>}
    </form>
  );
}
