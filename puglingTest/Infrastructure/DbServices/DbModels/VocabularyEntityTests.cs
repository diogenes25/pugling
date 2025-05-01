using FluentAssertions;
using pugling.Application;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Models;
using pugling.Models.Constants;
using System.ComponentModel.DataAnnotations;

namespace puglingTest.Infrastructure.DbServices.DbModels
{
    public class VocabularyEntityTests
    {
        [Fact]
        public void FillAndValidate_ShouldFillPropertiesCorrectlyTest()
        {
            // Arrange
            var vocabulary = new VocabularyDto
            {
                Id = "1",
                Version = "1.0",
                Word = "test",
                Translation = "test_translation",
                Description = "test_description",
                ExampleSentenceSrc = "This is a test sentence.",
                ExampleSentenceTarget = "Dies ist ein Testsatz.",
                ExampleSentenceTense = "Present",
                IdiomaticUsages = new[]
                {
                       new IdiomaticUsageDto { Phrase = "test phrase", Translation = "test translation" }
                   },
                Noun = new NounDetailsDto
                {
                    DeterminedArticle = "der",
                    Genus = EGenus.Masculine,
                    UndeterminedArticle = "ein"
                },
                PartOfSpeech = pugling.Models.Constants.EPartOfSpeech.Noun,
                Pronunciation = "test pronunciation",
                PronunciationAudioUrl = "http://example.com/audio.mp3",
                RelatedForms = [new VocabularyBaseDto { Id = "2", Word = "related", Translation = "related_translation" }],
                SourceLanguage = "en",
                TargetLanguage = "de",
                UpdatedAt = DateTime.UtcNow,
                Verb = new VerbDetailsDto
                {
                    BaseFormRef = new Uri("http://example.com/vocab/run"),
                    Infinitiv = "to test",
                    IsBaseForm = true,
                    Person = "third",
                    Tense = "Present"
                }
            };

            var entity = new VocabularyEntity();

            // Act
            var result = entity.FillAndValidate(Vocabulary.Create(vocabulary, null));

            // Assert
            result.Id.Should().Be(vocabulary.Id);
            result.Version.Should().Be(vocabulary.Version);
            result.Word.Should().Be(vocabulary.Word);
            result.Translation.Should().Be(vocabulary.Translation);
            result.Description.Should().Be(vocabulary.Description);
            result.ExampleSentenceSrc.Should().Be(vocabulary.ExampleSentenceSrc);
            result.ExampleSentenceTarget.Should().Be(vocabulary.ExampleSentenceTarget);
            result.ExampleSentenceTense.Should().Be(vocabulary.ExampleSentenceTense);
            result.IdiomaticUsages.Should().HaveCount(1);
            result.IdiomaticUsages[0].Phrase.Should().Be("test phrase");
            result.IdiomaticUsages[0].Translation.Should().Be("test translation");
            result.Noun.Should().NotBeNull();
            result.Noun!.DeterminedArticle.Should().Be("der");
            result.Noun.Genus.Should().Be(EGenus.Masculine);
            result.Noun.UndeterminedArticle.Should().Be("ein");
            result.PartOfSpeech.Should().Be(vocabulary.PartOfSpeech);
            result.Pronunciation.Should().Be(vocabulary.Pronunciation);
            result.PronunciationAudioUrl.Should().Be(vocabulary.PronunciationAudioUrl);
            result.RelatedForms.Should().HaveCount(1);
            result.RelatedForms[0].Id.Should().Be("2");
            result.RelatedForms[0].Word.Should().Be("related");
            result.RelatedForms[0].Translation.Should().Be("related_translation");
            result.SourceLanguage.Should().Be(vocabulary.SourceLanguage);
            result.TargetLanguage.Should().Be(vocabulary.TargetLanguage);
            result.UpdatedAt.Should().Be(vocabulary.UpdatedAt);
            result.Verb.Should().NotBeNull();
            result.Verb!.BaseFormRef.Should().Be(new Uri("http://example.com/vocab/run"));
            result.Verb.Infinitiv.Should().Be("to test");
            result.Verb.IsBaseForm.Should().BeTrue();
            result.Verb.Person.Should().Be("third");
            result.Verb.Tense.Should().Be("Present");
        }

        [Fact]
        public void Validate_ShouldReturnValidationErrors_WhenConstraintsAreViolatedTest()
        {
            // Arrange
            var entity = new VocabularyEntity
            {
                Word = new string('a', 501), // Exceeds max length
                Translation = string.Empty, // Must not be empty
                PartOfSpeech = pugling.Models.Constants.EPartOfSpeech.NotSet, // Exceeds max length
                Description = new string('b', 1001), // Exceeds max length
                ExampleSentenceSrc = new string('c', 2001), // Exceeds max length
                ExampleSentenceTarget = new string('d', 2001), // Exceeds max length
                ExampleSentenceTense = new string('e', 101), // Exceeds max length
                Pronunciation = new string('f', 501), // Exceeds max length
                PronunciationAudioUrl = "invalid_url", // Invalid URL
                SourceLanguage = "", // Empty
                TargetLanguage = "", // Empty
                Version = new string('g', 51) // Exceeds max length
            };

            // Act
            var validationResults = entity.Validate(new ValidationContext(entity)).ToList();

            // Assert
            validationResults.Should().HaveCount(11);
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("Word must be non-empty and at most 500 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("Translation must be non-empty and at most 500 characters."));
            //validationResults.Should().Contain(v => v.ErrorMessage!.Contains("PartOfSpeech must be non-empty and at most 500 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("Description must be at most 1000 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("ExampleSentenceSrc must be at most 2000 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("ExampleSentenceTarget must be at most 2000 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("ExampleSentenceTense must be at most 100 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("Pronunciation must be at most 500 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("PronunciationAudioUrl must be a valid URL."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("SourceLanguage must be non-empty and at most 100 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("TargetLanguage must be non-empty and at most 100 characters."));
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("Version must be non-empty and at most 50 characters."));
        }
    }
}