using FluentAssertions;
using Moq;
using pugling.Application;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Models;
using System.ComponentModel.DataAnnotations;

namespace puglingTest.Infrastructure.DbServices.DbModels
{
    public class NounDetailsEntityTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesCorrectly_Test()
        {
            // Arrange
            var determinedArticle = "der";
            var genus = "masculine";
            var undeterminedArticle = "ein";

            // Act
            var entity = new NounDetailsEntity(determinedArticle, genus, undeterminedArticle);

            // Assert
            entity.DeterminedArticle.Should().Be(determinedArticle);
            entity.Genus.Should().Be(genus);
            entity.UndeterminedArticle.Should().Be(undeterminedArticle);
        }

        [Fact]
        public void Constructor_WithINounDetails_ShouldInitializePropertiesCorrectly_Test()
        {
            // Arrange
            var mockNounDetails = new Mock<INounDetails>();
            mockNounDetails.Setup(nd => nd.DeterminedArticle).Returns("die");
            mockNounDetails.Setup(nd => nd.Genus).Returns("feminine");
            mockNounDetails.Setup(nd => nd.UndeterminedArticle).Returns("eine");

            // Act
            var entity = new NounDetailsEntity(mockNounDetails.Object);

            // Assert
            entity.DeterminedArticle.Should().Be("die");
            entity.Genus.Should().Be("feminine");
            entity.UndeterminedArticle.Should().Be("eine");
        }

        [Fact]
        public void FillAndValidate_WhenNounIsNull_Test()
        {
            // Arrange
            var entity = new NounDetailsEntity();

            // Act
            var nullNoun = entity.FillAndValidate(null);

            // Assert
            nullNoun.Should().BeNull();
        }

        [Fact]
        public void FillAndValidate_ShouldThrowArgumentException_WhenValidationFails_Test()
        {
            // Arrange
            var entity = new NounDetailsEntity();
            var noun = NounDetails.Create(new NounDetailsDto()
            {
                DeterminedArticle = new string('a', 101), // Exceeds max length
                Genus = "masculine"
            });

            // Act
            Action act = () => entity.FillAndValidate(noun);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("The following constraints were violated: *");
        }

        [Fact]
        public void FillAndValidate_ShouldFillPropertiesCorrectly_WhenValidationPasses_Test()
        {
            // Arrange
            var entity = new NounDetailsEntity();
            var noun = NounDetails.Create(new NounDetailsDto()
            {
                DeterminedArticle = "das",
                Genus = "neuter",
                UndeterminedArticle = "ein"
            });

            // Act
            var result = entity.FillAndValidate(noun);

            // Assert
            result.Should().Be(entity);
            entity.DeterminedArticle.Should().Be("das");
            entity.Genus.Should().Be("neuter");
            entity.UndeterminedArticle.Should().Be("ein");
        }

        [Fact]
        public void Validate_ShouldReturnValidationErrors_WhenPropertiesAreInvalid_Test()
        {
            // Arrange
            var entity = new NounDetailsEntity
            {
                DeterminedArticle = new string('a', 101), // Exceeds max length
                Genus = string.Empty // Invalid
            };

            // Act
            var validationResults = entity.Validate(new ValidationContext(entity)).ToList();

            // Assert
            validationResults.Should().HaveCount(2);
            validationResults.Should().Contain(v => v.ErrorMessage.Contains("DeterminedArticle must be a non-empty string with a maximum length of 100."));
            validationResults.Should().Contain(v => v.ErrorMessage.Contains("Genus must be a non-empty string with a maximum length of 50."));
        }

        [Fact]
        public void Validate_ShouldReturnNoValidationErrors_WhenPropertiesAreValid_Test()
        {
            // Arrange
            var entity = new NounDetailsEntity
            {
                DeterminedArticle = "der",
                Genus = "masculine",
                UndeterminedArticle = "ein"
            };

            // Act
            var validationResults = entity.Validate(new ValidationContext(entity)).ToList();

            // Assert
            validationResults.Should().BeEmpty();
        }
    }
}