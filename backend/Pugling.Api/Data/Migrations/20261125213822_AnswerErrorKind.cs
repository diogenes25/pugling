using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <summary>
    /// B-732: why a typed answer was wrong, on both answer logs, and the cross-plan box before the answer. All nullable and
    /// left empty for existing rows (decision 3: old answers are not classified afterwards).
    /// </summary>
    public partial class AnswerErrorKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorKind",
                table: "TestItemResult",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoxBefore",
                table: "ItemReviewEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorKind",
                table: "ItemReviewEvents",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorKind",
                table: "TestItemResult");

            migrationBuilder.DropColumn(
                name: "BoxBefore",
                table: "ItemReviewEvents");

            migrationBuilder.DropColumn(
                name: "ErrorKind",
                table: "ItemReviewEvents");
        }
    }
}
