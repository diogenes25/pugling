using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Pugling.Api.OpenApi;

/// <summary>
/// Restores the success response that ASP.NET drops as soon as an action declares <b>any</b>
/// <c>[ProducesResponseType]</c>: declaring error codes replaces the inferred set instead of adding to it, so
/// exactly those actions that document their errors carefully lose their <c>200</c>.
/// <para>
/// The consequence is not cosmetic. The checked-in contract document (docs/openapi/v1.json) is diffed as a
/// gate, and a response type that appears nowhere in it cannot move - so renaming a field of a response DTO
/// was invisible to that gate, and whole payload schemas were missing from the document.
/// </para>
/// <para>
/// Only <c>200</c> is added, and only where the return type <b>names a payload</b>. An action answering
/// <c>201</c>/<c>204</c> declares that itself; guessing <c>200</c> for a bare <c>IActionResult</c> would put a
/// wrong statement into the document, which is worse than the gap it fills.
/// </para>
/// </summary>
public sealed class SuccessResponseConvention : IActionModelConvention
{
    /// <summary>Adds the missing <c>200</c> to an action that declares errors but no success.</summary>
    public void Apply(ActionModel action)
    {
        var declared = action.Attributes.OfType<IApiResponseMetadataProvider>().ToList();
        // Nothing declared: the inference works by itself, do not interfere.
        if (declared.Count == 0) return;
        if (declared.Any(p => p.StatusCode is >= 200 and < 300)) return;

        // No named payload, no guess: the return type cannot tell 200 from 201/204, and a wrong success code
        // in the document would be waved through by the diff gate as "the contract".
        if (PayloadType(action.ActionMethod.ReturnType) is not { } payload) return;

        // Filters, not Attributes: the ApiExplorer collects response metadata from the filter descriptors.
        action.Filters.Add(new ProducesResponseTypeAttribute(payload, StatusCodes.Status200OK));
    }

    /// <summary>
    /// The payload behind an action's return type - <c>null</c> if the type does not name one
    /// (<c>IActionResult</c> and friends).
    /// </summary>
    private static Type? PayloadType(Type returnType)
    {
        var type = returnType;
        if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>)
            || type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
            type = type.GetGenericArguments()[0];

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ActionResult<>))
            type = type.GetGenericArguments()[0];

        if (type == typeof(void) || type == typeof(Task) || type == typeof(ValueTask)) return null;
        return typeof(IActionResult).IsAssignableFrom(type) || typeof(IResult).IsAssignableFrom(type)
            ? null
            : type;
    }
}
