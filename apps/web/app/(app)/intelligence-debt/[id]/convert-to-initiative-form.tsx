"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

const PRIORITIES = ["Low", "Medium", "High", "Critical"] as const;

export interface ConvertState {
  error: string | null;
}

export function ConvertToInitiativeForm({
  defaultTitle,
  action,
}: {
  defaultTitle: string;
  action: (state: ConvertState, formData: FormData) => Promise<ConvertState>;
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });

  return (
    <form action={formAction} className="flex flex-col gap-2 border-t pt-3">
      <Input name="title" defaultValue={defaultTitle} required placeholder="Initiative title" />
      <textarea
        name="description"
        placeholder="What work does this initiative cover?"
        rows={2}
        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
      />
      <div className="flex flex-wrap items-center gap-2">
        <select name="priority" defaultValue="Medium" className="h-8 rounded-md border border-input bg-background px-2 text-sm">
          {PRIORITIES.map((p) => (
            <option key={p} value={p}>{p}</option>
          ))}
        </select>
        <Input type="date" name="targetCompletionDate" className="w-auto" />
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Creating…" : "Convert to initiative"}
        </Button>
      </div>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </form>
  );
}
