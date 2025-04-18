using pugling.Models;

namespace pugling.Work
{
    public class IdiomaticUsage : IIdiomaticUsage
    {
        /// <summary>
        /// The idiomatic phrase in the source language.
        /// </summary>
        public string Phrase { get;  private set; }
        /// <summary>
        /// The translation of the idiomatic phrase in the target language.
        /// </summary>
        public string Translation { get;  private set; }
        public IdiomaticUsage()
        {
        }
    }
}