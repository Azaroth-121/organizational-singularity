namespace OrganizationalSingularity.Domain.Framework;

/// <summary>
/// Per OS-ASSESS-OIQ-001 §10: how directly seeded framework content traces to source
/// material. The 44 v1 questions and their scoring thresholds are Operationalized --
/// SoverAIgn's own measurement implementation -- not verbatim Canon material.
/// </summary>
public enum ProvenanceClassification
{
    Canonical,
    Master,
    Blueprint,
    Operationalized
}

/// <summary>
/// EF Core owned type (embedded columns, no separate table) attached to every seeded
/// framework-content entity -- Dimension, Capability, AssessmentQuestion, MaturityLevel,
/// MaturityBand. CreatedAtUtc (AuditableEntity) and the owning entity's own
/// FrameworkVersionId already satisfy the spec's createdAtUtc/frameworkVersion fields.
/// </summary>
public class Provenance
{
    public string SourceDocument { get; set; } = string.Empty;
    public string? SourceSection { get; set; }
    public ProvenanceClassification SourceClassification { get; set; }
    public string? MethodologyStatus { get; set; }
}
