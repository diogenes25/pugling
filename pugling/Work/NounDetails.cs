using pugling.Models;

namespace pugling.Work
{
    public class NounDetails : INounDetails
    {
        public string? DeterminedArticle { get; private set;}
        public string? Genus { get; private set; }
        public string? UndeterminedArticle { get; private set; }
    }
}