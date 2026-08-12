const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

export interface ApiHealth {
  status: string;
  environment: string;
  version: string;
}

export async function getApiHealth(): Promise<ApiHealth | null> {
  try {
    const res = await fetch(`${API_BASE_URL}/api/v1/health`, { cache: "no-store" });
    if (!res.ok) return null;
    return (await res.json()) as ApiHealth;
  } catch {
    return null;
  }
}

export type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; status: number | null; message?: string };

async function apiFetch<T>(
  path: string,
  accessToken: string,
  init?: RequestInit
): Promise<ApiResult<T>> {
  try {
    const res = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      cache: "no-store",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });

    if (!res.ok) {
      const message = await res
        .json()
        .then((body: unknown) =>
          typeof body === "object" && body !== null && "detail" in body && typeof body.detail === "string"
            ? body.detail
            : undefined
        )
        .catch(() => undefined);
      return { ok: false, status: res.status, message };
    }

    if (res.status === 204) {
      return { ok: true, data: undefined as T };
    }
    return { ok: true, data: (await res.json()) as T };
  } catch {
    return { ok: false, status: null };
  }
}

export interface ApiMe {
  name: string | null;
  email: string | null;
  oid: string | null;
  tenantId: string | null;
}

export function getMe(accessToken: string): Promise<ApiResult<ApiMe>> {
  return apiFetch<ApiMe>("/api/v1/me", accessToken);
}

export interface ApiMembership {
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  role: string;
}

export function getMyMemberships(
  accessToken: string
): Promise<ApiResult<{ userId: string; memberships: ApiMembership[] }>> {
  return apiFetch("/api/v1/me/memberships", accessToken);
}

export interface ApiOrganization {
  id: string;
  name: string;
  industry: string | null;
  employeeCount: number | null;
}

export function listOrganizations(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiOrganization[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/organizations`, accessToken);
}

export function createOrganization(
  accessToken: string,
  tenantId: string,
  body: { name: string; industry?: string; employeeCount?: number }
): Promise<ApiResult<ApiOrganization>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/organizations`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export interface ApiTenantMember {
  membershipId: string;
  userId: string;
  name: string;
  email: string;
  role: string;
  invitedAtUtc: string;
  acceptedAtUtc: string | null;
}

export function listTenantMembers(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiTenantMember[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships`, accessToken);
}

export interface ApiInvitation {
  invitationId: string;
  email: string;
  role: string;
  invitedAtUtc: string;
}

export type AddMemberResult =
  | ({ status: "granted" } & ApiTenantMember)
  | ({ status: "invited" } & ApiInvitation);

export function addTenantMember(
  accessToken: string,
  tenantId: string,
  body: { email: string; role: string }
): Promise<ApiResult<AddMemberResult>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function listTenantInvitations(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiInvitation[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/invitations`, accessToken);
}

export function cancelInvitation(
  accessToken: string,
  tenantId: string,
  invitationId: string
): Promise<ApiResult<undefined>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/invitations/${invitationId}`, accessToken, {
    method: "DELETE",
  });
}

export function updateMemberRole(
  accessToken: string,
  tenantId: string,
  membershipId: string,
  role: string
): Promise<ApiResult<{ membershipId: string; role: string; acceptedAtUtc: string | null }>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships/${membershipId}`, accessToken, {
    method: "PUT",
    body: JSON.stringify({ role }),
  });
}

