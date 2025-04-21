using FluentAssertions;
using pugling.Models;
using pugling.Models.Constants;
using System.Text.Json;

namespace puglingTest.Models.TestData
{
    public class ReadTestdataTests
    {
        [Fact]
        public void TestReadJsonAndDeserialize()
        {
            // Arrange
            var filePath = Path.Combine(AppContext.BaseDirectory, "Models", "TestData", "en_de", "Vocabulary", "en_run_noun_lauf.json");

            var expected = new VocabularyDto
            {
                Word = "run",
                Translation = "Lauf",
                PartOfSpeech = EPartOfSpeech.Noun,
                SourceLanguage = "en",
                TargetLanguage = "de",
            };

            // Act
            var jsonString = File.ReadAllText(filePath);

            // Ensure the JSON string is valid
            jsonString.Should().NotBeNullOrWhiteSpace("The JSON string should not be null or empty.");

            // Check if the JSON structure matches the VocabularyDto
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Ensure case-insensitive deserialization
            };
            var result = JsonSerializer.Deserialize<VocabularyDto>(jsonString, options);

            // Assert
            result.Should().NotBeNull("Deserialization should produce a non-null result.");
            result.Word.Should().Be(expected.Word, "The Word property should match the expected value.");
            result.Translation.Should().Be(expected.Translation, "The Translation property should match the expected value.");
            result.PartOfSpeech.Should().Be(expected.PartOfSpeech, "The PartOfSpeech property should match the expected value.");
        }
    }
}
