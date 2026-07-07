using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ItemProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    MasteryPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    SeenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IntroducedAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastAnswerAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastCorrect = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemProgress_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemProgress_ExerciseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemReviewEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    StageValue = table.Column<int>(type: "INTEGER", nullable: false),
                    GivenAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemReviewEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemReviewEvents_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemReviewEvents_ExerciseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ChildId_ItemId",
                table: "ItemProgress",
                columns: new[] { "ChildId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ChildId_VocabularyId",
                table: "ItemProgress",
                columns: new[] { "ChildId", "VocabularyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ItemId",
                table: "ItemProgress",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviewEvents_ChildId_ItemId_At",
                table: "ItemReviewEvents",
                columns: new[] { "ChildId", "ItemId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviewEvents_ChildId_VocabularyId",
                table: "ItemReviewEvents",
                columns: new[] { "ChildId", "VocabularyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviewEvents_ItemId",
                table: "ItemReviewEvents",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemProgress");

            migrationBuilder.DropTable(
                name: "ItemReviewEvents");
        }
    }
}
