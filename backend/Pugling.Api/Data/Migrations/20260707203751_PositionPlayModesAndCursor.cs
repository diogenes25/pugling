using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PositionPlayModesAndCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Cursor",
                table: "TestAttempts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Order",
                table: "TestAttempts",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "Cursor",
                table: "PracticeSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Bestand: bisherige Sitzungen waren echte Lern-Sitzungen (Info-Modus ist neu) → Default Lern (1),
            // damit der historische Ziel-/Serien-Verlauf erhalten bleibt.
            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "PracticeSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Order",
                table: "PracticeSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "OrderStrategy",
                table: "PlanPositions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cursor",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "Cursor",
                table: "PracticeSessions");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "PracticeSessions");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "PracticeSessions");

            migrationBuilder.DropColumn(
                name: "OrderStrategy",
                table: "PlanPositions");
        }
    }
}
