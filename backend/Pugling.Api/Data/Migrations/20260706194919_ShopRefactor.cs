using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShopRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShopPurchases_ShopArticles_ShopArticleId",
                table: "ShopPurchases");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "ShopArticles");

            migrationBuilder.DropColumn(
                name: "CoinPrice",
                table: "ShopArticles");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "ShopArticles");

            migrationBuilder.DropColumn(
                name: "CurrentStock",
                table: "ShopArticles");

            migrationBuilder.DropColumn(
                name: "GemPrice",
                table: "ShopArticles");

            migrationBuilder.DropColumn(
                name: "LastRefilledAtUtc",
                table: "ShopArticles");

            migrationBuilder.DropColumn(
                name: "RefillAtUtc",
                table: "ShopArticles");

            migrationBuilder.DropColumn(
                name: "RefillDayOfWeek",
                table: "ShopArticles");

            migrationBuilder.RenameColumn(
                name: "ShopArticleId",
                table: "ShopPurchases",
                newName: "ShopListingId");

            migrationBuilder.RenameIndex(
                name: "IX_ShopPurchases_ShopArticleId",
                table: "ShopPurchases",
                newName: "IX_ShopPurchases_ShopListingId");

            migrationBuilder.RenameColumn(
                name: "RefillKind",
                table: "ShopArticles",
                newName: "UnitType");

            migrationBuilder.RenameColumn(
                name: "MaxStock",
                table: "ShopArticles",
                newName: "ActionType");

            migrationBuilder.AddColumn<int>(
                name: "UnitsPerPurchase",
                table: "ShopPurchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ActivationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopArticleId = table.Column<int>(type: "INTEGER", nullable: true),
                    RequestedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticleTitle = table.Column<string>(type: "TEXT", nullable: false),
                    UnitType = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivationRequests_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivationRequests_ShopArticles_ShopArticleId",
                        column: x => x.ShopArticleId,
                        principalTable: "ShopArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChildInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopArticleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildInventories_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildInventories_ShopArticles_ShopArticleId",
                        column: x => x.ShopArticleId,
                        principalTable: "ShopArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShopArticleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CoinPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    GemPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitsPerPurchase = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentStock = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxStock = table.Column<int>(type: "INTEGER", nullable: false),
                    RefillKind = table.Column<int>(type: "INTEGER", nullable: false),
                    RefillAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefillDayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    LastRefilledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopListings_ShopArticles_ShopArticleId",
                        column: x => x.ShopArticleId,
                        principalTable: "ShopArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationRequests_ChildId",
                table: "ActivationRequests",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationRequests_ShopArticleId",
                table: "ActivationRequests",
                column: "ShopArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildInventories_ChildId_ShopArticleId",
                table: "ChildInventories",
                columns: new[] { "ChildId", "ShopArticleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildInventories_ShopArticleId",
                table: "ChildInventories",
                column: "ShopArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopListings_ShopArticleId",
                table: "ShopListings",
                column: "ShopArticleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShopPurchases_ShopListings_ShopListingId",
                table: "ShopPurchases",
                column: "ShopListingId",
                principalTable: "ShopListings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShopPurchases_ShopListings_ShopListingId",
                table: "ShopPurchases");

            migrationBuilder.DropTable(
                name: "ActivationRequests");

            migrationBuilder.DropTable(
                name: "ChildInventories");

            migrationBuilder.DropTable(
                name: "ShopListings");

            migrationBuilder.DropColumn(
                name: "UnitsPerPurchase",
                table: "ShopPurchases");

            migrationBuilder.RenameColumn(
                name: "ShopListingId",
                table: "ShopPurchases",
                newName: "ShopArticleId");

            migrationBuilder.RenameIndex(
                name: "IX_ShopPurchases_ShopListingId",
                table: "ShopPurchases",
                newName: "IX_ShopPurchases_ShopArticleId");

            migrationBuilder.RenameColumn(
                name: "UnitType",
                table: "ShopArticles",
                newName: "RefillKind");

            migrationBuilder.RenameColumn(
                name: "ActionType",
                table: "ShopArticles",
                newName: "MaxStock");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "ShopArticles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CoinPrice",
                table: "ShopArticles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyStamp",
                table: "ShopArticles",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "CurrentStock",
                table: "ShopArticles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GemPrice",
                table: "ShopArticles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefilledAtUtc",
                table: "ShopArticles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefillAtUtc",
                table: "ShopArticles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefillDayOfWeek",
                table: "ShopArticles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopPurchases_ShopArticles_ShopArticleId",
                table: "ShopPurchases",
                column: "ShopArticleId",
                principalTable: "ShopArticles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
