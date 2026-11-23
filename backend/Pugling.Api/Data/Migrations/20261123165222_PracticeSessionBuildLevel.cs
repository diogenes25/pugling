using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <summary>
    /// B-881: a practice round remembers the build level it plays, like a test attempt since B-849. A round on a staged
    /// position is stamped with the level the position stands on - the same approximation as <c>BuildLevelPromotion</c>,
    /// only for databases that ran <c>BuildLevelPlayed</c> on their own; every other round keeps <c>null</c>. A round open on
    /// the day of a raise still waiting for tomorrow is stamped with the raised level and refused as overtaken - accepted:
    /// a fresh round can be started, and a database that gets both migrations in one start has no waiting raise.
    /// </summary>
    public partial class PracticeSessionBuildLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuildLevel",
                table: "PracticeSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE PracticeSessions SET BuildLevel = " +
                "(SELECT p.CurrentBuildLevel FROM PlanPositions p WHERE p.Id = PracticeSessions.PlanPositionId) " +
                "WHERE PlanPositionId IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildLevel",
                table: "PracticeSessions");
        }
    }
}
