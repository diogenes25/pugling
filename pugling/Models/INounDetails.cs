namespace pugling.Models
{
    public interface INounDetails
    {
        string? DeterminedArticle { get; }
        string? Genus { get; }
        string? UndeterminedArticle { get; }
    }
}