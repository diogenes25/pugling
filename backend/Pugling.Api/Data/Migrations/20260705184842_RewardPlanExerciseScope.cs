using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RewardPlanExerciseScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExerciseId",
                table: "Rewards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudyPlanId",
                table: "Rewards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_ExerciseId",
                table: "Rewards",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_StudyPlanId",
                table: "Rewards",
                column: "StudyPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rewards_Exercises_ExerciseId",
                table: "Rewards",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Rewards_StudyPlans_StudyPlanId",
                table: "Rewards",
                column: "StudyPlanId",
                principalTable: "StudyPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rewards_Exercises_ExerciseId",
                table: "Rewards");

            migrationBuilder.DropForeignKey(
                name: "FK_Rewards_StudyPlans_StudyPlanId",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_ExerciseId",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_StudyPlanId",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "StudyPlanId",
                table: "Rewards");
        }
    }
}
