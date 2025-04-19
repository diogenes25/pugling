using pugling.Work;
using pugling.Models;
using Xunit;
using Moq;

namespace puglingTest.Work
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
            Assert.NotNull(result);
            Assert.Equal(form, result.Form);
            Assert.Equal(vocObjRef, result.VocObjRef);
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
            Assert.NotNull(result);
            Assert.Equal(mockDetails.Object.Form, result.Form);
            Assert.Equal(mockDetails.Object.VocObjRef, result.VocObjRef);
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
            Assert.Equal(hashCode1, hashCode2);
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
            Assert.NotEqual(hashCode1, hashCode2);
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
            Assert.True(result);
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
            Assert.False(result);
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
            Assert.False(result);
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
            Assert.True(result, "Expected details1 and details2 to be unequal, but they were considered equal.");
        }

    }
}
