using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PerformanceIndexesHotPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestAttempts_PlanPositionId",
                table: "TestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_TestAttempts_StudyPlanId",
                table: "TestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSessions_PlanPositionId",
                table: "PracticeSessions");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSessions_StudyPlanId",
                table: "PracticeSessions");

            migrationBuilder.DropIndex(
                name: "IX_PlanPositions_StudyPlanId",
                table: "PlanPositions");

            migrationBuilder.DropIndex(
                name: "IX_ChildPoints_ChildId",
                table: "ChildPoints");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_PlanPositionId_Day_CompletedAt_Passed",
                table: "TestAttempts",
                columns: new[] { "PlanPositionId", "Day", "CompletedAt", "Passed" });

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_StudyPlanId_Day",
                table: "TestAttempts",
                columns: new[] { "StudyPlanId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_PlanPositionId_Day_Mode",
                table: "PracticeSessions",
                columns: new[] { "PlanPositionId", "Day", "Mode" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_StudyPlanId_Day",
                table: "PracticeSessions",
                columns: new[] { "StudyPlanId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPositions_StudyPlanId_Order_Id",
                table: "PlanPositions",
                columns: new[] { "StudyPlanId", "Order", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ChildId_ExerciseId",
                table: "ItemProgress",
                columns: new[] { "ChildId", "ExerciseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildPoints_ChildId_CreatedAt_Id",
                table: "ChildPoints",
                columns: new[] { "ChildId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildPoints_ChildId_Kind",
                table: "ChildPoints",
                columns: new[] { "ChildId", "Kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestAttempts_PlanPositionId_Day_CompletedAt_Passed",
                table: "TestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_TestAttempts_StudyPlanId_Day",
                table: "TestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSessions_PlanPositionId_Day_Mode",
                table: "PracticeSessions");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSessions_StudyPlanId_Day",
                table: "PracticeSessions");

            migrationBuilder.DropIndex(
                name: "IX_PlanPositions_StudyPlanId_Order_Id",
                table: "PlanPositions");

            migrationBuilder.DropIndex(
                name: "IX_ItemProgress_ChildId_ExerciseId",
                table: "ItemProgress");

            migrationBuilder.DropIndex(
                name: "IX_ChildPoints_ChildId_CreatedAt_Id",
                table: "ChildPoints");

            migrationBuilder.DropIndex(
                name: "IX_ChildPoints_ChildId_Kind",
                table: "ChildPoints");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_PlanPositionId",
                table: "TestAttempts",
                column: "PlanPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_StudyPlanId",
                table: "TestAttempts",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_PlanPositionId",
                table: "PracticeSessions",
                column: "PlanPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_StudyPlanId",
                table: "PracticeSessions",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPositions_StudyPlanId",
                table: "PlanPositions",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPoints_ChildId",
                table: "ChildPoints",
                column: "ChildId");
        }
    }
}
