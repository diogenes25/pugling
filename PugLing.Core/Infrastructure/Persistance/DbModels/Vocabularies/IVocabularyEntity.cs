using PugLing.Core.Application.Vocabularies;
using PugLing.Model.Models;
using System.Diagnostics.CodeAnalysis;

namespace PugLing.Core.Infrastructure.Persistance.DbModels.Vocabularies;

public interface IVocabularyEntity : IVocabulary<IdiomaticUsageEntity, NounDetailsEntity, VocabularyBaseEntity, VerbDetailsEntity>
{
    VocabularyEntity FillAndValidate([NotNull] Vocabulary vocabulary);
}