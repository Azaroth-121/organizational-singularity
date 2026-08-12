"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";

export interface CreateAssessmentState {
  error: string | null;
}

export function CreateAssessmentForm({
  action,
  organizations,
}: {
  action: (state: CreateAssessmentState, formData: FormData) => Promise<CreateAssessmentState>;
  organizations: { id: string; name: string }[];
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });

  return (
    <form action={formAction} className="flex flex-wrap items-center gap-2 border-t pt-4">
      <select name="organizationId" required className="h-8 rounded-md border border-input bg-background px-2 text-sm">
        <option value="">Organization…</option>
        {organizations.map((o) => (
          <option key={o.id} value={o.id}>{o.name}</option>
        ))}
      </select>
      <Button type="submit" disabled={pending}>
        {pending ? "Starting…" : "Start assessment"}
      </Button>
      {state.error && <p className="text-sm text-destructive">{state.error}</p>}
    </form>
  );
}
