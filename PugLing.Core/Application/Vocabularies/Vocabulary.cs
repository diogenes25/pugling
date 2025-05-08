using PugLing.Core.Services;
using PugLing.Core.Application.Vocabularies.Converter;
using PugLing.Model.Models;
using PugLing.Model.Models.Constants;
using PugLing.Core.Infrastructure;

namespace PugLing.Core.Application.Vocabularies;

/// <summary>
/// Represents a vocabulary item with details about its usage, translation, and related forms.
/// </summary>
public sealed class Vocabulary : VocabularyBase, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>, IEquatable<IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails>?>, ISaveable<Vocabulary>
{
    /// <summary>
    /// Gets the description of the vocabulary item.
    /// </summary>
    public string? Description
    {
        get => this._description;
        private set
        {
            if (this._description != value)
            {
                this._description = value;
                this.OnPropertyChanged();
            }
        }
    }

    private string? _description;

    /// <summary>
    /// Gets the example sentence in the source language.
    /// </summary>
    public string? ExampleSentenceSrc
    {
        get => this._exampleSentenceSrc;
        private set
        {
            if (this._exampleSentenceSrc != value)
            {
                this._exampleSentenceSrc = value;
                this.OnPropertyChanged();
            }
        }
    }

    private string? _exampleSentenceSrc;

    /// <summary>
    /// Gets the example sentence in the target language.
    /// </summary>
    public string? ExampleSentenceTarget
    {
        get => this._exampleSentenceTarget;
        private set
        {
            if (this._exampleSentenceTarget != value)
            {
                this._exampleSentenceTarget = value;
                this.OnPropertyChanged();
            }
        }
    }

    private string? _exampleSentenceTarget;

    /// <summary>
    /// Gets the tense of the example sentence.
    /// </summary>
    public string? ExampleSentenceTense
    {
        get => this._exampleSentenceTense;
        private set
        {
            if (this._exampleSentenceTense != value)
            {
                this._exampleSentenceTense = value;
                this.OnPropertyChanged();
            }
        }
    }

    private string? _exampleSentenceTense;

    /// <summary>
    /// Gets the idiomatic usages associated with the vocabulary item.
    /// </summary>
    public IdiomaticUsage[]? IdiomaticUsages
    {
        get => this._idiomaticUsages;
        private set
        {
            if (this._idiomaticUsages != value)
            {
                this._idiomaticUsages = value;
                this.OnPropertyChanged();
            }
        }
    }

    private IdiomaticUsage[]? _idiomaticUsages;

    /// <summary>
    /// Gets the noun details of the vocabulary item, if applicable.
    /// </summary>
    public NounDetails? Noun
    {
        get => this._noun;
        private set
        {
            if (this._noun != value)
            {
                this._noun = value;
                this.OnPropertyChanged();
            }
        }
    }

    private NounDetails? _noun;

    /// <summary>
    /// Gets the part of speech of the vocabulary item.
    /// </summary>
    public EPartOfSpeech PartOfSpeech
    {
        get => this._partOfSpeech;
        private set
        {
            if (this._partOfSpeech != value)
            {
                this._partOfSpeech = value;
                this.OnPropertyChanged();
            }
        }
    }

    private EPartOfSpeech _partOfSpeech;

    /// <summary>
    /// Gets the pronunciation of the vocabulary item.
    /// </summary>
    public string? Pronunciation
    {
        get => this._pronunciation;
        private set
        {
            if (this._pronunciation != value)
            {
                this._pronunciation = value;
                this.OnPropertyChanged();
            }
        }
    }

    private string? _pronunciation;

    /// <summary>
    /// Gets the URL for the pronunciation audio of the vocabulary item.
    /// </summary>
    public Uri? PronunciationAudioUrl
    {
        get => this._pronunciationAudioUrl;
        private set
        {
            if (this._pronunciationAudioUrl != value)
            {
                this._pronunciationAudioUrl = value;
                this.OnPropertyChanged();
            }
        }
    }

