"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { ApiInitiativeMilestone } from "@/lib/api";

export interface MilestoneState {
  error: string | null;
}

type BoundMilestoneAction = (state: MilestoneState, formData: FormData) => Promise<MilestoneState>;
type ToggleAction = (milestoneId: string, isDone: boolean, state: MilestoneState, formData: FormData) => Promise<MilestoneState>;

function MilestoneRow({ milestone, toggleAction }: { milestone: ApiInitiativeMilestone; toggleAction: BoundMilestoneAction }) {
  const [state, formAction, pending] = useActionState(toggleAction, { error: null });
  return (
    <li className="flex flex-col gap-1 rounded-md bg-muted px-3 py-2 text-sm">
      <div className="flex items-center justify-between gap-2">
        <span className={milestone.isDone ? "text-muted-foreground line-through" : ""}>{milestone.title}</span>
        <div className="flex shrink-0 items-center gap-2">
          {milestone.dueDate && (
            <span className="text-xs text-muted-foreground">{new Date(milestone.dueDate).toLocaleDateString()}</span>
          )}
          <form action={formAction}>
            <Button type="submit" size="sm" variant={milestone.isDone ? "outline" : "default"} disabled={pending}>
              {pending ? "…" : milestone.isDone ? "Reopen" : "Mark done"}
            </Button>
          </form>
        </div>
      </div>
      {state.error && <p className="text-xs text-destructive">{state.error}</p>}
    </li>
  );
}

export function MilestonePanel({
  milestones,
  addAction,
  toggleAction,
}: {
  milestones: ApiInitiativeMilestone[];
  addAction: BoundMilestoneAction;
  toggleAction: ToggleAction;
}) {
  const [addState, addFormAction, addPending] = useActionState(addAction, { error: null });

  return (
    <div className="flex flex-col gap-3">
      {milestones.length === 0 ? (
        <p className="text-sm text-muted-foreground">No milestones yet.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {milestones.map((m) => (
            <MilestoneRow key={m.id} milestone={m} toggleAction={toggleAction.bind(null, m.id, !m.isDone)} />
          ))}
        </ul>
      )}
      <form action={addFormAction} className="flex flex-wrap gap-2 border-t pt-3">
        <Input name="title" placeholder="New milestone" required className="flex-1" />
        <Input name="dueDate" type="date" className="w-auto" />
        <Button type="submit" size="sm" disabled={addPending}>
          {addPending ? "Adding…" : "Add milestone"}
        </Button>
      </form>
      {addState.error && <p className="text-xs text-destructive">{addState.error}</p>}
    </div>
  );
}
