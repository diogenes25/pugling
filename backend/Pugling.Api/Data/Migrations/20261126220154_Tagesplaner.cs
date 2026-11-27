using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Tagesplaner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlannerMode",
                table: "Children",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannerSince",
                table: "Children",
                type: "TEXT",
                nullable: true);

            // Existing children start with the planner off (B-734): nothing is planned until the supervisor switches it on.
            migrationBuilder.Sql("UPDATE Children SET PlannerMode = 'Off';");

            migrationBuilder.CreateTable(
                name: "DailyPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", nullable: false),
                    NewWordsBudget = table.Column<int>(type: "INTEGER", nullable: false),
                    Warnings = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPlans_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DailyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    DueReviews = table.Column<int>(type: "INTEGER", nullable: false),
                    NewWords = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyTasks_DailyPlans_DailyPlanId",
                        column: x => x.DailyPlanId,
                        principalTable: "DailyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyTasks_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DailyTasks_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyPlans_ChildId_Day",
                table: "DailyPlans",
                columns: new[] { "ChildId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyTasks_DailyPlanId",
                table: "DailyTasks",
                column: "DailyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyTasks_PlanPositionId",
                table: "DailyTasks",
                column: "PlanPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyTasks_SubjectId",
                table: "DailyTasks",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyTasks");

            migrationBuilder.DropTable(
                name: "DailyPlans");

            migrationBuilder.DropColumn(
                name: "PlannerMode",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "PlannerSince",
                table: "Children");
        }
    }
}
