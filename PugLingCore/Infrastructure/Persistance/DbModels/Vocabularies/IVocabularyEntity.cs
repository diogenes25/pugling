using pugling.Application.Vocabularies;
using PugLingTransfer.Models;
using System.Diagnostics.CodeAnalysis;

namespace pugling.Infrastructure.Persistance.DbModels.Vocabularies
{
    public interface IVocabularyEntity : IVocabulary<IdiomaticUsageEntity, NounDetailsEntity, VocabularyBaseEntity, VerbDetailsEntity>
    {
        VocabularyEntity FillAndValidate([NotNull] Vocabulary vocabulary);
    }
}