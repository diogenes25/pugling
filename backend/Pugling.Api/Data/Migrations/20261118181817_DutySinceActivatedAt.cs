using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DutySinceActivatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ActivatedAt",
                table: "StudyPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DutySince",
                table: "PlanPositions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // B-613: every existing plan counts as switched on since its start, every existing position as a duty since the
            // day it was created - the rule before this migration, so no streak and no open penalty moves by the backfill.
            migrationBuilder.Sql("UPDATE StudyPlans SET ActivatedAt = StartDate;");
            migrationBuilder.Sql("UPDATE PlanPositions SET DutySince = date(CreatedAt);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "DutySince",
                table: "PlanPositions");
        }
    }
}
