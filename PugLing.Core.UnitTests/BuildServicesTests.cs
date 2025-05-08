using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using pugling.Infrastructure.Persistance.DbCosmosDb;
using pugling.Services;
using PugLing.Api.Configuration;

namespace puglingTest
{
    public class BuildServicesTests
    {
        [Fact]
        public void ServicesBuildTest()
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
            //services.AddDbFileServices();
            //services.AddScoped<ILogger<VocabularySaveServiceFile>>(services => new Mock<ILogger<VocabularySaveServiceFile>>().Object);
            services.AddScoped<ILogger<VocabularyService>>(services => new Mock<ILogger<VocabularyService>>().Object);
            services.AddScoped<VocabularyService>();
            services.AddScoped<VocabularyFactory>();

            var serv = services.BuildServiceProvider();
            serv.GetService<VocabularyService>();
        }
    }
}