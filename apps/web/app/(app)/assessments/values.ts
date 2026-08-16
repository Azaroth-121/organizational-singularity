// Mirrors OrganizationalSingularity.Domain.Assessments (apps/api). Keep in sync -- the API
// is the source of truth and validates server-side regardless of what's sent here.

import type { StatusTone } from "@/components/ui/status-badge";

export const STATUS_LABELS: Record<string, string> = {
  Draft: "Draft",
  InProgress: "In Progress",
  Submitted: "Submitted",
  UnderReview: "Under Review",
  Completed: "Completed",
  Superseded: "Superseded",
};

export const CONFIDENCE_LABELS: Record<string, string> = {
  AssertionOnly: "Assertion only",
  SupportingEvidence: "Supporting evidence",
  CorroboratedEvidence: "Corroborated evidence",
};

export const CONFIDENCE_VALUES = Object.keys(CONFIDENCE_LABELS);

export const BAND_TONE: Record<string, StatusTone> = {
  Fragmented: "critical",
  Emerging: "serious",
  Developing: "warning",
  Integrated: "good",
  Adaptive: "good",
};

// Assessment lifecycle status -- neutral while in progress, good once completed.
export const ASSESSMENT_STATUS_TONE: Record<string, StatusTone> = {
  Draft: "neutral",
  InProgress: "warning",
  Submitted: "neutral",
  UnderReview: "neutral",
  Completed: "good",
  Superseded: "neutral",
};

export function formatScore(score: number | null): string {
  return score === null ? "—" : score.toFixed(2);
}
