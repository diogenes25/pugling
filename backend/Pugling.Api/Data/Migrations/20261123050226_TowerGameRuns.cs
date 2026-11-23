using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TowerGameRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TowerGameMinutesPerDay",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TowerGameRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastBeatAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PlayedSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Won = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoinsAwarded = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerGameRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerGameRuns_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TowerGameRuns_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TowerGameRunItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    SolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerGameRunItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerGameRunItems_ExerciseItems_ExerciseItemId",
                        column: x => x.ExerciseItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TowerGameRunItems_TowerGameRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "TowerGameRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TowerGameRunItems_ExerciseItemId",
                table: "TowerGameRunItems",
                column: "ExerciseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TowerGameRunItems_RunId_ExerciseItemId",
                table: "TowerGameRunItems",
                columns: new[] { "RunId", "ExerciseItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerGameRuns_ChildId_Day",
                table: "TowerGameRuns",
                columns: new[] { "ChildId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_TowerGameRuns_PlanPositionId",
                table: "TowerGameRuns",
                column: "PlanPositionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TowerGameRunItems");

            migrationBuilder.DropTable(
                name: "TowerGameRuns");

            migrationBuilder.DropColumn(
                name: "TowerGameMinutesPerDay",
                table: "Children");
        }
    }
}
