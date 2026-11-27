using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TagesaufgabeMalus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DoneAt",
                table: "DailyTasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyTaskPenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DailyTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefundedByFeedbackId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyTaskPenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyTaskPenalties_DailyTasks_DailyTaskId",
                        column: x => x.DailyTaskId,
                        principalTable: "DailyTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyTaskPenalties_ExerciseFeedbacks_RefundedByFeedbackId",
                        column: x => x.RefundedByFeedbackId,
                        principalTable: "ExerciseFeedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyTaskPenalties_DailyTaskId",
                table: "DailyTaskPenalties",
                column: "DailyTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyTaskPenalties_RefundedByFeedbackId",
                table: "DailyTaskPenalties",
                column: "RefundedByFeedbackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyTaskPenalties");

            migrationBuilder.DropColumn(
                name: "DoneAt",
                table: "DailyTasks");
        }
    }
}
