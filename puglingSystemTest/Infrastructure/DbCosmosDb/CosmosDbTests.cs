using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using pugling.Infrastructure.Persistance.DbModels;
using pugling.Models;
using pugling.Models.Constants;
using pugling.Services;

namespace puglingSystemTest.Infrastructure.DbCosmosDb
{
    public class CosmosDbTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public CosmosDbTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact(Skip = "This test should only run when manually started or on local maschine.")]
        public async Task SaveCosmosDbAsync()
        {
            var serv = _factory.CreateServiceProvider();
            var vocabularyFactory = serv.GetService<VocabularyFactory>();

            var vocabulary = vocabularyFactory.CreateVocabulary(new VocabularyDto()
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
                },
            });

            var vocabularySave = await vocabulary.SaveAsync(CancellationToken.None);

            vocabularySave.Id.Should().NotBeNullOrEmpty();

            //var vocabularyRead = await vocabularyFactory.GetVocabularyAsync("4d02195a-8f9c-4b12-91ae-8c26dd3481a3");// vocabularySave.Id);

            //vocabularyRead.Should().NotBeNull();

            //vocabularyRead.Id.Should().Be(vocabularySave.Id);
        }

        [Fact(Skip = "This test should only run when manually started or on local maschine.")]
        public async Task GetVocabularyById()
        {
            var serv = _factory.CreateServiceProvider();
            var vocabularyFactory = serv.GetService<IReadableService<IVocabularyEntity>>();
            var vocabularyRead = await vocabularyFactory.GetById("4d02195a-8f9c-4b12-91ae-8c26dd3481a3");
            vocabularyRead.Should().NotBeNull();
            vocabularyRead.Id.Should().Be("4d02195a-8f9c-4b12-91ae-8c26dd3481a3");
        }
    }
}