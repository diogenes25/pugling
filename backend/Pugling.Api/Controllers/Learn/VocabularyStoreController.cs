using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pugling.Api.Auth;
using Pugling.Api.Data;
using Pugling.Api.Models;

namespace Pugling.Api.Controllers.Learn;

/// <summary>
/// Atomarer Vokabel-Store ("Single Source of Truth"). Sätze und Übungen
/// referenzieren diese Einträge später über ihren <c>Key</c>.
/// </summary>
[ApiController]
[Route("api/learn/vocabulary")]
[Tags("Learn – Vocabulary Store")]
[Produces("application/json")]
[Authorize(Roles = Roles.Vater)]
public class VocabularyStoreController(PuglingDbContext db) : ControllerBase
{
    public record VocabularyResponse(int Id, string Key, string Version, string SourceLanguage,
        string TargetLanguage, string Word, string Translation, PartOfSpeech PartOfSpeech,
        NounInfo? Noun, VerbInfo? Verb, int? BaseFormId, string? BaseFormKey,
        string? PronunciationAudioUrl, DateTime CreatedAt);

    static VocabularyResponse Map(Vocabulary v) =>
        new(v.Id, v.Key, v.Version, v.SourceLanguage, v.TargetLanguage, v.Word, v.Translation,
            v.PartOfSpeech, v.Noun, v.Verb, v.BaseFormId, v.BaseForm?.Key, v.PronunciationAudioUrl, v.CreatedAt);

    /// <summary>Liste der Vokabeln, optional gefiltert nach Volltext und/oder Wortart.</summary>
    [HttpGet]
    public async Task<IEnumerable<VocabularyResponse>> List(
        [FromQuery] string? search = null,
        [FromQuery] PartOfSpeech? partOfSpeech = null,
        [FromQuery] int take = 100)
    {
        var query = db.Vocabulary.Include(v => v.BaseForm).AsQueryable();

        if (partOfSpeech is not null)
            query = query.Where(v => v.PartOfSpeech == partOfSpeech);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Word.Contains(search)
                || v.Translation.Contains(search) || v.Key.Contains(search));

