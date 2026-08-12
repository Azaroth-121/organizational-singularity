"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EVIDENCE_TYPES } from "../values";

export interface EvidenceFormState {
  error: string | null;
}

export function EvidenceForm({
  action,
}: {
  action: (state: EvidenceFormState, formData: FormData) => Promise<EvidenceFormState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });

  return (
    <form action={formAction} className="flex flex-col gap-2 border-t pt-3">
      <div className="flex gap-2">
        <select name="evidenceType" defaultValue="Assertion" className="h-8 rounded-md border border-input bg-background px-2 text-sm">
          {EVIDENCE_TYPES.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        <Input name="description" placeholder="What does this evidence show?" required className="flex-1" />
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Adding…" : "Add evidence"}
        </Button>
      </div>
      <Input name="sourceReference" placeholder="Source reference or URL (optional)" />
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </form>
  );
}
