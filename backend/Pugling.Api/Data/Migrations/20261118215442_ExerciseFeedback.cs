using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastReportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DoneByAdultId = table.Column<int>(type: "INTEGER", nullable: true),
                    SeenByChildAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_Adults_DoneByAdultId",
                        column: x => x.DoneByAdultId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_ExerciseItems_ExerciseItemId",
                        column: x => x.ExerciseItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseFeedbacks_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_ChildId_Status",
                table: "ExerciseFeedbacks",
                columns: new[] { "ChildId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_DoneByAdultId",
                table: "ExerciseFeedbacks",
                column: "DoneByAdultId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_ExerciseId",
                table: "ExerciseFeedbacks",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_ExerciseItemId",
                table: "ExerciseFeedbacks",
                column: "ExerciseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseFeedbacks_PlanPositionId",
                table: "ExerciseFeedbacks",
                column: "PlanPositionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseFeedbacks");
        }
    }
}
