namespace Pugling.Api.OpenApi;

/// <summary>Verifiziertes Request/Response-Beispiel für die OpenAPI-Dokumentation.</summary>
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