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
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PinHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Adults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Pin = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BirthYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: true),
                    SchoolType = table.Column<int>(type: "INTEGER", nullable: false),
                    Gender = table.Column<string>(type: "TEXT", nullable: false),
                    Interests = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AllowedContentRating = table.Column<int>(type: "INTEGER", nullable: false),
                    Pin = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SelectedSkin = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OwnedSkins = table.Column<string>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClozeTexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Translation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Gaps = table.Column<string>(type: "TEXT", nullable: false),
                    WordBank = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClozeTexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterestTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Facet = table.Column<string>(type: "TEXT", nullable: false),
                    Synonyms = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    License = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Attribution = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Origin = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Placeholder = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Publishers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publishers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VocabTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vocabularies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Word = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Translation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    TranslationAlternatives = table.Column<string>(type: "TEXT", nullable: true),
                    PartOfSpeech = table.Column<string>(type: "TEXT", nullable: false),
                    Noun = table.Column<string>(type: "TEXT", nullable: true),
                    Verb = table.Column<string>(type: "TEXT", nullable: true),
                    BaseFormId = table.Column<int>(type: "INTEGER", nullable: true),
                    BaseFormRelation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PronunciationAudioUrl = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vocabularies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vocabularies_Vocabularies_BaseFormId",
                        column: x => x.BaseFormId,
                        principalTable: "Vocabularies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShopArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AdultId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticleNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    UnitType = table.Column<string>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopArticles_Adults_AdultId",
                        column: x => x.AdultId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    AdultId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountProfiles", x => x.Id);
                    table.CheckConstraint("CK_AccountProfile_SingleProfile", "(CASE WHEN \"AdultId\" IS NULL THEN 0 ELSE 1 END\r\n + CASE WHEN \"ChildId\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_AccountProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountProfiles_Adults_AdultId",
                        column: x => x.AdultId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountProfiles_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Metric = table.Column<string>(type: "TEXT", nullable: false),
                    Threshold = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Achievements_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChildPointsEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildPointsEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildPointsEntries_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyBoxClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CoinsAwarded = table.Column<int>(type: "INTEGER", nullable: false),
                    GemsAwarded = table.Column<int>(type: "INTEGER", nullable: false),
                    StreakAtClaim = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyBoxClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyBoxClaims_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Metric = table.Column<string>(type: "TEXT", nullable: false),
                    Target = table.Column<int>(type: "INTEGER", nullable: false),
                    Period = table.Column<string>(type: "TEXT", nullable: false),
                    RewardPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Missions_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Objectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Motivation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Start = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    RewardOnComplete = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardPerKeyResult = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Objectives_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupervisorLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Relation = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupervisorLinks_Adults_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupervisorLinks_Children_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
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
                name: "ChildInterests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildInterests_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildInterests_InterestTags_InterestTagId",
                        column: x => x.InterestTagId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaTagLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestTagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaTagLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaTagLinks_InterestTags_InterestTagId",
                        column: x => x.InterestTagId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaTagLinks_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Format = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Bytes = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaVariants_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseCategories_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
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
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Topic = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Grade = table.Column<decimal>(type: "TEXT", precision: 3, scale: 1, nullable: true),
                    GradeComment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
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
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
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
                name: "TextbookSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PublisherId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubjectName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    SchoolTypes = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceLanguage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TargetLanguage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    OwnerAdultId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextbookSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TextbookSeries_Adults_OwnerAdultId",
                        column: x => x.OwnerAdultId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TextbookSeries_Publishers_PublisherId",
                        column: x => x.PublisherId,
                        principalTable: "Publishers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TextbookSeries_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TimetableEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<string>(type: "TEXT", nullable: false),
                    TimeOfDay = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimetableEntries_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimetableEntries_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VocabTagLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VocabTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabTagLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabTagLinks_VocabTags_VocabTagId",
                        column: x => x.VocabTagId,
                        principalTable: "VocabTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VocabTagLinks_Vocabularies_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabularies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopArticleId = table.Column<int>(type: "INTEGER", nullable: true),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ArticleTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UnitType = table.Column<string>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivationRequests_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivationRequests_ShopArticles_ShopArticleId",
                        column: x => x.ShopArticleId,
                        principalTable: "ShopArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChildInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopArticleId = table.Column<int>(type: "INTEGER", nullable: true),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticleNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ArticleTitle = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UnitType = table.Column<string>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildInventories_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildInventories_ShopArticles_ShopArticleId",
                        column: x => x.ShopArticleId,
                        principalTable: "ShopArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShopListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShopArticleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CoinPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    GemPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitsPerPurchase = table.Column<int>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentStock = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxStock = table.Column<int>(type: "INTEGER", nullable: false),
                    RefillKind = table.Column<string>(type: "TEXT", nullable: false),
                    RefillAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RefillDayOfWeek = table.Column<string>(type: "TEXT", nullable: true),
                    LastRefilledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopListings_ShopArticles_ShopArticleId",
                        column: x => x.ShopArticleId,
                        principalTable: "ShopArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AchievementAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AchievementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchievementAwards_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MissionAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Period = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionAwards_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObjectiveId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaidKeyResultId = table.Column<int>(type: "INTEGER", nullable: true),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveRewards_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VocabularyTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VocabularyTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VocabularyTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VocabularyTags_Vocabularies_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabularies",
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
                name: "CreatorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OwnerAdultId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubjectName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    SchoolTypes = table.Column<int>(type: "INTEGER", nullable: false),
                    GradeMin = table.Column<int>(type: "INTEGER", nullable: true),
                    GradeMax = table.Column<int>(type: "INTEGER", nullable: true),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceLang = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TargetLang = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Persona = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Didactics = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DefaultTypes = table.Column<string>(type: "TEXT", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreatorProfiles_Adults_OwnerAdultId",
                        column: x => x.OwnerAdultId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreatorProfiles_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreatorProfiles_TextbookSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "TextbookSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SeriesUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    Grade = table.Column<int>(type: "INTEGER", nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BookType = table.Column<string>(type: "TEXT", nullable: false),
                    Topics = table.Column<string>(type: "TEXT", nullable: false),
                    Grammar = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    VocabularyNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesUnits_TextbookSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "TextbookSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShopListingId = table.Column<int>(type: "INTEGER", nullable: true),
                    SupervisorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArticleNumber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CoinPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    GemPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitsPerPurchase = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopPurchases_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopPurchases_ShopListings_ShopListingId",
                        column: x => x.ShopListingId,
                        principalTable: "ShopListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SeriesUnitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    RewardPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedBonus = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultStage = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultItemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultUseLeitner = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultRequireTypedTest = table.Column<bool>(type: "INTEGER", nullable: false),
                    GradeMin = table.Column<int>(type: "INTEGER", nullable: true),
                    GradeMax = table.Column<int>(type: "INTEGER", nullable: true),
                    SchoolTypes = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    AuthorAdultId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExecutePublic = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_Adults_AuthorAdultId",
                        column: x => x.AuthorAdultId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Exercises_ExerciseCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ExerciseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Exercises_SeriesUnits_SeriesUnitId",
                        column: x => x.SeriesUnitId,
                        principalTable: "SeriesUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Textbooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SubjectName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Grade = table.Column<int>(type: "INTEGER", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Isbn = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CurrentChapter = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentUnitId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Textbooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Textbooks_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Textbooks_SeriesUnits_CurrentUnitId",
                        column: x => x.CurrentUnitId,
                        principalTable: "SeriesUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Textbooks_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Textbooks_TextbookSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "TextbookSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseGrants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", nullable: false),
                    GrantedByAdultId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseGrants_Adults_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Adults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseGrants_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Hint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseItems_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseItems_Vocabularies_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabularies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
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
                name: "KeyResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObjectiveId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    SeriesUnitId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Metric = table.Column<string>(type: "TEXT", nullable: false),
                    TargetValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyResults_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyResults_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KeyResults_SeriesUnits_SeriesUnitId",
                        column: x => x.SeriesUnitId,
                        principalTable: "SeriesUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyResults_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "PlanPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Scope = table.Column<string>(type: "TEXT", nullable: false),
                    OrderStrategy = table.Column<string>(type: "TEXT", nullable: false),
                    Cadence = table.Column<string>(type: "TEXT", nullable: false),
                    GoalThreshold = table.Column<int>(type: "INTEGER", nullable: true),
                    RequireTypedTest = table.Column<bool>(type: "INTEGER", nullable: false),
                    PointsGoalMet = table.Column<int>(type: "INTEGER", nullable: false),
                    PenaltyCoins = table.Column<int>(type: "INTEGER", nullable: false),
                    NewContentPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    ComboThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    ComboBonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    SpeedThresholdSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    SpeedBonusPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeSlots = table.Column<string>(type: "TEXT", nullable: true),
                    UseLeitner = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxBox = table.Column<int>(type: "INTEGER", nullable: false),
                    BoxIntervalDays = table.Column<string>(type: "TEXT", nullable: true),
                    StageSchedule = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanPositions_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanPositions_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChildMediaPicks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rejected = table.Column<bool>(type: "INTEGER", nullable: false),
                    PickedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildMediaPicks", x => x.Id);
                    table.CheckConstraint("CK_ChildMediaPick_SingleCarrier", "(CASE WHEN \"VocabularyId\" IS NULL THEN 0 ELSE 1 END\r\n + CASE WHEN \"ExerciseItemId\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_ExerciseItems_ExerciseItemId",
                        column: x => x.ExerciseItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildMediaPicks_Vocabularies_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabularies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    MasteryPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    SeenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IntroducedAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LastAnswerAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastCorrect = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemProgress_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemProgress_ExerciseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemReviewEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    StageValue = table.Column<int>(type: "INTEGER", nullable: false),
                    GivenAnswer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemReviewEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemReviewEvents_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemReviewEvents_ExerciseItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MediaLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MediaAssetId = table.Column<int>(type: "INTEGER", nullable: false),
                    VocabularyId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaLinks", x => x.Id);
                    table.CheckConstraint("CK_MediaLink_SingleCarrier", "(CASE WHEN \"VocabularyId\" IS NULL THEN 0 ELSE 1 END\r\n + CASE WHEN \"ExerciseItemId\" IS NULL THEN 0 ELSE 1 END\r\n + CASE WHEN \"ExerciseId\" IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_MediaLinks_ExerciseItems_ExerciseItemId",
                        column: x => x.ExerciseItemId,
                        principalTable: "ExerciseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaLinks_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaLinks_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaLinks_Vocabularies_VocabularyId",
                        column: x => x.VocabularyId,
                        principalTable: "Vocabularies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionGoalPenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cadence = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionGoalPenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionGoalPenalties_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionGoalRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cadence = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionGoalRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionGoalRewards_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionItemProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Box = table.Column<int>(type: "INTEGER", nullable: false),
                    DueOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReviewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IntroducedAt = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionItemProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionItemProgress_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
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
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActiveSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<string>(type: "TEXT", nullable: false),
                    Cursor = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeSessions_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PracticeSessions_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Remarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AnsweredBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ParentRemarkId = table.Column<int>(type: "INTEGER", nullable: true),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorRole = table.Column<string>(type: "TEXT", nullable: false),
                    Route = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AppArea = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ChildId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExerciseId = table.Column<int>(type: "INTEGER", nullable: true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: true),
                    RecentErrorsJson = table.Column<string>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "TestAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudyPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanPositionId = table.Column<int>(type: "INTEGER", nullable: true),
                    Day = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StageValue = table.Column<int>(type: "INTEGER", nullable: false),
                    Graded = table.Column<bool>(type: "INTEGER", nullable: false),
                    BySupervisor = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalItems = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectItems = table.Column<int>(type: "INTEGER", nullable: false),
                    ScorePercent = table.Column<int>(type: "INTEGER", nullable: false),
                    Passed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<string>(type: "TEXT", nullable: false),
                    Cursor = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestAttempts_PlanPositions_PlanPositionId",
                        column: x => x.PlanPositionId,
                        principalTable: "PlanPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TestAttempts_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
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
                name: "RemarkComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RemarkId = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorLabel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "TestItemResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TestAttemptId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    StageValue = table.Column<int>(type: "INTEGER", nullable: false),
                    GivenAnswer = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    WasCorrect = table.Column<bool>(type: "INTEGER", nullable: false),
                    HintsUsed = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestItemResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestItemResult_TestAttempts_TestAttemptId",
                        column: x => x.TestAttemptId,
                        principalTable: "TestAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_AccountId_Role",
                table: "AccountProfiles",
                columns: new[] { "AccountId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_AdultId",
                table: "AccountProfiles",
                column: "AdultId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_ChildId",
                table: "AccountProfiles",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_Role_AdultId",
                table: "AccountProfiles",
                columns: new[] { "Role", "AdultId" },
                unique: true,
                filter: "[AdultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountProfiles_Role_ChildId",
                table: "AccountProfiles",
                columns: new[] { "Role", "ChildId" },
                unique: true,
                filter: "[ChildId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AchievementAwards_AchievementId",
                table: "AchievementAwards",
                column: "AchievementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_ChildId_Metric_Threshold",
                table: "Achievements",
                columns: new[] { "ChildId", "Metric", "Threshold" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivationRequests_ChildId_SupervisorId",
                table: "ActivationRequests",
                columns: new[] { "ChildId", "SupervisorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationRequests_ShopArticleId",
                table: "ActivationRequests",
                column: "ShopArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Adults_Email",
                table: "Adults",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChildInterests_ChildId_InterestTagId",
                table: "ChildInterests",
                columns: new[] { "ChildId", "InterestTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildInterests_InterestTagId",
                table: "ChildInterests",
                column: "InterestTagId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildInventories_ChildId_ShopArticleId",
                table: "ChildInventories",
                columns: new[] { "ChildId", "ShopArticleId" },
                unique: true,
                filter: "[ShopArticleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChildInventories_ChildId_SupervisorId",
                table: "ChildInventories",
                columns: new[] { "ChildId", "SupervisorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildInventories_ShopArticleId",
                table: "ChildInventories",
                column: "ShopArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ChildId_ExerciseItemId_MediaAssetId",
                table: "ChildMediaPicks",
                columns: new[] { "ChildId", "ExerciseItemId", "MediaAssetId" },
                unique: true,
                filter: "[ExerciseItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ChildId_VocabularyId_MediaAssetId",
                table: "ChildMediaPicks",
                columns: new[] { "ChildId", "VocabularyId", "MediaAssetId" },
                unique: true,
                filter: "[VocabularyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_ExerciseItemId",
                table: "ChildMediaPicks",
                column: "ExerciseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_MediaAssetId",
                table: "ChildMediaPicks",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildMediaPicks_VocabularyId",
                table: "ChildMediaPicks",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildPointsEntries_ChildId_CreatedAt_Id",
                table: "ChildPointsEntries",
                columns: new[] { "ChildId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ChildPointsEntries_ChildId_Kind",
                table: "ChildPointsEntries",
                columns: new[] { "ChildId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_ClozeTexts_Key",
                table: "ClozeTexts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_OwnerAdultId_Name",
                table: "CreatorProfiles",
                columns: new[] { "OwnerAdultId", "Name" },
                unique: true,
                filter: "[OwnerAdultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_SeriesId",
                table: "CreatorProfiles",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_CreatorProfiles_SubjectId_SeriesId",
                table: "CreatorProfiles",
                columns: new[] { "SubjectId", "SeriesId" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyBoxClaims_ChildId_Day",
                table: "DailyBoxClaims",
                columns: new[] { "ChildId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCategories_SubjectId_Name",
                table: "ExerciseCategories",
                columns: new[] { "SubjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseGrants_CreatorId",
                table: "ExerciseGrants",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseGrants_ExerciseId_CreatorId_Permission",
                table: "ExerciseGrants",
                columns: new[] { "ExerciseId", "CreatorId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseItems_ExerciseId_OrderIndex",
                table: "ExerciseItems",
                columns: new[] { "ExerciseId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseItems_ExerciseId_VocabularyId",
                table: "ExerciseItems",
                columns: new[] { "ExerciseId", "VocabularyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseItems_VocabularyId",
                table: "ExerciseItems",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_AuthorAdultId",
                table: "Exercises",
                column: "AuthorAdultId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_CategoryId",
                table: "Exercises",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_SeriesUnitId",
                table: "Exercises",
                column: "SeriesUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_Type",
                table: "Exercises",
                column: "Type");

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
                name: "IX_InterestTags_Slug",
                table: "InterestTags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ChildId_ExerciseId",
                table: "ItemProgress",
                columns: new[] { "ChildId", "ExerciseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ChildId_ItemId",
                table: "ItemProgress",
                columns: new[] { "ChildId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ChildId_VocabularyId",
                table: "ItemProgress",
                columns: new[] { "ChildId", "VocabularyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemProgress_ItemId",
                table: "ItemProgress",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviewEvents_ChildId_ItemId_At",
                table: "ItemReviewEvents",
                columns: new[] { "ChildId", "ItemId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviewEvents_ChildId_VocabularyId",
                table: "ItemReviewEvents",
                columns: new[] { "ChildId", "VocabularyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemReviewEvents_ItemId",
                table: "ItemReviewEvents",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_ExerciseId",
                table: "KeyResults",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_ObjectiveId_ExerciseId_Metric",
                table: "KeyResults",
                columns: new[] { "ObjectiveId", "ExerciseId", "Metric" },
                unique: true,
                filter: "[ExerciseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_ObjectiveId_SeriesUnitId_Metric",
                table: "KeyResults",
                columns: new[] { "ObjectiveId", "SeriesUnitId", "Metric" },
                unique: true,
                filter: "[SeriesUnitId] IS NOT NULL AND [ExerciseId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_ObjectiveId_SubjectId_Metric",
                table: "KeyResults",
                columns: new[] { "ObjectiveId", "SubjectId", "Metric" },
                unique: true,
                filter: "[SeriesUnitId] IS NULL AND [ExerciseId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_SeriesUnitId",
                table: "KeyResults",
                column: "SeriesUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_SubjectId",
                table: "KeyResults",
                column: "SubjectId");

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
                name: "IX_MediaAssets_Key",
                table: "MediaAssets",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Kind_Rating",
                table: "MediaAssets",
                columns: new[] { "Kind", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_ExerciseId",
                table: "MediaLinks",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_ExerciseItemId",
                table: "MediaLinks",
                column: "ExerciseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_MediaAssetId",
                table: "MediaLinks",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_MediaAssetId_ExerciseId",
                table: "MediaLinks",
                columns: new[] { "MediaAssetId", "ExerciseId" },
                unique: true,
                filter: "[ExerciseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_MediaAssetId_ExerciseItemId",
                table: "MediaLinks",
                columns: new[] { "MediaAssetId", "ExerciseItemId" },
                unique: true,
                filter: "[ExerciseItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_MediaAssetId_VocabularyId",
                table: "MediaLinks",
                columns: new[] { "MediaAssetId", "VocabularyId" },
                unique: true,
                filter: "[VocabularyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MediaLinks_VocabularyId",
                table: "MediaLinks",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTagLinks_InterestTagId",
                table: "MediaTagLinks",
                column: "InterestTagId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaTagLinks_MediaAssetId_InterestTagId",
                table: "MediaTagLinks",
                columns: new[] { "MediaAssetId", "InterestTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaVariants_MediaAssetId_Purpose_Format",
                table: "MediaVariants",
                columns: new[] { "MediaAssetId", "Purpose", "Format" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionAwards_MissionId_Period",
                table: "MissionAwards",
                columns: new[] { "MissionId", "Period" },
                unique: true,
                filter: "[PeriodStart] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MissionAwards_MissionId_Period_PeriodStart",
                table: "MissionAwards",
                columns: new[] { "MissionId", "Period", "PeriodStart" },
                unique: true,
                filter: "[PeriodStart] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ChildId_Active",
                table: "Missions",
                columns: new[] { "ChildId", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveRewards_ObjectiveId",
                table: "ObjectiveRewards",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveRewards_ObjectiveId_Complete",
                table: "ObjectiveRewards",
                column: "ObjectiveId",
                unique: true,
                filter: "[PaidKeyResultId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveRewards_ObjectiveId_PaidKeyResultId",
                table: "ObjectiveRewards",
                columns: new[] { "ObjectiveId", "PaidKeyResultId" },
                unique: true,
                filter: "[PaidKeyResultId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_ChildId",
                table: "Objectives",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPositions_ExerciseId",
                table: "PlanPositions",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPositions_StudyPlanId_Order_Id",
                table: "PlanPositions",
                columns: new[] { "StudyPlanId", "Order", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_PositionGoalPenalties_PlanPositionId_Cadence_PeriodStart",
                table: "PositionGoalPenalties",
                columns: new[] { "PlanPositionId", "Cadence", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionGoalRewards_PlanPositionId_Cadence_PeriodStart",
                table: "PositionGoalRewards",
                columns: new[] { "PlanPositionId", "Cadence", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionItemProgress_PlanPositionId_ItemIndex",
                table: "PositionItemProgress",
                columns: new[] { "PlanPositionId", "ItemIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_PlanPositionId_Day_Mode",
                table: "PracticeSessions",
                columns: new[] { "PlanPositionId", "Day", "Mode" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_StudyPlanId_Day",
                table: "PracticeSessions",
                columns: new[] { "StudyPlanId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_Publishers_Slug",
                table: "Publishers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemarkComments_AuthorAccountId",
                table: "RemarkComments",
                column: "AuthorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RemarkComments_RemarkId_CreatedAt",
                table: "RemarkComments",
                columns: new[] { "RemarkId", "CreatedAt" });

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

            migrationBuilder.CreateIndex(
                name: "IX_ReviewEvents_PracticeSessionId",
                table: "ReviewEvents",
                column: "PracticeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesUnits_SeriesId_Grade_OrderIndex",
                table: "SeriesUnits",
                columns: new[] { "SeriesId", "Grade", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopArticles_AdultId_ArticleNumber",
                table: "ShopArticles",
                columns: new[] { "AdultId", "ArticleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopListings_ShopArticleId",
                table: "ShopListings",
                column: "ShopArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ChildId_SupervisorId",
                table: "ShopPurchases",
                columns: new[] { "ChildId", "SupervisorId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ShopListingId",
                table: "ShopPurchases",
                column: "ShopListingId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_ChildId",
                table: "StudyPlans",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_SubjectId",
                table: "StudyPlans",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Name",
                table: "Subjects",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorLinks_StudentId",
                table: "SupervisorLinks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorLinks_SupervisorId_StudentId",
                table: "SupervisorLinks",
                columns: new[] { "SupervisorId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_ChildId_Name",
                table: "Tags",
                columns: new[] { "ChildId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_PlanPositionId_Day_CompletedAt_Passed",
                table: "TestAttempts",
                columns: new[] { "PlanPositionId", "Day", "CompletedAt", "Passed" });

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_StudyPlanId_Day",
                table: "TestAttempts",
                columns: new[] { "StudyPlanId", "Day" });

            migrationBuilder.CreateIndex(
                name: "IX_TestItemResult_TestAttemptId",
                table: "TestItemResult",
                column: "TestAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_ChildId",
                table: "Textbooks",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_CurrentUnitId",
                table: "Textbooks",
                column: "CurrentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_SeriesId",
                table: "Textbooks",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_Textbooks_SubjectId",
                table: "Textbooks",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TextbookSeries_OwnerAdultId",
                table: "TextbookSeries",
                column: "OwnerAdultId");

            migrationBuilder.CreateIndex(
                name: "IX_TextbookSeries_PublisherId",
                table: "TextbookSeries",
                column: "PublisherId");

            migrationBuilder.CreateIndex(
                name: "IX_TextbookSeries_Slug",
                table: "TextbookSeries",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TextbookSeries_SubjectId",
                table: "TextbookSeries",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEntries_ChildId_SubjectId_DayOfWeek",
                table: "TimetableEntries",
                columns: new[] { "ChildId", "SubjectId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableEntries_SubjectId",
                table: "TimetableEntries",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabTagLinks_VocabTagId_VocabularyId",
                table: "VocabTagLinks",
                columns: new[] { "VocabTagId", "VocabularyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabTagLinks_VocabularyId",
                table: "VocabTagLinks",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_VocabTags_Name",
                table: "VocabTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vocabularies_BaseFormId",
                table: "Vocabularies",
                column: "BaseFormId");

            migrationBuilder.CreateIndex(
                name: "IX_Vocabularies_Key",
                table: "Vocabularies",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vocabularies_Translation",
                table: "Vocabularies",
                column: "Translation");

            migrationBuilder.CreateIndex(
                name: "IX_Vocabularies_Word",
                table: "Vocabularies",
                column: "Word");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTags_TagId_VocabularyId",
                table: "VocabularyTags",
                columns: new[] { "TagId", "VocabularyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyTags_VocabularyId",
                table: "VocabularyTags",
                column: "VocabularyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountProfiles");

            migrationBuilder.DropTable(
                name: "AchievementAwards");

            migrationBuilder.DropTable(
                name: "ActivationRequests");

            migrationBuilder.DropTable(
                name: "ChildInterests");

            migrationBuilder.DropTable(
                name: "ChildInventories");

            migrationBuilder.DropTable(
                name: "ChildMediaPicks");

            migrationBuilder.DropTable(
                name: "ChildPointsEntries");

            migrationBuilder.DropTable(
                name: "ClozeTexts");

            migrationBuilder.DropTable(
                name: "CreatorProfiles");

            migrationBuilder.DropTable(
                name: "DailyBoxClaims");

            migrationBuilder.DropTable(
                name: "ExerciseGrants");

            migrationBuilder.DropTable(
                name: "ExerciseTags");

            migrationBuilder.DropTable(
                name: "ItemProgress");

            migrationBuilder.DropTable(
                name: "ItemReviewEvents");

            migrationBuilder.DropTable(
                name: "KeyResults");

            migrationBuilder.DropTable(
                name: "KlassenarbeitExercises");

            migrationBuilder.DropTable(
                name: "KlassenarbeitTags");

            migrationBuilder.DropTable(
                name: "MediaLinks");

            migrationBuilder.DropTable(
                name: "MediaTagLinks");

            migrationBuilder.DropTable(
                name: "MediaVariants");

            migrationBuilder.DropTable(
                name: "MissionAwards");

            migrationBuilder.DropTable(
                name: "ObjectiveRewards");

            migrationBuilder.DropTable(
                name: "PositionGoalPenalties");

            migrationBuilder.DropTable(
                name: "PositionGoalRewards");

            migrationBuilder.DropTable(
                name: "PositionItemProgress");

            migrationBuilder.DropTable(
                name: "RemarkComments");

            migrationBuilder.DropTable(
                name: "ReviewEvents");

            migrationBuilder.DropTable(
                name: "ShopPurchases");

            migrationBuilder.DropTable(
                name: "SupervisorLinks");

            migrationBuilder.DropTable(
                name: "TestItemResult");

            migrationBuilder.DropTable(
                name: "Textbooks");

            migrationBuilder.DropTable(
                name: "TimetableEntries");

            migrationBuilder.DropTable(
                name: "VocabTagLinks");

            migrationBuilder.DropTable(
                name: "VocabularyTags");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "Klassenarbeiten");

            migrationBuilder.DropTable(
                name: "ExerciseItems");

            migrationBuilder.DropTable(
                name: "InterestTags");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "Objectives");

            migrationBuilder.DropTable(
                name: "Remarks");

            migrationBuilder.DropTable(
                name: "PracticeSessions");

            migrationBuilder.DropTable(
                name: "ShopListings");

            migrationBuilder.DropTable(
                name: "TestAttempts");

            migrationBuilder.DropTable(
                name: "VocabTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Vocabularies");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "ShopArticles");

            migrationBuilder.DropTable(
                name: "PlanPositions");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "StudyPlans");

            migrationBuilder.DropTable(
                name: "ExerciseCategories");

            migrationBuilder.DropTable(
                name: "SeriesUnits");

            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropTable(
                name: "TextbookSeries");

            migrationBuilder.DropTable(
                name: "Adults");

            migrationBuilder.DropTable(
                name: "Publishers");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
