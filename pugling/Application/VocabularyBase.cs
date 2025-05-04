using pugling.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace pugling.Application
{
    /// <summary>
    /// Represents a related vocabulary item with an ID, word, and translation.
    /// </summary>
    public class VocabularyBase : IVocabularyBase, IEquatable<IVocabularyBase?>, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the ID of the related vocabulary item.
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        /// Gets the word or phrase of the related vocabulary item.
        /// </summary>
        public string Word { get; protected set; }

        /// <summary>
        /// Gets the translation of the related vocabulary item.
        /// </summary>
        public string Translation { get; protected set; }

        /// <summary>
        /// Gets the source language of the vocabulary item.
        /// </summary>
        public string SourceLanguage
        {
            get => _sourceLanguage;
            protected set
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
            protected set
            {
                if (_targetLanguage != value)
                {
                    _targetLanguage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _targetLanguage;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Creates a new instance of <see cref="VocabularyBase"/> with the specified ID, word, and translation.
        /// </summary>
        /// <param name="id">The ID of the related vocabulary item.</param>
        /// <param name="word">The word or phrase of the related vocabulary item.</param>
        /// <param name="translation">The translation of the related vocabulary item.</param>
        /// <returns>A new instance of <see cref="VocabularyBase"/>.</returns>
        public VocabularyBase(string id, string word, string translation, string sourcelanguage, string targetlanguae)
        {
            Id = id;
            Word = word;
            Translation = translation;
            SourceLanguage = sourcelanguage;
            TargetLanguage = targetlanguae;
        }

        /// <summary>
        /// Creates a new instance of <see cref="VocabularyBase"/> from an existing <see cref="IVocabularyBase"/>.
        /// </summary>
        /// <param name="relatedForm">The existing related form to copy.</param>
        /// <returns>A new instance of <see cref="VocabularyBase"/>.</returns>
        public static VocabularyBase Create(IVocabularyBase relatedForm)
        {
            return new VocabularyBase(
                        relatedForm.Id,
                        relatedForm.Word,
                        relatedForm.Translation,
                        relatedForm.SourceLanguage,
                        relatedForm.TargetLanguage
                    );
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as IVocabularyBase);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IVocabularyBase"/> is equal to the current object.
        /// </summary>
        /// <param name="other">The <see cref="IVocabularyBase"/> to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="IVocabularyBase"/> is equal to the current object; otherwise, <c>false</c>.</returns>
        public bool Equals(IVocabularyBase? other)
        {
            return other is not null &&
                   this.Id == other.Id &&
                   this.Word == other.Word &&
                   this.Translation == other.Translation &&
                   this.SourceLanguage == other.SourceLanguage &&
                     this.TargetLanguage == other.TargetLanguage;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Word, this.Translation, this.SourceLanguage, this.TargetLanguage);
        }

        /// <summary>
        /// Determines whether two <see cref="VocabularyBase"/> objects are equal.
        /// </summary>
        /// <param name="left">The first <see cref="VocabularyBase"/> to compare.</param>
        /// <param name="right">The second <see cref="IVocabularyBase"/> to compare.</param>
        /// <returns><c>true</c> if the two objects are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(VocabularyBase? left, IVocabularyBase? right)
        {
            return EqualityComparer<IVocabularyBase>.Default.Equals(left, right);
        }

        /// <summary>
        /// Determines whether two <see cref="VocabularyBase"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="VocabularyBase"/> to compare.</param>
        /// <param name="right">The second <see cref="IVocabularyBase"/> to compare.</param>
        /// <returns><c>true</c> if the two objects are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(VocabularyBase? left, IVocabularyBase? right)
        {
            return !(left == right);
        }

        protected readonly HashSet<string> _changedProperties = [];

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            _changedProperties.Add(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}