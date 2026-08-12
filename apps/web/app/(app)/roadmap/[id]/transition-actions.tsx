"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { STATUS_LABELS } from "../values";

export interface TransitionState {
  error: string | null;
}

type BoundTransitionAction = (state: TransitionState, formData: FormData) => Promise<TransitionState>;
type TransitionAction = (toStatus: string, state: TransitionState, formData: FormData) => Promise<TransitionState>;

function TransitionButton({ status, action }: { status: string; action: BoundTransitionAction }) {
  const [state, formAction, pending] = useActionState(action, { error: null });
  return (
    <form action={formAction} className="contents">
      <div className="flex flex-col gap-1">
        <Button type="submit" size="sm" variant="outline" disabled={pending}>
          {pending ? "…" : `Move to ${STATUS_LABELS[status] ?? status}`}
        </Button>
        {state.error && <p className="text-xs text-destructive">{state.error}</p>}
      </div>
    </form>
  );
}

export function TransitionActions({
  allowedTransitions,
  action,
}: {
  allowedTransitions: string[];
  action: TransitionAction;
}) {
  if (allowedTransitions.length === 0) return null;

  return (
    <div className="flex flex-wrap gap-2">
      {allowedTransitions.map((status) => (
        <TransitionButton key={status} status={status} action={action.bind(null, status)} />
      ))}
    </div>
  );
}
