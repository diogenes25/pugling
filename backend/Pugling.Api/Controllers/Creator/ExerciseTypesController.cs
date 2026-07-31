using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Self-description of exercise types: the single source from which a client reads routing, check mode,
/// renderer, and capabilities per type, instead of hard-wiring them. Child-neutral catalog –
/// therefore readable by both tiers (the student client also needs the play route and renderer).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercise-types")]
[Tags("Creator – Exercise Types")]
[Produces("application/json")]
[Authorize]
public class ExerciseTypesController(ExerciseTypeRegistry registry) : ControllerBase
{
    /// <summary>Manifest of all known exercise types.</summary>
    [HttpGet]
    public IReadOnlyList<ExerciseTypeManifest> List() => registry.Manifests;

    /// <summary>Manifest of a single exercise type.</summary>
    [HttpGet("{type}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ExerciseTypeManifest> Get(string type) =>
        registry.ByKey(type)?.Manifest is { } manifest ? manifest : NotFound();
}