export function removeMember(
  accessToken: string,
  tenantId: string,
  membershipId: string
): Promise<ApiResult<undefined>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/memberships/${membershipId}`, accessToken, {
    method: "DELETE",
  });
}

export interface ApiIntelligenceDebtSummary {
  id: string;
  code: string;
  title: string;
  category: string;
  severity: string;
  status: string;
  detectionSource: string;
  ownerUserId: string | null;
  ownerName: string | null;
  organizationId: string;
  version: number;
  createdAtUtc: string;
}

export interface ApiIntelligenceDebtEvidence {
  id: string;
  evidenceType: string;
  description: string;
  sourceReference: string | null;
  assessmentResponseId: string | null;
  documentId: string | null;
  externalUri: string | null;
  addedByUserId: string;
  addedByName: string | null;
  addedAtUtc: string;
}

export interface ApiIntelligenceDebtDetail {
  id: string;
  code: string;
  title: string;
  description: string;
  category: string;
  severity: string;
  status: string;
  detectionSource: string;
  businessImpact: string | null;
  affectedScope: string | null;
  ownerUserId: string | null;
  ownerName: string | null;
  targetResolutionDate: string | null;
  organizationId: string;
  organizationName: string | null;
  assessmentId: string | null;
  capabilityId: string | null;
  capabilityName: string | null;
  dimensionId: string | null;
  dimensionName: string | null;
  recommendedAction: string | null;
  remediationPlan: string | null;
  validationCriteria: string | null;
  createdByUserId: string;
  createdByName: string | null;
  createdAtUtc: string;
  approvedAtUtc: string | null;
  approvedByName: string | null;
  remediationStartedAtUtc: string | null;
  resolvedAtUtc: string | null;
  validatedAtUtc: string | null;
  validatedByName: string | null;
  outcome: string | null;
  version: number;
  evidence: ApiIntelligenceDebtEvidence[];
  dependsOn: ApiIntelligenceDebtDependencyRef[];
  dependedOnBy: ApiIntelligenceDebtDependencyRef[];
}

export interface ApiIntelligenceDebtDependencyRef {
  dependencyId: string;
  findingId: string;
  code: string | null;
  title: string | null;
  status: string | null;
}

export function listIntelligenceDebtFindings(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiIntelligenceDebtSummary[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt`, accessToken);
}

export function getIntelligenceDebtFinding(
  accessToken: string,
  tenantId: string,
  findingId: string
): Promise<ApiResult<ApiIntelligenceDebtDetail>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}`, accessToken);
}

export function createIntelligenceDebtFinding(
  accessToken: string,
  tenantId: string,
  body: {
    organizationId: string;
    title: string;
    description: string;
    category: string;
    severity: string;
    detectionSource: string;
    businessImpact?: string;
    affectedScope?: string;
    recommendedAction?: string;
  }
): Promise<ApiResult<ApiIntelligenceDebtSummary>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function updateIntelligenceDebtFinding(
  accessToken: string,
  tenantId: string,
  findingId: string,
  body: {
    expectedVersion: number;
    title: string;
    description: string;
    category: string;
    severity: string;
    businessImpact?: string;
    affectedScope?: string;
    ownerUserId?: string | null;
    targetResolutionDate?: string | null;
    recommendedAction?: string;
    remediationPlan?: string;
    validationCriteria?: string;
  }
): Promise<ApiResult<ApiIntelligenceDebtSummary>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}`, accessToken, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function transitionIntelligenceDebtFinding(
  accessToken: string,
  tenantId: string,
  findingId: string,
  body: { expectedVersion: number; toStatus: string; outcome?: string }
): Promise<ApiResult<ApiIntelligenceDebtSummary>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}/transition`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function reviewIntelligenceDebtFinding(
  accessToken: string,
  tenantId: string,
  findingId: string,
  body: { expectedVersion: number; outcome: string; rationale: string }
): Promise<ApiResult<ApiIntelligenceDebtSummary>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}/review`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export interface ApiIntelligenceDebtHistoryEvent {
  id: string;
  eventType: string;
  actorUserId: string | null;
  actorName: string | null;
  occurredAtUtc: string;
  payload: Record<string, unknown> | null;
}

export function getIntelligenceDebtHistory(
  accessToken: string,
  tenantId: string,
  findingId: string
): Promise<ApiResult<ApiIntelligenceDebtHistoryEvent[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}/history`, accessToken);
}

export function addIntelligenceDebtEvidence(
  accessToken: string,
  tenantId: string,
  findingId: string,
  body: { evidenceType: string; description: string; sourceReference?: string; externalUri?: string }
): Promise<ApiResult<ApiIntelligenceDebtEvidence>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}/evidence`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function addIntelligenceDebtDependency(
  accessToken: string,
  tenantId: string,
  findingId: string,
  dependsOnFindingId: string
): Promise<ApiResult<{ id: string }>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}/dependencies`, accessToken, {
    method: "POST",
    body: JSON.stringify({ dependsOnFindingId }),
  });
}

export function removeIntelligenceDebtDependency(
  accessToken: string,
  tenantId: string,
  findingId: string,
  dependencyId: string
): Promise<ApiResult<undefined>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/intelligence-debt/${findingId}/dependencies/${dependencyId}`, accessToken, {
    method: "DELETE",
  });
}

