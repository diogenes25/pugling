using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImprintedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ImprintedAt",
                table: "PositionItemProgress",
                type: "TEXT",
                nullable: true);

            // B-643: every word introduced before the learn card counted as imprinted - otherwise the next day would bring
            // all words the children already learn back as learn cards, and their golden rings would fall to zero.
            migrationBuilder.Sql("UPDATE PositionItemProgress SET ImprintedAt = IntroducedAt WHERE IntroducedAt IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImprintedAt",
                table: "PositionItemProgress");
        }
    }
}
