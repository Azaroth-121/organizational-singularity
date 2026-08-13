"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";

export interface ReassessState {
  error: string | null;
}

export function ReassessButton({
  action,
}: {
  action: (state: ReassessState, formData: FormData) => Promise<ReassessState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });

  return (
    <form action={formAction} className="flex flex-col items-start gap-1">
      <Button type="submit" variant="outline" size="sm" disabled={pending}>
        {pending ? "Starting…" : "Start reassessment"}
      </Button>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </form>
  );
}
