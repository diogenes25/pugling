using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using pugling;
using pugling.Infrastructure.DbCosmosDb;
using pugling.Services;

namespace puglingSystemTest.Infrastructure.DbCosmosDb
{
    public class CustomWebApplicationFactory<T>
    {
        public IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to the current directory
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true) // Load the JSON file
            .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddDbCosmosDbServices();
            services.AddScoped<ILogger<VocabularySaveServiceCosmosDb>>(services => new Mock<ILogger<VocabularySaveServiceCosmosDb>>().Object);
            services.AddScoped<ILogger<VocabularyReadServiceCosmosDb>>(services => new Mock<ILogger<VocabularyReadServiceCosmosDb>>().Object);
            services.AddScoped<VocabularyFactory>();

            var serv = services.BuildServiceProvider();
            return serv;
        }
    }
}