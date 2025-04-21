using FluentAssertions;
using pugling.Application;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Models;
using pugling.Models.Converter;
using System.Text.Json;

namespace puglingTest.Models.Converter
{
    public class VocabularyConverterTests
    {
        public static VocabularyDto CreateVocabulary() => new VocabularyDto
        {
            Id = "en_go_de",
            SourceLanguage = "en",
            Word = "go",
            TargetLanguage = "de",
            Translation = "gehen",
            PartOfSpeech = pugling.Models.Constants.EPartOfSpeech.Verb,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Version = "1.0",
            Pronunciation = "/ɡoʊ/",
            PronunciationAudioUrl = "https://example.com/audio/go.mp3",
            RelatedForms =
            [
                new() { Id = "en_go_de", Word = "go", Translation = "gehen" }
            ],
            IdiomaticUsages =
            [
                new() { Phrase = "go for a walk", Translation = "spazieren gehen" }
            ],

            ExampleSentenceSrc = "I go to the park.",
            ExampleSentenceTarget = "Ich gehe in den Park.",
            ExampleSentenceTense = "present"
        };


        [Fact]
        public void FromDtoToAppTest()
        {
            // Arrange
            var vocabulary = CreateVocabulary();

            // Act
            var vocabularyApp = Vocabulary.Create(vocabulary);

            // Assert
            vocabularyApp.Id.Should().Be(vocabulary.Id);
            vocabularyApp.Compare(vocabulary).Should().BeTrue();
        }

        [Fact]
        public void FromAppToDtoTest()
        {
            // Arrange
            var vocabulary = CreateVocabulary();

            // Act
            var vocabularyApp = Vocabulary.Create(vocabulary);
            var vocabularyDto = vocabularyApp.ToDomain();

            // Assert
            vocabularyApp.Id.Should().Be(vocabularyDto.Id);
            vocabularyApp.Compare(vocabularyDto).Should().BeTrue();
        }

        [Fact]
        public void FromDtoToAppToEntityTest()
        {
            // Arrange
            var vocabulary = CreateVocabulary();

            // Act
            var vocabularyApp = Vocabulary.Create(vocabulary);
            var vocabularyEntity = new VocabularyEntity().FillAndValidate(vocabularyApp);

            // Assert
            vocabularyApp.Id.Should().Be(vocabularyEntity.Id);
            vocabularyApp.Compare(vocabularyEntity).Should().BeTrue();
            vocabulary.Compare(vocabularyEntity).Should().BeTrue();
        }

        [Fact]
        public void FromEntityToAppToDtoTest()
        {
            // Arrange
            var vocabulary = CreateVocabulary();
            // Act
            var vocabularyApp = Vocabulary.Create(vocabulary);
            var vocabularyEntity = new VocabularyEntity().FillAndValidate(vocabularyApp);
            var vocabularyDto = vocabularyEntity.ToDomain();
            // Assert
            vocabularyApp.Id.Should().Be(vocabularyDto.Id);
            vocabularyApp.Compare(vocabularyDto).Should().BeTrue();
            vocabulary.Compare(vocabularyDto).Should().BeTrue();
        }

        [Fact]
        public void FromEntityToAppTest()
        {
            // Arrange
            var vocabulary = CreateVocabulary();
            // Act
            var vocabularyApp = Vocabulary.Create(vocabulary);
            var vocabularyEntity = new VocabularyEntity().FillAndValidate(vocabularyApp);
            vocabularyApp = Vocabulary.Create(vocabularyEntity);

            // Assert
            vocabularyApp.Id.Should().Be(vocabularyEntity.Id);
            vocabularyApp.Compare(vocabularyEntity).Should().BeTrue();
            vocabulary.Compare(vocabularyEntity).Should().BeTrue();
            vocabulary.Compare(vocabularyApp).Should().BeTrue();
        }

        [Fact]
        public void SerializeDtoTestTest()
        {
            // Arrange
            var vocabulary = CreateVocabulary();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Ensure case-insensitive deserialization
            };

            var jsonString = JsonSerializer.Serialize(vocabulary, options);
            // Ensure the JSON string is valid
            jsonString.Should().NotBeNullOrWhiteSpace($"The JSON string in file should not be null or empty.");
            // Deserialize the JSON into VocabularyDto
            try
            {
                var result = JsonSerializer.Deserialize<VocabularyDto>(jsonString, options);
                // Assert the deserialization result
                result.Should().NotBeNull($"Deserialization should produce a non-null result.");
                result.Word.Should().Be(vocabulary.Word, $"The Word property should match the original vocabulary word.");
                result.Translation.Should().Be(vocabulary.Translation, $"The Translation property should match the original vocabulary translation.");
                result.PartOfSpeech.Should().Be(vocabulary.PartOfSpeech, $"The PartOfSpeech property should match the original vocabulary part of speech.");
                result.SourceLanguage.Should().Be(vocabulary.SourceLanguage, $"The SourceLanguage property should match the original vocabulary source language.");
                result.TargetLanguage.Should().Be(vocabulary.TargetLanguage, $"The TargetLanguage property should match the original vocabulary target language.");
                result.Pronunciation.Should().Be(vocabulary.Pronunciation, $"The Pronunciation property should match the original vocabulary pronunciation.");
                result.PronunciationAudioUrl.Should().Be(vocabulary.PronunciationAudioUrl, $"The PronunciationAudioUrl property should match the original vocabulary pronunciation audio URL.");
                result.RelatedForms.Should().HaveCount(vocabulary.RelatedForms.Length, $"The RelatedForms property should match the original vocabulary related forms count.");
                result.IdiomaticUsages.Should().HaveCount(vocabulary.IdiomaticUsages.Length, $"The IdiomaticUsages property should match the original vocabulary idiomatic usages count.");
                result.ExampleSentenceSrc.Should().Be(vocabulary.ExampleSentenceSrc, $"The ExampleSentenceSrc property should match the original vocabulary example sentence source.");
                result.ExampleSentenceTarget.Should().Be(vocabulary.ExampleSentenceTarget, $"The ExampleSentenceTarget property should match the original vocabulary example sentence target.");
            }
            catch (JsonException ex)
            {
                // Handle JSON deserialization errors
                throw new Exception($"Failed to deserialize JSON: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                throw new Exception($"An unexpected error occurred while processing: {ex.Message}", ex);
            }
        }
    }
}
