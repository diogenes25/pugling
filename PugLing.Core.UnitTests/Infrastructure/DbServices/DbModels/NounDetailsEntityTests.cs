using FluentAssertions;
using Moq;
using PugLing.Core.Application.Vocabularies;
using pugling.Infrastructure.Persistance.DbModels.Vocabularies;
using PugLing.Model.Models;
using PugLing.Model.Models.Constants;
using System.ComponentModel.DataAnnotations;

namespace puglingTest.Infrastructure.DbServices.DbModels;

public class NounDetailsEntityTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly_Test()
    {
        // Arrange
        var determinedArticle = "der";
        var genus = EGenus.Masculine;
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
        mockNounDetails.Setup(nd => nd.Genus).Returns(EGenus.Feminine);
        mockNounDetails.Setup(nd => nd.UndeterminedArticle).Returns("eine");

        // Act
        var entity = new NounDetailsEntity(mockNounDetails.Object);

        // Assert
        entity.DeterminedArticle.Should().Be("die");
        entity.Genus.Should().Be(EGenus.Feminine);
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
            Genus = EGenus.Masculine
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
            Genus = EGenus.Neuter,
            UndeterminedArticle = "ein"
        });

        // Act
        var result = entity.FillAndValidate(noun);

        // Assert
        result.Should().Be(entity);
        entity.DeterminedArticle.Should().Be("das");
        entity.Genus.Should().Be(EGenus.Neuter);
        entity.UndeterminedArticle.Should().Be("ein");
    }

    [Fact]
    public void Validate_ShouldReturnValidationErrors_WhenPropertiesAreInvalid_Test()
    {
        // Arrange
        var entity = new NounDetailsEntity
        {
            DeterminedArticle = new string('a', 101), // Exceeds max length
            Genus = EGenus.NotSet // Invalid
        };

        // Act
        var validationResults = entity.Validate(new ValidationContext(entity)).ToList();

        // Assert
        validationResults.Should().HaveCount(1);
        validationResults.Should().Contain(v => v.ErrorMessage.Contains("DeterminedArticle must be a non-empty string with a maximum length of 100."));
        //validationResults.Should().Contain(v => v.ErrorMessage.Contains("Genus must be a non-empty string with a maximum length of 50."));
    }

    [Fact]
    public void Validate_ShouldReturnNoValidationErrors_WhenPropertiesAreValid_Test()
    {
        // Arrange
        var entity = new NounDetailsEntity
        {
            DeterminedArticle = "der",
            Genus = EGenus.Masculine,
            UndeterminedArticle = "ein"
        };

        // Act
        var validationResults = entity.Validate(new ValidationContext(entity)).ToList();

        // Assert
        validationResults.Should().BeEmpty();
    }
}