// Mirrors OrganizationalSingularity.Domain.Identity.MembershipRole (apps/api). Keep in sync --
// the API is the source of truth and validates server-side regardless of what's sent here.
export const MEMBERSHIP_ROLES = [
  "PlatformAdministrator",
  "SoverAIgnArchitect",
  "CustomerExecutive",
  "CustomerProgramManager",
  "Contributor",
  "ReviewerAuditor",
  "IntegrationService",
  "SupportOperator",
] as const;

export const ADMIN_TIER_ROLES = new Set<string>(["PlatformAdministrator", "SoverAIgnArchitect"]);
