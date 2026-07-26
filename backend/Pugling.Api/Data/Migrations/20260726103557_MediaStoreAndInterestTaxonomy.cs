using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaStoreAndInterestTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllowedContentRating",
                table: "Children",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InterestTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    Facet = table.Column<string>(type: "TEXT", nullable: false),
                    Synonyms = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    License = table.Column<string>(type: "TEXT", nullable: true),
                    Attribution = table.Column<string>(type: "TEXT", nullable: true),
                    Origin = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Placeholder = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChildInterests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildInterests_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildInterests_InterestTags_InterestTagId",
                        column: x => x.InterestTagId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaTagLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestTagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTagLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaTagLinks_InterestTags_InterestTagId",
                        column: x => x.InterestTagId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaTagLinks_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Format = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Bytes = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaVariants_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildInterests_ChildId_InterestTagId",
                table: "ChildInterests",
                columns: new[] { "ChildId", "InterestTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildInterests_InterestTagId",
                table: "ChildInterests",
                column: "InterestTagId");

            migrationBuilder.CreateIndex(
                name: "IX_InterestTags_Slug",
                table: "InterestTags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Key",
                table: "MediaAssets",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Kind_Rating",
                table: "MediaAssets",
                columns: new[] { "Kind", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaTagLinks_InterestTagId",
                table: "MediaTagLinks",
                column: "InterestTagId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTagLinks_MediaAssetId_InterestTagId",
                table: "MediaTagLinks",
                columns: new[] { "MediaAssetId", "InterestTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaVariants_MediaAssetId_Purpose_Format",
                table: "MediaVariants",
                columns: new[] { "MediaAssetId", "Purpose", "Format" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildInterests");

            migrationBuilder.DropTable(
                name: "MediaTagLinks");

            migrationBuilder.DropTable(
                name: "MediaVariants");

            migrationBuilder.DropTable(
                name: "InterestTags");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "AllowedContentRating",
                table: "Children");
        }
    }
}
