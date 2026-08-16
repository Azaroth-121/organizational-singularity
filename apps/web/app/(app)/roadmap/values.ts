// Mirrors OrganizationalSingularity.Domain.Roadmap (apps/api). Keep in sync -- the API is
// the source of truth and validates server-side regardless of what's sent here.

import type { StatusTone } from "@/components/ui/status-badge";

export const STATUS_LABELS: Record<string, string> = {
  Planned: "Planned",
  InProgress: "In Progress",
  OnHold: "On Hold",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

export const INITIATIVE_STATUS_TONE: Record<string, StatusTone> = {
  Planned: "neutral",
  InProgress: "warning",
  OnHold: "neutral",
  Completed: "good",
  Cancelled: "neutral",
};

export const PRIORITIES = ["Low", "Medium", "High", "Critical"] as const;

export const PRIORITY_TONE: Record<string, StatusTone> = {
  Low: "neutral",
  Medium: "good",
  High: "serious",
  Critical: "critical",
};

// Mirrors InitiativeStateMachine's allowed-transitions graph -- duplicated here only for
// which actions the UI offers; the API is the actual enforcement point regardless.
export const ALLOWED_TRANSITIONS: Record<string, string[]> = {
  Planned: ["InProgress", "OnHold", "Cancelled"],
  InProgress: ["OnHold", "Completed", "Cancelled"],
  OnHold: ["InProgress", "Cancelled"],
  Completed: [],
  Cancelled: ["Planned"],
};
