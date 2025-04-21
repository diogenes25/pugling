using FluentAssertions;
using pugling.Application;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Models;
using pugling.Models.Converter;

namespace puglingTest.Models.Converter
{
    public class VocabularyConverterTest
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
        public void FromDtoToApp()
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
        public void FromAppToDto()
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
        public void FromDtoToAppToEntity()
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
        public void FromEntityToAppToDto()
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
        public void FromEntityToApp()
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
    }
}
