using pugling.Infrastructure.DbFile;
using pugling.Infrastructure.DbServices;
using pugling.Infrastructure.DbServices.DbModels;

namespace pugling.Services
{
    public static class DbServicesInjections
    {
        public static IServiceCollection AddDbServices(this IServiceCollection services)
        {
            services.AddScoped<IVocabularyDbService<VocabularyEntity, VocabularyEntity>, VocabularyDbFileService>();

            return services;
        }
    }
}