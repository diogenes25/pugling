using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <summary>
    /// B-624: accent leniency per position, on for every position that exists (decision 4 - the father's existing plans
    /// grade milder from the start), plus the half-counted slip on both review logs. The two slip flags start false:
    /// no answer before this migration was counted half.
    /// </summary>
    public partial class AccentLeniency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccentSlip",
                table: "ReviewEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccentLenient",
                table: "PlanPositions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE PlanPositions SET AccentLenient = 1;");

            migrationBuilder.AddColumn<bool>(
                name: "AccentSlip",
                table: "ItemReviewEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentSlip",
                table: "ReviewEvents");

            migrationBuilder.DropColumn(
                name: "AccentLenient",
                table: "PlanPositions");

            migrationBuilder.DropColumn(
                name: "AccentSlip",
                table: "ItemReviewEvents");
        }
    }
}
