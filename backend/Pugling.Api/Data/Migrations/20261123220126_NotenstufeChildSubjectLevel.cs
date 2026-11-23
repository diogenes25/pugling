using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NotenstufeChildSubjectLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChildSubjectLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    BestLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    LastLoweredOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EvaluatedOn = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildSubjectLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildSubjectLevels_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildSubjectLevels_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildSubjectLevels_ChildId_SubjectId",
                table: "ChildSubjectLevels",
                columns: new[] { "ChildId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildSubjectLevels_SubjectId",
                table: "ChildSubjectLevels",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildSubjectLevels");
        }
    }
}
