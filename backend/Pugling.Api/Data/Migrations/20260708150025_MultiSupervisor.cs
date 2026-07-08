using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiSupervisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reihenfolge bewusst: erst Struktur + Spalten anlegen, dann aus Children.FatherId backfillen,
            // und die Spalte Children.FatherId ganz ZULETZT droppen – so gehen bestehende Beziehungen und
            // die Aussteller-Zuordnung der Ökonomie nicht verloren.
            migrationBuilder.DropForeignKey(
                name: "FK_Children_Fathers_FatherId",
                table: "Children");

            migrationBuilder.DropIndex(
                name: "IX_ShopPurchases_ChildId",
                table: "ShopPurchases");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_ChildId",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_ChildId",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_Children_FatherId",
                table: "Children");

            migrationBuilder.DropIndex(
                name: "IX_ActivationRequests_ChildId",
                table: "ActivationRequests");

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "ShopPurchases",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "Rewards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "RewardRedemptions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "ActivationRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SupervisorLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Relation = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupervisorLinks_Children_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupervisorLinks_Fathers_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Fathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ChildId_SupervisorId",
                table: "ShopPurchases",
                columns: new[] { "ChildId", "SupervisorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_ChildId_SupervisorId",
                table: "Rewards",
                columns: new[] { "ChildId", "SupervisorId" });

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_ChildId_SupervisorId",
                table: "RewardRedemptions",
                columns: new[] { "ChildId", "SupervisorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationRequests_ChildId_SupervisorId",
                table: "ActivationRequests",
                columns: new[] { "ChildId", "SupervisorId" });

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorLinks_StudentId",
                table: "SupervisorLinks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorLinks_SupervisorId_StudentId",
                table: "SupervisorLinks",
                columns: new[] { "SupervisorId", "StudentId" },
                unique: true);

            // Datenerhalt-Backfill aus der noch vorhandenen Spalte Children.FatherId:
            // 1) je Kind eine Betreuung Vater→Kind (Relation als String, s. HasConversion<string>).
            migrationBuilder.Sql(
                "INSERT INTO SupervisorLinks (SupervisorId, StudentId, Relation, CreatedAt) " +
                "SELECT FatherId, Id, 'Father', CURRENT_TIMESTAMP FROM Children;");
            // 2) Aussteller der Ökonomie-Zeilen aus der Herkunft ableiten (Fallback: Vater des Kindes).
            migrationBuilder.Sql(
                "UPDATE Rewards SET SupervisorId = (SELECT c.FatherId FROM Children c WHERE c.Id = Rewards.ChildId) WHERE SupervisorId = 0;");
            migrationBuilder.Sql(
                "UPDATE RewardRedemptions SET SupervisorId = COALESCE(" +
                "(SELECT r.SupervisorId FROM Rewards r WHERE r.Id = RewardRedemptions.RewardId), " +
                "(SELECT c.FatherId FROM Children c WHERE c.Id = RewardRedemptions.ChildId)) WHERE SupervisorId = 0;");
            migrationBuilder.Sql(
                "UPDATE ShopPurchases SET SupervisorId = COALESCE(" +
                "(SELECT a.FatherId FROM ShopListings l JOIN ShopArticles a ON a.Id = l.ShopArticleId WHERE l.Id = ShopPurchases.ShopListingId), " +
                "(SELECT c.FatherId FROM Children c WHERE c.Id = ShopPurchases.ChildId)) WHERE SupervisorId = 0;");
            migrationBuilder.Sql(
                "UPDATE ActivationRequests SET SupervisorId = COALESCE(" +
                "(SELECT a.FatherId FROM ShopArticles a WHERE a.Id = ActivationRequests.ShopArticleId), " +
                "(SELECT c.FatherId FROM Children c WHERE c.Id = ActivationRequests.ChildId)) WHERE SupervisorId = 0;");

            // 3) Erst jetzt die alte 1:1-Bindung entfernen.
            migrationBuilder.DropColumn(
                name: "FatherId",
                table: "Children");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupervisorLinks");

            migrationBuilder.DropIndex(
                name: "IX_ShopPurchases_ChildId_SupervisorId",
                table: "ShopPurchases");

            migrationBuilder.DropIndex(
                name: "IX_Rewards_ChildId_SupervisorId",
                table: "Rewards");

            migrationBuilder.DropIndex(
                name: "IX_RewardRedemptions_ChildId_SupervisorId",
                table: "RewardRedemptions");

            migrationBuilder.DropIndex(
                name: "IX_ActivationRequests_ChildId_SupervisorId",
                table: "ActivationRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "ShopPurchases");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "RewardRedemptions");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "ActivationRequests");

            migrationBuilder.AddColumn<int>(
                name: "FatherId",
                table: "Children",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ChildId",
                table: "ShopPurchases",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_ChildId",
                table: "Rewards",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_ChildId",
                table: "RewardRedemptions",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_FatherId",
                table: "Children",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationRequests_ChildId",
                table: "ActivationRequests",
                column: "ChildId");

            migrationBuilder.AddForeignKey(
                name: "FK_Children_Fathers_FatherId",
                table: "Children",
                column: "FatherId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
