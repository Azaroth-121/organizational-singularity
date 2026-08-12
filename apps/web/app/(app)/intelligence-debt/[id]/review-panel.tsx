"use client";

import { useActionState, useState } from "react";
import { Button } from "@/components/ui/button";

export interface ReviewState {
  error: string | null;
}

type BoundReviewAction = (state: ReviewState, formData: FormData) => Promise<ReviewState>;
type ReviewAction = (outcome: string, state: ReviewState, formData: FormData) => Promise<ReviewState>;

function ReviewOutcomeForm({ label, action }: { label: string; action: BoundReviewAction }) {
  const [state, formAction, pending] = useActionState(action, { error: null });
  const [open, setOpen] = useState(false);

  if (!open) {
    return (
      <Button type="button" variant="outline" size="sm" onClick={() => setOpen(true)}>
        {label}
      </Button>
    );
  }

  return (
    <form action={formAction} className="flex w-full flex-col gap-2 rounded-md border p-3">
      <label className="text-xs font-medium text-muted-foreground">
        Rationale for &ldquo;{label}&rdquo; (required)
      </label>
      <textarea
        name="rationale"
        required
        rows={2}
        autoFocus
        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
      />
      <div className="flex gap-2">
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "…" : `Confirm ${label}`}
        </Button>
        <Button type="button" size="sm" variant="ghost" onClick={() => setOpen(false)}>
          Cancel
        </Button>
      </div>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </form>
  );
}

export function ReviewPanel({ action }: { action: ReviewAction }) {
  return (
    <div className="flex flex-col gap-3">
      <p className="text-xs text-muted-foreground">
        Detected/AI-proposed candidates are not authoritative until reviewed. If you need to change the
        category, severity, or other fields first, use Edit details below, then come back and choose Modify.
      </p>
      <div className="flex flex-wrap gap-2">
        <ReviewOutcomeForm label="Accept" action={action.bind(null, "Accepted")} />
        <ReviewOutcomeForm label="Modify" action={action.bind(null, "Modified")} />
        <ReviewOutcomeForm label="Reject" action={action.bind(null, "Rejected")} />
        <ReviewOutcomeForm label="Request evidence" action={action.bind(null, "EvidenceRequired")} />
      </div>
    </div>
  );
}
