using Microsoft.EntityFrameworkCore;
using OrganizationalSingularity.Domain.Assessments;
using OrganizationalSingularity.Domain.Framework;
using OrganizationalSingularity.Domain.IntelligenceDebt;

namespace OrganizationalSingularity.Infrastructure.Persistence;

/// <summary>
/// Seeds Framework Version 1.0.0 from OS-ASSESS-OIQ-001 v1.0 (11 dimensions, 22
/// capabilities, 44 questions, 5 maturity levels, 5 maturity bands). Mirrors
/// UserProvisioningService.EnsureBootstrapMembershipAsync's pattern: creating the version
/// itself fires exactly once ever (skips if any FrameworkVersion already exists), since a
/// published FrameworkVersion is immutable and this is a one-time seed, not a repeatable
/// migration. All seeded content is marked Operationalized, not Canonical -- per explicit
/// instruction, the 44 questions and scoring thresholds are SoverAIgn's own measurement
/// implementation, not verbatim source material.
///
/// Intelligence Debt category/severity mappings are handled separately by
/// EnsureIntelligenceDebtMappingsSeededAsync, which runs every call (not gated by "does a
/// FrameworkVersion already exist") and is scoped + idempotent per FrameworkVersion --
/// see that method for why.
/// </summary>
public static class FrameworkSeeder
{
    private const string SourceDocument = "Organizational Singularity Assessment & OIQ Specification v1.0 (OS-ASSESS-OIQ-001)";
    private const string MethodologyStatus = "Initial controlled implementation specification";

    private sealed record QuestionSeed(string Code, string Text);

    private sealed record CapabilitySeed(
        string Code, string Name, string? Description, string? EvidenceGuidance, QuestionSeed[] Questions);

    /// <summary>DebtCategory here is the ONLY place v1's dimension-to-category methodology
    /// is written down in code -- EnsureIntelligenceDebtMappingsSeededAsync reads it from
    /// here (by Code) rather than keeping a second copy. Provisional engineering mapping,
    /// not sourced from an approved spec citation -- see IntelligenceDebtCategoryMapping.</summary>
    private sealed record DimensionSeed(
        string Code, string Name, string FundamentalQuestion, IntelligenceDebtCategory DebtCategory, CapabilitySeed[] Capabilities);

    /// <summary>DebtSeverity here is the ONLY place v1's band-to-severity methodology is
    /// written down in code. Provisional (reproduces AssessmentDebtDetector's former
    /// hardcoded rule exactly: Fragmented -> High, everything else -> Moderate), not
    /// sourced from an approved spec citation -- see IntelligenceDebtSeverityMapping.</summary>
    private sealed record MaturityBandSeed(string Name, decimal Min, decimal Max, int Sort, IntelligenceDebtSeverity DebtSeverity);

