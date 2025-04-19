using pugling.Application;
using pugling.Models;
using Xunit;
using Moq;
using System;
using System.Linq;
using FluentAssertions;

namespace puglingTest.Application
{
    /// <summary>
    /// Contains unit tests for the <see cref="Vocabulary"/> class.
    /// </summary>
    public class VocabularyTests
    {
        /// <summary>
        /// Tests that the <see cref="Vocabulary.Create(string, string, string, string, string, string)"/> method
        /// creates a valid instance when provided with valid parameters.
        /// </summary>
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstance()
        {
            // Arrange
            var id = "1";
            var word = "run";
            var translation = "laufen";
            var partOfSpeech = "verb";
            var sourceLanguage = "en";
            var targetLanguage = "de";

            // Act
            var result = Vocabulary.Create(id, word, translation, partOfSpeech, sourceLanguage, targetLanguage);

            // Assert
            result.Should().NotBeNull("because a valid Vocabulary instance should be created");
            result.Id.Should().Be(id, "because the ID should match the input value");
            result.Word.Should().Be(word, "because the Word should match the input value");
            result.Translation.Should().Be(translation, "because the Translation should match the input value");
            result.PartOfSpeech.Should().Be(partOfSpeech, "because the PartOfSpeech should match the input value");
            result.SourceLanguage.Should().Be(sourceLanguage, "because the SourceLanguage should match the input value");
            result.TargetLanguage.Should().Be(targetLanguage, "because the TargetLanguage should match the input value");
        }

        /// <summary>
        /// Tests that the <see cref="Vocabulary.Create(IVocabulary{IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails})"/> method
        /// creates a valid instance when provided with a valid <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TVocabularyBase, TVerbDetails}"/> object.
        /// </summary>
        [Fact]
        public void Create_FromIVocabulary_ReturnsExpectedInstance()
        {
            // Arrange
            var mockVocabulary = new Mock<IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails>>();
            mockVocabulary.Setup(m => m.Id).Returns("1");
            mockVocabulary.Setup(m => m.Word).Returns("run");
            mockVocabulary.Setup(m => m.Translation).Returns("laufen");
            mockVocabulary.Setup(m => m.PartOfSpeech).Returns("verb");
            mockVocabulary.Setup(m => m.SourceLanguage).Returns("en");
            mockVocabulary.Setup(m => m.TargetLanguage).Returns("de");
            mockVocabulary.Setup(m => m.Description).Returns("A verb meaning to move quickly.");
            mockVocabulary.Setup(m => m.ExampleSentenceSrc).Returns("I run every morning.");
            mockVocabulary.Setup(m => m.ExampleSentenceTarget).Returns("Ich laufe jeden Morgen.");
            mockVocabulary.Setup(m => m.ExampleSentenceTense).Returns("present");
            mockVocabulary.Setup(m => m.IdiomaticUsages).Returns(new[] { new Mock<IIdiomaticUsage>().Object });
            mockVocabulary.Setup(m => m.Noun).Returns(new Mock<INounDetails>().Object);
            mockVocabulary.Setup(m => m.Pronunciation).Returns("rʌn");
            mockVocabulary.Setup(m => m.PronunciationAudioUrl).Returns("http://example.com/audio/run.mp3");
            mockVocabulary.Setup(m => m.RelatedForms).Returns(new[] { new Mock<IVocabularyBase>().Object });
            mockVocabulary.Setup(m => m.UpdatedAt).Returns(DateTime.UtcNow);
            mockVocabulary.Setup(m => m.Verb).Returns(new Mock<IVerbDetails>().Object);

            // Act
            var result = Vocabulary.Create(mockVocabulary.Object);

            // Assert
            result.Should().NotBeNull("because a valid Vocabulary instance should be created from the mock");
            result.Id.Should().Be(mockVocabulary.Object.Id, "because the ID should match the mock value");
            result.Word.Should().Be(mockVocabulary.Object.Word, "because the Word should match the mock value");
            result.Translation.Should().Be(mockVocabulary.Object.Translation, "because the Translation should match the mock value");
            result.PartOfSpeech.Should().Be(mockVocabulary.Object.PartOfSpeech, "because the PartOfSpeech should match the mock value");
            result.SourceLanguage.Should().Be(mockVocabulary.Object.SourceLanguage, "because the SourceLanguage should match the mock value");
            result.TargetLanguage.Should().Be(mockVocabulary.Object.TargetLanguage, "because the TargetLanguage should match the mock value");
            result.Description.Should().Be(mockVocabulary.Object.Description, "because the Description should match the mock value");
            result.ExampleSentenceSrc.Should().Be(mockVocabulary.Object.ExampleSentenceSrc, "because the ExampleSentenceSrc should match the mock value");
            result.ExampleSentenceTarget.Should().Be(mockVocabulary.Object.ExampleSentenceTarget, "because the ExampleSentenceTarget should match the mock value");
            result.ExampleSentenceTense.Should().Be(mockVocabulary.Object.ExampleSentenceTense, "because the ExampleSentenceTense should match the mock value");
            result.IdiomaticUsages.Should().NotBeNull("because IdiomaticUsages should not be null");
            result.Noun.Should().NotBeNull("because Noun should not be null");
            result.Pronunciation.Should().Be(mockVocabulary.Object.Pronunciation, "because the Pronunciation should match the mock value");
            result.PronunciationAudioUrl.Should().Be(mockVocabulary.Object.PronunciationAudioUrl, "because the PronunciationAudioUrl should match the mock value");
            result.RelatedForms.Should().NotBeNull("because RelatedForms should not be null");
            result.UpdatedAt.Should().Be(mockVocabulary.Object.UpdatedAt, "because the UpdatedAt should match the mock value");
            result.Verb.Should().NotBeNull("because Verb should not be null");
        }

