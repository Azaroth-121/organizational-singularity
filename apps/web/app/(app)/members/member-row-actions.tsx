"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { MEMBERSHIP_ROLES } from "./roles";

export interface RowActionState {
  error: string | null;
}

export function MemberRowActions({
  currentRole,
  disabled,
  updateRoleAction,
  removeMemberAction,
}: {
  currentRole: string;
  disabled: boolean;
  updateRoleAction: (state: RowActionState, formData: FormData) => Promise<RowActionState>;
  removeMemberAction: (state: RowActionState, formData: FormData) => Promise<RowActionState>;
}) {
  const [roleState, roleFormAction, rolePending] = useActionState(updateRoleAction, { error: null });
  const [removeState, removeFormAction, removePending] = useActionState(removeMemberAction, { error: null });

  return (
    <div className="flex flex-col items-end gap-1">
      <div className="flex items-center gap-2">
        <form action={roleFormAction} className="flex items-center gap-2">
          <select
            name="role"
            defaultValue={currentRole}
            disabled={disabled || rolePending}
            className="h-8 rounded-md border border-input bg-background px-2 text-sm disabled:opacity-50"
          >
            {MEMBERSHIP_ROLES.map((role) => (
              <option key={role} value={role}>
                {role}
              </option>
            ))}
          </select>
          <Button type="submit" size="sm" variant="outline" disabled={disabled || rolePending}>
            {rolePending ? "Saving…" : "Update"}
          </Button>
        </form>
        <form action={removeFormAction}>
          <Button type="submit" size="sm" variant="destructive" disabled={disabled || removePending}>
            {removePending ? "Removing…" : "Remove"}
          </Button>
        </form>
      </div>
      {disabled && (
        <p className="text-xs text-muted-foreground">Last admin-tier member — protected.</p>
      )}
      {roleState.error && <p className="text-xs text-destructive">{roleState.error}</p>}
      {removeState.error && <p className="text-xs text-destructive">{removeState.error}</p>}
    </div>
  );
}