        var items = await query.OrderBy(v => v.Key).Take(Math.Clamp(take, 1, 500)).ToListAsync();
        return items.Select(Map);
    }

    /// <summary>Eine Vokabel per numerischer Id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> Get(int id)
    {
        var v = await db.Vocabulary.Include(x => x.BaseForm).FirstOrDefaultAsync(x => x.Id == id);
        return v is null ? NotFound() : Map(v);
    }

    /// <summary>Eine Vokabel per stabilem Key (Referenz-Slug).</summary>
    [HttpGet("by-key/{key}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> GetByKey(string key)
    {
        var v = await db.Vocabulary.Include(x => x.BaseForm).FirstOrDefaultAsync(x => x.Key == key);
        return v is null ? NotFound() : Map(v);
    }

    public record CreateVocabularyDto(string Key, string SourceLanguage, string TargetLanguage,
        string Word, string Translation, PartOfSpeech PartOfSpeech, string? Version = null,
        NounInfo? Noun = null, VerbInfo? Verb = null, string? BaseFormKey = null,
        string? PronunciationAudioUrl = null);

    /// <summary>Erstellt eine Vokabel. Key muss eindeutig sein; BaseFormKey (falls gesetzt) muss existieren.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VocabularyResponse>> Create(CreateVocabularyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key)) return BadRequest("Key ist erforderlich.");
        if (string.IsNullOrWhiteSpace(dto.Word) || string.IsNullOrWhiteSpace(dto.Translation))
            return BadRequest("Word und Translation sind erforderlich.");
        if (await db.Vocabulary.AnyAsync(v => v.Key == dto.Key))
            return Conflict($"Key '{dto.Key}' existiert bereits.");

        int? baseFormId = null;
        if (!string.IsNullOrWhiteSpace(dto.BaseFormKey))
        {
            baseFormId = await db.Vocabulary.Where(v => v.Key == dto.BaseFormKey)
                .Select(v => (int?)v.Id).FirstOrDefaultAsync();
            if (baseFormId is null) return BadRequest($"BaseFormKey '{dto.BaseFormKey}' nicht gefunden.");
        }

        var vocab = new Vocabulary
        {
            Key = dto.Key.Trim(),
            Version = string.IsNullOrWhiteSpace(dto.Version) ? "1.0" : dto.Version,
            SourceLanguage = dto.SourceLanguage,
            TargetLanguage = dto.TargetLanguage,
            Word = dto.Word,
            Translation = dto.Translation,
            PartOfSpeech = dto.PartOfSpeech,
            Noun = dto.Noun,
            Verb = dto.Verb,
            BaseFormId = baseFormId,
            PronunciationAudioUrl = dto.PronunciationAudioUrl,
        };
        db.Vocabulary.Add(vocab);
        await db.SaveChangesAsync();

        await db.Entry(vocab).Reference(v => v.BaseForm).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = vocab.Id }, Map(vocab));
    }

    /// <summary>Nur gesetzte Felder werden geändert. BaseFormKey = "" hebt die Verknüpfung auf.</summary>
    public record UpdateVocabularyDto(string? Version, string? SourceLanguage, string? TargetLanguage,
        string? Word, string? Translation, PartOfSpeech? PartOfSpeech, NounInfo? Noun, VerbInfo? Verb,
        string? BaseFormKey, string? PronunciationAudioUrl);

    /// <summary>Ändert eine Vokabel (partiell).</summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VocabularyResponse>> Update(int id, UpdateVocabularyDto dto)
    {
        var vocab = await db.Vocabulary.FirstOrDefaultAsync(v => v.Id == id);
        if (vocab is null) return NotFound();

        if (dto.Version is not null) vocab.Version = dto.Version;
        if (dto.SourceLanguage is not null) vocab.SourceLanguage = dto.SourceLanguage;
        if (dto.TargetLanguage is not null) vocab.TargetLanguage = dto.TargetLanguage;
        if (dto.Word is not null) vocab.Word = dto.Word;
        if (dto.Translation is not null) vocab.Translation = dto.Translation;
        if (dto.PartOfSpeech is not null) vocab.PartOfSpeech = dto.PartOfSpeech.Value;
        if (dto.Noun is not null) vocab.Noun = dto.Noun;
        if (dto.Verb is not null) vocab.Verb = dto.Verb;
        if (dto.PronunciationAudioUrl is not null) vocab.PronunciationAudioUrl = dto.PronunciationAudioUrl;

        if (dto.BaseFormKey is not null)
        {
            if (dto.BaseFormKey.Length == 0)
            {
                vocab.BaseFormId = null;
            }
            else
            {
                if (dto.BaseFormKey == vocab.Key) return BadRequest("Eine Vokabel kann nicht ihre eigene Grundform sein.");
                var baseFormId = await db.Vocabulary.Where(v => v.Key == dto.BaseFormKey)
                    .Select(v => (int?)v.Id).FirstOrDefaultAsync();
                if (baseFormId is null) return BadRequest($"BaseFormKey '{dto.BaseFormKey}' nicht gefunden.");
                vocab.BaseFormId = baseFormId;
            }
        }

        await db.SaveChangesAsync();
        await db.Entry(vocab).Reference(v => v.BaseForm).LoadAsync();
        return Map(vocab);
    }

    /// <summary>Löscht eine Vokabel. Nicht möglich, solange sie Grundform anderer Vokabeln ist.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var vocab = await db.Vocabulary.FindAsync(id);
        if (vocab is null) return NotFound();

        if (await db.Vocabulary.AnyAsync(v => v.BaseFormId == id))
            return Conflict("Vokabel ist Grundform anderer Einträge und kann nicht gelöscht werden.");

        db.Vocabulary.Remove(vocab);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
