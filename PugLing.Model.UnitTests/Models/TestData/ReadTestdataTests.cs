using FluentAssertions;
using PugLing.Model.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace puglingTest.Models.TestData
{
    public class ReadTestdataTests
    {
        [Fact]
        public void TestReadJsonAndDeserialize()
        {
            var folders = new[]
            {
                "de_en",
                "en_de"
            };

            foreach (var folder in folders)
            {
                // Arrange
                var directoryPath = Path.Combine(AppContext.BaseDirectory, "Models", "TestData", folder, "Vocabulary");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // Ensure case-insensitive deserialization
                    Converters = { new JsonStringEnumConverter() } // Read enums as strings
                };

                // Act & Assert
                foreach (var filePath in Directory.GetFiles(directoryPath, "*.json"))
                {
                    // Read the JSON file
                    var jsonString = File.ReadAllText(filePath);

                    // Ensure the JSON string is valid
                    jsonString.Should().NotBeNullOrWhiteSpace($"The JSON string in file '{filePath}' should not be null or empty.");

                    // Deserialize the JSON into VocabularyDto
                    try
                    {
                        var result = JsonSerializer.Deserialize<VocabularyDto>(jsonString, options);

                        // Assert the deserialization result
                        result.Should().NotBeNull($"Deserialization of file '{filePath}' should produce a non-null result.");
                        result.Word.Should().NotBeNullOrWhiteSpace($"The Word property in file '{filePath}' should not be null or empty.");
                        result.Translation.Should().NotBeNullOrWhiteSpace($"The Translation property in file '{filePath}' should not be null or empty.");
                    }
                    catch (JsonException ex)
                    {
                        // Handle JSON deserialization errors
                        throw new Exception($"Failed to deserialize JSON from file '{filePath}': {ex.Message}", ex);
                    }
                    catch (Exception ex)
                    {
                        // Handle other exceptions
                        throw new Exception($"An unexpected error occurred while processing file '{filePath}': {ex.Message}", ex);
                    }
                }
            }
        }
    }
}