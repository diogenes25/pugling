using pugling.Infrastructure.DbServices.DbModels;

namespace pugling.Infrastructure.DbCosmosDb
{
    public record VocabularyCosmosEntity : VocabularyEntity
    {
        public string Vocabularypartition { get; set; } = string.Empty;
    }
}