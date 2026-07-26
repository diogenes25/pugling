using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChildMediaPicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChildMediaPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rejected = table.Column<bool>(type: "INTEGER", nullable: false),
                    PickedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildMediaPicks", x => x.Id);
                    table.CheckConstraint("CK_ChildMediaPick_SingleCarrier", "(CASE WHEN \"VocabularyId\" IS NULL THEN 0 ELSE 1 END\r\n + CASE WHEN \"ExerciseItemId\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_ExerciseItems_ExerciseItemId",
                        column: x => x.ExerciseItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_Vocabulary_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabulary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ChildId_ExerciseItemId",
                table: "ChildMediaPicks",
                columns: new[] { "ChildId", "ExerciseItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ChildId_ExerciseItemId_MediaAssetId",
                table: "ChildMediaPicks",
                columns: new[] { "ChildId", "ExerciseItemId", "MediaAssetId" },
                unique: true,
                filter: "[ExerciseItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ChildId_VocabularyId",
                table: "ChildMediaPicks",
                columns: new[] { "ChildId", "VocabularyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ChildId_VocabularyId_MediaAssetId",
                table: "ChildMediaPicks",
                columns: new[] { "ChildId", "VocabularyId", "MediaAssetId" },
                unique: true,
                filter: "[VocabularyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ExerciseItemId",
                table: "ChildMediaPicks",
                column: "ExerciseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_MediaAssetId",
                table: "ChildMediaPicks",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_VocabularyId",
                table: "ChildMediaPicks",
                column: "VocabularyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildMediaPicks");
        }
    }
}
