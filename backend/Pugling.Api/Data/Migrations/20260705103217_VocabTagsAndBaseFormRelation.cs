using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class VocabTagsAndBaseFormRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseFormRelation",
                table: "Vocabulary",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VocabTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabTagLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VocabTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabTagLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabTagLinks_VocabTags_VocabTagId",
                        column: x => x.VocabTagId,
                        principalTable: "VocabTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VocabTagLinks_Vocabulary_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabulary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabTagLinks_VocabTagId_VocabularyId",
                table: "VocabTagLinks",
                columns: new[] { "VocabTagId", "VocabularyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabTagLinks_VocabularyId",
                table: "VocabTagLinks",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabTags_Name",
                table: "VocabTags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabTagLinks");

            migrationBuilder.DropTable(
                name: "VocabTags");

            migrationBuilder.DropColumn(
                name: "BaseFormRelation",
                table: "Vocabulary");
        }
    }
}
