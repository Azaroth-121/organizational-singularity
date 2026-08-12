using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalSingularity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntelligenceDebtCategoryMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntelligenceDebtCategoryMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DimensionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Provenance_SourceDocument = table.Column<string>(type: "text", nullable: false),
                    Provenance_SourceSection = table.Column<string>(type: "text", nullable: true),
                    Provenance_SourceClassification = table.Column<int>(type: "integer", nullable: false),
                    Provenance_MethodologyStatus = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceDebtCategoryMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtCategoryMappings_Dimensions_DimensionId",
                        column: x => x.DimensionId,
                        principalTable: "Dimensions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntelligenceDebtCategoryMappings_FrameworkVersions_Framewor~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "FrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtCategoryMappings_DimensionId",
                table: "IntelligenceDebtCategoryMappings",
                column: "DimensionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceDebtCategoryMappings_FrameworkVersionId_Dimensi~",
                table: "IntelligenceDebtCategoryMappings",
                columns: new[] { "FrameworkVersionId", "DimensionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntelligenceDebtCategoryMappings");
        }
    }
}
