using FluentAssertions;
using Moq;
using pugling.Application.Vocabularies;
using pugling.Models;

namespace puglingTest.Application.Vocabularies
{
    /// <summary>
    /// Unit tests for the <see cref="VerbDetails"/> class.
    /// </summary>
    public class VerbDetailsTests
    {
        /// <summary>
        /// Tests that the <see cref="VerbDetails.Create(bool, string, string, string, string, Dictionary{string, Dictionary{string, IConjugationDetails}})"/> method
        /// creates an instance with the expected property values when valid parameters are provided.
        /// </summary>
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstanceTest()
        {
            // Arrange
            var isBaseForm = true;
            var baseFormRef = new Uri("http://example.com/vocab/run");
            var person = "ich";
            var infinitiv = "laufen";
            var tense = "Präsens";
            var conjugations = new Dictionary<string, Dictionary<string, IConjugationDetails>>();

            // Act
            var result = VerbDetails.Create(isBaseForm, baseFormRef, person, infinitiv, tense, conjugations);

            // Assert
            result.Should().NotBeNull("the created VerbDetails instance should not be null");
            result.IsBaseForm.Should().Be(isBaseForm, "the IsBaseForm property should match the input value");
            result.BaseFormRef.Should().Be(baseFormRef, "the BaseFormRef property should match the input value");
            result.Person.Should().Be(person, "the Person property should match the input value");
            result.Infinitiv.Should().Be(infinitiv, "the Infinitiv property should match the input value");
            result.Tense.Should().Be(tense, "the Tense property should match the input value");
            result.Conjugations.Should().BeEquivalentTo(conjugations, "the Conjugations property should match the input value");
        }

        /// <summary>
        /// Tests that the <see cref="VerbDetails.Create(IVerbDetails)"/> method
        /// creates an instance with the expected property values when an <see cref="IVerbDetails"/> instance is provided.
        /// </summary>
        [Fact]
        public void Create_FromIVerbDetails_ReturnsExpectedInstanceTest()
        {
            // Arrange
            var mockVerbDetails = new Mock<IVerbDetails>();
            mockVerbDetails.Setup(m => m.IsBaseForm).Returns(true);
            mockVerbDetails.Setup(m => m.BaseFormRef).Returns(new Uri("http://example.com/vocab/run"));
            mockVerbDetails.Setup(m => m.Person).Returns("ich");
            mockVerbDetails.Setup(m => m.Infinitiv).Returns("laufen");
            mockVerbDetails.Setup(m => m.Tense).Returns("Präsens");
            mockVerbDetails.Setup(m => m.Conjugations).Returns(new Dictionary<string, Dictionary<string, IConjugationDetails>>());

            // Act
            var result = VerbDetails.Create(mockVerbDetails.Object);

            // Assert
            result.Should().NotBeNull("the created VerbDetails instance should not be null");
            result.IsBaseForm.Should().Be(mockVerbDetails.Object.IsBaseForm, "the IsBaseForm property should match the mock value");
            result.BaseFormRef.Should().Be(mockVerbDetails.Object.BaseFormRef, "the BaseFormRef property should match the mock value");
            result.Person.Should().Be(mockVerbDetails.Object.Person, "the Person property should match the mock value");
            result.Infinitiv.Should().Be(mockVerbDetails.Object.Infinitiv, "the Infinitiv property should match the mock value");
            result.Tense.Should().Be(mockVerbDetails.Object.Tense, "the Tense property should match the mock value");
            result.Conjugations.Should().BeEquivalentTo(mockVerbDetails.Object.Conjugations, "the Conjugations property should match the mock value");
        }

        /// <summary>
        /// Tests that the <see cref="VerbDetails.Equals(object)"/> method
        /// returns true when two instances have the same property values.
        /// </summary>
        [Fact]
        public void Equals_SameValues_ReturnsTrueTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run") , "ich", "laufen", "Präsens", null);

            // Act
            var result = details1.Equals(details2);

            // Assert
            result.Should().BeTrue("two VerbDetails instances with the same values should be equal");
        }

        /// <summary>
        /// Tests that the <see cref="VerbDetails.Equals(object)"/> method
        /// returns false when two instances have different property values.
        /// </summary>
        [Fact]
        public void Equals_DifferentValues_ReturnsFalseTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, new Uri("http://example.com/vocab/walk"), "du", "gehen", "Präteritum", null);

            // Act
            var result = details1.Equals(details2);

            // Assert
            result.Should().BeFalse("two VerbDetails instances with different values should not be equal");
        }

        /// <summary>
        /// Tests that the <see cref="VerbDetails.GetHashCode"/> method
        /// returns the same hash code for two instances with the same property values.
        /// </summary>
        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCodeTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);

            // Act
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2, "two VerbDetails instances with the same values should have the same hash code");
        }

        /// <summary>
        /// Tests that the <see cref="VerbDetails.GetHashCode"/> method
        /// returns different hash codes for two instances with different property values.
        /// </summary>
        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodesTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, new Uri("http://example.com/vocab/walk"), "du", "gehen", "Präteritum", null);

            // Act
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2, "two VerbDetails instances with different values should have different hash codes");
        }

        /// <summary>
        /// Tests that the equality operator (==) returns true when two instances have the same property values.
        /// </summary>
        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrueTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeTrue("two VerbDetails instances with the same values should be equal using the equality operator");
        }

        /// <summary>
        /// Tests that the equality operator (==) returns false when two instances have different property values.
        /// </summary>
        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalseTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, new Uri("http://example.com/vocab/walk"), "du", "gehen", "Präteritum", null);

            // Act
            var result = details1 == details2;

            // Assert
            result.Should().BeFalse("two VerbDetails instances with different values should not be equal using the equality operator");
        }

        /// <summary>
        /// Tests that the inequality operator (!=) returns false when two instances have the same property values.
        /// </summary>
        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalseTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);

            // Act
            var result = details1 != details2;

            // Assert
            result.Should().BeFalse("two VerbDetails instances with the same values should not be unequal using the inequality operator");
        }

        /// <summary>
        /// Tests that the inequality operator (!=) returns true when two instances have different property values.
        /// </summary>
        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrueTest()
        {
            // Arrange
            var details1 = VerbDetails.Create(true, new Uri("http://example.com/vocab/run"), "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, new Uri("http://example.com/vocab/walk"), "du", "gehen", "Präteritum", null);

            // Act
            var result = details1 != details2;

            // Assert
            result.Should().BeTrue("two VerbDetails instances with different values should be unequal using the inequality operator");
        }
    }
}