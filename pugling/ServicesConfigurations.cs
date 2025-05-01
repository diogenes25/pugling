using Microsoft.Azure.Cosmos;
using pugling.Application;
using pugling.Infrastructure.DbCosmosDb;
using pugling.Infrastructure.DbFile;
using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Infrastructure.Persistance;
using pugling.Infrastructure.Persistance.DbModels;
using pugling.Services;
using Swashbuckle.AspNetCore.Filters;

namespace pugling
{
    public static class ServicesConfigurations
    {
        public static IServiceCollection AddDbFileServices(this IServiceCollection services)
        {
            services.AddScoped<IInputOutputConverter<VocabularyEntity>, VocabularyFileDbService>();
            //services.AddScoped<VocabularyPersit<VocabularyEntity>>();
            //services.AddScoped<VocabularyService<VocabularyEntity>>();
            services.AddScoped<VocabularyFactory>();
            services.AddScoped<ISaveableService<Vocabulary>, VocabularySaveServiceFile>();
            services.AddScoped<IReadableService<IVocabularyEntity>, VocabularySaveServiceFile>();

            return services;
        }

        public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
        {
            // Include XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // Register the example provider
            services.AddSwaggerExamplesFromAssemblyOf<pugling.Controllers.ModelExamples.VocabularyDtoExample>();
            services.AddSwaggerExamplesFromAssemblyOf<pugling.Controllers.ModelExamples.VocabularyDtoSingleExample>();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Vocabulary API",
                    Version = "v1",
                    Description = "API for managing vocabulary items.",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Email = "huhu@huhu.com",
                        Name = "Huhu",
                        Url = new Uri("https://example.com")
                    }
                });

                options.IncludeXmlComments(xmlPath);
                options.ExampleFilters();
            });

            return services;
        }

        public static IServiceCollection AddDbCosmosDbServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<pugling.Infrastructure.DbCosmosDb.CosmosDbSettings>(configuration.GetSection("ConnectionStrings:CosmosDb"));

            // Register CosmosDB services
            services.AddSingleton<CosmosClient>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var cosmosConnectionString = configuration.GetConnectionString("CosmosDb");
                return new CosmosClient(cosmosConnectionString);
            });
            // Register the Cosmos DB service
            services.AddScoped<IInputOutputConverter<VocabularyCosmosEntity>, VocabularyCosmosDbService>();
            services.AddScoped<VocabularyPersit<VocabularyCosmosEntity>>();

            //var x = new VocabularyPersit<VocabularyCosmosEntity>(new VocabularyCosmosDbService(), new Microsoft.Extensions.Logging.Abstractions.NullLogger<VocabularyPersit<VocabularyCosmosEntity>>());
            return services;
        }
    }
}