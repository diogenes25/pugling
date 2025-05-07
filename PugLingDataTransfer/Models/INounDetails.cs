using PugLingDataTransfer.Models.Constants;

namespace PugLingDataTransfer.Models
{
    public interface INounDetails
    {
        string? DeterminedArticle { get; }
        EGenus Genus { get; }
        string? UndeterminedArticle { get; }
    }
}