        /// <summary>
        /// Tests that the <see cref="Vocabulary.Equals(object)"/> method returns true
        /// when comparing two instances with the same values.
        /// </summary>
        [Fact]
        public void Equals_WithSameValues_ReturnsTrue()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");

            // Act
            var result = vocabulary1.Equals(vocabulary2);

            // Assert
            result.Should().BeTrue("because both Vocabulary instances have the same values");
        }

        /// <summary>
        /// Tests that the <see cref="Vocabulary.Equals(object)"/> method returns false
        /// when comparing two instances with different values.
        /// </summary>
        [Fact]
        public void Equals_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("2", "walk", "gehen", "verb", "en", "de");

            // Act
            var result = vocabulary1.Equals(vocabulary2);

            // Assert
            result.Should().BeFalse("because the Vocabulary instances have different values");
        }

        /// <summary>
        /// Tests that the <see cref="Vocabulary.GetHashCode"/> method returns the same hash code
        /// for instances with the same values.
        /// </summary>
        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCode()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");

            // Act
            var hashCode1 = vocabulary1.GetHashCode();
            var hashCode2 = vocabulary2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2, "because Vocabulary instances with the same values should have the same hash code");
        }

        /// <summary>
        /// Tests that the <see cref="Vocabulary.GetHashCode"/> method returns different hash codes
        /// for instances with different values.
        /// </summary>
        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodes()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("2", "walk", "gehen", "verb", "en", "de");

            // Act
            var hashCode1 = vocabulary1.GetHashCode();
            var hashCode2 = vocabulary2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2, "because Vocabulary instances with different values should have different hash codes");
        }

        /// <summary>
        /// Tests that the equality operator (==) returns true
        /// when comparing two instances with the same values.
        /// </summary>
        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");

            // Act
            var result = vocabulary1 == vocabulary2;

            // Assert
            result.Should().BeTrue("because both Vocabulary instances have the same values");
        }

        /// <summary>
        /// Tests that the equality operator (==) returns false
        /// when comparing two instances with different values.
        /// </summary>
        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("2", "walk", "gehen", "verb", "en", "de");

            // Act
            var result = vocabulary1 == vocabulary2;

            // Assert
            result.Should().BeFalse("because the Vocabulary instances have different values");
        }

        /// <summary>
        /// Tests that the inequality operator (!=) returns false
        /// when comparing two instances with the same values.
        /// </summary>
        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");

            // Act
            var result = vocabulary1 != vocabulary2;

            // Assert
            result.Should().BeFalse("because both Vocabulary instances have the same values");
        }

        /// <summary>
        /// Tests that the inequality operator (!=) returns true
        /// when comparing two instances with different values.
        /// </summary>
        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
        {
            // Arrange
            var vocabulary1 = Vocabulary.Create("1", "run", "laufen", "verb", "en", "de");
            var vocabulary2 = Vocabulary.Create("2", "walk", "gehen", "verb", "en", "de");

            // Act
            var result = vocabulary1 != vocabulary2;

            // Assert
            result.Should().BeTrue("because the Vocabulary instances have different values");
        }
    }
}
