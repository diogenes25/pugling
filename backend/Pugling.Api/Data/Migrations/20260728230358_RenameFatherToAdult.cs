using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameFatherToAdult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountProfiles_Fathers_FatherId",
                table: "AccountProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CreatorProfiles_Fathers_OwnerFatherId",
                table: "CreatorProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseGrants_Fathers_CreatorId",
                table: "ExerciseGrants");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Fathers_AuthorFatherId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopArticles_Fathers_FatherId",
                table: "ShopArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_SupervisorLinks_Fathers_SupervisorId",
                table: "SupervisorLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_TextbookSeries_Fathers_OwnerFatherId",
                table: "TextbookSeries");

            // HAND KORRIGIERT: EF hat hier DropTable("Fathers") + CreateTable("Adults") erzeugt – es kann
            // eine Umbenennung nicht von „alt weg, neu her" unterscheiden. Ausgeführt hätte das JEDEN
            // Erwachsenen gelöscht, samt Autorschaft, Rechten und Konto-Verknüpfung. Es ist eine
            // Umbenennung, und nur so bleiben die Zeilen (und ihre Ids) erhalten.
            migrationBuilder.RenameTable(
                name: "Fathers",
                newName: "Adults");

            migrationBuilder.DropIndex(
                name: "IX_CreatorProfiles_OwnerFatherId_Name",
                table: "CreatorProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AccountProfiles_Role_FatherId",
                table: "AccountProfiles");

            migrationBuilder.RenameColumn(
                name: "OwnerFatherId",
                table: "TextbookSeries",
                newName: "OwnerAdultId");

            migrationBuilder.RenameIndex(
                name: "IX_TextbookSeries_OwnerFatherId",
                table: "TextbookSeries",
                newName: "IX_TextbookSeries_OwnerAdultId");

            migrationBuilder.RenameColumn(
                name: "FatherId",
                table: "ShopArticles",
                newName: "AdultId");

            migrationBuilder.RenameIndex(
                name: "IX_ShopArticles_FatherId_ArticleNumber",
                table: "ShopArticles",
                newName: "IX_ShopArticles_AdultId_ArticleNumber");

            migrationBuilder.RenameColumn(
                name: "AuthorFatherId",
                table: "Exercises",
                newName: "AuthorAdultId");

            migrationBuilder.RenameIndex(
                name: "IX_Exercises_AuthorFatherId",
                table: "Exercises",
                newName: "IX_Exercises_AuthorAdultId");

            migrationBuilder.RenameColumn(
                name: "GrantedByFatherId",
                table: "ExerciseGrants",
                newName: "GrantedByAdultId");

            migrationBuilder.RenameColumn(
                name: "OwnerFatherId",
                table: "CreatorProfiles",
                newName: "OwnerAdultId");

            migrationBuilder.RenameColumn(
                name: "FatherId",
                table: "AccountProfiles",
                newName: "AdultId");

            migrationBuilder.RenameIndex(
                name: "IX_AccountProfiles_FatherId",
                table: "AccountProfiles",
                newName: "IX_AccountProfiles_AdultId");

            migrationBuilder.AlterColumn<bool>(
                name: "ExecutePublic",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_OwnerAdultId_Name",
                table: "CreatorProfiles",
                columns: new[] { "OwnerAdultId", "Name" },
                unique: true,
                filter: "[OwnerAdultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_Role_AdultId",
                table: "AccountProfiles",
                columns: new[] { "Role", "AdultId" },
                unique: true,
                filter: "[AdultId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountProfiles_Adults_AdultId",
                table: "AccountProfiles",
                column: "AdultId",
                principalTable: "Adults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CreatorProfiles_Adults_OwnerAdultId",
                table: "CreatorProfiles",
                column: "OwnerAdultId",
                principalTable: "Adults",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseGrants_Adults_CreatorId",
                table: "ExerciseGrants",
                column: "CreatorId",
                principalTable: "Adults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Adults_AuthorAdultId",
                table: "Exercises",
                column: "AuthorAdultId",
                principalTable: "Adults",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopArticles_Adults_AdultId",
                table: "ShopArticles",
                column: "AdultId",
                principalTable: "Adults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupervisorLinks_Adults_SupervisorId",
                table: "SupervisorLinks",
                column: "SupervisorId",
                principalTable: "Adults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TextbookSeries_Adults_OwnerAdultId",
                table: "TextbookSeries",
                column: "OwnerAdultId",
                principalTable: "Adults",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountProfiles_Adults_AdultId",
                table: "AccountProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CreatorProfiles_Adults_OwnerAdultId",
                table: "CreatorProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseGrants_Adults_CreatorId",
                table: "ExerciseGrants");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Adults_AuthorAdultId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopArticles_Adults_AdultId",
                table: "ShopArticles");

            migrationBuilder.DropForeignKey(
                name: "FK_SupervisorLinks_Adults_SupervisorId",
                table: "SupervisorLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_TextbookSeries_Adults_OwnerAdultId",
                table: "TextbookSeries");

            // Gegenstück: ebenfalls eine Umbenennung, kein Neuaufbau.
            migrationBuilder.RenameTable(
                name: "Adults",
                newName: "Fathers");

            migrationBuilder.DropIndex(
                name: "IX_CreatorProfiles_OwnerAdultId_Name",
                table: "CreatorProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AccountProfiles_Role_AdultId",
                table: "AccountProfiles");

            migrationBuilder.RenameColumn(
                name: "OwnerAdultId",
                table: "TextbookSeries",
                newName: "OwnerFatherId");

            migrationBuilder.RenameIndex(
                name: "IX_TextbookSeries_OwnerAdultId",
                table: "TextbookSeries",
                newName: "IX_TextbookSeries_OwnerFatherId");

            migrationBuilder.RenameColumn(
                name: "AdultId",
                table: "ShopArticles",
                newName: "FatherId");

            migrationBuilder.RenameIndex(
                name: "IX_ShopArticles_AdultId_ArticleNumber",
                table: "ShopArticles",
                newName: "IX_ShopArticles_FatherId_ArticleNumber");

            migrationBuilder.RenameColumn(
                name: "AuthorAdultId",
                table: "Exercises",
                newName: "AuthorFatherId");

            migrationBuilder.RenameIndex(
                name: "IX_Exercises_AuthorAdultId",
                table: "Exercises",
                newName: "IX_Exercises_AuthorFatherId");

            migrationBuilder.RenameColumn(
                name: "GrantedByAdultId",
                table: "ExerciseGrants",
                newName: "GrantedByFatherId");

            migrationBuilder.RenameColumn(
                name: "OwnerAdultId",
                table: "CreatorProfiles",
                newName: "OwnerFatherId");

            migrationBuilder.RenameColumn(
                name: "AdultId",
                table: "AccountProfiles",
                newName: "FatherId");

            migrationBuilder.RenameIndex(
                name: "IX_AccountProfiles_AdultId",
                table: "AccountProfiles",
                newName: "IX_AccountProfiles_FatherId");

            migrationBuilder.AlterColumn<bool>(
                name: "ExecutePublic",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_OwnerFatherId_Name",
                table: "CreatorProfiles",
                columns: new[] { "OwnerFatherId", "Name" },
                unique: true,
                filter: "[OwnerFatherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_Role_FatherId",
                table: "AccountProfiles",
                columns: new[] { "Role", "FatherId" },
                unique: true,
                filter: "[FatherId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountProfiles_Fathers_FatherId",
                table: "AccountProfiles",
                column: "FatherId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CreatorProfiles_Fathers_OwnerFatherId",
                table: "CreatorProfiles",
                column: "OwnerFatherId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseGrants_Fathers_CreatorId",
                table: "ExerciseGrants",
                column: "CreatorId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Fathers_AuthorFatherId",
                table: "Exercises",
                column: "AuthorFatherId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopArticles_Fathers_FatherId",
                table: "ShopArticles",
                column: "FatherId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupervisorLinks_Fathers_SupervisorId",
                table: "SupervisorLinks",
                column: "SupervisorId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TextbookSeries_Fathers_OwnerFatherId",
                table: "TextbookSeries",
                column: "OwnerFatherId",
                principalTable: "Fathers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
