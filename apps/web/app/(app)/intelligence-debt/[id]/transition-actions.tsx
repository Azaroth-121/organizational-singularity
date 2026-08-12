"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { STATUS_LABELS } from "../values";

export interface TransitionState {
  error: string | null;
}

type BoundTransitionAction = (state: TransitionState, formData: FormData) => Promise<TransitionState>;
type TransitionAction = (toStatus: string, state: TransitionState, formData: FormData) => Promise<TransitionState>;

function SimpleTransitionButton({ status, action }: { status: string; action: BoundTransitionAction }) {
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

function ValidateForm({ action }: { action: BoundTransitionAction }) {
  const [state, formAction, pending] = useActionState(action, { error: null });
  return (
    <form action={formAction} className="flex flex-col gap-2 rounded-md border p-3">
      <label className="text-xs font-medium text-muted-foreground">
        Outcome — what was validated, and how
      </label>
      <div className="flex gap-2">
        <Input name="outcome" placeholder="e.g. sampled 5 decision records, all traceable" className="flex-1" />
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "…" : "Mark Validated"}
        </Button>
      </div>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
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

  const simple = allowedTransitions.filter((s) => s !== "Validated");
  const hasValidate = allowedTransitions.includes("Validated");

  return (
    <div className="flex flex-col gap-3">
      {simple.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {simple.map((status) => (
            <SimpleTransitionButton key={status} status={status} action={action.bind(null, status)} />
          ))}
        </div>
      )}
      {hasValidate && <ValidateForm action={action.bind(null, "Validated")} />}
    </div>
  );
}
