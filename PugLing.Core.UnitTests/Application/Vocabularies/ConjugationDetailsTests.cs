using FluentAssertions;
using Moq;
using PugLing.Core.Application.Vocabularies;
using PugLing.Model.Models;

namespace puglingTest.Application.Vocabularies;

/// <summary>
/// Contains unit tests for the <see cref="ConjugationDetails"/> class.
/// </summary>
public class ConjugationDetailsTests
{
    /// <summary>
    /// Tests that the <see cref="ConjugationDetails.Create(string, string)"/> method
    /// creates an instance with the expected property values when valid parameters are provided.
    /// </summary>
    [Fact]
    public void Create_WithValidParameters_ReturnsExpectedInstanceTest()
    {
        // Arrange
        var form = "run";
        var vocObjRef = "http://example.com/vocab/run";

        // Act
        var result = ConjugationDetails.Create(form, vocObjRef);

        // Assert
        result.Should().NotBeNull("Expected a non-null instance of ConjugationDetails.");
        result.Form.Should().Be(form, "Expected the Form property to match the input value.");
        result.VocObjRef.Should().Be(vocObjRef, "Expected the VocObjRef property to match the input value.");
    }

    /// <summary>
    /// Tests that the <see cref="ConjugationDetails.Create(IConjugationDetails)"/> method
    /// creates an instance with the expected property values when a valid <see cref="IConjugationDetails"/> object is provided.
    /// </summary>
    [Fact]
    public void Create_FromIConjugationDetails_ReturnsExpectedInstanceTest()
    {
        // Arrange
        var mockDetails = new Mock<IConjugationDetails>();
        mockDetails.Setup(m => m.Form).Returns("run");
        mockDetails.Setup(m => m.VocObjRef).Returns("http://example.com/vocab/run");

        // Act
        var result = ConjugationDetails.Create(mockDetails.Object);

        // Assert
        result.Should().NotBeNull("Expected a non-null instance of ConjugationDetails.");
        result.Form.Should().Be(mockDetails.Object.Form, "Expected the Form property to match the mock object's value.");
        result.VocObjRef.Should().Be(mockDetails.Object.VocObjRef, "Expected the VocObjRef property to match the mock object's value.");
    }

    /// <summary>
    /// Tests that the <see cref="ConjugationDetails.GetHashCode"/> method
    /// returns the same hash code for instances with identical property values.
    /// </summary>
    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHashCodeTest()
    {
        // Arrange
        var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
        var details2 = ConjugationDetails.Create("run", "http://example.com/vocab/run");

        // Act
        var hashCode1 = details1.GetHashCode();
        var hashCode2 = details2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2, "Expected the hash codes to be the same for identical values.");
    }

    /// <summary>
    /// Tests that the <see cref="ConjugationDetails.GetHashCode"/> method
    /// returns different hash codes for instances with different property values.
    /// </summary>
    [Fact]
    public void GetHashCode_DifferentValues_ReturnsDifferentHashCodesTest()
    {
        // Arrange
        var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
        var details2 = ConjugationDetails.Create("walk", "http://example.com/vocab/walk");

        // Act
        var hashCode1 = details1.GetHashCode();
        var hashCode2 = details2.GetHashCode();

        // Assert
        hashCode1.Should().NotBe(hashCode2, "Expected the hash codes to be different for different values.");
    }

    /// <summary>
    /// Tests that the equality operator (<c>==</c>) returns true for instances with identical property values.
    /// </summary>
    [Fact]
    public void EqualityOperator_SameValues_ReturnsTrueTest()
    {
        // Arrange
        var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
        var details2 = ConjugationDetails.Create("run", "http://example.com/vocab/run");

        // Act
        var result = details1 == details2;

        // Assert
        result.Should().BeTrue("Expected the equality operator to return true for identical values.");
    }

    /// <summary>
    /// Tests that the equality operator (<c>==</c>) returns false for instances with different property values.
    /// </summary>
    [Fact]
    public void EqualityOperator_DifferentValues_ReturnsFalseTest()
    {
        // Arrange
        var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
        var details2 = ConjugationDetails.Create("walk", "http://example.com/vocab/walk");

        // Act
        var result = details1 == details2;

        // Assert
        result.Should().BeFalse("Expected the equality operator to return false for different values.");
    }

    /// <summary>
    /// Tests that the inequality operator (<c>!=</c>) returns false for instances with identical property values.
    /// </summary>
    [Fact]
    public void InequalityOperator_SameValues_ReturnsFalseTest()
    {
        // Arrange
        var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
        var details2 = ConjugationDetails.Create("run", "http://example.com/vocab/run");

        // Act
        var result = details1 != details2;

        // Assert
        result.Should().BeFalse("Expected the inequality operator to return false for identical values.");
    }

    /// <summary>
    /// Tests that the inequality operator (<c>!=</c>) returns true for instances with different property values.
    /// </summary>
    [Fact]
    public void InequalityOperator_DifferentValues_ReturnsTrueTest()
    {
        // Arrange
        var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
        var details2 = ConjugationDetails.Create("walk", "http://example.com/vocab/walk");

        // Act
        var result = details1 != details2;

        // Assert
        result.Should().BeTrue("Expected the inequality operator to return true for different values.");
    }
}