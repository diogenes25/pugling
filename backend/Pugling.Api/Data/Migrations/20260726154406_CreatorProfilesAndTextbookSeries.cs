using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreatorProfilesAndTextbookSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentUnitId",
                table: "Textbooks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeriesId",
                table: "Textbooks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TextbookSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectName = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    SchoolTypes = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    TargetLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerFatherId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextbookSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TextbookSeries_Fathers_OwnerFatherId",
                        column: x => x.OwnerFatherId,
                        principalTable: "Fathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TextbookSeries_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CreatorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerFatherId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubjectName = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    SchoolTypes = table.Column<int>(type: "INTEGER", nullable: false),
                    GradeMin = table.Column<int>(type: "INTEGER", nullable: true),
                    GradeMax = table.Column<int>(type: "INTEGER", nullable: true),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceLang = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLang = table.Column<string>(type: "TEXT", nullable: false),
                    Persona = table.Column<string>(type: "TEXT", nullable: true),
                    Didactics = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultTypes = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreatorProfiles_Fathers_OwnerFatherId",
                        column: x => x.OwnerFatherId,
                        principalTable: "Fathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreatorProfiles_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreatorProfiles_TextbookSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "TextbookSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SeriesUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    Grade = table.Column<int>(type: "INTEGER", nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    Topics = table.Column<string>(type: "TEXT", nullable: true),
                    Grammar = table.Column<string>(type: "TEXT", nullable: true),
                    VocabularyNotes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesUnits_TextbookSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "TextbookSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_CurrentUnitId",
                table: "Textbooks",
                column: "CurrentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_SeriesId",
                table: "Textbooks",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_OwnerFatherId_Name",
                table: "CreatorProfiles",
                columns: new[] { "OwnerFatherId", "Name" },
                unique: true,
                filter: "[OwnerFatherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_SeriesId",
                table: "CreatorProfiles",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_SubjectId_SeriesId",
                table: "CreatorProfiles",
                columns: new[] { "SubjectId", "SeriesId" });

            migrationBuilder.CreateIndex(
                name: "IX_SeriesUnits_SeriesId_Grade_OrderIndex",
                table: "SeriesUnits",
                columns: new[] { "SeriesId", "Grade", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_TextbookSeries_OwnerFatherId",
                table: "TextbookSeries",
                column: "OwnerFatherId");

            migrationBuilder.CreateIndex(
                name: "IX_TextbookSeries_Slug",
                table: "TextbookSeries",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TextbookSeries_SubjectId",
                table: "TextbookSeries",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Textbooks_SeriesUnits_CurrentUnitId",
                table: "Textbooks",
                column: "CurrentUnitId",
                principalTable: "SeriesUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Textbooks_TextbookSeries_SeriesId",
                table: "Textbooks",
                column: "SeriesId",
                principalTable: "TextbookSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Textbooks_SeriesUnits_CurrentUnitId",
                table: "Textbooks");

            migrationBuilder.DropForeignKey(
                name: "FK_Textbooks_TextbookSeries_SeriesId",
                table: "Textbooks");

            migrationBuilder.DropTable(
                name: "CreatorProfiles");

            migrationBuilder.DropTable(
                name: "SeriesUnits");

            migrationBuilder.DropTable(
                name: "TextbookSeries");

            migrationBuilder.DropIndex(
                name: "IX_Textbooks_CurrentUnitId",
                table: "Textbooks");

            migrationBuilder.DropIndex(
                name: "IX_Textbooks_SeriesId",
                table: "Textbooks");

            migrationBuilder.DropColumn(
                name: "CurrentUnitId",
                table: "Textbooks");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "Textbooks");
        }
    }
}
