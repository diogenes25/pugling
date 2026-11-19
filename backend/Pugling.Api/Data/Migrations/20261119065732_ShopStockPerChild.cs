using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <summary>
    /// B-645 and B-665: every child gets its own stock of each shop listing, a listing gets an audience and a refill
    /// quantity, and an activation request records who opened it. Written by hand: the scaffold renamed
    /// <c>CurrentStock</c> into <c>RefillQuantity</c>, which would have thrown the family stock away instead of handing
    /// it to the children.
    /// </summary>
    public partial class ShopStockPerChild : Migration
    {
        // SQLite has no GUID function; this builds the same uppercase text shape EF writes for a Guid.
        private const string NewGuidSql =
            "hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(2)) || '-' || hex(randomblob(6))";

        // A Once listing that has not fired and whose instant is still ahead (or missing) starts empty for everyone.
        private const string OnceNotYetSql =
            "l.RefillKind = 'Once' AND l.LastRefilledAtUtc IS NULL AND (l.RefillAtUtc IS NULL OR l.RefillAtUtc > strftime('%Y-%m-%d %H:%M:%S', 'now'))";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RefillQuantity",
                table: "ShopListings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestedBy",
                table: "ActivationRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ShopListingAudiences",
                columns: table => new
                {
                    ShopListingId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopListingAudiences", x => new { x.ShopListingId, x.ChildId });
                    table.ForeignKey(
                        name: "FK_ShopListingAudiences_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopListingAudiences_ShopListings_ShopListingId",
                        column: x => x.ShopListingId,
                        principalTable: "ShopListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopListingStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShopListingId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStock = table.Column<int>(type: "INTEGER", nullable: false),
                    LastRefilledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopListingStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopListingStocks_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopListingStocks_ShopListings_ShopListingId",
                        column: x => x.ShopListingId,
                        principalTable: "ShopListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopListingAudiences_ChildId",
                table: "ShopListingAudiences",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopListingStocks_ChildId",
                table: "ShopListingStocks",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopListingStocks_ShopListingId_ChildId",
                table: "ShopListingStocks",
                columns: new[] { "ShopListingId", "ChildId" },
                unique: true);

            // MaxStock = 0 used to mean "no cap, never refilled". Switch a refill kind that never fired to None, and give
            // the listing the cap it effectively had (the stock plus everything bought from it) - so every existing row
            // passes the new validation and a PATCH of the price alone is not refused.
            // Order matters: the refill kind is switched off while MaxStock still reads 0.
            migrationBuilder.Sql("UPDATE ShopListings SET RefillKind = 'None' WHERE RefillKind <> 'None' AND MaxStock = 0;");
            migrationBuilder.Sql("""
                UPDATE ShopListings
                SET MaxStock = CurrentStock + (SELECT COUNT(*) FROM ShopPurchases p
                                               WHERE p.ShopListingId = ShopListings.Id AND p.Status <> 'Cancelled')
                WHERE MaxStock = 0;
                """);

            // Decision 4: quantity = MaxStock is "leftovers expire" - exactly today's behaviour for every listing.
            migrationBuilder.Sql("UPDATE ShopListings SET RefillQuantity = MaxStock;");

            // Decision 11, the same rule as ShopRefillSchedule.StartStock: each linked child starts with MaxStock minus its
            // OWN purchases since the last refill (since creation if there was none) - not the family stock, which would
            // charge one sibling's purchase to all of them.
            migrationBuilder.Sql($"""
                INSERT INTO ShopListingStocks (ShopListingId, ChildId, CurrentStock, LastRefilledAtUtc, ConcurrencyStamp)
                SELECT l.Id, sl.StudentId,
                       CASE WHEN {OnceNotYetSql} THEN 0
                            ELSE MAX(0, l.MaxStock - (SELECT COUNT(*) FROM ShopPurchases p
                                                     WHERE p.ShopListingId = l.Id AND p.ChildId = sl.StudentId
                                                       AND p.Status <> 'Cancelled'
                                                       AND p.PurchasedAt >= COALESCE(l.LastRefilledAtUtc, l.CreatedAt)))
                       END,
                       CASE WHEN {OnceNotYetSql} THEN NULL ELSE l.LastRefilledAtUtc END,
                       {NewGuidSql}
                FROM ShopListings l
                JOIN ShopArticles a ON a.Id = l.ShopArticleId
                JOIN SupervisorLinks sl ON sl.SupervisorId = a.AdultId;
                """);

            // B-665: everything recorded so far was filed by the child.
            migrationBuilder.Sql("UPDATE ActivationRequests SET RequestedBy = 'Child';");

            migrationBuilder.DropColumn(
                name: "CurrentStock",
                table: "ShopListings");

            migrationBuilder.DropColumn(
                name: "LastRefilledAtUtc",
                table: "ShopListings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStock",
                table: "ShopListings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefilledAtUtc",
                table: "ShopListings",
                type: "TEXT",
                nullable: true);

            // Back to one family stock: the smallest child stock is the one nobody can exceed.
            migrationBuilder.Sql("""
                UPDATE ShopListings
                SET CurrentStock = COALESCE((SELECT MIN(s.CurrentStock) FROM ShopListingStocks s WHERE s.ShopListingId = ShopListings.Id), MaxStock),
                    LastRefilledAtUtc = (SELECT MAX(s.LastRefilledAtUtc) FROM ShopListingStocks s WHERE s.ShopListingId = ShopListings.Id);
                """);

            migrationBuilder.DropTable(
                name: "ShopListingAudiences");

            migrationBuilder.DropTable(
                name: "ShopListingStocks");

            migrationBuilder.DropColumn(
                name: "RequestedBy",
                table: "ActivationRequests");

            migrationBuilder.DropColumn(
                name: "RefillQuantity",
                table: "ShopListings");
        }
    }
}
