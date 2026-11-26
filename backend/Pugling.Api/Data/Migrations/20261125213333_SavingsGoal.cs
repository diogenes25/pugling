using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SavingsGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SavingsGoalListingId",
                table: "Children",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavingsGoalTitle",
                table: "Children",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Children_SavingsGoalListingId",
                table: "Children",
                column: "SavingsGoalListingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Children_ShopListings_SavingsGoalListingId",
                table: "Children",
                column: "SavingsGoalListingId",
                principalTable: "ShopListings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Children_ShopListings_SavingsGoalListingId",
                table: "Children");

            migrationBuilder.DropIndex(
                name: "IX_Children_SavingsGoalListingId",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SavingsGoalListingId",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SavingsGoalTitle",
                table: "Children");
        }
    }
}
