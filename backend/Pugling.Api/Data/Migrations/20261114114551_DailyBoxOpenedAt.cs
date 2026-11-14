using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DailyBoxOpenedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedAt",
                table: "DailyBoxClaims",
                type: "TEXT",
                nullable: true);

            // Existing claims were booked to the ledger at award time (the rule before B-527) - mark them opened, or they
            // would show up as waiting chests and be credited a second time when the child opens them.
            migrationBuilder.Sql("UPDATE \"DailyBoxClaims\" SET \"OpenedAt\" = \"AwardedAt\" WHERE \"OpenedAt\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenedAt",
                table: "DailyBoxClaims");
        }
    }
}
