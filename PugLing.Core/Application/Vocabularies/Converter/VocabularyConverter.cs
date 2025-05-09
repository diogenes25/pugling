using PugLing.Model.Models;

namespace PugLing.Core.Application.Vocabularies.Converter;

public static class VocabularyConverter
{
    public static VocabularyBaseDto ToDomain(this IVocabularyBase vocabulary)
    {
        return new VocabularyBaseDto
        {
            Id = vocabulary.Id,
            Word = vocabulary.Word,
            Translation = vocabulary.Translation,
            SourceLanguage = vocabulary.SourceLanguage,
            TargetLanguage = vocabulary.TargetLanguage
        };
    }

    public static VocabularyBaseDto[] ToDomain(this IVocabularyBase[] vocabularies)
    {
        return [.. vocabularies.Select(v => v.ToDomain())];
    }

    public static VocabularyDto ToDomain(this IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary)
    {
        return new VocabularyDto
        {
            Id = vocabulary.Id,
            SourceLanguage = vocabulary.SourceLanguage,
            TargetLanguage = vocabulary.TargetLanguage,
            Word = vocabulary.Word,
            Translation = vocabulary.Translation,
            PartOfSpeech = vocabulary.PartOfSpeech,
            ExampleSentenceSrc = vocabulary.ExampleSentenceSrc,
            ExampleSentenceTarget = vocabulary.ExampleSentenceTarget,
            ExampleSentenceTense = vocabulary.ExampleSentenceTense,
            IdiomaticUsages = vocabulary.IdiomaticUsages?.ToDomain(),
            Noun = vocabulary.Noun?.ToDomain(),
            Verb = vocabulary.Verb?.ToDomain()
        };
    }

    public static IdiomaticUsageDto? ToDomain(this IIdiomaticUsage idiomaticUsage)
    {
        return idiomaticUsage is null
            ? null
            : new IdiomaticUsageDto
            {
                Phrase = idiomaticUsage.Phrase,
                Translation = idiomaticUsage.Translation
            };
    }

    public static IdiomaticUsageDto[]? ToDomain(this IIdiomaticUsage[]? idiomaticUsages) => idiomaticUsages?.Select(v => v.ToDomain()).ToArray() ?? null;

    public static NounDetailsDto? ToDomain(this INounDetails? nounDetails)
    {
        return nounDetails is null ? null : new NounDetailsDto
        {
            DeterminedArticle = nounDetails.DeterminedArticle,
            Genus = nounDetails.Genus,
            UndeterminedArticle = nounDetails.UndeterminedArticle
        };
    }

    public static VerbDetailsDto? ToDomain(this IVerbDetails? verbDetails)
    {
        return verbDetails is null
        ? null
        : new VerbDetailsDto
        {
            IsBaseForm = verbDetails.IsBaseForm,
            BaseFormRef = verbDetails.BaseFormRef,
            Person = verbDetails.Person,
            Infinitiv = verbDetails.Infinitiv,
            Tense = verbDetails.Tense,
            //Conjugations = verbDetails.Conjugations.ToDomain()
        };
    }

    public static Dictionary<string, Dictionary<string, ConjugationDetailsDto>> ToDomain(this Dictionary<string, Dictionary<string, IConjugationDetails>> conjugations)
    {
        return conjugations.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDomain());
    }

    public static Dictionary<string, ConjugationDetailsDto> ToDomain(this Dictionary<string, IConjugationDetails> conjugations)
    {
        return conjugations.ToDictionary(
            kvp => kvp.Key,
            kvp => new ConjugationDetailsDto
            {
                Form = kvp.Value.Form,
                VocObjRef = kvp.Value.VocObjRef
            });
    }
}