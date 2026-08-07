"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { MEMBERSHIP_ROLES } from "./roles";

export interface AddMemberState {
  error: string | null;
  success: string | null;
}

export function AddMemberForm({
  action,
}: {
  action: (state: AddMemberState, formData: FormData) => Promise<AddMemberState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null, success: null });

  return (
    <form action={formAction} className="flex flex-col gap-2 border-t pt-4">
      <div className="flex gap-2">
        <Input name="email" type="email" placeholder="person@example.com" required className="flex-1" />
        <select
          name="role"
          defaultValue="Contributor"
          className="h-8 rounded-md border border-input bg-background px-2 text-sm"
        >
          {MEMBERSHIP_ROLES.map((role) => (
            <option key={role} value={role}>
              {role}
            </option>
          ))}
        </select>
        <Button type="submit" disabled={pending}>
          {pending ? "Adding…" : "Add member"}
        </Button>
      </div>
      <p className="text-xs text-muted-foreground">
        If they&apos;ve never signed in, this creates a pending invitation instead — it
        activates automatically the moment they do.
      </p>
      {state.success && <p className="text-sm text-green-600 dark:text-green-400">{state.success}</p>}
      {state.error && <p className="text-sm text-destructive">{state.error}</p>}
    </form>
  );
}
