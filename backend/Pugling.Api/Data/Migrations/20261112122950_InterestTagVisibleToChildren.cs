using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InterestTagVisibleToChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VisibleToChildren",
                table: "InterestTags",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Existing keywords stay visible (B-207, decision 6): the live database holds the family's own keywords,
            // the children already pick from them, and only keywords created from now on need a creator's release.
            migrationBuilder.Sql("UPDATE \"InterestTags\" SET \"VisibleToChildren\" = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibleToChildren",
                table: "InterestTags");
        }
    }
}
