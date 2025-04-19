using System.Text.RegularExpressions;

namespace pugling.Models
{
    public static class VocabularyUtil
    {
        /// <summary>
        /// Generiert eine RESTful-freundliche ID für ein Vocabulary-Objekt.
        /// </summary>
        /// <param name="vocabulary">Das Vocabulary-Objekt.</param>
        /// <returns>Die generierte RESTful-freundliche ID.</returns>
        public static string GenerateRestfulId(VocabularyDto vocabulary)
        {
            if (vocabulary == null)
            {
                throw new ArgumentNullException(nameof(vocabulary));
            }

            string sourceLanguage = vocabulary.SourceLanguage.ToLowerInvariant();
            string targetLanguage = vocabulary.TargetLanguage.ToLowerInvariant();
            string baseWord = NormalizeForUrl(vocabulary.Word);

            if (vocabulary.PartOfSpeech?.ToLowerInvariant() == "verb" && vocabulary.Verb?.IsBaseForm == false && !string.IsNullOrEmpty(vocabulary.Verb?.Infinitiv) && !string.IsNullOrEmpty(vocabulary.Verb?.Person) && !string.IsNullOrEmpty(vocabulary.Verb?.Tense))
            {
                string tense = GetUrlFriendlyTense(vocabulary.Verb.Tense);
                string person = GetUrlFriendlyPerson(vocabulary.Verb.Person);

                if (!string.IsNullOrEmpty(tense) && !string.IsNullOrEmpty(person))
                {
                    return $"{sourceLanguage}-{NormalizeForUrl(vocabulary.Verb.Infinitiv)}-{targetLanguage}-{tense}-{person}";
                }
                else
                {
                    // Fallback, falls Tempus oder Person nicht URL-freundlich abgebildet werden können
                    return $"{sourceLanguage}-{NormalizeForUrl(vocabulary.Verb.Infinitiv)}-{targetLanguage}-{vocabulary.Verb.Tense?.ToLowerInvariant()}-{vocabulary.Verb.Person?.ToLowerInvariant()?.Replace("/", "-")}";
                }
            }
            else
            {
                return $"{sourceLanguage}-{baseWord}-{targetLanguage}";
            }
        }

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

        private static string ReplaceUmlauts(string text)
        {
            return text.Replace("ä", "ae")
                       .Replace("ö", "oe")
                       .Replace("ü", "ue")
                       .Replace("ß", "ss");
        }

        private static string RemoveNonAlphaNumericExceptDash(string text)
        {
            return Regex.Replace(text, @"[^a-z0-9-]", "");
        }

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
                _ => null, // Oder eine Standarddarstellung, wenn kein Match
            };
        }

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
                _ => person.ToLowerInvariant().Replace("/", "-"), // Fallback, falls keine direkte Entsprechung
            };
        }
    }
}