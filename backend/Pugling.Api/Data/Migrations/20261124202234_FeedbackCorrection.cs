using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeedbackCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectedItems",
                table: "TestAttempts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PassedAfterCorrection",
                table: "TestAttempts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "PositionGoalPenalties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundedByFeedbackId",
                table: "PositionGoalPenalties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionGoalPenalties_RefundedByFeedbackId",
                table: "PositionGoalPenalties",
                column: "RefundedByFeedbackId");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionGoalPenalties_ExerciseFeedbacks_RefundedByFeedbackId",
                table: "PositionGoalPenalties",
                column: "RefundedByFeedbackId",
                principalTable: "ExerciseFeedbacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PositionGoalPenalties_ExerciseFeedbacks_RefundedByFeedbackId",
                table: "PositionGoalPenalties");

            migrationBuilder.DropIndex(
                name: "IX_PositionGoalPenalties_RefundedByFeedbackId",
                table: "PositionGoalPenalties");

            migrationBuilder.DropColumn(
                name: "CorrectedItems",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "PassedAfterCorrection",
                table: "TestAttempts");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "PositionGoalPenalties");

            migrationBuilder.DropColumn(
                name: "RefundedByFeedbackId",
                table: "PositionGoalPenalties");
        }
    }
}
