namespace Pugling.Api.Errors;

/// <summary>
/// A rejected request together with its machine code. Validators return this instead of a bare text because the caller
/// cannot tell from a text whether the payload was malformed (<c>validation_error</c>) or named something that does not
/// exist (<c>invalid_reference</c>) - and clients branch on exactly that code (B-258, B-329).
/// </summary>
/// <param name="Error">The registered error (code and status).</param>
/// <param name="Detail">The English, human-readable detail.</param>
public sealed record RequestProblem(ApiError Error, string Detail)
{
    /// <summary>The payload is malformed or breaks a rule.</summary>
    public static RequestProblem Invalid(string detail) => new(ApiErrors.ValidationError, detail);

    /// <summary>The payload names an id or key that does not exist.</summary>
    public static RequestProblem UnknownReference(string detail) => new(ApiErrors.InvalidReference, detail);
}
