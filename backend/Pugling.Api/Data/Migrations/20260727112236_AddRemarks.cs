using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRemarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Remarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AnsweredBy = table.Column<string>(type: "TEXT", nullable: true),
                    ParentRemarkId = table.Column<int>(type: "INTEGER", nullable: true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorRole = table.Column<string>(type: "TEXT", nullable: false),
                    Route = table.Column<string>(type: "TEXT", nullable: false),
                    AppArea = table.Column<string>(type: "TEXT", nullable: false),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: true),
                    RecentErrorsJson = table.Column<string>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Remarks_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Remarks_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Remarks_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Remarks_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Remarks_Remarks_ParentRemarkId",
                        column: x => x.ParentRemarkId,
                        principalTable: "Remarks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Remarks_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Remarks_AccountId_CreatedAt",
                table: "Remarks",
                columns: new[] { "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Remarks_ChildId",
                table: "Remarks",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Remarks_ExerciseId",
                table: "Remarks",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Remarks_ParentRemarkId",
                table: "Remarks",
                column: "ParentRemarkId");

            migrationBuilder.CreateIndex(
                name: "IX_Remarks_PlanPositionId",
                table: "Remarks",
                column: "PlanPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Remarks_Status",
                table: "Remarks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Remarks_StudyPlanId",
                table: "Remarks",
                column: "StudyPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Remarks");
        }
    }
}
