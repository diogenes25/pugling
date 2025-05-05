namespace pugling.Models;

public interface IVerbDetails
{
    Uri? BaseFormRef { get; }
    Dictionary<string, Dictionary<string, IConjugationDetails>>? Conjugations { get; }
    string? Infinitiv { get; }
    bool IsBaseForm { get; }
    string? Person { get; }
    string? Tense { get; }
}