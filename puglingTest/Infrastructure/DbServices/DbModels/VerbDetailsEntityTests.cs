using FluentAssertions;
using Moq;
using pugling.Application;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Models;
using System.ComponentModel.DataAnnotations;

namespace puglingTest.Infrastructure.DbServices.DbModels
{
    public class VerbDetailsEntityTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly_Test()
        {
            // Arrange
            var baseFormRef = new Uri("http://example.com/vocab/run");
            var infinitiv = "to run";
            var isBaseForm = true;
            var person = "first";
            var tense = "present";

            // Act
            var entity = new VerbDetailsEntity(baseFormRef, infinitiv, isBaseForm, person, tense, null);

            // Assert
            entity.BaseFormRef.Should().Be(baseFormRef);
            entity.Infinitiv.Should().Be(infinitiv);
            entity.IsBaseForm.Should().Be(isBaseForm);
            entity.Person.Should().Be(person);
            entity.Tense.Should().Be(tense);
        }

        [Fact]
        public void Constructor_WithIVerbDetails_ShouldInitializePropertiesCorrectly_Test()
        {
            // Arrange
            var mockVerbDetails = new Mock<IVerbDetails>();
            mockVerbDetails.Setup(vd => vd.BaseFormRef).Returns(new Uri("http://example.com/vocab/run"));
            mockVerbDetails.Setup(vd => vd.Infinitiv).Returns("to run");
            mockVerbDetails.Setup(vd => vd.IsBaseForm).Returns(true);
            mockVerbDetails.Setup(vd => vd.Person).Returns("first");
            mockVerbDetails.Setup(vd => vd.Tense).Returns("present");

            // Act
            var entity = new VerbDetailsEntity(mockVerbDetails.Object);

            // Assert
            entity.BaseFormRef.Should().Be(new Uri("http://example.com/vocab/run"));
            entity.Infinitiv.Should().Be("to run");
            entity.IsBaseForm.Should().BeTrue();
            entity.Person.Should().Be("first");
            entity.Tense.Should().Be("present");
        }

        [Fact]
        public void FillAndValidate_ShouldBeNull_WhenVerbIsNull_Test()
        {
            // Arrange
            var entity = new VerbDetailsEntity();

            // Act
            var result = entity.FillAndValidate(null);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void FillAndValidate_ShouldThrowArgumentException_WhenValidationFails_Test()
        {
            // Arrange
            var entity = new VerbDetailsEntity();
            var verb = VerbDetails.Create(new VerbDetailsDto()
            {
                Infinitiv = new string('a', 101), // Exceeds max length
            });

            // Act
            Action act = () => entity.FillAndValidate(verb);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("The following constraints were violated: *");
        }

        [Fact]
        public void FillAndValidate_ShouldFillPropertiesCorrectly_WhenValidationPasses_Test()
        {
            // Arrange
            var entity = new VerbDetailsEntity();
            var verb = VerbDetails.Create(new VerbDetailsDto()
            {
                BaseFormRef = new Uri("http://example.com/vocab/run"),
                Infinitiv = "to run",
                IsBaseForm = true,
                Person = "first",
                Tense = "present"
            });

            // Act
            var result = entity.FillAndValidate(verb);

            // Assert
            result.Should().Be(entity);
            entity.BaseFormRef.Should().Be(new Uri("http://example.com/vocab/run"));
            entity.Infinitiv.Should().Be("to run");
            entity.IsBaseForm.Should().BeTrue();
            entity.Person.Should().Be("first");
            entity.Tense.Should().Be("present");
        }

        [Fact]
        public void Validate_ShouldReturnValidationErrors_WhenPropertiesAreInvalid_Test()
        {
            // Arrange
            var entity = new VerbDetailsEntity
            {
                //BaseFormRef = new string('a', 101), // Exceeds max length
                Infinitiv = string.Empty // Invalid
            };

            // Act
            var validationResults = entity.Validate(new ValidationContext(entity)).ToList();

            // Assert
            validationResults.Should().HaveCount(1);
            //validationResults.Should().Contain(v => v.ErrorMessage.Contains("BaseFormRef is either null, empty, or exceeds the maximum length of 100."));
            validationResults.Should().Contain(v => v.ErrorMessage.Contains("Infinitiv is either null, empty, or exceeds the maximum length of 100."));
        }

        [Fact]
        public void Validate_ShouldReturnNoValidationErrors_WhenPropertiesAreValid_Test()
        {
            // Arrange
            var entity = new VerbDetailsEntity
            {
                BaseFormRef = new Uri("http://example.com/vocab/run"),
                Infinitiv = "to run",
                IsBaseForm = true,
                Person = "first",
                Tense = "present"
            };

            // Act
            var validationResults = entity.Validate(new ValidationContext(entity)).ToList();

            // Assert
            validationResults.Should().BeEmpty();
        }
    }
}