using pugling.Infrastructure.DbFile;
using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.DbServices.DbModels;
using Swashbuckle.AspNetCore.Filters;

namespace pugling
{
    public static class ServicesConfigurations
    {
        public static IServiceCollection AddDbServices(this IServiceCollection services)
        {
            services.AddScoped<IVocabularyDbService<VocabularyEntity, VocabularyEntity>, VocabularyDbFileService>();

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
    }
}