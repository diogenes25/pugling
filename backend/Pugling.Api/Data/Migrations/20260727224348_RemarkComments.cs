using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemarkComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemarkComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RemarkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorLabel = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorAccountId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemarkComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemarkComments_Accounts_AuthorAccountId",
                        column: x => x.AuthorAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RemarkComments_Remarks_RemarkId",
                        column: x => x.RemarkId,
                        principalTable: "Remarks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemarkComments_AuthorAccountId",
                table: "RemarkComments",
                column: "AuthorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RemarkComments_RemarkId_CreatedAt",
                table: "RemarkComments",
                columns: new[] { "RemarkId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemarkComments");
        }
    }
}
