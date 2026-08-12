"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { CATEGORIES, CATEGORY_LABELS, SEVERITIES, DETECTION_SOURCES } from "./values";

export interface CreateFindingState {
  error: string | null;
}

export function CreateFindingForm({
  action,
  organizations,
}: {
  action: (state: CreateFindingState, formData: FormData) => Promise<CreateFindingState>;
  organizations: { id: string; name: string }[];
}) {
  const [state, formAction, pending] = useActionState(action, { error: null });

  return (
    <form action={formAction} className="flex flex-col gap-3 border-t pt-4">
      <div className="grid grid-cols-2 gap-2">
        <Input name="title" placeholder="Finding title" required className="col-span-2" />
        <select name="organizationId" required className="h-8 rounded-md border border-input bg-background px-2 text-sm">
          <option value="">Organization…</option>
          {organizations.map((o) => (
            <option key={o.id} value={o.id}>{o.name}</option>
          ))}
        </select>
        <select name="category" required defaultValue="" className="h-8 rounded-md border border-input bg-background px-2 text-sm">
          <option value="" disabled>Category…</option>
          {CATEGORIES.map((c) => (
            <option key={c} value={c}>{CATEGORY_LABELS[c]}</option>
          ))}
        </select>
        <select name="severity" defaultValue="Moderate" className="h-8 rounded-md border border-input bg-background px-2 text-sm">
          {SEVERITIES.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
        <select name="detectionSource" defaultValue="Human" className="h-8 rounded-md border border-input bg-background px-2 text-sm">
          {DETECTION_SOURCES.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>
      <textarea
        name="description"
        placeholder="What is wrong, and why does it matter?"
        rows={2}
        className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
      />
      <div>
        <Button type="submit" disabled={pending}>
          {pending ? "Creating…" : "Create finding"}
        </Button>
      </div>
      {state.error && <p className="text-sm text-destructive">{state.error}</p>}
    </form>
  );
}
