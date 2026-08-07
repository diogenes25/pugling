using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Data;
using Pugling.Api.Errors;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// The shared "validate, derive slug, look up existing" half of a slug-idempotent create – shared by
/// <see cref="InterestTagsController"/>, <see cref="PublishersController"/>, and
/// <see cref="TextbookSeriesController"/> so a fix to the idempotency contract (empty-slug handling, the
/// validation messages) lands in one place instead of three near-identical copies.
/// </summary>
internal static class SlugCatalogHelpers
{
    /// <summary>
    /// Requires <paramref name="requiredText"/>, then derives the slug from <paramref name="slugSource"/>
    /// (or <paramref name="requiredText"/> itself if no separate source is given, e.g. an explicit slug
    /// override). <c>Slug</c> is <c>null</c> exactly when <c>Problem</c> is set.
    /// </summary>
    public static (string? Slug, ObjectResult? Problem) DeriveRequiredSlug(
        this ControllerBase controller, string? requiredText, string fieldName, string? slugSource = null)
    {
        if (string.IsNullOrWhiteSpace(requiredText))
            return (null, controller.ProblemWithCode(ApiErrors.ValidationError, $"{fieldName} is required."));

        var slug = InterestSlug.From(string.IsNullOrWhiteSpace(slugSource) ? requiredText : slugSource);
        return slug.Length == 0
            ? (null, controller.ProblemWithCode(ApiErrors.ValidationError, $"{fieldName} must contain at least one letter or digit."))
            : (slug, null);
    }
}
