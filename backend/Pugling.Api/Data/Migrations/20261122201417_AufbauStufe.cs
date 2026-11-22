using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AufbauStufe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BuildLevel",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuildTargetExerciseId",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_BuildTargetExerciseId",
                table: "Exercises",
                column: "BuildTargetExerciseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Exercises_BuildTargetExerciseId",
                table: "Exercises",
                column: "BuildTargetExerciseId",
                principalTable: "Exercises",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Exercises_BuildTargetExerciseId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_BuildTargetExerciseId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "BuildLevel",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "BuildTargetExerciseId",
                table: "Exercises");
        }
    }
}
