using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewBonusPaidAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NewBonusPaidAt",
                table: "PositionItemProgress",
                type: "TEXT",
                nullable: true);

            // Existing rows: the old rule paid the new-content points on the first graded review (ReviewCount 0), so a row
            // with a graded review already had its one chance - mark it paid, or the new rule would pay it a second time
            // (measured 2026-11-14 on a copy of the live DB: one child had 11 such payments). Untouched rows stay null.
            migrationBuilder.Sql(
                "UPDATE \"PositionItemProgress\" SET \"NewBonusPaidAt\" = COALESCE(\"LastReviewedAt\", '2026-11-14 00:00:00') " +
                "WHERE \"ReviewCount\" > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewBonusPaidAt",
                table: "PositionItemProgress");
        }
    }
}
