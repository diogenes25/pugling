using pugling.Models;
using Swashbuckle.AspNetCore.Filters;

namespace pugling.Controllers.ModelExamples
{
    // Example class for Swagger
    public class VocabularyDtoExample : IMultipleExamplesProvider<VocabularyDto>
    {
        IEnumerable<SwaggerExample<VocabularyDto>> IMultipleExamplesProvider<VocabularyDto>.GetExamples()
        {
            return
       [
           new SwaggerExample<VocabularyDto>
           {
               Name = "201 Created",
               Summary = "Example response for a successful creation.",
               Value = new VocabularyDto
               {
                   Id = "en_run_de",
                   SourceLanguage = "en",
                   TargetLanguage = "de",
                   PartOfSpeech = "Verb",
                   Verb = new VerbDetailsDto
                   {
                       IsBaseForm = true,
                       Conjugations = new Dictionary<string, Dictionary<string, IConjugationDetails>>
                       {
                           {
                               "Präsens", new Dictionary<string, IConjugationDetails>
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