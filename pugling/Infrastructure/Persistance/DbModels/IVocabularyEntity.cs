using pugling.Application;
using pugling.Infrastructure.DbServices.DbModels;
using pugling.Models;
using System.Diagnostics.CodeAnalysis;

namespace pugling.Infrastructure.Persistance.DbModels
{
    public interface IVocabularyEntity : IVocabulary<IdiomaticUsageEntity, NounDetailsEntity, VocabularyBaseEntity, VerbDetailsEntity>
    {
        VocabularyEntity FillAndValidate([NotNull] Vocabulary vocabulary);
    }
}