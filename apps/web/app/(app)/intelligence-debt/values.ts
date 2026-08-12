// Mirrors OrganizationalSingularity.Domain.IntelligenceDebt (apps/api). Keep in sync --
// the API is the source of truth and validates server-side regardless of what's sent here.

export const CATEGORY_LABELS: Record<string, string> = {
  FragmentedKnowledge: "Fragmented Knowledge",
  DisconnectedSystems: "Disconnected Systems",
  UndocumentedDecisions: "Undocumented Decisions",
  DuplicatedWork: "Duplicated Work",
  InconsistentProcesses: "Inconsistent Processes",
  InaccessibleExpertise: "Inaccessible Expertise",
  ConflictingDefinitionsAndData: "Conflicting Definitions & Data",
  UnownedOrUnobservableProcesses: "Unowned or Unobservable Processes",
  UngovernedAiAndAutomation: "Ungoverned AI & Automation",
};

export const CATEGORIES = Object.keys(CATEGORY_LABELS);

export const SEVERITIES = ["Informational", "Low", "Moderate", "High", "Critical"] as const;

export const DETECTION_SOURCES = ["Assessment", "Human", "System", "AI", "Integration"] as const;

export const EVIDENCE_TYPES = ["Assertion", "Document", "AssessmentResponse", "ExternalLink"] as const;

export const STATUS_LABELS: Record<string, string> = {
  Detected: "Detected",
  EvidenceReviewed: "Evidence Reviewed",
  ApprovedFinding: "Approved",
  Remediation: "Remediation",
  Validation: "Validation",
  Validated: "Validated",
  Rejected: "Rejected",
  Deferred: "Deferred",
};

// Mirrors IntelligenceDebtStateMachine's allowed-transitions graph -- duplicated here only
// for which actions the UI offers; the API is the actual enforcement point regardless.
export const ALLOWED_TRANSITIONS: Record<string, string[]> = {
  Detected: ["EvidenceReviewed", "Rejected", "Deferred"],
  EvidenceReviewed: ["ApprovedFinding", "Rejected", "Deferred", "Detected"],
  ApprovedFinding: ["Remediation", "Deferred"],
  Remediation: ["Validation", "Deferred"],
  Validation: ["Validated", "Remediation"],
  Validated: ["Remediation"],
  Deferred: ["Detected"],
  Rejected: ["Detected"],
};

export const SEVERITY_TONE: Record<string, "default" | "outline" | "destructive"> = {
  Informational: "outline",
  Low: "outline",
  Moderate: "default",
  High: "destructive",
  Critical: "destructive",
};
