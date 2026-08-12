using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalSingularity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenIntelligenceDebtMethodology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IntelligenceDebtCategoryMappings_FrameworkVersionId_Dimensi~",
                table: "IntelligenceDebtCategoryMappings");

            migrationBuilder.CreateTable(
                name: "IntelligenceDebtSeverityMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaturityBandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Provenance_SourceDocument = table.Column<string>(type: "text", nullable: false),
                    Provenance_SourceSection = table.Column<string>(type: "text", nullable: true),
                    Provenance_SourceClassification = table.Column<int>(type: "integer", nullable: false),
                    Provenance_MethodologyStatus = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceDebtSeverityMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtSeverityMappings_FrameworkVersions_Framewor~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "FrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtSeverityMappings_MaturityBands_MaturityBand~",
                        column: x => x.MaturityBandId,
                        principalTable: "MaturityBands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntelligenceDebtDetectionProvenances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeverityMappingId = table.Column<Guid>(type: "uuid", nullable: false),
                    DimensionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapabilityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ObservedScore = table.Column<decimal>(type: "numeric", nullable: false),
                    MaturityBand = table.Column<string>(type: "text", nullable: false),
                    ThresholdUsed = table.Column<decimal>(type: "numeric", nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceDebtDetectionProvenances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtDetectionProvenances_Assessments_Assessment~",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtDetectionProvenances_FrameworkVersions_Fram~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "FrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtCatego~",
                        column: x => x.CategoryMappingId,
                        principalTable: "IntelligenceDebtCategoryMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtFindin~",
                        column: x => x.FindingId,
                        principalTable: "IntelligenceDebtFindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtDetectionProvenances_IntelligenceDebtSeveri~",
                        column: x => x.SeverityMappingId,
                        principalTable: "IntelligenceDebtSeverityMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtCategoryMappings_FrameworkVersionId_Dimensi~",
                table: "IntelligenceDebtCategoryMappings",
                columns: new[] { "FrameworkVersionId", "DimensionId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDetectionProvenances_AssessmentId",
                table: "IntelligenceDebtDetectionProvenances",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDetectionProvenances_CategoryMappingId",
                table: "IntelligenceDebtDetectionProvenances",
                column: "CategoryMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDetectionProvenances_FindingId",
                table: "IntelligenceDebtDetectionProvenances",
                column: "FindingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDetectionProvenances_FrameworkVersionId",
                table: "IntelligenceDebtDetectionProvenances",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDetectionProvenances_SeverityMappingId",
                table: "IntelligenceDebtDetectionProvenances",
                column: "SeverityMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtDetectionProvenances_TenantId",
                table: "IntelligenceDebtDetectionProvenances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtSeverityMappings_FrameworkVersionId_Maturit~",
                table: "IntelligenceDebtSeverityMappings",
                columns: new[] { "FrameworkVersionId", "MaturityBandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtSeverityMappings_MaturityBandId",
                table: "IntelligenceDebtSeverityMappings",
                column: "MaturityBandId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntelligenceDebtDetectionProvenances");

            migrationBuilder.DropTable(
                name: "IntelligenceDebtSeverityMappings");

            migrationBuilder.DropIndex(
                name: "IX_IntelligenceDebtCategoryMappings_FrameworkVersionId_Dimensi~",
                table: "IntelligenceDebtCategoryMappings");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtCategoryMappings_FrameworkVersionId_Dimensi~",
                table: "IntelligenceDebtCategoryMappings",
                columns: new[] { "FrameworkVersionId", "DimensionId" },
                unique: true);
        }
    }
}
