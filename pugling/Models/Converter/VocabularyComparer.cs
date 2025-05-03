namespace pugling.Models.Converter
{
    /// <summary>
    /// Provides extension methods for comparing various vocabulary-related objects.
    /// </summary>
    public static class VocabularyComparer
    {
        /// <summary>
        /// Compares two <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TVocabularyBase, TVerbDetails}"/> objects for equality.
        /// </summary>
        /// <param name="orig">The original vocabulary object.</param>
        /// <param name="other">The vocabulary object to compare against.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Compares two <see cref="IVocabularyBase"/> objects for equality.
        /// </summary>
        /// <param name="orig">The original vocabulary base object.</param>
        /// <param name="other">The vocabulary base object to compare against.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
        public static bool Compare(this IVocabularyBase orig, IVocabularyBase other)
        {
            return other is not null &&
                    orig.Id == other.Id &&
                    orig.Word == other.Word &&
                    orig.Translation == other.Translation;
        }

        /// <summary>
        /// Compares two <see cref="INounDetails"/> objects for equality.
        /// </summary>
        /// <param name="orig">The original noun details object.</param>
        /// <param name="other">The noun details object to compare against.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Compares two <see cref="IIdiomaticUsage"/> objects for equality.
        /// </summary>
        /// <param name="orig">The original idiomatic usage object.</param>
        /// <param name="other">The idiomatic usage object to compare against.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Compares two <see cref="IVerbDetails"/> objects for equality.
        /// </summary>
        /// <param name="orig">The original verb details object.</param>
        /// <param name="other">The verb details object to compare against.</param>
        /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
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