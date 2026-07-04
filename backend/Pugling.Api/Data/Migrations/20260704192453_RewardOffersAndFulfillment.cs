using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RewardOffersAndFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestedAt",
                table: "RewardRedemptions",
                newName: "PurchasedAt");

            migrationBuilder.RenameColumn(
                name: "DecidedAt",
                table: "RewardRedemptions",
                newName: "FulfilledAt");

            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "Rewards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Default 1 (nicht 0): bestehende Angebote sollen weiterhin genau 1× je Periode kaufbar sein –
            // Quantity 0 würde das Kontingent sofort als erschöpft werten.
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Rewards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Period",
                table: "Rewards");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Rewards");

            migrationBuilder.RenameColumn(
                name: "PurchasedAt",
                table: "RewardRedemptions",
                newName: "RequestedAt");

            migrationBuilder.RenameColumn(
                name: "FulfilledAt",
                table: "RewardRedemptions",
                newName: "DecidedAt");
        }
    }
}
