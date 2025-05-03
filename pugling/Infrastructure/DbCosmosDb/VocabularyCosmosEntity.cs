using pugling.Infrastructure.DbServices.DbModels;
using System.Text.Json.Serialization;

namespace pugling.Infrastructure.DbCosmosDb
{
    public record VocabularyCosmosEntity : VocabularyEntity
    {
        [JsonPropertyName("vocabularypartition")]
        public string vocabularypartition { get; set; } = "vocabulary";

        public string _ttl { get; set; } = "10000";
    }
}