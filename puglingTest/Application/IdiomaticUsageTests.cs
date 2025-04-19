using pugling.Application;
using pugling.Models;
using Xunit;
using Moq;
using FluentAssertions;

namespace puglingTest.Application
{
    public class IdiomaticUsageTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstance()
        {
            // Arrange
            var phrase = "Break the ice";
            var translation = "Romper el hielo";

            // Act
            var result = new IdiomaticUsage(phrase, translation);

            // Assert
            result.Should().NotBeNull("because a valid IdiomaticUsage instance should be created");
            result.Phrase.Should().Be(phrase, "because the phrase should match the input parameter");
            result.Translation.Should().Be(translation, "because the translation should match the input parameter");
        }

        [Fact]
        public void Create_FromIIdiomaticUsage_ReturnsExpectedInstance()
        {
            // Arrange
            var mockUsage = new Mock<IIdiomaticUsage>();
            mockUsage.Setup(m => m.Phrase).Returns("Break the ice");
            mockUsage.Setup(m => m.Translation).Returns("Romper el hielo");

            // Act
            var result = IdiomaticUsage.Create(mockUsage.Object);

            // Assert
            result.Should().NotBeNull("because a valid IdiomaticUsage instance should be created from IIdiomaticUsage");
            result.Phrase.Should().Be(mockUsage.Object.Phrase, "because the phrase should match the mocked IIdiomaticUsage");
            result.Translation.Should().Be(mockUsage.Object.Translation, "because the translation should match the mocked IIdiomaticUsage");
        }

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCode()
        {
            // Arrange
            var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
            var usage2 = new IdiomaticUsage("Break the ice", "Romper el hielo");

            // Act
            var hashCode1 = usage1.GetHashCode();
            var hashCode2 = usage2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2, "because identical values should produce the same hash code");
        }

        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodes()
        {
            // Arrange
            var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
            var usage2 = new IdiomaticUsage("Hit the nail", "Dar en el clavo");

            // Act
            var hashCode1 = usage1.GetHashCode();
            var hashCode2 = usage2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2, "because different values should produce different hash codes");
        }

        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            // Arrange
            var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
            var usage2 = new IdiomaticUsage("Break the ice", "Romper el hielo");

            // Act
            var result = usage1 == usage2;

            // Assert
            result.Should().BeTrue("because identical values should be considered equal");
        }

        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
            var usage2 = new IdiomaticUsage("Hit the nail", "Dar en el clavo");

            // Act
            var result = usage1 == usage2;

            // Assert
            result.Should().BeFalse("because different values should not be considered equal");
        }

        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            // Arrange
            var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
            var usage2 = new IdiomaticUsage("Break the ice", "Romper el hielo");

            // Act
            var result = usage1 != usage2;

            // Assert
            result.Should().BeFalse("because identical values should not be considered unequal");
        }

        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
        {
            // Arrange
            var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
            var usage2 = new IdiomaticUsage("Hit the nail", "Dar en el clavo");

            // Act
            var result = usage1 != usage2;

            // Assert
            result.Should().BeTrue("because different values should be considered unequal");
        }
    }
}
