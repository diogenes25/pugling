using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseAuthorship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bestehende Übungen bekommen bewusst keinen Autor (AuthorFatherId bleibt null): Vor diesem
            // Feature gab es kein Autorschafts-Konzept, es existiert also keine verlässliche Quelle für
            // den ursprünglichen Ersteller. Sie gelten damit als System-Übungen – global nutzbar, aber
            // von niemandem editierbar. Da die App vor der Publikation steht und die DB neu geseedet wird
            // (siehe CLAUDE.md), ist das die bewusste, korrekte Wahl statt einer erfundenen Zuordnung.
            migrationBuilder.AddColumn<int>(
                name: "AuthorFatherId",
                table: "Exercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_AuthorFatherId",
                table: "Exercises",
                column: "AuthorFatherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Fathers_AuthorFatherId",
                table: "Exercises",
                column: "AuthorFatherId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Fathers_AuthorFatherId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_AuthorFatherId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "AuthorFatherId",
                table: "Exercises");
        }
    }
}
