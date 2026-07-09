using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Creator;

/// <summary>
/// Selbstbeschreibung der Übungstypen: die eine Quelle, aus der ein Client Routing, Prüfmodus,
/// Renderer und Fähigkeiten je Typ liest, statt sie fest zu verdrahten. Kindneutraler Katalog –
/// daher für beide Rollen lesbar (auch der Sohn-Client braucht Play-Route und Renderer).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route(ApiRoutes.Creator + "/exercise-types")]
[Tags("Creator – Exercise Types")]
[Produces("application/json")]
[Authorize]
public class ExerciseTypesController : ControllerBase
{
    /// <summary>Manifest aller bekannten Übungstypen.</summary>
    [HttpGet]
    public IReadOnlyList<ExerciseTypeManifest> List() => ExerciseManifests.All;

    /// <summary>Manifest eines einzelnen Übungstyps.</summary>
    [HttpGet("{type}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ExerciseTypeManifest> Get(ExerciseType type) =>
        ExerciseManifests.ByType(type) is { } manifest ? manifest : NotFound();
}
