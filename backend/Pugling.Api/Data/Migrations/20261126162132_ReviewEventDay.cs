using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewEventDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Day",
                table: "ItemReviewEvents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviewEvents_ChildId_Day",
                table: "ItemReviewEvents",
                columns: new[] { "ChildId", "Day" });

            // B-1028: an existing answer belongs to the German day of its timestamp - until now every answer was written on its
            // own day. SQLite knows no time zone; April to October counts as summer time, which is off only in the last week of
            // March and October, and then only for an answer between midnight and 1 a.m.
            migrationBuilder.Sql(
                "UPDATE ItemReviewEvents SET Day = date(At, CASE WHEN strftime('%m', At) BETWEEN '04' AND '10' " +
                "THEN '+2 hours' ELSE '+1 hours' END);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemReviewEvents_ChildId_Day",
                table: "ItemReviewEvents");

            migrationBuilder.DropColumn(
                name: "Day",
                table: "ItemReviewEvents");
        }
    }
}
