using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SkinOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bestandskinder starten mit dem Gratis-Starter freigeschaltet und ausgerüstet,
            // damit die JSON-Liste gültig ist und niemand ohne Skin dasteht.
            migrationBuilder.AddColumn<string>(
                name: "OwnedSkins",
                table: "Children",
                type: "TEXT",
                nullable: false,
                defaultValue: "[\"pug\"]");

            migrationBuilder.AddColumn<string>(
                name: "SelectedSkin",
                table: "Children",
                type: "TEXT",
                nullable: false,
                defaultValue: "pug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnedSkins",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SelectedSkin",
                table: "Children");
        }
    }
}