    private Uri? _pronunciationAudioUrl;

    /// <summary>
    /// Gets the related forms of the vocabulary item.
    /// </summary>
    public VocabularyBase[]? RelatedForms
    {
        get => this._relatedForms;
        private set
        {
            if (this._relatedForms != value)
            {
                this._relatedForms = value;
                this.OnPropertyChanged();
            }
        }
    }

    private VocabularyBase[]? _relatedForms;

    /// <summary>
    /// Gets the last updated timestamp of the vocabulary item.
    /// </summary>
    public DateTime? UpdatedAt
    {
        get => this._updatedAt;
        private set
        {
            if (this._updatedAt != value)
            {
                this._updatedAt = value;
                this.OnPropertyChanged();
            }
        }
    }

    private DateTime? _updatedAt;

    /// <summary>
    /// Gets the verb details of the vocabulary item, if applicable.
    /// </summary>
    public VerbDetails? Verb
    {
        get => this._verb;
        private set
        {
            if (this._verb != value)
            {
                this._verb = value;
                this.OnPropertyChanged();
            }
        }
    }

    private VerbDetails? _verb;

    /// <summary>
    /// Gets the version of the vocabulary item.
    /// </summary>
    public string Version
    {
        get => this._version;
        private set
        {
            if (this._version != value)
            {
                this._version = value;
                this.OnPropertyChanged();
            }
        }
    }

    private string _version = "1.0";

    /// <summary>
    /// Gets the URL for the target of the example sentence.
    /// </summary>
    public Uri? ExampleSentenceTargetUrl
    {
        get => this._exampleSentenceTargetUrl;
        private set
        {
            if (this._exampleSentenceTargetUrl != value)
            {
                this._exampleSentenceTargetUrl = value;
                this.OnPropertyChanged();
            }
        }
    }

    private Uri? _exampleSentenceTargetUrl;

    /// <summary>
    /// Gets the subcategory part of speech of the vocabulary item.
    /// </summary>
    public EPartOfSpeechSubcategory? PartOfSpeechSubcategory
    {
        get => this._partOfSpeechSubcategory;
        private set
        {
            if (this._partOfSpeechSubcategory != value)
            {
                this._partOfSpeechSubcategory = value;
                this.OnPropertyChanged();
            }
        }
    }

    private EPartOfSpeechSubcategory? _partOfSpeechSubcategory;

    public ISaveableService<Vocabulary>? SaveableService { get; set; }

    public bool HasUnsavedChanges => this._changedProperties.Count != 0;

    #region create

