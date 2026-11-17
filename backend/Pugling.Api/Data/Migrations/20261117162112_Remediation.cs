using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Remediation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EasedStages",
                table: "PracticeSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RemediationDoneAt",
                table: "PositionItemProgress",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolvedByStage",
                table: "PositionItemProgress",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Remediation",
                table: "PlanPositions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EasedStages",
                table: "PracticeSessions");

            migrationBuilder.DropColumn(
                name: "RemediationDoneAt",
                table: "PositionItemProgress");

            migrationBuilder.DropColumn(
                name: "SolvedByStage",
                table: "PositionItemProgress");

            migrationBuilder.DropColumn(
                name: "Remediation",
                table: "PlanPositions");
        }
    }
}
