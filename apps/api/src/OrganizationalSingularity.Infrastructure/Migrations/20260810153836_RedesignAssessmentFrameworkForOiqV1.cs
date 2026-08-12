using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalSingularity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignAssessmentFrameworkForOiqV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Capabilities_FrameworkVersionId",
                table: "Capabilities");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentQuestions_CapabilityId",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "EvidenceRequired",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "Dimension",
                table: "Capabilities");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Capabilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "AssessmentResponses",
                newName: "RespondentComment");

            migrationBuilder.AddColumn<string>(
                name: "Provenance_MethodologyStatus",
                table: "MaturityLevels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provenance_SourceClassification",
                table: "MaturityLevels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Provenance_SourceDocument",
                table: "MaturityLevels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provenance_SourceSection",
                table: "MaturityLevels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Capabilities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "DimensionId",
                table: "Capabilities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "EvidenceGuidance",
                table: "Capabilities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provenance_MethodologyStatus",
                table: "Capabilities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provenance_SourceClassification",
                table: "Capabilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Provenance_SourceDocument",
                table: "Capabilities",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provenance_SourceSection",
                table: "Capabilities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAtUtc",
                table: "Assessments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesAssessmentId",
                table: "Assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnswerState",
                table: "AssessmentResponses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Confidence",
                table: "AssessmentResponses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "EvidenceReferences",
                table: "AssessmentResponses",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedMaturityLevelId",
                table: "AssessmentResponses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "AssessmentQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provenance_MethodologyStatus",
                table: "AssessmentQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provenance_SourceClassification",
                table: "AssessmentQuestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Provenance_SourceDocument",
                table: "AssessmentQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Provenance_SourceSection",
                table: "AssessmentQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "AssessmentQuestions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AssessmentResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalculatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompositeAverage = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dimensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FundamentalQuestion = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Provenance_SourceDocument = table.Column<string>(type: "text", nullable: false),
                    Provenance_SourceSection = table.Column<string>(type: "text", nullable: true),
                    Provenance_SourceClassification = table.Column<int>(type: "integer", nullable: false),
                    Provenance_MethodologyStatus = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dimensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dimensions_FrameworkVersions_FrameworkVersionId",
                        column: x => x.FrameworkVersionId,
                        principalTable: "FrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaturityBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MinScore = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Provenance_SourceDocument = table.Column<string>(type: "text", nullable: false),
                    Provenance_SourceSection = table.Column<string>(type: "text", nullable: true),
                    Provenance_SourceClassification = table.Column<int>(type: "integer", nullable: false),
                    Provenance_MethodologyStatus = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaturityBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaturityBands_FrameworkVersions_FrameworkVersionId",
                        column: x => x.FrameworkVersionId,
                        principalTable: "FrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CapabilityScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: true),
                    AnsweredQuestionCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapabilityScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapabilityScores_AssessmentResults_AssessmentResultId",
                        column: x => x.AssessmentResultId,
                        principalTable: "AssessmentResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CapabilityScores_Capabilities_CapabilityId",
                        column: x => x.CapabilityId,
                        principalTable: "Capabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DimensionScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    DimensionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: true),
                    MaturityBand = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimensionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DimensionScores_AssessmentResults_AssessmentResultId",
                        column: x => x.AssessmentResultId,
                        principalTable: "AssessmentResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DimensionScores_Dimensions_DimensionId",
                        column: x => x.DimensionId,
                        principalTable: "Dimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_DimensionId",
                table: "Capabilities",
                column: "DimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_FrameworkVersionId_Code",
                table: "Capabilities",
                columns: new[] { "FrameworkVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SupersedesAssessmentId",
                table: "Assessments",
                column: "SupersedesAssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_ReviewedMaturityLevelId",
                table: "AssessmentResponses",
                column: "ReviewedMaturityLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_CapabilityId_Code",
                table: "AssessmentQuestions",
                columns: new[] { "CapabilityId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_AssessmentId",
                table: "AssessmentResults",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_TenantId",
                table: "AssessmentResults",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityScores_AssessmentResultId_CapabilityId",
                table: "CapabilityScores",
                columns: new[] { "AssessmentResultId", "CapabilityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityScores_CapabilityId",
                table: "CapabilityScores",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityScores_TenantId",
                table: "CapabilityScores",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Dimensions_FrameworkVersionId_Code",
                table: "Dimensions",
                columns: new[] { "FrameworkVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimensionScores_AssessmentResultId_DimensionId",
                table: "DimensionScores",
                columns: new[] { "AssessmentResultId", "DimensionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimensionScores_DimensionId",
                table: "DimensionScores",
                column: "DimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_DimensionScores_TenantId",
                table: "DimensionScores",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MaturityBands_FrameworkVersionId_Name",
                table: "MaturityBands",
                columns: new[] { "FrameworkVersionId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResponses_MaturityLevels_ReviewedMaturityLevelId",
                table: "AssessmentResponses",
                column: "ReviewedMaturityLevelId",
                principalTable: "MaturityLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_Assessments_SupersedesAssessmentId",
                table: "Assessments",
                column: "SupersedesAssessmentId",
                principalTable: "Assessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Capabilities_Dimensions_DimensionId",
                table: "Capabilities",
                column: "DimensionId",
                principalTable: "Dimensions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResponses_MaturityLevels_ReviewedMaturityLevelId",
                table: "AssessmentResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_Assessments_SupersedesAssessmentId",
                table: "Assessments");

            migrationBuilder.DropForeignKey(
                name: "FK_Capabilities_Dimensions_DimensionId",
                table: "Capabilities");

            migrationBuilder.DropTable(
                name: "CapabilityScores");

            migrationBuilder.DropTable(
                name: "DimensionScores");

            migrationBuilder.DropTable(
                name: "MaturityBands");

            migrationBuilder.DropTable(
                name: "AssessmentResults");

            migrationBuilder.DropTable(
                name: "Dimensions");

            migrationBuilder.DropIndex(
                name: "IX_Capabilities_DimensionId",
                table: "Capabilities");

            migrationBuilder.DropIndex(
                name: "IX_Capabilities_FrameworkVersionId_Code",
                table: "Capabilities");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_SupersedesAssessmentId",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResponses_ReviewedMaturityLevelId",
                table: "AssessmentResponses");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentQuestions_CapabilityId_Code",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "Provenance_MethodologyStatus",
                table: "MaturityLevels");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceClassification",
                table: "MaturityLevels");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceDocument",
                table: "MaturityLevels");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceSection",
                table: "MaturityLevels");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Capabilities");

            migrationBuilder.DropColumn(
                name: "DimensionId",
                table: "Capabilities");

            migrationBuilder.DropColumn(
                name: "EvidenceGuidance",
                table: "Capabilities");

            migrationBuilder.DropColumn(
                name: "Provenance_MethodologyStatus",
                table: "Capabilities");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceClassification",
                table: "Capabilities");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceDocument",
                table: "Capabilities");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceSection",
                table: "Capabilities");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "SupersedesAssessmentId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "AnswerState",
                table: "AssessmentResponses");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "AssessmentResponses");

            migrationBuilder.DropColumn(
                name: "EvidenceReferences",
                table: "AssessmentResponses");

            migrationBuilder.DropColumn(
                name: "ReviewedMaturityLevelId",
                table: "AssessmentResponses");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "Provenance_MethodologyStatus",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceClassification",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceDocument",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "Provenance_SourceSection",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "AssessmentQuestions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Capabilities");

            migrationBuilder.AddColumn<int>(
                name: "Dimension",
                table: "Capabilities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.RenameColumn(
                name: "RespondentComment",
                table: "AssessmentResponses",
                newName: "Notes");

            migrationBuilder.AddColumn<bool>(
                name: "EvidenceRequired",
                table: "AssessmentQuestions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_FrameworkVersionId",
                table: "Capabilities",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_CapabilityId",
                table: "AssessmentQuestions",
                column: "CapabilityId");
        }
    }
}
