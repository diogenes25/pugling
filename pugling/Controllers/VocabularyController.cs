using Microsoft.AspNetCore.Mvc;
using pugling.Controllers.ModelExamples;
using pugling.Models;
using Swashbuckle.AspNetCore.Filters;

namespace pugling.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class VocabularyController : ControllerBase
    {
        // In-memory storage for demonstration purposes
        private static readonly List<VocabularyDto> VocabularyList = new();

        // GET: api/vocabulary
        /// <summary>
        /// Retrieves all vocabulary items.
        /// </summary>
        /// <returns>A list of all vocabulary items.</returns>
        /// <response code="200">Returns the list of vocabulary items.</response>
        [HttpPost]
        [ProducesResponseType(typeof(VocabularyDto), 201)]
        [ProducesResponseType(400)]
        [SwaggerRequestExample(typeof(VocabularyDto), typeof(VocabularyDtoExample))]
        [SwaggerResponseExample(200, typeof(VocabularyDtoExample))]
        public ActionResult<VocabularyDto> Create([FromBody] VocabularyDto vocabularyDto)
        {
            VocabularyList.Add(vocabularyDto);
            return CreatedAtAction(nameof(GetById), new { id = vocabularyDto.Id }, vocabularyDto);
        }

        // GET: api/vocabulary/{id}
        /// <summary>
        /// Retrieves a specific vocabulary item by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary item.</param>
        /// <returns>The vocabulary item with the specified ID.</returns>
        /// <response code="200">Returns the vocabulary item.</response>
        /// <response code="404">If the vocabulary item is not found.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VocabularyDto), 200)]
        [ProducesResponseType(404)]
        public ActionResult<VocabularyDto> GetById(string id)
        {
            var vocabulary = VocabularyList.Find(v => v.Id == id);
            if (vocabulary == null)
            {
                return NotFound();
            }
            return Ok(vocabulary);
        }

        // PUT: api/vocabulary/{id}
        /// <summary>
        /// Updates an existing vocabulary item.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary item to update.</param>
        /// <param name="vocabularyDto">The updated vocabulary item data.</param>
        /// <response code="204">Indicates the update was successful.</response>
        /// <response code="404">If the vocabulary item is not found.</response>
        /// <response code="400">If the input is invalid.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public ActionResult Update(string id, [FromBody] VocabularyDto vocabularyDto)
        {
            var existingVocabulary = VocabularyList.Find(v => v.Id == id);
            if (existingVocabulary == null)
            {
                return NotFound();
            }

            // Update logic here
            return NoContent();
        }

        // DELETE: api/vocabulary/{id}
        /// <summary>
        /// Deletes a specific vocabulary item by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the vocabulary item to delete.</param>
        /// <response code="204">Indicates the deletion was successful.</response>
        /// <response code="404">If the vocabulary item is not found.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public ActionResult Delete(string id)
        {
            var vocabulary = VocabularyList.Find(v => v.Id == id);
            if (vocabulary == null)
            {
                return NotFound();
            }

            VocabularyList.Remove(vocabulary);
            return NoContent();
        }
    }
}