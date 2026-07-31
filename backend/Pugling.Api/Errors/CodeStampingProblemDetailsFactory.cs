using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Pugling.Api.Errors;

/// <summary>
/// Replaces MVC's default <see cref="ProblemDetailsFactory"/> and stamps a machine-readable
/// <c>code</c> (and the matching <c>type</c> URI) onto EVERY ProblemDetails created through it –
/// but only if none is set yet, so that specific codes (via <c>ProblemWithCode</c>) win.
/// This also covers the status results that [ApiController] automatically converts to ProblemDetails
/// (e.g. <c>NotFound()</c>/<c>Conflict()</c>), which do NOT run through <c>CustomizeProblemDetails</c>.
/// Reproduces the defaults of the internal <c>DefaultProblemDetailsFactory</c> (title fallback from
/// <see cref="ApiBehaviorOptions.ClientErrorMapping"/> and the <c>traceId</c> extension).
/// <para>
/// <b>Deliberate deviation:</b> for codeless errors, the <c>type</c> is normalized to the pugling error URI
/// (not the RFC dummy links), so that all errors share the same dereferenceable
/// <c>type</c> space. This factory is therefore <b>not</b> a 1:1 drop-in for the default factory.
/// </para>
/// </summary>
public sealed class CodeStampingProblemDetailsFactory(IOptions<ApiBehaviorOptions> options) : ProblemDetailsFactory
{
    private readonly ApiBehaviorOptions _options = options.Value;

    /// <summary>Creates a <see cref="ProblemDetails"/> and stamps title, trace id, and a status-based default <c>code</c>, unless one is already set.</summary>
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

    /// <summary>Creates a <see cref="ValidationProblemDetails"/> from the model state and stamps the fixed code <see cref="ApiErrors.ValidationError"/> as well as title/trace id.</summary>
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
