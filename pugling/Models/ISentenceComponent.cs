namespace pugling.Models;

public interface ISentenceComponent
{
    string? Case { get; }
    string? SentencePart { get; }
    string Text { get; }
    string? VocabularyId { get; }
}