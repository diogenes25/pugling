using Microsoft.AspNetCore.Mvc;
using pugling.Controllers.ModelExamples;
using pugling.Models;
using pugling.Services;
using Swashbuckle.AspNetCore.Filters;

namespace pugling.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class VocabularyController(VocabularyService _vocabularyService, ILogger<VocabularyController> _logger) : ControllerBase
    {
        // POST: api/vocabulary
        /// <summary>
        /// Create a vocabulary.
        /// </summary>
        /// <returns>The new created Vocabulary</returns>
        /// <response code="201">Returns the created vocabulary.</response>
        [HttpPost]
        //[ProducesResponseType(typeof(VocabularyDto), 201)]
        //[ProducesResponseType(400)]
        [SwaggerRequestExample(typeof(VocabularyDto), typeof(VocabularyDtoExample))]
        [SwaggerResponseExample(201, typeof(VocabularyDtoExample))]
        public async Task<ActionResult<VocabularyDto>> CreateAsync([FromBody] VocabularyDto vocabularyDto)
        {
            var vocabulary = await _vocabularyService.AddVocabularyAsync(vocabularyDto);
            return CreatedAtAction(nameof(GetById), new { id = vocabulary.Id }, vocabulary);
        }

        // GET: api/vocabulary/
        /// <summary>
        /// Retrieves a list of vocabulary items.
        /// </summary>
        /// <returns>A list of all vocabulary items.</returns>
        /// <response code="200">Returns the list of vocabulary items.</response>
        [HttpGet()]
        [ProducesResponseType(typeof(List<VocabularyDto>), 200)]
        public ActionResult<VocabularyDto> GetAll()
        {
            var vocabulary = _vocabularyService.GetAllVocabulariesAsync();
            if (vocabulary == null)
            {
                return NotFound();
            }
            return Ok(vocabulary);
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
        public async Task<ActionResult<VocabularyDto>> GetById(string id)
        {
            try
            {
                var vocabulary = await _vocabularyService.GetVocabularyByIdAsync(id);
                if (vocabulary == null)
                {
                    return NotFound();
                }
                return Ok(vocabulary);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "vocabulary with ID {Id} not found", id);
                return NotFound(new ProblemDetails()
                {
                    Detail = ex.Message,
                    Status = 404,
                    Title = "Vocabulary Not Found",
                    Extensions = new Dictionary<string, object> {
                        { "VocabularyId", id }
                    },
                    Instance = HttpContext.Request.Path,
                    Type = "https://example.com/vocabulary-not-found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving vocabulary with ID {Id}", id);
                return Problem(detail: ex.Message, statusCode: 500);
            }
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
            var existingVocabulary = _vocabularyService.GetVocabularyByIdAsync(id);
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
            var vocabulary = _vocabularyService.DeleteVocabularyAsync(id);
            return NoContent();
        }
    }
}