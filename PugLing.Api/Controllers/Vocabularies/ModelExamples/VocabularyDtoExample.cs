using PugLing.Model.Models;
using PugLing.Model.Models.Constants;
using Swashbuckle.AspNetCore.Filters;

namespace PugLing.Api.Controllers.Vocabularies.ModelExamples
{
    /// <summary>
    /// Provides multiple examples of <see cref="VocabularyDto"/> for Swagger documentation.
    /// </summary>
    public class VocabularyDtoExample : IMultipleExamplesProvider<VocabularyDto>
    {
        /// <summary>
        /// Gets a collection of examples for <see cref="VocabularyDto"/>.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="SwaggerExample{T}"/> objects containing example data for <see cref="VocabularyDto"/>.
        /// </returns>
        public IEnumerable<SwaggerExample<VocabularyDto>> GetExamples()
        {
            return
            [
                new SwaggerExample<VocabularyDto>
                    {
                        Name = "201 Created Verb",
                        Summary = "Example request/response for a successful creation of a verb.",
                        Value = new VocabularyDto
                        {
                            Id = "en_run_de",
                            SourceLanguage = "en",
                            TargetLanguage = "de",
                            Word = "run",
                            Translation = "rennen",
                            PartOfSpeech = EPartOfSpeech.Verb,
                            Verb = new VerbDetailsDto
                            {
                                IsBaseForm = true,
                                Conjugations = new Dictionary<string, Dictionary<string, ConjugationDetailsDto>>
                                {
                                    {
                                        "Praesens", new Dictionary<string, ConjugationDetailsDto>
                                        {
                                            { "ich", new ConjugationDetailsDto { Form = "renne", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praesens_ich.json" } },
                                            {"du", new ConjugationDetailsDto { Form = "rennst", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praesens_du.json" } },
                                            {"er/sie/es", new ConjugationDetailsDto { Form = "rennt", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praesens_er_sie_es.json" } },
                                            {"wir", new ConjugationDetailsDto { Form = "rennen", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praesens_wir.json" } },
                                            {"ihr", new ConjugationDetailsDto { Form = "rennt", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praesens_ihr.json" } },
                                            {"sie/Sie", new ConjugationDetailsDto { Form = "rennen", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praesens_sie_Sie.json" }}
                                        }
                                    },
                                    {
                                        "Präteritum", new Dictionary<string, ConjugationDetailsDto>
                                        {
                                            { "ich", new ConjugationDetailsDto { Form = "rannte", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praeteritum_ich.json" } },
                                            {"du", new ConjugationDetailsDto { Form = "ranntest", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praeteritum_du.json" } },
                                            {"er/sie/es", new ConjugationDetailsDto { Form = "rannte", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praeteritum_er_sie_es.json" } },
                                            {"wir", new ConjugationDetailsDto { Form = "rannten", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praeteritum_wir.json" } },
                                            {"ihr", new ConjugationDetailsDto { Form = "ranntet", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praeteritum_ihr.json" } },
                                            {"sie/Sie", new ConjugationDetailsDto { Form = "rannten", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praeteritum_sie_Sie.json" }}
                                        }
                                    }
                                }
                            },
                            ExampleSentenceSrc = "I run every morning.",
                            ExampleSentenceTarget = "Ich renne jeden Morgen.",
                            ExampleSentenceTense = "present",
                            Description = "To move quickly on foot.",
                            Pronunciation = "/rʌn/",
                            PronunciationAudioUrl = new Uri("https://example.com/audio/run.mp3"),
                            RelatedForms =
                            [
                                new VocabularyBaseDto { Id = "en_sprint_de", Word = "sprint", Translation = "sprinten", SourceLanguage="en", TargetLanguage="de" }
                            ],
                            IdiomaticUsages =
                            [
                                new IdiomaticUsageDto { Phrase = "run out of time", Translation = "Die Zeit läuft ab." }
                            ]
                        }
                    },
                    new SwaggerExample<VocabularyDto>
                    {
                        Name = "201 Created Noun",
                         Summary = "Example response for a successful creation.",
                        Value = new VocabularyDto
                        {
                            Id = "en_house_de",
                            SourceLanguage = "en",
                            TargetLanguage = "de",
                            Word = "run",
                            Translation = "Haus",
                            PartOfSpeech = EPartOfSpeech.Noun,
                            Noun = new NounDetailsDto
                            {
                                DeterminedArticle = "das",
                                Genus = EGenus.Neuter,
                                UndeterminedArticle = "ein",
                            },
                            ExampleSentenceSrc = "I live in a house.",
                            ExampleSentenceTarget = "Ich wohne in einem Haus.",
                            ExampleSentenceTense = "present",
                            Description = "A building for human habitation.",
                            Pronunciation = "/haʊs/",
                            PronunciationAudioUrl = new Uri("https://example.com/audio/house.mp3"),
                            RelatedForms =
                            [
                                new VocabularyBaseDto {Id = "en_building_de", Word = "building", Translation = "Gebäude", SourceLanguage = "en", TargetLanguage = "de"},
                                new VocabularyBaseDto {Id = "en_home_de", Word = "home", Translation = "Zuhause", SourceLanguage = "en", TargetLanguage = "de"},
                                new VocabularyBaseDto {Id = "en_residence_de", Word = "residence", Translation = "Wohnsitz", SourceLanguage = "en", TargetLanguage = "de"},
                                new VocabularyBaseDto {Id = "en_abode_de", Word = "abode", Translation = "Wohnung", SourceLanguage = "en", TargetLanguage = "de"}
                            ],
                            IdiomaticUsages =
                            [
                                new IdiomaticUsageDto { Phrase = "house of cards", Translation = "Haus aus Karten" },
                                new IdiomaticUsageDto { Phrase = "housewarming party", Translation = "Einweihungsparty" },
                                new IdiomaticUsageDto { Phrase = "house arrest", Translation = "Hausarrest" }
                            ]
                        }
                    },
                    new SwaggerExample<VocabularyDto>
                    {
                        Name = "500 Internal Server Error",
                        Summary = "Example response for a server error.",
                        Value = null
                    }
            ];
        }
    }
}