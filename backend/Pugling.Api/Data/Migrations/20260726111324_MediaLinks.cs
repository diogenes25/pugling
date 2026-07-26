using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaLinks", x => x.Id);
                    table.CheckConstraint("CK_MediaLink_SingleCarrier", "(CASE WHEN \"VocabularyId\" IS NULL THEN 0 ELSE 1 END\r\n + CASE WHEN \"ExerciseItemId\" IS NULL THEN 0 ELSE 1 END\r\n + CASE WHEN \"ExerciseId\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_MediaLinks_ExerciseItems_ExerciseItemId",
                        column: x => x.ExerciseItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaLinks_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaLinks_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaLinks_Vocabulary_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabulary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_ExerciseId",
                table: "MediaLinks",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_ExerciseItemId",
                table: "MediaLinks",
                column: "ExerciseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_MediaAssetId_ExerciseId",
                table: "MediaLinks",
                columns: new[] { "MediaAssetId", "ExerciseId" },
                unique: true,
                filter: "[ExerciseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_MediaAssetId_ExerciseItemId",
                table: "MediaLinks",
                columns: new[] { "MediaAssetId", "ExerciseItemId" },
                unique: true,
                filter: "[ExerciseItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_MediaAssetId_VocabularyId",
                table: "MediaLinks",
                columns: new[] { "MediaAssetId", "VocabularyId" },
                unique: true,
                filter: "[VocabularyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_VocabularyId",
                table: "MediaLinks",
                column: "VocabularyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaLinks");
        }
    }
}
