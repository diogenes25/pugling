using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlanContainerCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentRatings");

            migrationBuilder.DropTable(
                name: "StudyDayRewards");

            migrationBuilder.DropTable(
                name: "StudyPlanItems");

            migrationBuilder.DropColumn(
                name: "BoxIntervalDays",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "ComboBonusPoints",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "ComboThreshold",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "DailyMinutesRequired",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "DailyTestPassPercent",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "DailyTestRequired",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "DefaultStage",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "MaxBox",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "NewContentPoints",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "NewItemsPerLesson",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "PointsDayCompleteBonus",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "PointsMinutesMet",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "PointsTestPassed",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "RequireTypedTest",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "SpeedBonusPoints",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "SpeedThresholdSeconds",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "StageSchedule",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "UseLeitner",
                table: "StudyPlans");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Day",
                table: "PositionGoalRewards",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Day",
                table: "PositionGoalRewards");

            migrationBuilder.AddColumn<string>(
                name: "BoxIntervalDays",
                table: "StudyPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ComboBonusPoints",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ComboThreshold",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DailyMinutesRequired",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DailyTestPassPercent",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DailyTestRequired",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DefaultStage",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxBox",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewContentPoints",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewItemsPerLesson",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsDayCompleteBonus",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsMinutesMet",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsTestPassed",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireTypedTest",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SpeedBonusPoints",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpeedThresholdSeconds",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StageSchedule",
                table: "StudyPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseLeitner",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ContentRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Feedback = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRatings_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyDayRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyDayRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClozeTextId = table.Column<int>(type: "INTEGER", nullable: true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: true),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IntroducedAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_ClozeTexts_ClozeTextId",
                        column: x => x.ClozeTextId,
                        principalTable: "ClozeTexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_Vocabulary_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabulary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentRatings_StudyPlanId",
                table: "ContentRatings",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyDayRewards_StudyPlanId_Day_Kind",
                table: "StudyDayRewards",
                columns: new[] { "StudyPlanId", "Day", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_ClozeTextId",
                table: "StudyPlanItems",
                column: "ClozeTextId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_StudyPlanId",
                table: "StudyPlanItems",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_VocabularyId",
                table: "StudyPlanItems",
                column: "VocabularyId");
        }
    }
}
