using pugling.Models;
using pugling.Models.Constants;
using System.Text.RegularExpressions;

namespace pugling.Application
{

    /// <summary>
    /// Utility class for handling vocabulary-related operations.
    /// </summary>
    public static class VocabularyUtil
    {
        /// <summary>
        /// Generates a RESTful-friendly ID for a vocabulary object.
        /// </summary>
        /// <param name="vocabulary">The vocabulary object.</param>
        /// <returns>The generated RESTful-friendly ID.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the vocabulary object is null.</exception>
        public static string GenerateRestfulId(IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary)
        {
            ArgumentNullException.ThrowIfNull(vocabulary);

            string sourceLanguage = vocabulary.SourceLanguage.ToLowerInvariant();
            string targetLanguage = vocabulary.TargetLanguage.ToLowerInvariant();
            string baseWord = NormalizeForUrl(vocabulary.Word);

            if (vocabulary.PartOfSpeech == EPartOfSpeech.Verb && vocabulary.Verb?.IsBaseForm == false && !string.IsNullOrEmpty(vocabulary.Verb?.Infinitiv) && !string.IsNullOrEmpty(vocabulary.Verb?.Person) && !string.IsNullOrEmpty(vocabulary.Verb?.Tense))
            {
                string tense = GetUrlFriendlyTense(vocabulary.Verb.Tense);
                string person = GetUrlFriendlyPerson(vocabulary.Verb.Person);

                if (!string.IsNullOrEmpty(tense) && !string.IsNullOrEmpty(person))
                {
                    return $"{sourceLanguage}-{NormalizeForUrl(vocabulary.Verb.Infinitiv)}-{targetLanguage}-{tense}-{person}";
                }
                else
                {
                    // Fallback if tense or person cannot be represented in a URL-friendly way
                    return $"{sourceLanguage}-{NormalizeForUrl(vocabulary.Verb.Infinitiv)}-{targetLanguage}-{vocabulary.Verb.Tense?.ToLowerInvariant()}-{vocabulary.Verb.Person?.ToLowerInvariant()?.Replace("/", "-")}";
                }
            }
            else
            {
                return $"{sourceLanguage}-{baseWord}-{targetLanguage}";
            }
        }

        /// <summary>
        /// Normalizes a string to make it URL-friendly.
        /// </summary>
        /// <param name="text">The text to normalize.</param>
        /// <returns>The normalized text.</returns>
        private static string NormalizeForUrl(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            text = text.ToLowerInvariant();
            text = ReplaceUmlauts(text);
            text = text.Replace(" ", "-");
            text = RemoveNonAlphaNumericExceptDash(text);
            return text;
        }

        /// <summary>
        /// Replaces German umlauts and special characters with their URL-friendly equivalents.
        /// </summary>
        /// <param name="text">The text containing umlauts.</param>
        /// <returns>The text with umlauts replaced.</returns>
        private static string ReplaceUmlauts(string text)
        {
            return text.Replace("ä", "ae")
                       .Replace("ö", "oe")
                       .Replace("ü", "ue")
                       .Replace("ß", "ss");
        }

        /// <summary>
        /// Removes all non-alphanumeric characters except dashes from a string.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <returns>The processed text.</returns>
        private static string RemoveNonAlphaNumericExceptDash(string text)
        {
            return Regex.Replace(text, @"[^a-z0-9-]", "");
        }

        /// <summary>
        /// Converts a tense string into a URL-friendly format.
        /// </summary>
        /// <param name="tense">The tense string.</param>
        /// <returns>The URL-friendly tense string, or null if no match is found.</returns>
        private static string GetUrlFriendlyTense(string tense)
        {
            if (string.IsNullOrEmpty(tense))
            {
                return null;
            }

            return tense.ToLowerInvariant() switch
            {
                "präsens" => "pres",
                "präteritum" => "past",
                "perfekt" => "perf",
                "plusquamperfekt" => "plup",
                "futur i" => "fut1",
                "futur ii" => "fut2",
                _ => null, // Default representation if no match is found
            };
        }

        /// <summary>
        /// Converts a person string into a URL-friendly format.
        /// </summary>
        /// <param name="person">The person string.</param>
        /// <returns>The URL-friendly person string, or the original string with slashes replaced if no match is found.</returns>
        private static string GetUrlFriendlyPerson(string person)
        {
            if (string.IsNullOrEmpty(person))
            {
                return null;
            }

            return person.ToLowerInvariant() switch
            {
                "ich" => "ich",
                "du" => "du",
                "er/sie/es" => "ers",
                "wir" => "wir",
                "ihr" => "ihr",
                "sie" => "sie",
                _ => person.ToLowerInvariant().Replace("/", "-"), // Fallback if no direct match
            };
        }
    }
}