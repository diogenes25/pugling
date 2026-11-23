using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClassTestPrepDuty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceKlassenarbeitId",
                table: "PlanPositions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PrepStartedOn",
                table: "Klassenarbeiten",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrepWindowDays",
                table: "Klassenarbeiten",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanPositions_SourceKlassenarbeitId",
                table: "PlanPositions",
                column: "SourceKlassenarbeitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanPositions_Klassenarbeiten_SourceKlassenarbeitId",
                table: "PlanPositions",
                column: "SourceKlassenarbeitId",
                principalTable: "Klassenarbeiten",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanPositions_Klassenarbeiten_SourceKlassenarbeitId",
                table: "PlanPositions");

            migrationBuilder.DropIndex(
                name: "IX_PlanPositions_SourceKlassenarbeitId",
                table: "PlanPositions");

            migrationBuilder.DropColumn(
                name: "SourceKlassenarbeitId",
                table: "PlanPositions");

            migrationBuilder.DropColumn(
                name: "PrepStartedOn",
                table: "Klassenarbeiten");

            migrationBuilder.DropColumn(
                name: "PrepWindowDays",
                table: "Klassenarbeiten");
        }
    }
}
