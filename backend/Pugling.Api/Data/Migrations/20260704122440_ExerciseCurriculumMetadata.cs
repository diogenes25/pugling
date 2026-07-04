using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseCurriculumMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradeMax",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradeMin",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SchoolTypes",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Exercises",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExerciseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseCategories_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_CategoryId",
                table: "Exercises",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCategories_SubjectId_Name",
                table: "ExerciseCategories",
                columns: new[] { "SubjectId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_ExerciseCategories_CategoryId",
                table: "Exercises",
                column: "CategoryId",
                principalTable: "ExerciseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_ExerciseCategories_CategoryId",
                table: "Exercises");

            migrationBuilder.DropTable(
                name: "ExerciseCategories");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_CategoryId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "GradeMax",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "GradeMin",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "SchoolTypes",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Exercises");
        }
    }
}
