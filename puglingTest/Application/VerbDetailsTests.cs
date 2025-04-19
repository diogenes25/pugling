using pugling.Application;
using pugling.Models;
using Xunit;
using Moq;
using System.Collections.Generic;
using FluentAssertions;

namespace puglingTest.Application
{
    public class VerbDetailsTests
    {
        [Fact]
        public void Create_WithValidParameters_ReturnsExpectedInstance()
        {
            // Arrange  
            var isBaseForm = true;
            var baseFormRef = "http://example.com/vocab/run";
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

        [Fact]
        public void Create_FromIVerbDetails_ReturnsExpectedInstance()
        {
            // Arrange  
            var mockVerbDetails = new Mock<IVerbDetails>();
            mockVerbDetails.Setup(m => m.IsBaseForm).Returns(true);
            mockVerbDetails.Setup(m => m.BaseFormRef).Returns("http://example.com/vocab/run");
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

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);

            // Act  
            var result = details1.Equals(details2);

            // Assert  
            result.Should().BeTrue("two VerbDetails instances with the same values should be equal");
        }

        [Fact]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, "http://example.com/vocab/walk", "du", "gehen", "Präteritum", null);

            // Act  
            var result = details1.Equals(details2);

            // Assert  
            result.Should().BeFalse("two VerbDetails instances with different values should not be equal");
        }

        [Fact]
        public void GetHashCode_SameValues_ReturnsSameHashCode()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);

            // Act  
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert  
            hashCode1.Should().Be(hashCode2, "two VerbDetails instances with the same values should have the same hash code");
        }

        [Fact]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashCodes()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, "http://example.com/vocab/walk", "du", "gehen", "Präteritum", null);

            // Act  
            var hashCode1 = details1.GetHashCode();
            var hashCode2 = details2.GetHashCode();

            // Assert  
            hashCode1.Should().NotBe(hashCode2, "two VerbDetails instances with different values should have different hash codes");
        }

        [Fact]
        public void EqualityOperator_SameValues_ReturnsTrue()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);

            // Act  
            var result = details1 == details2;

            // Assert  
            result.Should().BeTrue("two VerbDetails instances with the same values should be equal using the equality operator");
        }

        [Fact]
        public void EqualityOperator_DifferentValues_ReturnsFalse()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, "http://example.com/vocab/walk", "du", "gehen", "Präteritum", null);

            // Act  
            var result = details1 == details2;

            // Assert  
            result.Should().BeFalse("two VerbDetails instances with different values should not be equal using the equality operator");
        }

        [Fact]
        public void InequalityOperator_SameValues_ReturnsFalse()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);

            // Act  
            var result = details1 != details2;

            // Assert  
            result.Should().BeFalse("two VerbDetails instances with the same values should not be unequal using the inequality operator");
        }

        [Fact]
        public void InequalityOperator_DifferentValues_ReturnsTrue()
        {
            // Arrange  
            var details1 = VerbDetails.Create(true, "http://example.com/vocab/run", "ich", "laufen", "Präsens", null);
            var details2 = VerbDetails.Create(false, "http://example.com/vocab/walk", "du", "gehen", "Präteritum", null);

            // Act  
            var result = details1 != details2;

            // Assert  
            result.Should().BeTrue("two VerbDetails instances with different values should be unequal using the inequality operator");
        }
    }
}
