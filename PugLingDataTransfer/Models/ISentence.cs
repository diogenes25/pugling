namespace PugLingDataTransfer.Models;

public interface ISentence
{
    SentenceComponentDto[]? Components { get; }
    string? SentenceAudio { get; }
    string SourceSentence { get; }
    string? Tense { get; }
    string Translation { get; }
}