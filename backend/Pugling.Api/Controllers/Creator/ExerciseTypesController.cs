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
public class ExerciseTypesController(ExerciseTypeRegistry registry) : ControllerBase
{
    /// <summary>Manifest aller bekannten Übungstypen.</summary>
    [HttpGet]
    public IReadOnlyList<ExerciseTypeManifest> List() => registry.Manifests;

    /// <summary>Manifest eines einzelnen Übungstyps.</summary>
    [HttpGet("{type}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ExerciseTypeManifest> Get(string type) =>
        registry.ByKey(type)?.Manifest is { } manifest ? manifest : NotFound();
}
