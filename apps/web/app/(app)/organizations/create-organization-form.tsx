"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export interface CreateOrganizationState {
  error: string | null;
}

export function CreateOrganizationForm({
  action,
}: {
  action: (state: CreateOrganizationState, formData: FormData) => Promise<CreateOrganizationState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });

  return (
    <form action={formAction} className="flex flex-col gap-2 border-t pt-4">
      <div className="flex gap-2">
        <Input name="name" placeholder="New organization name" required />
        <Button type="submit" disabled={pending}>
          {pending ? "Creating…" : "Create"}
        </Button>
      </div>
      {state.error && <p className="text-sm text-destructive">{state.error}</p>}
    </form>
  );
}
