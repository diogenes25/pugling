using pugling.Models;

namespace pugling.Work
{
    public class RelatedForm : IRelatedForm
    {
        /// <summary>
        /// The ID of the related vocabulary item.
        /// </summary>
        public string Id { get;  private set; }
        /// <summary>
        /// The word or phrase of the related vocabulary item.
        /// </summary>
        public string Word { get;  private set; }
        /// <summary>
        /// The translation of the related vocabulary item.
        /// </summary>
        public string Translation { get;  private set; }
    }
    
    
}