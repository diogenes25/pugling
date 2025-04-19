using pugling.Models;

namespace pugling.Application
{
    /// <summary>
    /// Represents a related vocabulary item with an ID, word, and translation.
    /// </summary>
    public class VocabularyBase : IVocabularyBase, IEquatable<IVocabularyBase?>
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
        /// Creates a new instance of <see cref="VocabularyBase"/> with the specified ID, word, and translation.
        /// </summary>
        /// <param name="id">The ID of the related vocabulary item.</param>
        /// <param name="word">The word or phrase of the related vocabulary item.</param>
        /// <param name="translation">The translation of the related vocabulary item.</param>
        /// <returns>A new instance of <see cref="VocabularyBase"/>.</returns>
        public static VocabularyBase Create(string id, string word, string translation)
        {
            return new VocabularyBase
            {
                Id = id,
                Word = word,
                Translation = translation
            };
        }

        /// <summary>
        /// Creates a new instance of <see cref="VocabularyBase"/> from an existing <see cref="IVocabularyBase"/>.
        /// </summary>
        /// <param name="relatedForm">The existing related form to copy.</param>
        /// <returns>A new instance of <see cref="VocabularyBase"/>.</returns>
        public static VocabularyBase Create(IVocabularyBase relatedForm)
        {
            return new VocabularyBase
            {
                Id = relatedForm.Id,
                Word = relatedForm.Word,
                Translation = relatedForm.Translation
            };
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
                   this.Translation == other.Translation;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Word, this.Translation);
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
    }
}