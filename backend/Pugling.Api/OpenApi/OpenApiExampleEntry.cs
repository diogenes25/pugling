namespace Pugling.Api.OpenApi;

/// <summary>Verified request/response example for the OpenAPI documentation.</summary>
public sealed record OpenApiExampleEntry(
    string Key,
    string ResourceGroup,
    string Title,
    string Method,
    string Path,
    string Role,
    string? RequestBodyJson,
    int ExpectedStatus,
    string? ResponseBodyJson,
    bool IsError,
    string? ExpectedCode);
