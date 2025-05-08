using FluentAssertions;
using Moq;
using PugLing.Model.Models;

namespace PugLing.Core.Application.Vocabularies;

/// <summary>
/// Contains unit tests for the <see cref="IdiomaticUsage"/> class.
/// </summary>
public class IdiomaticUsageTests
{
    /// <summary>
    /// Tests that creating an <see cref="IdiomaticUsage"/> instance with valid parameters returns the expected instance.
    /// </summary>
    [Fact]
    public void Create_WithValidParameters_ReturnsExpectedInstanceTest()
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

    /// <summary>
    /// Tests that creating an <see cref="IdiomaticUsage"/> instance from an <see cref="IIdiomaticUsage"/> returns the expected instance.
    /// </summary>
    [Fact]
    public void Create_FromIIdiomaticUsage_ReturnsExpectedInstanceTest()
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

    /// <summary>
    /// Tests that two <see cref="IdiomaticUsage"/> instances with the same values return the same hash code.
    /// </summary>
    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHashCodeTest()
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

    /// <summary>
    /// Tests that two <see cref="IdiomaticUsage"/> instances with different values return different hash codes.
    /// </summary>
    [Fact]
    public void GetHashCode_DifferentValues_ReturnsDifferentHashCodesTest()
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

    /// <summary>
    /// Tests that the equality operator returns true for two <see cref="IdiomaticUsage"/> instances with the same values.
    /// </summary>
    [Fact]
    public void EqualityOperator_SameValues_ReturnsTrueTest()
    {
        // Arrange
        var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
        var usage2 = new IdiomaticUsage("Break the ice", "Romper el hielo");

        // Act
        var result = usage1 == usage2;

        // Assert
        result.Should().BeTrue("because identical values should be considered equal");
    }

    /// <summary>
    /// Tests that the equality operator returns false for two <see cref="IdiomaticUsage"/> instances with different values.
    /// </summary>
    [Fact]
    public void EqualityOperator_DifferentValues_ReturnsFalseTest()
    {
        // Arrange
        var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
        var usage2 = new IdiomaticUsage("Hit the nail", "Dar en el clavo");

        // Act
        var result = usage1 == usage2;

        // Assert
        result.Should().BeFalse("because different values should not be considered equal");
    }

    /// <summary>
    /// Tests that the inequality operator returns false for two <see cref="IdiomaticUsage"/> instances with the same values.
    /// </summary>
    [Fact]
    public void InequalityOperator_SameValues_ReturnsFalseTest()
    {
        // Arrange
        var usage1 = new IdiomaticUsage("Break the ice", "Romper el hielo");
        var usage2 = new IdiomaticUsage("Break the ice", "Romper el hielo");

        // Act
        var result = usage1 != usage2;

        // Assert
        result.Should().BeFalse("because identical values should not be considered unequal");
    }

    /// <summary>
    /// Tests that the inequality operator returns true for two <see cref="IdiomaticUsage"/> instances with different values.
    /// </summary>
    [Fact]
    public void InequalityOperator_DifferentValues_ReturnsTrueTest()
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