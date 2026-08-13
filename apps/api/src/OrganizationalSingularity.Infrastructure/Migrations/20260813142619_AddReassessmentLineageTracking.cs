using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalSingularity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReassessmentLineageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assessments_SupersedesAssessmentId",
                table: "Assessments");

            migrationBuilder.AddColumn<Guid>(
                name: "CarriedForwardFromResponseId",
                table: "AssessmentResponses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAtUtc",
                table: "AssessmentResponses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCarriedForward",
                table: "AssessmentResponses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SupersedesAssessmentId",
                table: "Assessments",
                column: "SupersedesAssessmentId",
                unique: true,
                filter: "\"SupersedesAssessmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResponses_CarriedForwardFromResponseId",
                table: "AssessmentResponses",
                column: "CarriedForwardFromResponseId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentResponses_AssessmentResponses_CarriedForwardFromR~",
                table: "AssessmentResponses",
                column: "CarriedForwardFromResponseId",
                principalTable: "AssessmentResponses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentResponses_AssessmentResponses_CarriedForwardFromR~",
                table: "AssessmentResponses");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_SupersedesAssessmentId",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentResponses_CarriedForwardFromResponseId",
                table: "AssessmentResponses");

            migrationBuilder.DropColumn(
                name: "CarriedForwardFromResponseId",
                table: "AssessmentResponses");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "AssessmentResponses");

            migrationBuilder.DropColumn(
                name: "IsCarriedForward",
                table: "AssessmentResponses");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SupersedesAssessmentId",
                table: "Assessments",
                column: "SupersedesAssessmentId");
        }
    }
}
