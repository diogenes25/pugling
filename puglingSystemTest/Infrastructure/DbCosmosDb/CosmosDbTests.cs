using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using pugling.Infrastructure.Persistance.DbModels.Vocabularies;
using pugling.Models;
using pugling.Models.Constants;
using pugling.Services;

namespace puglingSystemTest.Infrastructure.DbCosmosDb;

public class CosmosDbTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public CosmosDbTests(CustomWebApplicationFactory<Program> factory) => this._factory = factory;

    [Fact(Skip = "This test should only run when manually started or on local maschine.")]
    public async Task SaveCosmosDbAsync()
    {
        var serv = this._factory.CreateServiceProvider();
        var vocabularyFactory = serv.GetRequiredService<VocabularyFactory>();

        var vocabulary = vocabularyFactory.CreateVocabulary("en", "de", new VocabularyDto()
        {
            Id = Guid.NewGuid().ToString(),
            SourceLanguage = "en",
            TargetLanguage = "de",
            Word = "go",
            Translation = "gehen",
            PartOfSpeech = EPartOfSpeech.Verb,
            Verb = new VerbDetailsDto()
            {
                IsBaseForm = true,
                Conjugations = new Dictionary<string, Dictionary<string, ConjugationDetailsDto>>()
                {
                    {
                        "Praesens", new Dictionary<string, ConjugationDetailsDto>()
                        {
                            { "ich", new ConjugationDetailsDto() { Form = "gehe" } },
                            { "du", new ConjugationDetailsDto() { Form = "gehst" } },
                            { "er/sie/es", new ConjugationDetailsDto() { Form = "geht" } },
                            { "wir", new ConjugationDetailsDto() { Form = "gehen" } },
                            { "ihr", new ConjugationDetailsDto() { Form = "geht" } },
                            { "sie/Sie", new ConjugationDetailsDto() { Form = "gehen" } }
                        }
                    }
                },
                BaseFormRef = new Uri("http://example.com/vocab/go"),
                Infinitiv = "to go",
                Person = "third",
                Tense = "Present"
            },
            ExampleSentenceSrc = "I go every morning.",
            ExampleSentenceTarget = "Ich gehe jeden Morgen.",
            ExampleSentenceTense = "present",
            Description = "To move from one place to another.",
            Pronunciation = "/ɡoʊ/",
            PronunciationAudioUrl = new Uri("https://example.com/audio/go.mp3"),
            RelatedForms =
            [
                new VocabularyBaseDto() {Id = "en_come_de", Word = "come", Translation = "kommen", SourceLanguage = "en", TargetLanguage = "de"}
            ],
            IdiomaticUsages = [
                new IdiomaticUsageDto() { Phrase = "go for a walk", Translation = "spazieren gehen" }
            ]
        });

        vocabulary.HasUnsavedChanges.Should().BeTrue();

        var vocabularySave = await vocabulary.SaveAsync(CancellationToken.None);

        vocabulary.HasUnsavedChanges.Should().BeFalse();

        vocabularySave.Id.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "This test should only run when manually started or on local maschine.")]
    public async Task GetVocabularyById()
    {
        var serv = this._factory.CreateServiceProvider();
        var vocabularyFactory = serv.GetService<IReadableService<IVocabularyEntity>>();
        var vocabularyRead = await vocabularyFactory.GetById("en", "de", "5327ea8b-0ed0-4472-9359-618cf6fb3a85");
        vocabularyRead.Should().NotBeNull();
        vocabularyRead.Id.Should().Be("5327ea8b-0ed0-4472-9359-618cf6fb3a85");
    }
}