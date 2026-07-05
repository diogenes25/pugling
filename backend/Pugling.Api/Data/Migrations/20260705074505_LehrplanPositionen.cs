using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LehrplanPositionen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultItemCount",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultStage",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    Cadence = table.Column<int>(type: "INTEGER", nullable: false),
                    GoalThreshold = table.Column<int>(type: "INTEGER", nullable: true),
                    RequireTypedTest = table.Column<bool>(type: "INTEGER", nullable: false),
                    PointsGoalMet = table.Column<int>(type: "INTEGER", nullable: false),
                    NewContentPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    ComboThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    ComboBonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    SpeedThresholdSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    SpeedBonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    UseLeitner = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxBox = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxIntervalDays = table.Column<string>(type: "TEXT", nullable: true),
                    StageSchedule = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanPositions_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanPositions_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionItemProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReviewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IntroducedAt = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionItemProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionItemProgress_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPositions_ExerciseId",
                table: "PlanPositions",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPositions_StudyPlanId",
                table: "PlanPositions",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionItemProgress_PlanPositionId_ItemIndex",
                table: "PositionItemProgress",
                columns: new[] { "PlanPositionId", "ItemIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PositionItemProgress");

            migrationBuilder.DropTable(
                name: "PlanPositions");

            migrationBuilder.DropColumn(
                name: "DefaultItemCount",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "DefaultStage",
                table: "Exercises");
        }
    }
}
