using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ComboSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default 5 (nicht 0): Bestandspläne behalten die Combo mit sinnvoller Standard-Einstellung.
            migrationBuilder.AddColumn<int>(
                name: "ComboBonusPoints",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "ComboThreshold",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComboBonusPoints",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "ComboThreshold",
                table: "StudyPlans");
        }
    }
}
