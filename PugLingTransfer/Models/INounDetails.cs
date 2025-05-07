using PugLingTransfer.Models.Constants;

namespace PugLingTransfer.Models
{
    public interface INounDetails
    {
        string? DeterminedArticle { get; }
        EGenus Genus { get; }
        string? UndeterminedArticle { get; }
    }
}