export interface ApiAssessmentSummary {
  id: string;
  organizationId: string;
  organizationName: string | null;
  frameworkVersionId: string;
  frameworkVersionLabel: string | null;
  status: string;
  supersedesAssessmentId: string | null;
  createdAtUtc: string;
  submittedAtUtc: string | null;
  completedAtUtc: string | null;
  answeredCount: number;
  totalCount: number;
}

export interface ApiMaturityLevel {
  id: string;
  level: number;
  name: string;
  description: string | null;
}

export interface ApiAssessmentResponse {
  answerState: string;
  selectedMaturityLevelId: string | null;
  respondentComment: string | null;
  confidence: string | null;
  evidenceReferences: string[] | null;
}

export interface ApiAssessmentQuestion {
  id: string;
  code: string;
  text: string;
  response: ApiAssessmentResponse | null;
}

export interface ApiAssessmentCapability {
  id: string;
  code: string;
  name: string;
  description: string | null;
  evidenceGuidance: string | null;
  questions: ApiAssessmentQuestion[];
}

export interface ApiAssessmentDimension {
  id: string;
  code: string;
  name: string;
  fundamentalQuestion: string | null;
  capabilities: ApiAssessmentCapability[];
}

export interface ApiAssessmentDetail {
  id: string;
  organizationId: string;
  organizationName: string | null;
  frameworkVersionLabel: string;
  status: string;
  supersedesAssessmentId: string | null;
  createdAtUtc: string;
  submittedAtUtc: string | null;
  completedAtUtc: string | null;
  maturityLevels: ApiMaturityLevel[];
  dimensions: ApiAssessmentDimension[];
}

export interface ApiDimensionScore {
  dimensionId: string;
  code: string;
  name: string;
  score: number | null;
  maturityBand: string | null;
}

export interface ApiCapabilityScore {
  capabilityId: string;
  code: string;
  name: string;
  dimensionId: string;
  score: number | null;
  answeredQuestionCount: number;
}

export interface ApiAssessmentResult {
  assessmentId: string;
  calculatedAtUtc: string;
  compositeAverage: number | null;
  dimensionScores: ApiDimensionScore[];
  capabilityScores: ApiCapabilityScore[];
}

export function listAssessments(
  accessToken: string,
  tenantId: string
): Promise<ApiResult<ApiAssessmentSummary[]>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/assessments`, accessToken);
}

export function getAssessment(
  accessToken: string,
  tenantId: string,
  id: string
): Promise<ApiResult<ApiAssessmentDetail>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/assessments/${id}`, accessToken);
}

export function createAssessment(
  accessToken: string,
  tenantId: string,
  body: { organizationId: string; supersedesAssessmentId?: string }
): Promise<ApiResult<ApiAssessmentSummary>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/assessments`, accessToken, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function saveAssessmentResponse(
  accessToken: string,
  tenantId: string,
  assessmentId: string,
  questionId: string,
  body: {
    answerState: string;
    selectedMaturityLevelId?: string | null;
    respondentComment?: string;
    confidence?: string;
    evidenceReferences?: string[];
  }
): Promise<ApiResult<ApiAssessmentResponse>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/assessments/${assessmentId}/responses/${questionId}`, accessToken, {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function submitAssessment(
  accessToken: string,
  tenantId: string,
  assessmentId: string
): Promise<ApiResult<{ assessmentId: string; status: string }>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/assessments/${assessmentId}/submit`, accessToken, {
    method: "POST",
  });
}

export function getAssessmentResult(
  accessToken: string,
  tenantId: string,
  assessmentId: string
): Promise<ApiResult<ApiAssessmentResult>> {
  return apiFetch(`/api/v1/tenants/${tenantId}/assessments/${assessmentId}/result`, accessToken);
}
