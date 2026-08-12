using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalSingularity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntelligenceDebtRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntelligenceDebtFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DetectionSource = table.Column<int>(type: "integer", nullable: false),
                    BusinessImpact = table.Column<string>(type: "text", nullable: true),
                    AffectedScope = table.Column<string>(type: "text", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetResolutionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapabilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    DimensionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecommendedAction = table.Column<string>(type: "text", nullable: true),
                    RemediationPlan = table.Column<string>(type: "text", nullable: true),
                    ValidationCriteria = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RemediationStartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceDebtFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Capabilities_CapabilityId",
                        column: x => x.CapabilityId,
                        principalTable: "Capabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Dimensions_DimensionId",
                        column: x => x.DimensionId,
                        principalTable: "Dimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtFindings_Users_ValidatedByUserId",
                        column: x => x.ValidatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntelligenceDebtDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnFindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceDebtDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtDependencies_IntelligenceDebtFindings_Depen~",
                        column: x => x.DependsOnFindingId,
                        principalTable: "IntelligenceDebtFindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtDependencies_IntelligenceDebtFindings_Findi~",
                        column: x => x.FindingId,
                        principalTable: "IntelligenceDebtFindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntelligenceDebtEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SourceReference = table.Column<string>(type: "text", nullable: true),
                    AssessmentResponseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalUri = table.Column<string>(type: "text", nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceDebtEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtEvidence_AssessmentResponses_AssessmentResp~",
                        column: x => x.AssessmentResponseId,
                        principalTable: "AssessmentResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtEvidence_IntelligenceDebtFindings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "IntelligenceDebtFindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtEvidence_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDependencies_DependsOnFindingId",
                table: "IntelligenceDebtDependencies",
                column: "DependsOnFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDependencies_FindingId_DependsOnFindingId",
                table: "IntelligenceDebtDependencies",
                columns: new[] { "FindingId", "DependsOnFindingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDependencies_TenantId",
                table: "IntelligenceDebtDependencies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtEvidence_AddedByUserId",
                table: "IntelligenceDebtEvidence",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtEvidence_AssessmentResponseId",
                table: "IntelligenceDebtEvidence",
                column: "AssessmentResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtEvidence_FindingId",
                table: "IntelligenceDebtEvidence",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtEvidence_TenantId",
                table: "IntelligenceDebtEvidence",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_ApprovedByUserId",
                table: "IntelligenceDebtFindings",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_AssessmentId",
                table: "IntelligenceDebtFindings",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_CapabilityId",
                table: "IntelligenceDebtFindings",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_CreatedByUserId",
                table: "IntelligenceDebtFindings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_DimensionId",
                table: "IntelligenceDebtFindings",
                column: "DimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_OrganizationId",
                table: "IntelligenceDebtFindings",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_OwnerUserId",
                table: "IntelligenceDebtFindings",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_TenantId",
                table: "IntelligenceDebtFindings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_TenantId_Code",
                table: "IntelligenceDebtFindings",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_TenantId_Status",
                table: "IntelligenceDebtFindings",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtFindings_ValidatedByUserId",
                table: "IntelligenceDebtFindings",
                column: "ValidatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntelligenceDebtDependencies");

            migrationBuilder.DropTable(
                name: "IntelligenceDebtEvidence");

            migrationBuilder.DropTable(
                name: "IntelligenceDebtFindings");
        }
    }
}
