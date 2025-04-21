using pugling.Models;
using pugling.Models.Converter;

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

        public static NounDetails? Create(INounDetails? nounDetails)
        {
            if (nounDetails == null)
            {
                return null;
            }

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
            return this.Compare(other);
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