using pugling.Models;
using pugling.Models.Constants;
using pugling.Models.Converter;
using pugling.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace pugling.Application
{
    /// <summary>
    /// Represents a vocabulary item with details about its usage, translation, and related forms.
    /// </summary>
    public sealed class Vocabulary : VocabularyBase, IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>, IEquatable<IVocabulary<IdiomaticUsage, NounDetails, VocabularyBase, VerbDetails>?>, ISaveable<Vocabulary>, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the description of the vocabulary item.
        /// </summary>
        public string? Description
        {
            get => _description;
            private set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _description;

        /// <summary>
        /// Gets the example sentence in the source language.
        /// </summary>
        public string? ExampleSentenceSrc
        {
            get => _exampleSentenceSrc;
            private set
            {
                if (_exampleSentenceSrc != value)
                {
                    _exampleSentenceSrc = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _exampleSentenceSrc;

        /// <summary>
        /// Gets the example sentence in the target language.
        /// </summary>
        public string? ExampleSentenceTarget
        {
            get => _exampleSentenceTarget;
            private set
            {
                if (_exampleSentenceTarget != value)
                {
                    _exampleSentenceTarget = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _exampleSentenceTarget;

        /// <summary>
        /// Gets the tense of the example sentence.
        /// </summary>
        public string? ExampleSentenceTense
        {
            get => _exampleSentenceTense;
            private set
            {
                if (_exampleSentenceTense != value)
                {
                    _exampleSentenceTense = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _exampleSentenceTense;

        /// <summary>
        /// Gets the idiomatic usages associated with the vocabulary item.
        /// </summary>
        public IdiomaticUsage[]? IdiomaticUsages
        {
            get => _idiomaticUsages;
            private set
            {
                if (_idiomaticUsages != value)
                {
                    _idiomaticUsages = value;
                    OnPropertyChanged();
                }
            }
        }

        private IdiomaticUsage[]? _idiomaticUsages;

        /// <summary>
        /// Gets the noun details of the vocabulary item, if applicable.
        /// </summary>
        public NounDetails? Noun
        {
            get => _noun;
            private set
            {
                if (_noun != value)
                {
                    _noun = value;
                    OnPropertyChanged();
                }
            }
        }

        private NounDetails? _noun;

        /// <summary>
        /// Gets the part of speech of the vocabulary item.
        /// </summary>
        public EPartOfSpeech PartOfSpeech
        {
            get => _partOfSpeech;
            private set
            {
                if (_partOfSpeech != value)
                {
                    _partOfSpeech = value;
                    OnPropertyChanged();
                }
            }
        }

        private EPartOfSpeech _partOfSpeech;

        /// <summary>
        /// Gets the pronunciation of the vocabulary item.
        /// </summary>
        public string? Pronunciation
        {
            get => _pronunciation;
            private set
            {
                if (_pronunciation != value)
                {
                    _pronunciation = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _pronunciation;

        /// <summary>
        /// Gets the URL for the pronunciation audio of the vocabulary item.
        /// </summary>
        public Uri? PronunciationAudioUrl
        {
            get => _pronunciationAudioUrl;
            private set
            {
                if (_pronunciationAudioUrl != value)
                {
                    _pronunciationAudioUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        private Uri? _pronunciationAudioUrl;

        /// <summary>
        /// Gets the related forms of the vocabulary item.
        /// </summary>
        public VocabularyBase[]? RelatedForms
        {
            get => _relatedForms;
            private set
            {
                if (_relatedForms != value)
                {
                    _relatedForms = value;
                    OnPropertyChanged();
                }
            }
        }

        private VocabularyBase[]? _relatedForms;

        /// <summary>
        /// Gets the source language of the vocabulary item.
        /// </summary>
        public string SourceLanguage
        {
            get => _sourceLanguage;
            private set
            {
                if (_sourceLanguage != value)
                {
                    _sourceLanguage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _sourceLanguage;

        /// <summary>
        /// Gets the target language of the vocabulary item.
        /// </summary>
        public string TargetLanguage
        {
            get => _targetLanguage;
            private set
            {
                if (_targetLanguage != value)
                {
                    _targetLanguage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _targetLanguage;

        /// <summary>
        /// Gets the last updated timestamp of the vocabulary item.
        /// </summary>
        public DateTime? UpdatedAt
        {
            get => _updatedAt;
            private set
            {
                if (_updatedAt != value)
                {
                    _updatedAt = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime? _updatedAt;

        /// <summary>
        /// Gets the verb details of the vocabulary item, if applicable.
        /// </summary>
        public VerbDetails? Verb
        {
            get => _verb;
            private set
            {
                if (_verb != value)
                {
                    _verb = value;
                    OnPropertyChanged();
                }
            }
        }

        private VerbDetails? _verb;

        /// <summary>
        /// Gets the version of the vocabulary item.
        /// </summary>
        public string Version
        {
            get => _version;
            private set
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _version = "1.0";

        /// <summary>
        /// Gets the URL for the target of the example sentence.
        /// </summary>
        public Uri? ExampleSentenceTargetUrl
        {
            get => _exampleSentenceTargetUrl;
            private set
            {
                if (_exampleSentenceTargetUrl != value)
                {
                    _exampleSentenceTargetUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        private Uri? _exampleSentenceTargetUrl;

        /// <summary>
        /// Gets the subcategory part of speech of the vocabulary item.
        /// </summary>
        public EPartOfSpeechSubcategory? PartOfSpeechSubcategory
        {
            get => _partOfSpeechSubcategory;
            private set
            {
                if (_partOfSpeechSubcategory != value)
                {
                    _partOfSpeechSubcategory = value;
                    OnPropertyChanged();
                }
            }
        }

        private EPartOfSpeechSubcategory? _partOfSpeechSubcategory;

        public ISaveableService<Vocabulary> SaveableService { get; set; }

        public bool HasUnsavedChanges => _changedProperties.Any();

        public Vocabulary(ISaveableService<Vocabulary> saveableService)
        {
            this.SaveableService = saveableService;
        }

        #region create

        /// <summary>
        /// Initializes a new instance of the <see cref="Vocabulary"/> class with the specified details.
        /// </summary>
        public Vocabulary(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage)
        {
            this.Id = id;
            this.Word = word;
            this.Translation = translation;
            this.PartOfSpeech = partOfSpeech;
            this.SourceLanguage = sourceLanguage;
            this.TargetLanguage = targetLanguage;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Vocabulary"/> class with the specified details.
        /// </summary>
        public static Vocabulary Create(string id, string word, string translation, EPartOfSpeech partOfSpeech, string sourceLanguage, string targetLanguage) =>
            new(id, word, translation, partOfSpeech, sourceLanguage, targetLanguage);

        /// <summary>
        /// Creates a new instance of the <see cref="Vocabulary"/> class from an existing <see cref="IVocabulary{TIdiomaticUsage, TNounDetails, TRelatedForm, TVerbDetails}"/>.
        /// </summary>
        public static Vocabulary Create(IVocabulary<IIdiomaticUsage, INounDetails, IVocabularyBase, IVerbDetails> vocabulary, ISaveableService<Vocabulary>? vocabularySaveServiceFile)
        {
            var result = new Vocabulary(vocabulary.Id, vocabulary.Word, vocabulary.Translation, vocabulary.PartOfSpeech, vocabulary.SourceLanguage, vocabulary.TargetLanguage)
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
                RelatedForms = vocabulary.RelatedForms?.Select(VocabularyBase.Create).ToArray(),
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

            return result;
        }

        #endregion create

        public event PropertyChangedEventHandler? PropertyChanged;

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

            if (PartOfSpeech == EPartOfSpeech.NotSet)
            {
                PartOfSpeech = EPartOfSpeech.Verb;
            }
            else
            {
                if (PartOfSpeech != EPartOfSpeech.Verb)
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
            if (PartOfSpeech == EPartOfSpeech.NotSet)
            {
                PartOfSpeech = EPartOfSpeech.Noun;
            }
            else
            {
                if (PartOfSpeech != EPartOfSpeech.Noun)
                {
                    throw new InvalidOperationException("Cannot set noun details for a non-noun vocabulary item.");
                }
            }
            this.Noun = NounDetails.Create(noun);
            this.Noun.ParentVocabulary = this;
        }

        private readonly HashSet<string> _changedProperties = [];

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            _changedProperties.Add(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Task<Vocabulary> SaveAsync(CancellationToken cancellationToken)
        {
            if (!_changedProperties.Any())
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
            _changedProperties.Clear();
            return this.SaveableService.SaveAsync(this, cancellationToken);
            //return this.SaveableService.SaveUpdateAsync(this, cancellationToken);
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

        #endregion compare
    }
}