"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PRIORITIES } from "../values";
import type { ApiInitiativeDetail, ApiTenantMember } from "@/lib/api";

export interface EditInitiativeState {
  error: string | null;
  success: string | null;
}

export function EditInitiativeForm({
  initiative,
  members,
  action,
}: {
  initiative: ApiInitiativeDetail;
  members: ApiTenantMember[];
  action: (state: EditInitiativeState, formData: FormData) => Promise<EditInitiativeState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null, success: null });

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <input type="hidden" name="expectedVersion" value={initiative.version} />

      <div>
        <label className="text-xs font-medium text-muted-foreground">Title</label>
        <Input name="title" defaultValue={initiative.title} required />
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Description</label>
        <textarea
          name="description"
          defaultValue={initiative.description}
          rows={3}
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
        />
      </div>

      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="text-xs font-medium text-muted-foreground">Priority</label>
          <select name="priority" defaultValue={initiative.priority} className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm">
            {PRIORITIES.map((p) => (
              <option key={p} value={p}>{p}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="text-xs font-medium text-muted-foreground">Owner</label>
          <select name="ownerUserId" defaultValue={initiative.ownerUserId ?? ""} className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm">
            <option value="">Unassigned</option>
            {members.map((m) => (
              <option key={m.userId} value={m.userId}>{m.name}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="text-xs font-medium text-muted-foreground">Target start</label>
          <Input type="date" name="targetStartDate" defaultValue={initiative.targetStartDate?.slice(0, 10) ?? ""} />
        </div>
        <div>
          <label className="text-xs font-medium text-muted-foreground">Target completion</label>
          <Input type="date" name="targetCompletionDate" defaultValue={initiative.targetCompletionDate?.slice(0, 10) ?? ""} />
        </div>
      </div>

      <div>
        <label className="text-xs font-medium text-muted-foreground">Expected outcome</label>
        <textarea
          name="expectedOutcome"
          defaultValue={initiative.expectedOutcome ?? ""}
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
