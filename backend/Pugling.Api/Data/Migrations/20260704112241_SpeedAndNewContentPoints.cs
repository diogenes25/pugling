using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpeedAndNewContentPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NewContentPoints",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpeedBonusPoints",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpeedThresholdSeconds",
                table: "StudyPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewContentPoints",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "SpeedBonusPoints",
                table: "StudyPlans");

            migrationBuilder.DropColumn(
                name: "SpeedThresholdSeconds",
                table: "StudyPlans");
        }
    }
}
