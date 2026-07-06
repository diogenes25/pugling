using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Pugling.Api.Errors;

/// <summary>
/// Ersetzt die Standard-<see cref="ProblemDetailsFactory"/> von MVC und stempelt in JEDES darüber
/// erzeugte ProblemDetails einen maschinenlesbaren <c>code</c> (und den passenden <c>type</c>-URI) –
/// aber nur, wenn noch keiner gesetzt ist, damit spezifische Codes (via <c>ProblemWithCode</c>) gewinnen.
/// Deckt damit auch die vom [ApiController] automatisch in ProblemDetails gewandelten Status-Ergebnisse
/// ab (z. B. <c>NotFound()</c>/<c>Conflict()</c>), die NICHT durch <c>CustomizeProblemDetails</c> laufen.
/// Reproduziert die Defaults der internen <c>DefaultProblemDetailsFactory</c> (Titel-Fallback aus
/// <see cref="ApiBehaviorOptions.ClientErrorMapping"/> und die <c>traceId</c>-Extension).
/// <para>
/// <b>Bewusste Abweichung:</b> der <c>type</c> wird für codelose Fehler auf den pugling-Fehler-URI
/// normalisiert (nicht die RFC-Dummy-Links), damit alle Fehler denselben dereferenzierbaren
/// <c>type</c>-Raum teilen. Diese Factory ist daher <b>kein</b> 1:1-Drop-in für die Default-Factory.
/// </para>
/// </summary>
public sealed class CodeStampingProblemDetailsFactory(IOptions<ApiBehaviorOptions> options) : ProblemDetailsFactory
{
    private readonly ApiBehaviorOptions _options = options.Value;

    public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null,
        string? title = null, string? type = null, string? detail = null, string? instance = null)
    {
        statusCode ??= 500;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance,
        };
        ApplyDefaults(httpContext, problem, statusCode.Value);
        return problem;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
        ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null,
        string? type = null, string? detail = null, string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(modelStateDictionary);
        statusCode ??= 400;
        var problem = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode,
            Type = type,
            Detail = detail,
            Instance = instance,
        };
        if (title is not null) problem.Title = title;
        // Validierungsfehler sind ein SPEZIFISCHER Code (nicht der generische bad_request-Default), damit
        // ein direkter ValidationProblem()-Aufruf denselben Code liefert wie der Model-Binding-Pfad.
        ProblemDetailsStamping.StampSpecific(problem, ApiErrors.ValidationError);
        ApplyDefaults(httpContext, problem, statusCode.Value);
        return problem;
    }

    private void ApplyDefaults(HttpContext httpContext, ProblemDetails problem, int statusCode)
    {
        problem.Status ??= statusCode;
        if (_options.ClientErrorMapping.TryGetValue(statusCode, out var mapping))
            problem.Title ??= mapping.Title;

        ProblemDetailsStamping.ApplyTraceId(problem, httpContext);
        ProblemDetailsStamping.StampFallback(problem, statusCode);
    }
}