    /// <summary>
    /// Initializes a new instance of the <see cref="Vocabulary"/> class with the specified details.
    /// </summary>
    public Vocabulary(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage) : base(id, word, translation, sourceLanguage, targetLanguage)
    {
        this.PartOfSpeech = partOfSpeech;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Vocabulary"/> class with the specified details.
    /// </summary>
    public static Vocabulary Create(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage) =>
        new(id, word, translation, partOfSpeech, sourceLanguage, targetLanguage);

    /// <summary>
    /// Creates a new instance of the <see cref="Vocabulary"/> class from an existing <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TRelatedForm, TVerbDetails}"/>.
    /// </summary>
    public static Vocabulary Create(string sourceLanguage, string targetLanguage, IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary, ISaveableService<Vocabulary>? vocabularySaveServiceFile)
    {
        var result = new Vocabulary(vocabulary.Id, vocabulary.Word, vocabulary.Translation, vocabulary.PartOfSpeech, sourceLanguage, targetLanguage)
        {
            Version = vocabulary.Version,
            Description = vocabulary.Description,
            ExampleSentenceSrc = vocabulary.ExampleSentenceSrc,
            ExampleSentenceTarget = vocabulary.ExampleSentenceTarget,
            ExampleSentenceTense = vocabulary.ExampleSentenceTense,
            IdiomaticUsages = vocabulary.IdiomaticUsages?.Select(IdiomaticUsage.Create).ToArray(),
            Noun = NounDetails.Create(vocabulary.Noun),
            Pronunciation = vocabulary.Pronunciation,
            PronunciationAudioUrl = vocabulary.PronunciationAudioUrl,
            RelatedForms = vocabulary.RelatedForms?.Select(Create).ToArray(),
            UpdatedAt = vocabulary.UpdatedAt,
            Verb = VerbDetails.Create(vocabulary.Verb),
            ExampleSentenceTargetUrl = vocabulary.ExampleSentenceTargetUrl,
            PartOfSpeechSubcategory = vocabulary.PartOfSpeechSubcategory,
            SaveableService = vocabularySaveServiceFile,
        };
        result.Id = string.IsNullOrWhiteSpace(vocabulary.Id) ? VocabularyUtil.GenerateRestfulId(result) : vocabulary.Id;

        if (result.Noun != null)
        {
            result.Noun.ParentVocabulary = result;
        }

        if (result.Verb != null)
        {
            result.Verb.ParentVocabulary = result;
        }

        result.SourceLanguage ??= vocabulary.SourceLanguage;
        result.TargetLanguage ??= vocabulary.TargetLanguage;
        return result;
    }

    #endregion create

    public void SetVerb(IVerbDetails? verb)
    {
        if (this.Verb != null)
        {
            this.Verb.ParentVocabulary = null;
        }

        if (verb == null)
        {
            return;
        }

        if (this.PartOfSpeech == EPartOfSpeech.NotSet)
        {
            this.PartOfSpeech = EPartOfSpeech.Verb;
        }
        else
        {
            if (this.PartOfSpeech != EPartOfSpeech.Verb)
            {
                throw new InvalidOperationException("Cannot set verb details for a non-verb vocabulary item.");
            }
        }
        this.Verb = VerbDetails.Create(verb);
        this.Verb.ParentVocabulary = this;
    }

    public void SetNoun(INounDetails? noun)
    {
        if (this.Noun != null)
        {
            this.Noun.ParentVocabulary = null;
        }
        if (noun == null)
        {
            return;
        }
        if (this.PartOfSpeech == EPartOfSpeech.NotSet)
        {
            this.PartOfSpeech = EPartOfSpeech.Noun;
        }
        else
        {
            if (this.PartOfSpeech != EPartOfSpeech.Noun)
            {
                throw new InvalidOperationException("Cannot set noun details for a non-noun vocabulary item.");
            }
        }
        this.Noun = NounDetails.Create(noun);
        if (this.Noun != null)
        {
            this.Noun.ParentVocabulary = this;
        }
    }

    public Task<Vocabulary> SaveAsync(CancellationToken cancellationToken)
    {
        if (this._changedProperties.Count == 0)
        {
            return Task.FromResult(this);
        }

        // Check if the SaveableService is initialized and Id is set
        if (this.SaveableService == null)
        {
            throw new InvalidOperationException("SaveableService is not initialized.");
        }
        if (this.Id == null)
        {
            throw new InvalidOperationException("Id is not set.");
        }
        if (this.Id == string.Empty)
        {
            this.Id = VocabularyUtil.GenerateRestfulId(this);
        }
        // Save the changes
        this.UpdatedAt = DateTime.UtcNow;
        this._changedProperties.Clear();
        return this.SaveableService.SaveAsync(this, cancellationToken);
    }

    #region compare

    public static bool operator ==(Vocabulary? left, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? right) =>
        EqualityComparer<IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>>.Default.Equals(left, right);

    public static bool operator !=(Vocabulary? left, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? right) => !(left == right);

    /// <inheritdoc />
    public override bool Equals(object? obj) => this.Equals(obj as IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>);

    /// <inheritdoc />
    public bool Equals(IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>? other) => this.Compare(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Id, this.Noun, this.PartOfSpeech, this.SourceLanguage, this.TargetLanguage, this.Translation, this.Verb, this.Word);
    }

    public bool Equals(IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails>? other)
    {
        return this.Compare(other);
    }

    #endregion compare
}