
namespace pugling.Models
{
    public interface IVocabulary<out TIdiomaticUsage, out TNounDetails, out TRelatedForm, out TVerbDetails>
    where TIdiomaticUsage : IIdiomaticUsage
    where TNounDetails : INounDetails
    where TRelatedForm : IRelatedForm
    where TVerbDetails : IVerbDetails
    {
        string? Description { get; }
        string? ExampleSentenceSrc { get; }
        string? ExampleSentenceTarget { get; }
        string? ExampleSentenceTense { get; }
        string Id { get; }
        TIdiomaticUsage[]? IdiomaticUsages { get; }
        TNounDetails? Noun { get; }
        string PartOfSpeech { get; }
        string? Pronunciation { get; }
        string? PronunciationAudioUrl { get; }
        TRelatedForm[]? RelatedForms { get; }
        string SourceLanguage { get; }
        string TargetLanguage { get; }
        string Translation { get; }
        DateTime? UpdatedAt { get; }
        TVerbDetails? Verb { get; }
        string Version { get; }
        string Word { get; }
    }
}