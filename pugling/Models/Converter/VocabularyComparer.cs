namespace pugling.Models.Converter
{
    public static class VocabularyComparer
    {

        public static bool Compare(this IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> orig, IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails>? other)
        {
            return other is not null &&
                    orig.Id == other.Id &&
                    orig.Noun.Compare(other.Noun) &&
                    orig.PartOfSpeech == other.PartOfSpeech &&
                    orig.SourceLanguage == other.SourceLanguage &&
                    orig.TargetLanguage == other.TargetLanguage &&
                    orig.Translation == other.Translation &&
                    orig.Verb.Compare(other.Verb) &&
                    orig.Word == other.Word;
        }

        public static bool Compare(this IVocabularyBase orig, IVocabularyBase other)
        {
            return other is not null &&
                    orig.Id == other.Id &&
                    orig.Word == other.Word &&
                    orig.Translation == other.Translation;
        }

        public static bool Compare(this INounDetails orig, INounDetails? other)
        {
            if (orig is null && other is null)
            {
                return true;
            }
            return other is not null &&
            orig.Genus == other.Genus &&
            orig.DeterminedArticle == other.DeterminedArticle &&
            orig.UndeterminedArticle == other.UndeterminedArticle;
        }

        public static bool Compare(this IIdiomaticUsage orig, IIdiomaticUsage? other)
        {
            if (orig is null && other is null)
            {
                return true;
            }
            return other is not null &&
            orig.Phrase == other.Phrase &&
            orig.Translation == other.Translation;
        }

        public static bool Compare(this IVerbDetails orig, IVerbDetails? other)
        {
            if (orig is null && other is null)
            {
                return true;
            }
            return other is not null &&
                  orig.IsBaseForm == other.IsBaseForm &&
                  orig.Person == other.Person &&
                  orig.Infinitiv == other.Infinitiv &&
                  orig.Tense == other.Tense;
        }
    }
}
