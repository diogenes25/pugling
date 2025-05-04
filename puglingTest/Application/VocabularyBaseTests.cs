using FluentAssertions;
using Moq;
using pugling.Application;
using pugling.Models;

namespace puglingTest.Application
{
    /// <summary>
    /// Unit tests for the <see cref="VocabularyBase"/> class.
    /// </summary>
    public class VocabularyBaseTests
    {
        /// <summary>
        /// Tests that the <see cref="VocabularyBase.Create(string, string, string)"/> method
        /// creates an instance with the expected values when valid parameters are provided.
        /// </summary>
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstanceTest()
        {
            // Arrange
            var id = "1";
            var word = "hello";
            var translation = "hola";
            var sourceLanguage = "en";
            var targetLanguage = "es";

            // Act
            var result = new VocabularyBase(id, word, translation, sourceLanguage,targetLanguage);

            // Assert
            result.Should().NotBeNull("the created instance should not be null");
            result.Id.Should().Be(id, "the Id should match the provided value");
            result.Word.Should().Be(word, "the Word should match the provided value");
            result.Translation.Should().Be(translation, "the Translation should match the provided value");
        }

        /// <summary>
        /// Tests that the <see cref="VocabularyBase.Create(IVocabularyBase)"/> method
        /// creates an instance with the expected values when a valid <see cref="IVocabularyBase"/> is provided.
        /// </summary>
        [Fact]
        public void Create_FromIVocabularyBase_ReturnsExpectedInstanceTest()
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

        /// <summary>
        /// Tests that the <see cref="VocabularyBase.Equals(object)"/> method
        /// returns true when two instances have the same values.
        /// </summary>
        [Fact]
        public void Equals_SameValues_ReturnsTrueTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("1", "hello", "hola", "en", "es");

            // Act
            var result = vocab1.Equals(vocab2);

            // Assert
            result.Should().BeTrue("two instances with the same values should be equal");
        }

        /// <summary>
        /// Tests that the <see cref="VocabularyBase.Equals(object)"/> method
        /// returns false when two instances have different values.
        /// </summary>
        [Fact]
        public void Equals_DifferentValues_ReturnsFalseTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("2", "goodbye", "adios", "en", "es");

            // Act
            var result = vocab1.Equals(vocab2);

            // Assert
            result.Should().BeFalse("two instances with different values should not be equal");
        }

        /// <summary>
        /// Tests that the <see cref="VocabularyBase.GetHashCode"/> method
        /// returns the same hash code for instances with the same values.
        /// </summary>
        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCodeTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("1", "hello", "hola", "en", "es");

            // Act
            var hashCode1 = vocab1.GetHashCode();
            var hashCode2 = vocab2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2, "two instances with the same values should have the same hash code");
        }

        /// <summary>
        /// Tests that the <see cref="VocabularyBase.GetHashCode"/> method
        /// returns different hash codes for instances with different values.
        /// </summary>
        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodesTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("2", "goodbye", "adios", "en", "es");

            // Act
            var hashCode1 = vocab1.GetHashCode();
            var hashCode2 = vocab2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2, "two instances with different values should have different hash codes");
        }

        /// <summary>
        /// Tests that the equality operator (==) returns true for instances with the same values.
        /// </summary>
        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrueTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("1", "hello", "hola", "en", "es");

            // Act
            var result = vocab1 == vocab2;

            // Assert
            result.Should().BeTrue("the equality operator should return true for instances with the same values");
        }

        /// <summary>
        /// Tests that the equality operator (==) returns false for instances with different values.
        /// </summary>
        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalseTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("2", "goodbye", "adios", "en", "es");

            // Act
            var result = vocab1 == vocab2;

            // Assert
            result.Should().BeFalse("the equality operator should return false for instances with different values");
        }

        /// <summary>
        /// Tests that the inequality operator (!=) returns false for instances with the same values.
        /// </summary>
        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalseTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("1", "hello", "hola", "en", "es");

            // Act
            var result = vocab1 != vocab2;

            // Assert
            result.Should().BeFalse("the inequality operator should return false for instances with the same values");
        }

        /// <summary>
        /// Tests that the inequality operator (!=) returns true for instances with different values.
        /// </summary>
        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrueTest()
        {
            // Arrange
            var vocab1 = new VocabularyBase("1", "hello", "hola", "en", "es");
            var vocab2 = new VocabularyBase("2", "goodbye", "adios", "en", "es");

            // Act
            var result = vocab1 != vocab2;

            // Assert
            result.Should().BeTrue("the inequality operator should return true for instances with different values");
        }
    }
}