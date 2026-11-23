using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <summary>
    /// B-848: a position may play a prerequisite of its exercise (<c>CurrentBuildLevel</c>), and its item progress is
    /// kept per played exercise. Every existing progress row was made on the position's own exercise, so it is filled
    /// with that one before the foreign key arrives - box, due date and learn-card state stay as they are.
    /// </summary>
    public partial class BuildLevelPlayed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PositionItemProgress_PlanPositionId_ItemIndex",
                table: "PositionItemProgress");

            migrationBuilder.AddColumn<int>(
                name: "ExerciseId",
                table: "PositionItemProgress",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE PositionItemProgress SET ExerciseId = " +
                "(SELECT p.ExerciseId FROM PlanPositions p WHERE p.Id = PositionItemProgress.PlanPositionId);");

            migrationBuilder.AddColumn<int>(
                name: "CurrentBuildLevel",
                table: "PlanPositions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionItemProgress_ExerciseId",
                table: "PositionItemProgress",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionItemProgress_PlanPositionId_ExerciseId_ItemIndex",
                table: "PositionItemProgress",
                columns: new[] { "PlanPositionId", "ExerciseId", "ItemIndex" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionItemProgress_Exercises_ExerciseId",
                table: "PositionItemProgress",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PositionItemProgress_Exercises_ExerciseId",
                table: "PositionItemProgress");

            migrationBuilder.DropIndex(
                name: "IX_PositionItemProgress_ExerciseId",
                table: "PositionItemProgress");

            migrationBuilder.DropIndex(
                name: "IX_PositionItemProgress_PlanPositionId_ExerciseId_ItemIndex",
                table: "PositionItemProgress");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "PositionItemProgress");

            migrationBuilder.DropColumn(
                name: "CurrentBuildLevel",
                table: "PlanPositions");

            migrationBuilder.CreateIndex(
                name: "IX_PositionItemProgress_PlanPositionId_ItemIndex",
                table: "PositionItemProgress",
                columns: new[] { "PlanPositionId", "ItemIndex" },
                unique: true);
        }
    }
}
