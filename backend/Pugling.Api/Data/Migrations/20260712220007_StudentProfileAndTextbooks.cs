using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudentProfileAndTextbooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bestandskinder bekommen gültige Defaults: das Geschlecht als String-Enum "None"
            // (leerer String ließe sich nicht zurück-parsen) und die Interessen als leere JSON-Liste
            // "[]" (leerer String wäre kein gültiges JSON und würde beim Lesen werfen).
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Children",
                type: "TEXT",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "Interests",
                table: "Children",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ProfileNotes",
                table: "Children",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Textbooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectName = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    Isbn = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentChapter = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Textbooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Textbooks_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Textbooks_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_ChildId",
                table: "Textbooks",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_SubjectId",
                table: "Textbooks",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Textbooks");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "Interests",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "ProfileNotes",
                table: "Children");
        }
    }
}
