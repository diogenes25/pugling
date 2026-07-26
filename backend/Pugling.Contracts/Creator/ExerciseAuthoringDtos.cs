using System.Text.Json.Serialization;

namespace Pugling.Contracts.Creator;

// Vertrag der typisierten Übungs-CRUD (ein Controller je Übungstyp, gemeinsame generische Hülle),
// der Vokabel-Items als eigener Ebene und der Birkenbihl-Dekodierung.

/// <summary>
/// Übung zum Anlegen/Ändern: gemeinsame Felder + typ-spezifische Config + optionaler Bonus-Vorschlag.
/// Die Metadaten (Klassenstufe, Schulart, Quelle, Art) dienen der Lehrplan-Vorfilterung und sind optional.
/// </summary>
public record ExercisePayload<TConfig>(string Title, int OrderIndex, int RewardPoints, TConfig Config,
    SuggestedBonus? SuggestedBonus = null,
    int? GradeMin = null, int? GradeMax = null, SchoolTypes SchoolTypes = SchoolTypes.None,
    string? Source = null, int? CategoryId = null, string? Description = null,
    bool DefaultUseLeitner = false, bool DefaultRequireTypedTest = false, int? DefaultStage = null,
    int? DefaultItemCount = null, bool ExecutePublic = true);

/// <summary>
/// Übung in der Antwort. <paramref name="IsOwn"/> = der anfragende Creator darf sie <b>ändern</b> (Owner- oder
/// Write-Grant); <paramref name="IsOwner"/> = darf sie <b>verwalten</b> (Owner: löschen, Rechte vergeben,
/// Sichtbarkeit umschalten); <paramref name="ExecutePublic"/> = für alle zuweisbar.
/// </summary>
public record ExerciseResponse<TConfig>(int Id, int ChapterId, string Type, string Title,
    int OrderIndex, int RewardPoints, DateTime CreatedAt, TConfig Config, SuggestedBonus? SuggestedBonus,
    int? GradeMin, int? GradeMax, SchoolTypes SchoolTypes, string? Source, int? CategoryId, string? CategoryName,
    int? AuthorFatherId, bool IsOwn, bool IsOwner, bool ExecutePublic, int GrantCount, string? Description,
    bool DefaultUseLeitner, bool DefaultRequireTypedTest,
    int? DefaultStage, int? DefaultItemCount);

/// <summary>
/// Antworten des Kindes für einen Katalog-Direktcheck, positionsbezogen (Index in der Aufgaben-/Paarliste).
/// <paramref name="Seed"/> ist nur für seed-gebundene Typen (Rechen-Drill) nötig – der beim Generieren erhaltene.
/// </summary>
public record CheckDto(List<Shared.GivenAnswer> Answers, int? Seed = null);

/// <summary>Auswahl der Vokabeln per Tag statt manueller Referenzliste.</summary>
public record RefsFromTagsDto(List<string> Tags, bool MatchAll = false, bool BaseFormsOnly = false);

/// <summary>Ein einzelnes Vokabelpaar der Übung. Front/Rückseite kommen aus dem verknüpften Store-Eintrag.</summary>
/// <param name="Id">Stabile Item-Id (ItemId).</param>
/// <param name="OrderIndex">Sortierschlüssel innerhalb der Übung.</param>
/// <param name="VocabularyId">Verknüpfter Vokabel-Store-Eintrag.</param>
/// <param name="Front">Wort der Lernsprache (aus dem Store).</param>
/// <param name="Back">Übersetzung (aus dem Store).</param>
/// <param name="Hint">Übungslokaler Hinweis; überschreibt den abgeleiteten Store-Hinweis.</param>
/// <param name="Self">HATEOAS-Link auf das Item selbst.</param>
/// <param name="Vocabulary">HATEOAS-Link auf den Store-Eintrag.</param>
public record VocabItemResponse(int Id, int OrderIndex, int VocabularyId, string Front, string Back, string? Hint,
    [property: JsonPropertyName("_self")] string Self,
    [property: JsonPropertyName("vocabulary")] string Vocabulary);

/// <summary>
/// Anlegen/Ändern eines Items: entweder per <paramref name="VocabularyId"/> (bestehende Store-Vokabel) oder inline
/// per <paramref name="Front"/>/<paramref name="Back"/> (wird im Store angelegt/gefunden). <paramref name="Hint"/>
/// leer = löschen, gesetzt = überschreiben; beim PATCH bleibt jedes weggelassene Feld unverändert.
/// </summary>
public record VocabItemInput(int? VocabularyId = null, string? Front = null, string? Back = null,
    string? Hint = null, int? OrderIndex = null);

/// <summary>Ein austauschbarer Vokabel-Kandidat für ein Wort (bei Homonymen mehrere).</summary>
/// <param name="VocabularyId">Vokabel-Id.</param>
/// <param name="Word">Wort in der Lernsprache.</param>
/// <param name="Translation">Muttersprachliche Glosse dieser Bedeutung.</param>
/// <param name="PartOfSpeech">Wortart (hilft beim Unterscheiden gleicher Schreibweisen).</param>
/// <param name="Self">Link auf die Vokabelkarte (<c>_self</c>).</param>
public record VocabCandidate(int VocabularyId, string Word, string Translation, string PartOfSpeech,
    [property: JsonPropertyName("_self")] string Self);

/// <summary>
/// Ein dekodiertes Wort der Ausgabe: <paramref name="LearningWord"/> der Lernsprache → wörtliche Glosse
/// <paramref name="Gloss"/>. <paramref name="Gloss"/>/<paramref name="VocabularyId"/>/<paramref name="Self"/>
/// sind <c>null</c>, wenn das Wort (noch) nicht im Vokabelspeicher liegt. <paramref name="Candidates"/> ist nur
/// bei mehrdeutigen Wörtern gefüllt (mehrere passende Karten – der Vater kann per Wort-Endpunkt die richtige wählen).
/// </summary>
public record DecodedWord(int WordId, string LearningWord, string? Gloss, int? VocabularyId,
    [property: JsonPropertyName("_self")] string? Self, IReadOnlyList<VocabCandidate>? Candidates);

/// <summary>Ein dekodierter Satz: Original + natürliche Übersetzung + die Wort-für-Wort-Tuple.</summary>
public record DecodedSentence(int SentenceId, string LearningSentence, string NaturalTranslation, IReadOnlyList<DecodedWord> Result);

/// <summary>Eingabe zum Hinzufügen eines Satzes: der Satz der Lernsprache + seine natürliche, korrekte Übersetzung.</summary>
public record BirkenbihlSentenceInput(string LearningSentence, string NaturalTranslation);

/// <summary>
/// Korrektur eines einzelnen Worts. <paramref name="VocabularyId"/> gesetzt → die Glosse folgt dieser Karte
/// (richtige Bedeutung bei Homonymen). Nur <paramref name="Gloss"/> gesetzt → freie Glosse ohne Karte. Beides
/// leer → Glosse entfernen (Wort bleibt undekodiert).
/// </summary>
public record WordOverride(int? VocabularyId, string? Gloss);

/// <summary>Eingabe der zustandslosen Vorschau: Sprachen + der zu dekodierende Satz samt Übersetzung.</summary>
public record DecodePreviewInput(string LearningLang, string NativeLang, string LearningSentence, string NaturalTranslation);

/// <summary>Ein frisch erzeugter Aufgabensatz zu einer Drill-Übung.</summary>
public record GeneratedDrill(int ExerciseId, string Title, int Seed, IReadOnlyList<Shared.GeneratedProblem> Problems);
