using pugling.Infrastructure.DbServices.DbModels;

namespace pugling.Infrastructure.DbCosmosDb
{
    public record VocabularyCosmosEntity : VocabularyEntity
    {
        public string vocabularypartition { get; set; } = "vocabulary";
    }
}