using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TowerGameCollect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Trophies",
                table: "TowerGameRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Wave",
                table: "TowerGameRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Value",
                table: "TowerGameRunItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TowerGameChests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Gems = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerGameChests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerGameChests_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TowerTrophyRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Trophies = table.Column<int>(type: "INTEGER", nullable: false),
                    Gems = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TowerTrophyRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TowerTrophyRewards_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TowerGameChests_ChildId_Day",
                table: "TowerGameChests",
                columns: new[] { "ChildId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TowerTrophyRewards_ChildId_Trophies",
                table: "TowerTrophyRewards",
                columns: new[] { "ChildId", "Trophies" },
                unique: true);

            // A word frozen before the column existed has no recorded value. Only a run still open at the upgrade reads
            // it (ended runs keep their 0 trophies); it gets the middle value, as the app assumes for an unrated word.
            migrationBuilder.Sql("UPDATE TowerGameRunItems SET Value = 2;");

            // Every existing child gets the tower game's three daily tasks, as the seed gives them to a fresh family.
            // Idempotent per metric, so a child that already has a task on a tower metric keeps exactly that one.
            foreach (var (title, metric, target) in new[]
            {
                ("Vokabel-Türme: 20 Vokabeln richtig", "TowerWordsCorrect", 20),
                ("Vokabel-Türme: Welle 5 erreichen", "TowerBestWave", 5),
                ("Vokabel-Türme: 3 neue Wörter richtig", "TowerNewWordsCorrect", 3),
            })
                migrationBuilder.Sql(
                    "INSERT INTO Missions (ChildId, Title, Metric, Target, Period, RewardPoints, Active, CreatedAt) " +
                    $"SELECT c.Id, '{title}', '{metric}', {target}, 'Daily', 10, 1, datetime('now') FROM Children c " +
                    $"WHERE NOT EXISTS (SELECT 1 FROM Missions m WHERE m.ChildId = c.Id AND m.Metric = '{metric}');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TowerGameChests");

            migrationBuilder.DropTable(
                name: "TowerTrophyRewards");

            migrationBuilder.DropColumn(
                name: "Trophies",
                table: "TowerGameRuns");

            migrationBuilder.DropColumn(
                name: "Wave",
                table: "TowerGameRuns");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "TowerGameRunItems");
        }
    }
}
