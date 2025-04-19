namespace pugling.Models
{
    public interface IVocabulary<out TIdiomaticUsage, out TNounDetails, out TVocabularyBase, out TVerbDetails> : IVocabularyBase
    where TIdiomaticUsage : IIdiomaticUsage
    where TNounDetails : INounDetails
    where TVocabularyBase : IVocabularyBase
    where TVerbDetails : IVerbDetails
    {
        string? Description { get; }
        string? ExampleSentenceSrc { get; }
        string? ExampleSentenceTarget { get; }
        string? ExampleSentenceTense { get; }
        TIdiomaticUsage[]? IdiomaticUsages { get; }
        TNounDetails? Noun { get; }
        string PartOfSpeech { get; }
        string? Pronunciation { get; }
        string? PronunciationAudioUrl { get; }
        TVocabularyBase[]? RelatedForms { get; }
        string SourceLanguage { get; }
        string TargetLanguage { get; }
        DateTime? UpdatedAt { get; }
        TVerbDetails? Verb { get; }
        string Version { get; }
    }
}