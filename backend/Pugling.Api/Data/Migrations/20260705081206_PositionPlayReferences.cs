using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PositionPlayReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemIndex",
                table: "TestItemResults",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanPositionId",
                table: "TestAttempts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemIndex",
                table: "ReviewEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanPositionId",
                table: "PracticeSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_PlanPositionId",
                table: "TestAttempts",
                column: "PlanPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_PlanPositionId",
                table: "PracticeSessions",
                column: "PlanPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeSessions_PlanPositions_PlanPositionId",
                table: "PracticeSessions",
                column: "PlanPositionId",
                principalTable: "PlanPositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TestAttempts_PlanPositions_PlanPositionId",
                table: "TestAttempts",
                column: "PlanPositionId",
                principalTable: "PlanPositions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeSessions_PlanPositions_PlanPositionId",
                table: "PracticeSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_TestAttempts_PlanPositions_PlanPositionId",
                table: "TestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_TestAttempts_PlanPositionId",
                table: "TestAttempts");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSessions_PlanPositionId",
                table: "PracticeSessions");

            migrationBuilder.DropColumn(
                name: "ItemIndex",
                table: "TestItemResults");

            migrationBuilder.DropColumn(
                name: "PlanPositionId",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "ItemIndex",
                table: "ReviewEvents");

            migrationBuilder.DropColumn(
                name: "PlanPositionId",
                table: "PracticeSessions");
        }
    }
}
