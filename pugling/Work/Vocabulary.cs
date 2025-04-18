using pugling.Models;

namespace pugling.Work
{
    public class Vocabulary : IVocabulary<IdiomaticUsage, NounDetails, RelatedForm, VerbDetails>
    {
        public string? Description { get; private set; }

        public string? ExampleSentenceSrc { get; private set; }

        public string? ExampleSentenceTarget { get; private set; }

        public string? ExampleSentenceTense { get; private set; }

        public string Id { get; private set; }

        public IdiomaticUsage[]? IdiomaticUsages { get; private set; }

        public NounDetails? Noun { get; private set; }

        public string PartOfSpeech { get; private set; }

        public string? Pronunciation { get; private set; }

        public string? PronunciationAudioUrl { get; private set; }

        public RelatedForm[]? RelatedForms { get; private set; }

        public string SourceLanguage { get; private set; }

        public string TargetLanguage { get; private set; }

        public string Translation { get; private set; }

        public DateTime? UpdatedAt { get; private set; }

        public VerbDetails? Verb { get; private set; }

        public string Version { get; private set; }

        public string Word { get; private set; }
    }
}
