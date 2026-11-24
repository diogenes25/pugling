using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeedbackAnswerOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "ExerciseFeedbacks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestItemResultId",
                table: "ExerciseFeedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_TestItemResultId",
                table: "ExerciseFeedbacks",
                column: "TestItemResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseFeedbacks_TestItemResult_TestItemResultId",
                table: "ExerciseFeedbacks",
                column: "TestItemResultId",
                principalTable: "TestItemResult",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseFeedbacks_TestItemResult_TestItemResultId",
                table: "ExerciseFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseFeedbacks_TestItemResultId",
                table: "ExerciseFeedbacks");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "ExerciseFeedbacks");

            migrationBuilder.DropColumn(
                name: "TestItemResultId",
                table: "ExerciseFeedbacks");
        }
    }
}
