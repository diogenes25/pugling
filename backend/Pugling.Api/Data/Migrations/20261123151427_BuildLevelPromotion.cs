using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <summary>
    /// B-849: an attempt remembers the build level it examined, and a position the day its raised level starts. Until now
    /// a staged position could only change level by the father's hand, so an attempt on it is stamped with the level the
    /// position stands on - an approximation where the father changed the level since or a level's prerequisite is gone
    /// (that attempt examined the target). Only databases that ran <c>BuildLevelPlayed</c> on their own have such rows;
    /// existing positions keep playing at once (<c>null</c>).
    /// </summary>
    public partial class BuildLevelPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuildLevel",
                table: "TestAttempts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE TestAttempts SET BuildLevel = " +
                "(SELECT p.CurrentBuildLevel FROM PlanPositions p WHERE p.Id = TestAttempts.PlanPositionId) " +
                "WHERE PlanPositionId IS NOT NULL;");

            migrationBuilder.AddColumn<DateOnly>(
                name: "BuildLevelEffectiveOn",
                table: "PlanPositions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildLevel",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "BuildLevelEffectiveOn",
                table: "PlanPositions");
        }
    }
}
