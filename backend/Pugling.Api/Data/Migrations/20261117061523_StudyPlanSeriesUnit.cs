using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudyPlanSeriesUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeriesUnitId",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_SeriesUnitId",
                table: "StudyPlans",
                column: "SeriesUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudyPlans_SeriesUnits_SeriesUnitId",
                table: "StudyPlans",
                column: "SeriesUnitId",
                principalTable: "SeriesUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // B-539: the subject becomes the group of the activation rule. A legacy plan without one takes the majority
            // subject of its positions (position -> exercise -> unit -> series); a tie or no derivable subject leaves it
            // empty rather than guessing, and a plan that already has a subject is never touched.
            migrationBuilder.Sql("""
                WITH counts AS (
                    SELECT pp.StudyPlanId AS PlanId, ts.SubjectId AS SubjectId, COUNT(*) AS N
                    FROM PlanPositions pp
                    JOIN Exercises e ON e.Id = pp.ExerciseId
                    JOIN SeriesUnits su ON su.Id = e.SeriesUnitId
                    JOIN TextbookSeries ts ON ts.Id = su.SeriesId
                    WHERE ts.SubjectId IS NOT NULL
                    GROUP BY pp.StudyPlanId, ts.SubjectId
                ),
                winners AS (
                    SELECT c.PlanId, c.SubjectId
                    FROM counts c
                    WHERE c.N = (SELECT MAX(c2.N) FROM counts c2 WHERE c2.PlanId = c.PlanId)
                      AND (SELECT COUNT(*) FROM counts c3
                           WHERE c3.PlanId = c.PlanId
                             AND c3.N = (SELECT MAX(c4.N) FROM counts c4 WHERE c4.PlanId = c.PlanId)) = 1
                )
                UPDATE StudyPlans
                SET SubjectId = (SELECT w.SubjectId FROM winners w WHERE w.PlanId = StudyPlans.Id)
                WHERE SubjectId IS NULL
                  AND Id IN (SELECT PlanId FROM winners);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudyPlans_SeriesUnits_SeriesUnitId",
                table: "StudyPlans");

            migrationBuilder.DropIndex(
                name: "IX_StudyPlans_SeriesUnitId",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "SeriesUnitId",
                table: "StudyPlans");
        }
    }
}
