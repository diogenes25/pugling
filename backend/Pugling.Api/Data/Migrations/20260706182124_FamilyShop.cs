using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FamilyShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FatherId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticleNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CoinPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    GemPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentStock = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxStock = table.Column<int>(type: "INTEGER", nullable: false),
                    RefillKind = table.Column<int>(type: "INTEGER", nullable: false),
                    RefillAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefillDayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    LastRefilledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopArticles_Fathers_FatherId",
                        column: x => x.FatherId,
                        principalTable: "Fathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopArticleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ArticleNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CoinPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    GemPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopPurchases_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopPurchases_ShopArticles_ShopArticleId",
                        column: x => x.ShopArticleId,
                        principalTable: "ShopArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopArticles_FatherId_ArticleNumber",
                table: "ShopArticles",
                columns: new[] { "FatherId", "ArticleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ChildId",
                table: "ShopPurchases",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ShopArticleId",
                table: "ShopPurchases",
                column: "ShopArticleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopPurchases");

            migrationBuilder.DropTable(
                name: "ShopArticles");
        }
    }
}
