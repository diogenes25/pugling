using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using pugling;
using pugling.Infrastructure.DbCosmosDb;
using pugling.Models;
using pugling.Models.Constants;
using pugling.Services;

namespace puglingSystemTest.Infrastructure.DbCosmosDb
{
    public class CosmosDbTests
    {
#if DEBUG
        [Fact]
#elif RELEASE
        [Fact(Skip = "This test should only run when manually started or on local maschine.")]
#endif
        public async Task SaveCosmosDbAsync()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to the current directory  
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true) // Load the JSON file  
            .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddDbCosmosDbServices();
            services.AddSingleton<CosmosDbSettings>();
            services.AddScoped<ILogger<VocabularySaveServiceCosmosDb>>(services => new Mock<ILogger<VocabularySaveServiceCosmosDb>>().Object);
            services.AddScoped<ILogger<VocabularyReadServiceCosmosDb>>(services => new Mock<ILogger<VocabularyReadServiceCosmosDb>>().Object);
            services.AddScoped<VocabularyFactory>();

            var serv = services.BuildServiceProvider();
            var vocabularyFactory = serv.GetService<VocabularyFactory>();

            var vocabulary = vocabularyFactory.CreateVocabulary(new VocabularyDto()
            {
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

            var result = await vocabulary.SaveAsync(CancellationToken.None);

            result.Id.Should().NotBeNullOrEmpty();
        }
    }
}