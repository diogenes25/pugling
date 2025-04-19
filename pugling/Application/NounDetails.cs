using pugling.Models;

namespace pugling.Application
{
    public sealed class NounDetails : INounDetails, IEquatable<INounDetails?>
    {
        public string? DeterminedArticle { get; private set; }
        public string? Genus { get; private set; }
        public string? UndeterminedArticle { get; private set; }

        public static NounDetails Create(string? determinedArticle, string? genus, string? undeterminedArticle)
        {
            return new NounDetails
            {
                DeterminedArticle = determinedArticle,
                Genus = genus,
                UndeterminedArticle = undeterminedArticle
            };
        }

        public static NounDetails Create(INounDetails nounDetails)
        {
            return new NounDetails
            {
                DeterminedArticle = nounDetails.DeterminedArticle,
                Genus = nounDetails.Genus,
                UndeterminedArticle = nounDetails.UndeterminedArticle
            };
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as INounDetails);
        }

        public bool Equals(INounDetails? other)
        {
            return other is not null &&
                   this.DeterminedArticle == other.DeterminedArticle &&
                   this.Genus == other.Genus &&
                   this.UndeterminedArticle == other.UndeterminedArticle;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.DeterminedArticle, this.Genus, this.UndeterminedArticle);
        }

        public static bool operator ==(NounDetails? left, INounDetails? right)
        {
            return EqualityComparer<INounDetails>.Default.Equals(left, right);
        }

        public static bool operator !=(NounDetails? left, INounDetails? right)
        {
            return !(left == right);
        }
    }
}