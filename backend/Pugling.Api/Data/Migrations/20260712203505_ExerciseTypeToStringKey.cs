using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pugling.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExerciseTypeToStringKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Exercises",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            // Übungstyp: int-Enum → String-Key (= IExerciseType.Key). Bestandszeilen tragen nach dem
            // Spalten-Rebuild den früheren Enum-Wert als Ziffern-Text; auf die stabilen Schlüssel abbilden.
            migrationBuilder.Sql(
                """
                UPDATE Exercises SET Type = CASE Type
                    WHEN '0' THEN 'Vocabulary'
                    WHEN '1' THEN 'Reading'
                    WHEN '2' THEN 'Cloze'
                    WHEN '3' THEN 'Essay'
                    WHEN '4' THEN 'Listening'
                    WHEN '5' THEN 'Grammar'
                    WHEN '6' THEN 'Matching'
                    WHEN '7' THEN 'Translation'
                    WHEN '8' THEN 'Arithmetic'
                    WHEN '9' THEN 'ArithmeticDrill'
                    WHEN '10' THEN 'List'
                    WHEN '11' THEN 'Birkenbihl'
                    ELSE Type
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Schlüssel zurück auf die früheren Enum-Ziffern, bevor die Spalte wieder INTEGER wird.
            migrationBuilder.Sql(
                """
                UPDATE Exercises SET Type = CASE Type
                    WHEN 'Vocabulary' THEN '0'
                    WHEN 'Reading' THEN '1'
                    WHEN 'Cloze' THEN '2'
                    WHEN 'Essay' THEN '3'
                    WHEN 'Listening' THEN '4'
                    WHEN 'Grammar' THEN '5'
                    WHEN 'Matching' THEN '6'
                    WHEN 'Translation' THEN '7'
                    WHEN 'Arithmetic' THEN '8'
                    WHEN 'ArithmeticDrill' THEN '9'
                    WHEN 'List' THEN '10'
                    WHEN 'Birkenbihl' THEN '11'
                    ELSE '0'
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
