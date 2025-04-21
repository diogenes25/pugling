using pugling.Models.Constants;

namespace pugling.Models
{
    public interface INounDetails
    {
        string? DeterminedArticle { get; }
        EGenus Genus { get; }
        string? UndeterminedArticle { get; }
    }
}