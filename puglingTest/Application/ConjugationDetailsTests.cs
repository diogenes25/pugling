using FluentAssertions;
using Moq;
using pugling.Application;
using pugling.Models;

namespace puglingTest.Application
{
    public class ConjugationDetailsTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstance()
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

        [Fact]
        public void Create_FromIConjugationDetails_ReturnsExpectedInstance()
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

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCode()
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

        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodes()
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

        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            // Arrange
            var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
            var details2 = ConjugationDetails.Create("run", "http://example.com/vocab/run");

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeTrue("Expected the equality operator to return true for identical values.");
        }

        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            // Arrange
            var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
            var details2 = ConjugationDetails.Create("walk", "http://example.com/vocab/walk");

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeFalse("Expected the equality operator to return false for different values.");
        }

        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            // Arrange
            var details1 = ConjugationDetails.Create("run", "http://example.com/vocab/run");
            var details2 = ConjugationDetails.Create("run", "http://example.com/vocab/run");

            // Act
            var result = details1 != details2;

            // Assert
            result.Should().BeFalse("Expected the inequality operator to return false for identical values.");
        }

        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
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
}