    public static async Task EnsureFrameworkV1SeededAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.FrameworkVersions.AnyAsync(ct))
        {
            var framework = new FrameworkVersion
            {
                Name = "Organizational Singularity OIQ Framework",
                Version = "1.0.0",
                IsPublished = true,
                PublishedAtUtc = DateTimeOffset.UtcNow,
            };
            db.FrameworkVersions.Add(framework);

            SeedMaturityLevels(db, framework.Id);
            SeedMaturityBands(db, framework.Id);
            SeedDimensions(db, framework.Id);

            await db.SaveChangesAsync(ct);
        }

        // Single authoritative path for category/severity mappings, run unconditionally so
        // fresh creation and an environment upgrading from before these tables existed both
        // arrive at the same persisted result through the same code.
        await EnsureIntelligenceDebtMappingsSeededAsync(db, ct);
    }

    /// <summary>
    /// Seeds IntelligenceDebtCategoryMapping and IntelligenceDebtSeverityMapping rows for
    /// every FrameworkVersion that doesn't already have them. Deliberately scoped and
    /// checked per FrameworkVersion -- never a table-wide "does any mapping exist anywhere"
    /// check -- so this remains correct whether there is one FrameworkVersion or several:
    /// a version that already has mappings is left alone, one that doesn't gets backfilled,
    /// and nothing here ever guesses which version is "the" one to touch.
    ///
    /// Matches dimensions/bands by Code/Name against GetDimensionSeeds()/GetMaturityBandSeeds()
    /// -- today that data only describes Framework Version 1.0.0, so this can only ever
    /// actually seed mappings for a v1-shaped FrameworkVersion. A hypothetical future
    /// FrameworkVersion with different dimension codes would simply get no mappings from
    /// this method (structurally correct: this method is v1's own seed data, not a general
    /// methodology generator; a v2 needs its own seeding code with its own methodology,
    /// which this hardening pass does not add). AssessmentDebtDetector already treats "no
    /// mapping for this dimension" as an explicit, visible skip rather than a silent guess.
    /// </summary>
    private static async Task EnsureIntelligenceDebtMappingsSeededAsync(AppDbContext db, CancellationToken ct)
    {
        var dimensionSeedsByCode = GetDimensionSeeds().ToDictionary(s => s.Code);
        var bandSeedsByName = GetMaturityBandSeeds().ToDictionary(s => s.Name);

        var frameworkVersionIds = await db.FrameworkVersions.Select(f => f.Id).ToListAsync(ct);
        foreach (var frameworkVersionId in frameworkVersionIds)
        {
            var hasCategoryMappings = await db.IntelligenceDebtCategoryMappings
                .AnyAsync(m => m.FrameworkVersionId == frameworkVersionId, ct);
            if (!hasCategoryMappings)
            {
                var dimensions = await db.Dimensions
                    .Where(d => d.FrameworkVersionId == frameworkVersionId)
                    .ToListAsync(ct);
                foreach (var dimension in dimensions)
                {
                    if (!dimensionSeedsByCode.TryGetValue(dimension.Code, out var seed))
                    {
                        continue;
                    }
                    db.IntelligenceDebtCategoryMappings.Add(new IntelligenceDebtCategoryMapping
                    {
                        FrameworkVersionId = frameworkVersionId,
                        DimensionId = dimension.Id,
                        Category = seed.DebtCategory,
                        Provenance = MakeProvenance(
                            "Intelligence Debt Register v1.0 (dimension-to-category mapping, not a spec citation -- provisional, requires SoverAIgn review)"),
                    });
                }
            }

            var hasSeverityMappings = await db.IntelligenceDebtSeverityMappings
                .AnyAsync(m => m.FrameworkVersionId == frameworkVersionId, ct);
            if (!hasSeverityMappings)
            {
                var bands = await db.MaturityBands
                    .Where(b => b.FrameworkVersionId == frameworkVersionId)
                    .ToListAsync(ct);
                foreach (var band in bands)
                {
                    if (!bandSeedsByName.TryGetValue(band.Name, out var seed))
                    {
                        continue;
                    }
                    db.IntelligenceDebtSeverityMappings.Add(new IntelligenceDebtSeverityMapping
                    {
                        FrameworkVersionId = frameworkVersionId,
                        MaturityBandId = band.Id,
                        Severity = seed.DebtSeverity,
                        Provenance = MakeProvenance(
                            "Intelligence Debt Register v1.0 (band-to-severity mapping, not a spec citation -- provisional, requires SoverAIgn review)"),
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static Provenance MakeProvenance(string section) => new()
    {
        SourceDocument = SourceDocument,
        SourceSection = section,
        SourceClassification = ProvenanceClassification.Operationalized,
        MethodologyStatus = MethodologyStatus,
    };

    private static void SeedMaturityLevels(AppDbContext db, Guid frameworkVersionId)
    {
        (int Level, string Name, string Description)[] levels =
        [
            (1, "Fragmented", "Capability is mostly informal, isolated, reactive, undocumented, inconsistent or dependent upon individuals."),
            (2, "Emerging", "Useful practices exist in some teams or situations, but they are inconsistent and not organization-wide."),
            (3, "Defined", "The organization has documented, repeatable practices with defined ownership and expectations."),
            (4, "Integrated", "The capability operates across relevant people, processes, systems, data and decisions and is measured or governed."),
            (5, "Adaptive", "The capability continuously learns from evidence and outcomes and systematically improves future organizational behavior."),
        ];

        foreach (var (level, name, description) in levels)
        {
            db.MaturityLevels.Add(new MaturityLevel
            {
                FrameworkVersionId = frameworkVersionId,
                Level = level,
                Name = name,
                Description = description,
                Provenance = MakeProvenance("§2"),
            });
        }
    }

    private static MaturityBandSeed[] GetMaturityBandSeeds() =>
    [
        new("Fragmented", 1.00m, 1.79m, 1, IntelligenceDebtSeverity.High),
        new("Emerging", 1.80m, 2.59m, 2, IntelligenceDebtSeverity.Moderate),
        new("Defined", 2.60m, 3.39m, 3, IntelligenceDebtSeverity.Moderate),
        new("Integrated", 3.40m, 4.19m, 4, IntelligenceDebtSeverity.Moderate),
        new("Adaptive", 4.20m, 5.00m, 5, IntelligenceDebtSeverity.Moderate),
    ];

    private static void SeedMaturityBands(AppDbContext db, Guid frameworkVersionId)
    {
        foreach (var seed in GetMaturityBandSeeds())
        {
            db.MaturityBands.Add(new MaturityBand
            {
                FrameworkVersionId = frameworkVersionId,
                Name = seed.Name,
                MinScore = seed.Min,
                MaxScore = seed.Max,
                SortOrder = seed.Sort,
                Provenance = MakeProvenance("§5"),
            });
        }
    }

    private static DimensionSeed[] GetDimensionSeeds() =>
    [
        new("D01", "Sensing", "Can the organization detect meaningful change?", IntelligenceDebtCategory.ConflictingDefinitionsAndData,
        [
            new("C01.1", "Environmental Sensing",
                "Ability to identify material changes in customers, markets, operations, technology, risk and the external environment.",
                "KPI alerts, customer feedback, risk registers, operational monitoring, market intelligence, incident reporting.",
                [
                    new("Q001", "How consistently does the organization identify important changes, risks and opportunities early enough to respond effectively?"),
                    new("Q002", "How effectively are signals from customers, employees, operations, systems and external sources brought together rather than remaining isolated?"),
                ]),
            new("C01.2", "Internal State Awareness", null, null,
                [
                    new("Q003", "How accurately can leaders understand the organization's current operational condition using timely and trusted information?"),
                    new("Q004", "How consistently are important operational exceptions, bottlenecks and emerging problems detected before they become larger problems?"),
                ]),
        ]),
        new("D02", "Understanding", "Can it turn information into shared context?", IntelligenceDebtCategory.FragmentedKnowledge,
        [
            new("C02.1", "Shared Context", null, null,
                [
                    new("Q005", "When important information crosses departments, how consistently do teams share definitions, context and meaning?"),
                    new("Q006", "How effectively can decision-makers connect information from multiple sources into a coherent understanding of a business issue?"),
                ]),
            new("C02.2", "Evidence-Based Understanding",
                "Technical connectivity alone is insufficient because connected systems can exchange data without sharing meaning, authority, trust or coordinated action.",
                null,
                [
                    new("Q007", "How consistently can important conclusions be traced to authoritative data, knowledge or other evidence?"),
                    new("Q008", "When sources disagree, how clearly can the organization determine which information is authoritative and resolve the conflict?"),
                ]),
        ]),
        new("D03", "Decision-Making", "Can it make sound, traceable decisions?", IntelligenceDebtCategory.UndocumentedDecisions,
        [
            new("C03.1", "Decision Quality & Authority", null, null,
                [
                    new("Q009", "How consistently are significant decisions made using relevant evidence, appropriate expertise and clearly defined authority?"),
                    new("Q010", "How clearly does the organization define who may decide, approve, escalate or override consequential decisions?"),
                ]),
            new("C03.2", "Decision Traceability",
                "The canonical product example defines Decision Traceability through decision records, owners, supporting documents, expected outcomes, review dates and actual results.",
                null,
                [
                    new("Q011", "How consistently are significant decisions recorded with their owner, reasoning, assumptions and supporting evidence?"),
                    new("Q012", "How consistently are expected outcomes and actual results linked back to the decisions that produced them?"),
                ]),
        ]),
        new("D04", "Coordinated Action", "Can decisions become coordinated execution?", IntelligenceDebtCategory.DuplicatedWork,
        [
            new("C04.1", "Decision-to-Execution Coordination", null, null,
                [
                    new("Q013", "Once an important decision is made, how reliably is it translated into coordinated action across responsible teams and systems?"),
                    new("Q014", "How clearly are owners, dependencies, timelines and expected outcomes established for important actions?"),
                ]),
            new("C04.2", "Cross-Functional Coordination",
                "Decision Velocity is not speed alone; the framework defines it around movement from relevant information to sound decisions and then coordinated execution, with quality, accountability, context and feedback.",
                null,
                [
                    new("Q015", "How effectively can departments coordinate work that crosses organizational boundaries?"),
                    new("Q016", "When priorities or conditions change, how quickly can affected teams adjust without creating conflicting actions?"),
                ]),
        ]),
        new("D05", "Learning", "Do outcomes improve future behavior?", IntelligenceDebtCategory.InconsistentProcesses,
        [
            new("C05.1", "Outcome Learning", null, null,
                [
                    new("Q017", "How consistently does the organization compare the actual outcomes of important actions with what was originally expected?"),
                    new("Q018", "When results differ from expectations, how consistently are causes examined and lessons captured?"),
                ]),
            new("C05.2", "Continuous Improvement",
                "The Organizational Intelligence Cycle explicitly requires learning and improvement to feed future behavior; simply acting without preserving and reusing the lesson leaves the cycle incomplete.",
                null,
                [
                    new("Q019", "How reliably are lessons from successes, failures, incidents and projects incorporated into future processes and decisions?"),
                    new("Q020", "How consistently can the organization demonstrate that prior experience changes future organizational behavior?"),
                ]),
        ]),
        new("D06", "Knowledge Accessibility", "Can people and AI find authoritative organizational knowledge?", IntelligenceDebtCategory.InaccessibleExpertise,
        [
            new("C06.1", "Organizational Memory",
                "Organizational Memory exists precisely because organizations otherwise forget as people leave, projects end and knowledge fragments. The Canon emphasizes preservation of explicit, tacit, institutional and enterprise knowledge.",
                null,
                [
                    new("Q021", "How easily can employees find the authoritative knowledge required to perform important work?"),
                    new("Q022", "How effectively does the organization preserve important expertise, decision context and lessons when employees change roles or leave?"),
                ]),
            new("C06.2", "Knowledge Reuse", null, null,
                [
                    new("Q023", "How consistently can teams discover and reuse relevant work already performed elsewhere in the organization?"),
                    new("Q024", "How effectively can authorized people and systems retrieve knowledge in context rather than depending on personal memory or informal networks?"),
                ]),
        ]),
        new("D07", "Process Observability", "Can the organization understand how work actually flows?", IntelligenceDebtCategory.UnownedOrUnobservableProcesses,
        [
            new("C07.1", "Process Visibility", null, null,
                [
                    new("Q025", "How clearly can the organization see how important work actually moves across people, departments and systems?"),
                    new("Q026", "How consistently are process owners, handoffs, dependencies, exceptions and outcomes visible?"),
                ]),
            new("C07.2", "Process Measurement", null, null,
                [
                    new("Q027", "How consistently are important processes measured for time, quality, cost, errors or business outcomes?"),
                    new("Q028", "How effectively does process evidence lead to changes when bottlenecks, repeated work or failures are identified?"),
                ]),
        ]),
        new("D08", "System Interoperability", "Can systems and data contribute to coordinated intelligence?", IntelligenceDebtCategory.DisconnectedSystems,
        [
            new("C08.1", "Information Connectivity", null, null,
                [
                    new("Q029", "How effectively can important systems exchange the information required for cross-functional work?"),
                    new("Q030", "How much manual re-entry, copying, reconciliation or duplicate storage is still required because systems are disconnected?"),
                ]),
            new("C08.2", "Semantic & Operational Interoperability",
                "This distinction matters because integration itself does not guarantee organizational alignment.",
                null,
                [
                    new("Q031", "When systems exchange information, how consistently do they share common definitions and authoritative sources?"),
                    new("Q032", "How effectively can workflows span multiple systems without losing context, ownership or traceability?"),
                ]),
        ]),
        new("D09", "AI Governance", "Is AI deployed under defined authority, controls and accountability?", IntelligenceDebtCategory.UngovernedAiAndAutomation,
        [
            new("C09.1", "Governed AI Use", null, null,
                [
                    new("Q033", "How clearly does the organization know which AI capabilities are being used, by whom, for what purpose and with what data?"),
                    new("Q034", "How consistently are AI systems subject to approved policies, permissions, evaluation and monitoring appropriate to their risk?"),
                ]),
            new("C09.2", "AI Authority & Boundaries",
                "The framework specifically rejects increasing autonomy without corresponding auditability, permissions, escalation and outcome measurement.",
                null,
                [
                    new("Q035", "How clearly are AI systems limited to actions and information they are authorized to access?"),
                    new("Q036", "For consequential AI-supported actions, how consistently are approval, escalation and override requirements defined?"),
                ]),
        ]),
        new("D10", "Security & Trust", "Can organizational intelligence operate securely and reliably?", IntelligenceDebtCategory.ConflictingDefinitionsAndData,
        [
            new("C10.1", "Identity & Information Protection", null, null,
                [
                    new("Q037", "How consistently is access to organizational data and knowledge controlled according to identity, role and legitimate need?"),
                    new("Q038", "How effectively can the organization prevent one user, system or AI capability from accessing information outside its authorized scope?"),
                ]),
            new("C10.2", "Auditability & Trust", null, null,
                [
                    new("Q039", "How reliably can significant human, system and AI actions be traced to who or what performed them and what information was used?"),
                    new("Q040", "How consistently can leaders determine whether important information and AI-supported conclusions are trustworthy, current and appropriately sourced?"),
                ]),
        ]),
        new("D11", "Human Accountability", "Are people meaningfully responsible for consequential decisions and actions?", IntelligenceDebtCategory.UnownedOrUnobservableProcesses,
        [
            new("C11.1", "Responsible Human Oversight", null, null,
                [
                    new("Q041", "For decisions involving significant financial, legal, security, workforce or customer consequences, how clearly is human accountability assigned?"),
                    new("Q042", "How effectively can responsible people review, challenge, stop or override automated or AI-supported actions when necessary?"),
                ]),
            new("C11.2", "Human-AI Collaboration",
                "People remain central to Organizational Singularity because they contribute knowledge, judgment, creativity and experience; AI is an extension of organizational capability, not a replacement for accountability.",
                null,
                [
                    new("Q043", "How deliberately does the organization assign work according to the complementary strengths of people, AI and automation?"),
                    new("Q044", "How consistently are human judgment, experience and creativity preserved rather than treating AI output as automatically authoritative?"),
                ]),
        ]),
    ];

    private static void SeedDimensions(AppDbContext db, Guid frameworkVersionId)
    {
        var dimensionSort = 0;
        foreach (var dimensionSeed in GetDimensionSeeds())
        {
            dimensionSort++;
            var dimension = new Dimension
            {
                FrameworkVersionId = frameworkVersionId,
                Code = dimensionSeed.Code,
                Name = dimensionSeed.Name,
                FundamentalQuestion = dimensionSeed.FundamentalQuestion,
                SortOrder = dimensionSort,
                Provenance = MakeProvenance("§1"),
            };
            db.Dimensions.Add(dimension);

            var capabilitySort = 0;
            foreach (var capabilitySeed in dimensionSeed.Capabilities)
            {
                capabilitySort++;
                var capability = new Capability
                {
                    FrameworkVersionId = frameworkVersionId,
                    DimensionId = dimension.Id,
                    Code = capabilitySeed.Code,
                    Name = capabilitySeed.Name,
                    Description = capabilitySeed.Description,
                    EvidenceGuidance = capabilitySeed.EvidenceGuidance,
                    SortOrder = capabilitySort,
                    Provenance = MakeProvenance($"§3 ({capabilitySeed.Code})"),
                };
                db.Capabilities.Add(capability);

                var questionSort = 0;
                foreach (var questionSeed in capabilitySeed.Questions)
                {
                    questionSort++;
                    db.AssessmentQuestions.Add(new AssessmentQuestion
                    {
                        CapabilityId = capability.Id,
                        Code = questionSeed.Code,
                        Text = questionSeed.Text,
                        SortOrder = questionSort,
                        Provenance = MakeProvenance($"§3 ({capabilitySeed.Code})"),
                    });
                }
            }
        }
    }
}
