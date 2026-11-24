using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MixMixedReviewRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MixedReviewRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CoinsAwarded = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixedReviewRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MixedReviewRuns_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MixedReviewCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    GivenAnswer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: true),
                    CoinsAwarded = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixedReviewCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MixedReviewCards_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MixedReviewCards_MixedReviewRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "MixedReviewRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MixedReviewCards_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MixedReviewCards_ExerciseId",
                table: "MixedReviewCards",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_MixedReviewCards_PlanPositionId",
                table: "MixedReviewCards",
                column: "PlanPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_MixedReviewCards_RunId",
                table: "MixedReviewCards",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_MixedReviewRuns_ChildId",
                table: "MixedReviewRuns",
                column: "ChildId",
                unique: true,
                filter: "[CompletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MixedReviewRuns_ChildId_Day",
                table: "MixedReviewRuns",
                columns: new[] { "ChildId", "Day" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MixedReviewCards");

            migrationBuilder.DropTable(
                name: "MixedReviewRuns");
        }
    }
}
