using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class VocabularyChildTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VocabularyTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaggedByRole = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VocabularyTags_Vocabulary_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabulary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTags_TagId_VocabularyId",
                table: "VocabularyTags",
                columns: new[] { "TagId", "VocabularyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTags_VocabularyId",
                table: "VocabularyTags",
                column: "VocabularyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VocabularyTags");
        }
    }
}
