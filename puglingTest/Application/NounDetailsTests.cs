using FluentAssertions;
using Moq;
using pugling.Application;
using pugling.Models;
using pugling.Models.Constants;

namespace puglingTest.Application
{
    /// <summary>
    /// Unit tests for the <see cref="NounDetails"/> class.
    /// </summary>
    public class NounDetailsTests
    {
        /// <summary>
        /// Tests that the <see cref="NounDetails.Create(string, string, string)"/> method
        /// creates an instance with the expected values when valid parameters are provided.
        /// </summary>
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstanceTest()
        {
            // Arrange
            var determinedArticle = "the";
            var genus = EGenus.Masculine;
            var undeterminedArticle = "a";

            // Act
            var result = NounDetails.Create(determinedArticle, genus, undeterminedArticle);

            // Assert
            result.Should().NotBeNull("the created instance should not be null");
            result.DeterminedArticle.Should().Be(determinedArticle, "the determined article should match the input");
            result.Genus.Should().Be(genus, "the genus should match the input");
            result.UndeterminedArticle.Should().Be(undeterminedArticle, "the undetermined article should match the input");
        }

        /// <summary>
        /// Tests that the <see cref="NounDetails.Create(INounDetails)"/> method
        /// creates an instance with the expected values when an <see cref="INounDetails"/> object is provided.
        /// </summary>
        [Fact]
        public void Create_FromINounDetails_ReturnsExpectedInstanceTest()
        {
            // Arrange
            var mockDetails = new Mock<INounDetails>();
            mockDetails.Setup(m => m.DeterminedArticle).Returns("the");
            mockDetails.Setup(m => m.Genus).Returns(EGenus.Masculine);
            mockDetails.Setup(m => m.UndeterminedArticle).Returns("a");

            // Act
            var result = NounDetails.Create(mockDetails.Object);

            // Assert
            result.Should().NotBeNull("the created instance should not be null");
            result.DeterminedArticle.Should().Be(mockDetails.Object.DeterminedArticle, "the determined article should match the mock value");
            result.Genus.Should().Be(mockDetails.Object.Genus, "the genus should match the mock value");
            result.UndeterminedArticle.Should().Be(mockDetails.Object.UndeterminedArticle, "the undetermined article should match the mock value");
        }

        /// <summary>
        /// Tests that the <see cref="NounDetails.GetHashCode"/> method
        /// returns the same hash code for instances with the same values.
        /// </summary>
        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCodeTest()
        {
            // Arrange
            var details1 = NounDetails.Create("the", EGenus.Masculine, "a");
            var details2 = NounDetails.Create("the", EGenus.Masculine   , "a");

            // Act
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2, "hash codes should be the same for instances with the same values");
        }

        /// <summary>
        /// Tests that the <see cref="NounDetails.GetHashCode"/> method
        /// returns different hash codes for instances with different values.
        /// </summary>
        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodesTest()
        {
            // Arrange
            var details1 = NounDetails.Create("the", EGenus.Masculine, "a");
            var details2 = NounDetails.Create("a", EGenus.Feminine, "an");

            // Act
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2, "hash codes should be different for instances with different values");
        }

        /// <summary>
        /// Tests that the equality operator (<c>==</c>) returns true for instances with the same values.
        /// </summary>
        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrueTest()
        {
            // Arrange
            var details1 = NounDetails.Create("the", EGenus.Masculine, "a");
            var details2 = NounDetails.Create("the", EGenus.Masculine, "a");

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeTrue("the equality operator should return true for instances with the same values");
        }

        /// <summary>
        /// Tests that the equality operator (<c>==</c>) returns false for instances with different values.
        /// </summary>
        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalseTest()
        {
            // Arrange
            var details1 = NounDetails.Create("the", EGenus.Masculine, "a");
            var details2 = NounDetails.Create("a", EGenus.Feminine, "an");

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeFalse("the equality operator should return false for instances with different values");
        }

        /// <summary>
        /// Tests that the inequality operator (<c>!=</c>) returns false for instances with the same values.
        /// </summary>
        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalseTest()
        {
            // Arrange
            var details1 = NounDetails.Create("the", EGenus.Masculine, "a");
            var details2 = NounDetails.Create("the", EGenus.Masculine, "a");

            // Act
            var result = details1 != details2;

            // Assert
            result.Should().BeFalse("the inequality operator should return false for instances with the same values");
        }

        /// <summary>
        /// Tests that the inequality operator (<c>!=</c>) returns true for instances with different values.
        /// </summary>
        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrueTest()
        {
            // Arrange
            var details1 = NounDetails.Create("the", EGenus.Masculine   , "a");
            var details2 = NounDetails.Create("a", EGenus.Feminine, "an");

            // Act
            var result = details1 != details2;

            // Assert
            result.Should().BeTrue("the inequality operator should return true for instances with different values");
        }
    }
}