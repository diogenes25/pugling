using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <summary>
    /// B-731: the supervisor's five planner limits on the child (nullable, existing children stay "not set up") and the
    /// child's pauses (new, empty table). No backfill: nothing existed to carry over.
    /// </summary>
    public partial class LerntagePause : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyNewWordsMax",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyNewWordsMin",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyTasksMax",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyTasksMin",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PenaltyCoinsPerMissedTask",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChildPauses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    From = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Until = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildPauses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildPauses_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildPauses_ChildId_From",
                table: "ChildPauses",
                columns: new[] { "ChildId", "From" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildPauses");

            migrationBuilder.DropColumn(
                name: "DailyNewWordsMax",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "DailyNewWordsMin",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "DailyTasksMax",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "DailyTasksMin",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "PenaltyCoinsPerMissedTask",
                table: "Children");
        }
    }
}
