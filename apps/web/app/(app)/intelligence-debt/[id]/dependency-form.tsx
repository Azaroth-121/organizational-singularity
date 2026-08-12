"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export interface DependencyFormState {
  error: string | null;
}

export function AddDependencyForm({
  action,
}: {
  action: (state: DependencyFormState, formData: FormData) => Promise<DependencyFormState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });

  return (
    <form action={formAction} className="flex flex-col gap-2 border-t pt-3">
      <div className="flex gap-2">
        <Input name="dependsOnCode" placeholder="Depends on finding code, e.g. ID-007" required className="flex-1" />
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Adding…" : "Add dependency"}
        </Button>
      </div>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </form>
  );
}

export function RemoveDependencyButton({
  action,
}: {
  action: (state: DependencyFormState, formData: FormData) => Promise<DependencyFormState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });
  return (
    <form action={formAction}>
      <Button type="submit" size="sm" variant="ghost" disabled={pending}>
        {pending ? "…" : "Remove"}
      </Button>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </form>
  );
}
