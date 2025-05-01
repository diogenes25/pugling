using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using pugling;
using pugling.Services;

namespace puglingTest
{
    public class BuildServicesTests
    {
        [Fact]
        public void ServicesBuildTest()
        {
            var services = new ServiceCollection();

            // Create a mock IConfiguration object for testing
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "CosmosDb:Endpoint", "https://localhost:8081" },
                    { "CosmosDb:Key", "test-key" },
                    { "CosmosDb:DatabaseName", "TestDatabase" }
                })
                .Build();

            services.AddDbCosmosDbServices(configuration);
            services.AddScoped<VocabularyService>();

            var serv = services.BuildServiceProvider();
            serv.GetService<VocabularyService>();
        }
    }
}