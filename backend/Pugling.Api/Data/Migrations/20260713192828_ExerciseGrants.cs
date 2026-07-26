using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default true bewahrt das bisherige Verhalten: alle Bestands-Übungen bleiben für jeden zuweisbar.
            migrationBuilder.AddColumn<bool>(
                name: "ExecutePublic",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ExerciseGrants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", nullable: false),
                    GrantedByFatherId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseGrants_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseGrants_Fathers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Fathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseGrants_CreatorId",
                table: "ExerciseGrants",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseGrants_ExerciseId_CreatorId_Permission",
                table: "ExerciseGrants",
                columns: new[] { "ExerciseId", "CreatorId", "Permission" },
                unique: true);

            // Bestehende Autorschaft in das RWX-Modell überführen: jeder bisherige Autor wird erster Owner
            // seiner Übung (behält so das Editier-/Löschrecht). Geseedete System-Übungen (Autor NULL) bleiben
            // ownerlos. datetime('now') statt Literal, um Formatfragen der TEXT-Zeitspalte zu umgehen.
            migrationBuilder.Sql(
                "INSERT INTO ExerciseGrants (ExerciseId, CreatorId, Permission, GrantedByFatherId, CreatedAt) " +
                "SELECT Id, AuthorFatherId, 'Owner', NULL, datetime('now') FROM Exercises WHERE AuthorFatherId IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseGrants");

            migrationBuilder.DropColumn(
                name: "ExecutePublic",
                table: "Exercises");
        }
    }
}
