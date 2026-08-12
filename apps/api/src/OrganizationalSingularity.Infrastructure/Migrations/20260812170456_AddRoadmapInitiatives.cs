using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalSingularity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadmapInitiatives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Initiatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedOutcome = table.Column<string>(type: "text", nullable: true),
                    TargetStartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TargetCompletionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Initiatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Initiatives_IntelligenceDebtFindings_SourceFindingId",
                        column: x => x.SourceFindingId,
                        principalTable: "IntelligenceDebtFindings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Initiatives_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Initiatives_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Initiatives_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DependsOnInitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeDependencies_Initiatives_DependsOnInitiativeId",
                        column: x => x.DependsOnInitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InitiativeDependencies_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiativeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeMilestones_Initiatives_InitiativeId",
                        column: x => x.InitiativeId,
                        principalTable: "Initiatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeDependencies_DependsOnInitiativeId",
                table: "InitiativeDependencies",
                column: "DependsOnInitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeDependencies_InitiativeId_DependsOnInitiativeId",
                table: "InitiativeDependencies",
                columns: new[] { "InitiativeId", "DependsOnInitiativeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeDependencies_TenantId",
                table: "InitiativeDependencies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMilestones_InitiativeId",
                table: "InitiativeMilestones",
                column: "InitiativeId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeMilestones_TenantId",
                table: "InitiativeMilestones",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_CreatedByUserId",
                table: "Initiatives",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_OrganizationId",
                table: "Initiatives",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_OwnerUserId",
                table: "Initiatives",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_SourceFindingId",
                table: "Initiatives",
                column: "SourceFindingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_TenantId",
                table: "Initiatives",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Initiatives_TenantId_Code",
                table: "Initiatives",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InitiativeDependencies");

            migrationBuilder.DropTable(
                name: "InitiativeMilestones");

            migrationBuilder.DropTable(
                name: "Initiatives");
        }
    }
}
