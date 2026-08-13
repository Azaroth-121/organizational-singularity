"use client";

import { useActionState, useState } from "react";
import { Button } from "@/components/ui/button";

export interface CancelAssessmentState {
  error: string | null;
}

export function CancelAssessmentButton({
  action,
  supersedesLabel,
}: {
  action: (state: CancelAssessmentState, formData: FormData) => Promise<CancelAssessmentState>;
  supersedesLabel: string | null;
}) {
  const [armed, setArmed] = useState(false);
  const [state, formAction, pending] = useActionState(action, { error: null });

  if (!armed) {
    return (
      <Button type="button" variant="outline" size="sm" onClick={() => setArmed(true)}>
        Cancel this assessment
      </Button>
    );
  }

  return (
    <form action={formAction} className="flex flex-col gap-2 rounded-md border border-destructive/40 bg-destructive/5 p-3">
      <p className="text-sm">
        This permanently deletes this draft assessment and its answers.
        {supersedesLabel && ` This will let ${supersedesLabel} be reassessed again.`}
      </p>
      <div className="flex gap-2">
        <Button type="submit" variant="destructive" size="sm" disabled={pending}>
          {pending ? "Cancelling…" : "Yes, cancel it"}
        </Button>
        <Button type="button" variant="ghost" size="sm" onClick={() => setArmed(false)} disabled={pending}>
          Never mind
        </Button>
      </div>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </form>
  );
}
