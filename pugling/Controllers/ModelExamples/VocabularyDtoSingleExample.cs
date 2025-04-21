using pugling.Models;
using pugling.Models.Constants;
using Swashbuckle.AspNetCore.Filters;

namespace pugling.Controllers.ModelExamples
{
    public class VocabularyDtoSingleExample : IExamplesProvider<VocabularyDto>
    {
        public VocabularyDto GetExamples()
        {
            return new VocabularyDto
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
                            "Präsens", new Dictionary<string, ConjugationDetailsDto>
                            {
                                { "ich", new ConjugationDetailsDto { Form = "renne", VocObjRef = "/api/en/de/vocabularies/en_run_de_Praesens_ich.json" } }
                            }
                        }
                    }
                },
                ExampleSentenceSrc = "I run every morning.",
                ExampleSentenceTarget = "Ich renne jeden Morgen.",
                ExampleSentenceTense = "present",
                Description = "To move quickly on foot.",
                Pronunciation = "/rʌn/",
                PronunciationAudioUrl = "https://example.com/audio/run.mp3",
                RelatedForms =
                [
                    new VocabularyBaseDto { Id = "en_sprint_de", Word = "sprint", Translation = "sprinten" }
                ],
                IdiomaticUsages =
                [
                    new IdiomaticUsageDto { Phrase = "run out of time", Translation = "Die Zeit läuft ab." }
                ]
            };
        }

    }
}
