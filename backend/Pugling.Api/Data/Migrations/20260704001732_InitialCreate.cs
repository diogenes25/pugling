using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClozeTexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Translation = table.Column<string>(type: "TEXT", nullable: true),
                    Gaps = table.Column<string>(type: "TEXT", nullable: false),
                    WordBank = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClozeTexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fathers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Pin = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fathers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyDayRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyDayRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Multiplier = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vocabulary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", nullable: false),
                    Word = table.Column<string>(type: "TEXT", nullable: false),
                    Translation = table.Column<string>(type: "TEXT", nullable: false),
                    PartOfSpeech = table.Column<int>(type: "INTEGER", nullable: false),
                    Noun = table.Column<string>(type: "TEXT", nullable: true),
                    Verb = table.Column<string>(type: "TEXT", nullable: true),
                    BaseFormId = table.Column<int>(type: "INTEGER", nullable: true),
                    PronunciationAudioUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vocabulary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vocabulary_Vocabulary_BaseFormId",
                        column: x => x.BaseFormId,
                        principalTable: "Vocabulary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FatherId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    BirthYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Pin = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Children_Fathers_FatherId",
                        column: x => x.FatherId,
                        principalTable: "Fathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChildPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildPoints_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Klassenarbeiten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Topic = table.Column<string>(type: "TEXT", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Grade = table.Column<decimal>(type: "TEXT", precision: 3, scale: 1, nullable: true),
                    GradeComment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Klassenarbeiten", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Klassenarbeiten_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Klassenarbeiten_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    NewItemsPerLesson = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DailyMinutesRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyTestRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    DailyTestPassPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultStage = table.Column<int>(type: "INTEGER", nullable: false),
                    RequireTypedTest = table.Column<bool>(type: "INTEGER", nullable: false),
                    StageSchedule = table.Column<string>(type: "TEXT", nullable: true),
                    UseLeitner = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxBox = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxIntervalDays = table.Column<string>(type: "TEXT", nullable: true),
                    PointsMinutesMet = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsTestPassed = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsDayCompleteBonus = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyPlans_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyPlans_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Timetable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeOfDay = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timetable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timetable_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Timetable_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChapterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Feedback = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRatings_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActiveSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeSessions_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClozeTextId = table.Column<int>(type: "INTEGER", nullable: true),
                    IntroducedAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReviewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_ClozeTexts_ClozeTextId",
                        column: x => x.ClozeTextId,
                        principalTable: "ClozeTexts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyPlanItems_Vocabulary_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabulary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StageValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Graded = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalItems = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectItems = table.Column<int>(type: "INTEGER", nullable: false),
                    ScorePercent = table.Column<int>(type: "INTEGER", nullable: false),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestAttempts_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KlassenarbeitTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KlassenarbeitId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KlassenarbeitTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KlassenarbeitTags_Klassenarbeiten_KlassenarbeitId",
                        column: x => x.KlassenarbeitId,
                        principalTable: "Klassenarbeiten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KlassenarbeitTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaggedByRole = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseTags_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KlassenarbeitExercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KlassenarbeitId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KlassenarbeitExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KlassenarbeitExercises_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KlassenarbeitExercises_Klassenarbeiten_KlassenarbeitId",
                        column: x => x.KlassenarbeitId,
                        principalTable: "Klassenarbeiten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PracticeSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StageValue = table.Column<int>(type: "INTEGER", nullable: false),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewEvents_PracticeSessions_PracticeSessionId",
                        column: x => x.PracticeSessionId,
                        principalTable: "PracticeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestItemResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TestAttemptId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<int>(type: "INTEGER", nullable: false),
                    GapIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    StageValue = table.Column<int>(type: "INTEGER", nullable: false),
                    GivenAnswer = table.Column<string>(type: "TEXT", nullable: true),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    HintsUsed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestItemResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestItemResults_TestAttempts_TestAttemptId",
                        column: x => x.TestAttemptId,
                        principalTable: "TestAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SubjectId",
                table: "Chapters",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPoints_ChildId",
                table: "ChildPoints",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_FatherId",
                table: "Children",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_ClozeTexts_Key",
                table: "ClozeTexts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentRatings_StudyPlanId",
                table: "ContentRatings",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ChapterId",
                table: "Exercises",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTags_ExerciseId",
                table: "ExerciseTags",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTags_TagId_ExerciseId",
                table: "ExerciseTags",
                columns: new[] { "TagId", "ExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Klassenarbeiten_ChildId",
                table: "Klassenarbeiten",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Klassenarbeiten_SubjectId",
                table: "Klassenarbeiten",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_KlassenarbeitExercises_ExerciseId",
                table: "KlassenarbeitExercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_KlassenarbeitExercises_KlassenarbeitId_ExerciseId",
                table: "KlassenarbeitExercises",
                columns: new[] { "KlassenarbeitId", "ExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KlassenarbeitTags_KlassenarbeitId_TagId",
                table: "KlassenarbeitTags",
                columns: new[] { "KlassenarbeitId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KlassenarbeitTags_TagId",
                table: "KlassenarbeitTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_StudyPlanId",
                table: "PracticeSessions",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewEvents_PracticeSessionId",
                table: "ReviewEvents",
                column: "PracticeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyDayRewards_StudyPlanId_Day_Kind",
                table: "StudyDayRewards",
                columns: new[] { "StudyPlanId", "Day", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_ClozeTextId",
                table: "StudyPlanItems",
                column: "ClozeTextId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_StudyPlanId",
                table: "StudyPlanItems",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanItems_VocabularyId",
                table: "StudyPlanItems",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_ChildId",
                table: "StudyPlans",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_SubjectId",
                table: "StudyPlans",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ChildId_Name",
                table: "Tags",
                columns: new[] { "ChildId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_StudyPlanId",
                table: "TestAttempts",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TestItemResults_TestAttemptId",
                table: "TestItemResults",
                column: "TestAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetable_ChildId_SubjectId_DayOfWeek",
                table: "Timetable",
                columns: new[] { "ChildId", "SubjectId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timetable_SubjectId",
                table: "Timetable",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Vocabulary_BaseFormId",
                table: "Vocabulary",
                column: "BaseFormId");

            migrationBuilder.CreateIndex(
                name: "IX_Vocabulary_Key",
                table: "Vocabulary",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChildPoints");

            migrationBuilder.DropTable(
                name: "ContentRatings");

            migrationBuilder.DropTable(
                name: "ExerciseTags");

            migrationBuilder.DropTable(
                name: "KlassenarbeitExercises");

            migrationBuilder.DropTable(
                name: "KlassenarbeitTags");

            migrationBuilder.DropTable(
                name: "ReviewEvents");

            migrationBuilder.DropTable(
                name: "StudyDayRewards");

            migrationBuilder.DropTable(
                name: "StudyPlanItems");

            migrationBuilder.DropTable(
                name: "TestItemResults");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropTable(
                name: "Timetable");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "Klassenarbeiten");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "PracticeSessions");

            migrationBuilder.DropTable(
                name: "ClozeTexts");

            migrationBuilder.DropTable(
                name: "Vocabulary");

            migrationBuilder.DropTable(
                name: "TestAttempts");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "StudyPlans");

            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Fathers");
        }
    }
}
