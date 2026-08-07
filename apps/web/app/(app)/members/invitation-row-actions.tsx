"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";

export interface CancelInvitationState {
  error: string | null;
}

export function InvitationRowActions({
  cancelAction,
}: {
  cancelAction: (state: CancelInvitationState, formData: FormData) => Promise<CancelInvitationState>;
}) {
  const [state, formAction, pending] = useActionState(cancelAction, { error: null });

  return (
    <div className="flex flex-col items-end gap-1">
      <form action={formAction}>
        <Button type="submit" size="sm" variant="destructive" disabled={pending}>
          {pending ? "Cancelling…" : "Cancel"}
        </Button>
      </form>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </div>
  );
}
