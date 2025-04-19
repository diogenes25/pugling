using pugling.Application;
using pugling.Models;
using Xunit;
using Moq;
using FluentAssertions;

namespace puglingTest.Application
{
    public class NounDetailsTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstance()
        {
            // Arrange
            var determinedArticle = "the";
            var genus = "masculine";
            var undeterminedArticle = "a";

            // Act
            var result = NounDetails.Create(determinedArticle, genus, undeterminedArticle);

            // Assert
            result.Should().NotBeNull("the created instance should not be null");
            result.DeterminedArticle.Should().Be(determinedArticle, "the determined article should match the input");
            result.Genus.Should().Be(genus, "the genus should match the input");
            result.UndeterminedArticle.Should().Be(undeterminedArticle, "the undetermined article should match the input");
        }

        [Fact]
        public void Create_FromINounDetails_ReturnsExpectedInstance()
        {
            // Arrange
            var mockDetails = new Mock<INounDetails>();
            mockDetails.Setup(m => m.DeterminedArticle).Returns("the");
            mockDetails.Setup(m => m.Genus).Returns("masculine");
            mockDetails.Setup(m => m.UndeterminedArticle).Returns("a");

            // Act
            var result = NounDetails.Create(mockDetails.Object);

            // Assert
            result.Should().NotBeNull("the created instance should not be null");
            result.DeterminedArticle.Should().Be(mockDetails.Object.DeterminedArticle, "the determined article should match the mock value");
            result.Genus.Should().Be(mockDetails.Object.Genus, "the genus should match the mock value");
            result.UndeterminedArticle.Should().Be(mockDetails.Object.UndeterminedArticle, "the undetermined article should match the mock value");
        }

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCode()
        {
            // Arrange
            var details1 = NounDetails.Create("the", "masculine", "a");
            var details2 = NounDetails.Create("the", "masculine", "a");

            // Act
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2, "hash codes should be the same for instances with the same values");
        }

        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodes()
        {
            // Arrange
            var details1 = NounDetails.Create("the", "masculine", "a");
            var details2 = NounDetails.Create("a", "feminine", "an");

            // Act
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2, "hash codes should be different for instances with different values");
        }

        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            // Arrange
            var details1 = NounDetails.Create("the", "masculine", "a");
            var details2 = NounDetails.Create("the", "masculine", "a");

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeTrue("the equality operator should return true for instances with the same values");
        }

        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var details1 = NounDetails.Create("the", "masculine", "a");
            var details2 = NounDetails.Create("a", "feminine", "an");

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeFalse("the equality operator should return false for instances with different values");
        }

        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            // Arrange
            var details1 = NounDetails.Create("the", "masculine", "a");
            var details2 = NounDetails.Create("the", "masculine", "a");

            // Act
            var result = details1 != details2;

            // Assert
            result.Should().BeFalse("the inequality operator should return false for instances with the same values");
        }

        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
        {
            // Arrange
            var details1 = NounDetails.Create("the", "masculine", "a");
            var details2 = NounDetails.Create("a", "feminine", "an");

            // Act
            var result = details1 != details2;

            // Assert
            result.Should().BeTrue("the inequality operator should return true for instances with different values");
        }
    }
}
