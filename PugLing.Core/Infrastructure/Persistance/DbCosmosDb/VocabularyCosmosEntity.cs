using PugLing.Core.Application.Vocabularies;
using pugling.Infrastructure.Persistance.DbModels.Vocabularies;
using System.Text.Json.Serialization;

namespace pugling.Infrastructure.Persistance.DbCosmosDb;

public record VocabularyCosmosEntity : VocabularyEntity
{
    [JsonPropertyName("vocabularypartition")]
    public string vocabularypartition { get; set; } = "vocabulary";

    public string _ttl { get; set; } = "10000";

    public new VocabularyCosmosEntity FillAndValidate(Vocabulary saveObj)
    {
        base.FillAndValidate(saveObj);
        this.vocabularypartition = $"{saveObj.SourceLanguage}-{saveObj.TargetLanguage}-vocabulary";
        return this;
    }
}