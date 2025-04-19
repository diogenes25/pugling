using FluentAssertions;
using Moq;
using pugling.Application;
using pugling.Models;

namespace puglingTest.Application
{
    public class VocabularyBaseTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstance()
        {
            // Arrange
            var id = "1";
            var word = "hello";
            var translation = "hola";

            // Act
            var result = VocabularyBase.Create(id, word, translation);

            // Assert
            result.Should().NotBeNull("the created instance should not be null");
            result.Id.Should().Be(id, "the Id should match the provided value");
            result.Word.Should().Be(word, "the Word should match the provided value");
            result.Translation.Should().Be(translation, "the Translation should match the provided value");
        }

        [Fact]
        public void Create_FromIVocabularyBase_ReturnsExpectedInstance()
        {
            // Arrange
            var mockVocabulary = new Mock<IVocabularyBase>();
            mockVocabulary.Setup(m => m.Id).Returns("1");
            mockVocabulary.Setup(m => m.Word).Returns("hello");
            mockVocabulary.Setup(m => m.Translation).Returns("hola");

            // Act
            var result = VocabularyBase.Create(mockVocabulary.Object);

            // Assert
            result.Should().NotBeNull("the created instance should not be null");
            result.Id.Should().Be(mockVocabulary.Object.Id, "the Id should match the mock's Id");
            result.Word.Should().Be(mockVocabulary.Object.Word, "the Word should match the mock's Word");
            result.Translation.Should().Be(mockVocabulary.Object.Translation, "the Translation should match the mock's Translation");
        }

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("1", "hello", "hola");

            // Act
            var result = vocab1.Equals(vocab2);

            // Assert
            result.Should().BeTrue("two instances with the same values should be equal");
        }

        [Fact]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("2", "goodbye", "adios");

            // Act
            var result = vocab1.Equals(vocab2);

            // Assert
            result.Should().BeFalse("two instances with different values should not be equal");
        }

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCode()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("1", "hello", "hola");

            // Act
            var hashCode1 = vocab1.GetHashCode();
            var hashCode2 = vocab2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2, "two instances with the same values should have the same hash code");
        }

        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodes()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("2", "goodbye", "adios");

            // Act
            var hashCode1 = vocab1.GetHashCode();
            var hashCode2 = vocab2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2, "two instances with different values should have different hash codes");
        }

        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("1", "hello", "hola");

            // Act
            var result = vocab1 == vocab2;

            // Assert
            result.Should().BeTrue("the equality operator should return true for instances with the same values");
        }

        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("2", "goodbye", "adios");

            // Act
            var result = vocab1 == vocab2;

            // Assert
            result.Should().BeFalse("the equality operator should return false for instances with different values");
        }

        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("1", "hello", "hola");

            // Act
            var result = vocab1 != vocab2;

            // Assert
            result.Should().BeFalse("the inequality operator should return false for instances with the same values");
        }

        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
        {
            // Arrange
            var vocab1 = VocabularyBase.Create("1", "hello", "hola");
            var vocab2 = VocabularyBase.Create("2", "goodbye", "adios");

            // Act
            var result = vocab1 != vocab2;

            // Assert
            result.Should().BeTrue("the inequality operator should return true for instances with different values");
        }
    }
}