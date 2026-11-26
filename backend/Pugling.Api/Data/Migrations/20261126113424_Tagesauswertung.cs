using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Tagesauswertung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    NewWords = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsolidatedWords = table.Column<int>(type: "INTEGER", nullable: false),
                    FellBackWords = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "INTEGER", nullable: false),
                    WrongAnswers = table.Column<int>(type: "INTEGER", nullable: false),
                    PracticedSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorKinds = table.Column<string>(type: "TEXT", nullable: false),
                    DutiesMet = table.Column<int>(type: "INTEGER", nullable: false),
                    DutiesMissed = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyEvaluations_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyEvaluations_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluatedDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluatedDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluatedDays_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyEvaluations_ChildId_Day",
                table: "DailyEvaluations",
                columns: new[] { "ChildId", "Day" },
                unique: true,
                filter: "[SubjectId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyEvaluations_ChildId_Day_SubjectId",
                table: "DailyEvaluations",
                columns: new[] { "ChildId", "Day", "SubjectId" },
                unique: true,
                filter: "[SubjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyEvaluations_SubjectId",
                table: "DailyEvaluations",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluatedDays_ChildId_Day",
                table: "EvaluatedDays",
                columns: new[] { "ChildId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyEvaluations");

            migrationBuilder.DropTable(
                name: "EvaluatedDays");
        }
    }
}
