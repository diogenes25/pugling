using PugLing.Model.Models.Constants;

namespace PugLing.Model.Models
{
    public interface INounDetails
    {
        string? DeterminedArticle { get; }
        EGenus Genus { get; }
        string? UndeterminedArticle { get; }
    }
}