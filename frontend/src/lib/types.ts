// DTOs der Pugling-API (Ausschnitt für den Vokabel-Durchstich).
// Enums werden serverseitig als Strings serialisiert (JsonStringEnumConverter).

// Technischer Rollen-Diskriminator aus dem Login (die Ebenen-Rolle); die UI-Vokabel bleibt Vater/Sohn.
export type Role = "Supervisor" | "Student";

export type PartOfSpeech =
  | "Noun" | "Verb" | "Adjective" | "Adverb" | "Pronoun" | "Preposition"
  | "Conjunction" | "Article" | "Numeral" | "Interjection" | "Phrase" | "Other";

/** Grammatikalisches Geschlecht eines Substantivs. */
export type Genus = "Masculine" | "Feminine" | "Neuter";

/** Substantiv-spezifische Angaben (Teil des komplexen Vokabel-Datensatzes). */
export interface NounInfo {
  article?: string | null;
  genus?: Genus | null;
  plural?: string | null;
}

/** Verb-spezifische Angaben / Konjugations-Metadaten. */
export interface VerbInfo {
  isBaseForm: boolean;
  infinitive?: string | null;
  tense?: string | null;
  person?: string | null;
  number?: string | null;
}

/**
 * Schularten – serverseitig ein [Flags]-Enum. Einzelwerte für Auswahl/Filter; der Server kann
 * bei einem Kind auch eine Kombination als kommaseparierten String liefern ("Realschule, Gymnasium").
 */
export type SchoolType =
  | "None" | "Grundschule" | "Hauptschule" | "Realschule" | "Gymnasium" | "Gesamtschule" | "Berufsschule";

/** Geschlecht des Kindes – Teil des Profils, aus dem der KI-Creator seine Anrede/Einkleidung ableitet. */
export type Gender = "None" | "Male" | "Female" | "Diverse";

export interface LoginResponse {
  token: string;
  role: Role;
  id: number;
  name: string;
  expiresAt: string;
}

// ---- Vater: eigenes Konto (Registrierung + Selbstauskunft) ----

/** Der eigene Vater-Datensatz; die PIN liefert der Server nie aus. */
export interface FatherResponse {
  id: number;
  name: string;
  email: string | null;
  createdAt: string;
  childrenCount: number;
}

/** Registrierung eines Vaters – der einzige Weg, ohne Anmeldung ein Konto zu erzeugen. */
export interface CreateFatherDto {
  name: string;
  email?: string | null;
  pin?: string | null;
}

/** Partielle Änderung des eigenen Kontos; weggelassene Felder bleiben unverändert. */
export interface UpdateFatherDto {
  name?: string | null;
  email?: string | null;
  pin?: string | null;
}

// ---- Vater: Kinder & Vokabel-Store ----

export interface ChildResponse {
  id: number;
  name: string;
  birthYear: number | null;
  grade: number | null;
  schoolType: string;
  gender: Gender;
  /** Freitext-Interessen: die Sprache des KI-Creators. Die gewichteten Tags stehen separat. */
  interests: string[];
  profileNotes: string | null;
  /** Obergrenze der Bild-Eignung; nur der Vater darf sie heben. */
  allowedContentRating: ContentRating;
  createdAt: string;
  coins: number;
  gems: number;
}

// ---- Unterrichtsmaterial: Lehrwerk-Reihe → Unit → Lehrbuch des Kindes ----

/**
 * Eine Lehrwerk-Reihe („Access") – bewusst ein **geteilter** Katalog-Eintrag: das Lehrbuch des Kindes
 * und das Creator-Profil zeigen auf denselben Datensatz. Nur dadurch ist die Frage „welcher Creator
 * kennt das Material dieses Kindes?" berechenbar statt ein Namensvergleich.
 */
export interface TextbookSeriesResponse {
  id: number;
  name: string;
  /** Normalisierter Schlüssel; unveränderlich und global eindeutig (macht das Anlegen idempotent). */
  slug: string;
  publisher: string | null;
  subjectName: string | null;
  subjectId: number | null;
  schoolTypes: string;
  sourceLanguage: string | null;
  targetLanguage: string | null;
  notes: string | null;
  ownerFatherId: number | null;
  /** Ob das angemeldete Konto die Reihe ändern darf (lesen darf jeder Creator). */
  isOwn: boolean;
  unitCount: number;
  createdAt: string;
}

export interface CreateTextbookSeriesDto {
  name: string;
  publisher?: string | null;
  subjectName?: string | null;
  subjectId?: number | null;
  schoolTypes?: SchoolType | null;
  sourceLanguage?: string | null;
  targetLanguage?: string | null;
  notes?: string | null;
}

/** Partielle Änderung einer Reihe; der Slug bleibt fest. */
export type UpdateTextbookSeriesDto = Partial<CreateTextbookSeriesDto>;

/**
 * Eine Unit der Reihe samt Band (`grade`). `topics`/`grammar`/`vocabularyNotes` sind der eigentliche
 * Gewinn: sie sind der Stoff, den ein KI-Creator liest, statt ihn zu erfinden.
 */
export interface SeriesUnitResponse {
  id: number;
  seriesId: number;
  grade: number | null;
  orderIndex: number;
  label: string;
  topics: string | null;
  grammar: string | null;
  vocabularyNotes: string | null;
  createdAt: string;
}

export interface CreateSeriesUnitDto {
  label: string;
  grade?: number | null;
  orderIndex?: number | null;
  topics?: string | null;
  grammar?: string | null;
  vocabularyNotes?: string | null;
}

export type UpdateSeriesUnitDto = Partial<CreateSeriesUnitDto>;

/**
 * Ein vom Kind benutztes Lehrbuch. `seriesId`/`currentUnitId` sind die katalogisierte Form von Titel
 * und Kapitel – erst sie machen aus „irgendein Buch" den auffindbaren Stoff.
 */
export interface TextbookResponse {
  id: number;
  title: string;
  subjectName: string | null;
  subjectId: number | null;
  grade: number | null;
  publisher: string | null;
  isbn: string | null;
  currentChapter: string | null;
  createdAt: string;
  seriesId: number | null;
  seriesName: string | null;
  currentUnitId: number | null;
  currentUnitLabel: string | null;
}

export interface CreateTextbookDto {
  title: string;
  subjectName?: string | null;
  subjectId?: number | null;
  grade?: number | null;
  publisher?: string | null;
  isbn?: string | null;
  currentChapter?: string | null;
  seriesId?: number | null;
  currentUnitId?: number | null;
}

/**
 * Änderung eines Lehrbuchs. Die `clear…`-Schalter sind nötig, weil `null` im PATCH „nicht angegeben"
 * heißt: ohne sie ließe sich ein einmal gesetztes Fach, eine Klasse, eine Reihe oder eine Unit nie
 * wieder loswerden (der Server würde das `null` überlesen und still den alten Wert behalten).
 */
export type UpdateTextbookDto = Partial<CreateTextbookDto> & {
  clearSeries?: boolean;
  clearUnit?: boolean;
  clearSubject?: boolean;
  clearGrade?: boolean;
};

// ---- Creator-Profile („Fachlehrer") ----

/**
 * Ein Creator-Profil ist der *Lehrer*, in dessen Namen der KI-Creator entwirft: ein Fach, ein
 * Schulzweig, ein Klassenstufen-Bereich, optional eine Buchreihe. `persona`/`didactics` prägen seine
 * Rolle im Sprachmodell – die festen Inhalts-Regeln des Agenten weichen sie nie auf.
 */
export interface CreatorProfileResponse {
  id: number;
  name: string;
  ownerFatherId: number | null;
  isOwn: boolean;
  subjectName: string | null;
  subjectId: number | null;
  schoolTypes: string;
  gradeMin: number | null;
  gradeMax: number | null;
  seriesId: number | null;
  seriesName: string | null;
  sourceLang: string;
  targetLang: string;
  persona: string | null;
  didactics: string | null;
  /** Bevorzugte Übungstypen (Schlüssel aus dem Typ-Manifest). */
  defaultTypes: string[];
  active: boolean;
  createdAt: string;
}

export interface CreateCreatorProfileDto {
  name: string;
  subjectName?: string | null;
  subjectId?: number | null;
  schoolTypes?: SchoolType | null;
  gradeMin?: number | null;
  gradeMax?: number | null;
  seriesId?: number | null;
  sourceLang?: string | null;
  targetLang?: string | null;
  persona?: string | null;
  didactics?: string | null;
  defaultTypes?: string[] | null;
  active?: boolean | null;
}

/** Änderung eines Profils; `clear…` leert ein Feld (ein `null` gilt als „nicht angegeben"). */
export type UpdateCreatorProfileDto = Partial<CreateCreatorProfileDto> & {
  clearSubject?: boolean;
  clearSeries?: boolean;
  clearGradeMin?: boolean;
  clearGradeMax?: boolean;
};

/**
 * Ein Treffer der Profil-Suche zu einem Kind. `score` entsteht deterministisch (Reihe wiegt am
 * schwersten), `reasons` sind **stabile Codes** – die Formulierung macht die Oberfläche
 * (siehe `matchReasonLabel`).
 */
export interface CreatorProfileMatch {
  profile: CreatorProfileResponse;
  score: number;
  reasons: string[];
}

// ---- Bilder & Interessen (Individualisierung der Lerninhalte) ----

/**
 * Eignung eines Bildes. Aufsteigend geordnet – die Auswahl liefert einem Kind nie ein Asset über
 * seiner Freigabe.
 */
export type ContentRating = "Everyone" | "Teen" | "Mature";

/** Semantischer Auslieferungs-Slot einer Auflösung (der Client fragt nach Zweck, nicht nach Pixeln). */
export type MediaPurpose = "Thumb" | "Card" | "Full" | "Hero";

export type MediaKind = "Image" | "Audio" | "Video";
export type MediaOrigin = "Unknown" | "Upload" | "Stock" | "Generated";

/**
 * Facette eines Interessen-Schlagworts. `Style` (Comic/Foto/Pixel-Art) liegt bewusst im selben
 * Vokabular wie die Themen – bei der Bildauswahl verhält es sich gleich, nur schwächer gewichtet.
 */
export type InterestFacet =
  | "Other" | "Franchise" | "Sport" | "Animal" | "Vehicle" | "Music" | "Hobby" | "Nature" | "Style";

/** Ein Schlagwort der geteilten Taxonomie – Bilder *und* Kinder referenzieren dieselben Einträge. */
export interface InterestTagResponse {
  id: number;
  slug: string;
  label: string;
  facet: InterestFacet;
  synonyms: string[];
  color: string | null;
  mediaCount: number;
  childCount: number;
  createdAt: string;
}

export interface CreateInterestTagDto {
  label: string;
  slug?: string;
  facet?: InterestFacet;
  synonyms?: string[];
}

/** Ein gewichtetes Interesse des Kindes; negatives Gewicht = Abneigung (schließt Bilder hart aus). */
export interface ChildInterestResponse {
  tagId: number;
  slug: string;
  label: string;
  facet: InterestFacet;
  weight: number;
  createdAt: string;
}

/** Eingabe beim Setzen: entweder eine bestehende `tagId` oder ein Label (wird bei Bedarf angelegt). */
export interface ChildInterestInput {
  weight: number;
  tagId?: number | null;
  label?: string | null;
  facet?: InterestFacet | null;
}

/** Eine technische Ausprägung eines Bildes (dieselbe Darstellung, andere Auflösung/Format). */
export interface MediaVariantResponse {
  id: number;
  purpose: MediaPurpose;
  width: number;
  height: number;
  format: string;
  url: string;
  bytes: number | null;
}

/** Eine *Darstellung* eines Motivs („laufendes Einhorn im Comic-Stil") samt Auflösungen und Tags. */
export interface MediaAssetResponse {
  id: number;
  key: string;
  /** Was zu sehen ist – zugleich Alt-Text für die Barrierefreiheit. */
  description: string;
  kind: MediaKind;
  rating: ContentRating;
  license: string | null;
  attribution: string | null;
  origin: MediaOrigin;
  source: string | null;
  placeholder: string | null;
  variants: MediaVariantResponse[];
  /** Slugs der verknüpften Interessen-/Stil-Schlagworte. */
  tags: string[];
  createdAt: string;
}

export interface CreateMediaVariantDto {
  purpose: MediaPurpose;
  url: string;
  width: number;
  height: number;
  format?: string;
}

export interface CreateMediaAssetDto {
  description: string;
  key?: string;
  rating?: ContentRating;
  origin?: MediaOrigin;
  source?: string | null;
  license?: string | null;
  attribution?: string | null;
  tags?: string[];
  variants?: CreateMediaVariantDto[];
}

/** Eine Bild-Zuordnung an einem Träger (Vokabel/Item/Übung) samt des zugeordneten Assets. */
export interface MediaLinkResponse {
  id: number;
  /** Redaktioneller Rang – bricht nur Gleichstände der Interessens-Bewertung. */
  weight: number;
  asset: MediaAssetResponse;
}

/** Wo ein Asset zugeordnet ist (Rückrichtung; `carrier` = vocabulary | item | exercise). */
export interface MediaUsage {
  carrier: string;
  carrierId: number;
  label: string;
  weight: number;
}

/** Das nach „anderes Bild" gültige Bild. */
export interface SelectedMediaResponse {
  mediaAssetId: number;
  imageUrl: string;
  imageAlt: string;
}

export interface CreateChildDto {
  name: string;
  pin?: string;
  birthYear?: number | null;
  grade?: number | null;
  schoolType?: SchoolType;
  gender?: Gender;
  /** Freitext-Interessen (Sprache des KI-Creators); die gewichteten Tags laufen über eigene Endpunkte. */
  interests?: string[];
  profileNotes?: string | null;
  /** Obergrenze der Bild-Eignung. Ohne Angabe die strengste Stufe. */
  allowedContentRating?: ContentRating;
}

/** Änderung eines Kindes; `clear…` leert ein Feld (ein `null` gilt im PATCH als „nicht angegeben"). */
export type UpdateChildDto = Partial<CreateChildDto> & {
  clearBirthYear?: boolean;
  clearGrade?: boolean;
};

// ---- Lernstand eines Kindes (plan-übergreifend, nicht an eine Position gebunden) ----

/**
 * Aggregierter Lernstand über eine Menge Vokabel-Items – auf jeder Katalog-Ebene identisch aufgebaut.
 * `weakItems` sind die Kandidaten für gezielte Wiederholung (Beherrschung unter 50 %).
 */
export interface MasteryRollup {
  totalItems: number;
  introducedItems: number;
  masteredItems: number;
  weakItems: number;
  avgMasteryPercent: number;
  seenCount: number;
  correctCount: number;
  correctPercent: number;
  lastActivityAt: string | null;
}

/** `active` = enthält mindestens eine über einen aktiven Plan zugewiesene Übung (sonst nur Historie). */
export interface SubjectProgress {
  subjectId: number;
  name: string;
  chapterCount: number;
  exerciseCount: number;
  active: boolean;
  progress: MasteryRollup;
}

export interface ChapterProgress {
  chapterId: number;
  name: string;
  orderIndex: number;
  exerciseCount: number;
  active: boolean;
  progress: MasteryRollup;
}

export interface ExerciseProgress {
  exerciseId: number;
  title: string;
  orderIndex: number;
  active: boolean;
  progress: MasteryRollup;
}

/** Lernstand des Kindes zu einem einzelnen Übungs-Item. */
export interface ItemProgressResponse {
  itemId: number;
  exerciseId: number;
  vocabularyId: number;
  front: string;
  back: string;
  box: number;
  maxBox: number;
  masteryPercent: number;
  seenCount: number;
  correctCount: number;
  introducedAt: string | null;
  lastAnswerAt: string | null;
  lastCorrect: boolean | null;
}

/** Beherrschung eines Store-Wortes über **alle** Übungen, die es benutzen – die „schlecht gelernten Wörter". */
export interface WordMastery {
  vocabularyId: number;
  word: string;
  translation: string;
  itemCount: number;
  avgMasteryPercent: number;
  minBox: number;
  seenCount: number;
  correctCount: number;
  correctPercent: number;
}

/** Ein protokolliertes Antwort-Ereignis (Übung oder Test). */
export interface ItemHistoryEntry {
  at: string;
  source: string;
  stageValue: number;
  givenAnswer: string | null;
  wasCorrect: boolean;
  planPositionId: number | null;
}

// ---- Ziele über dem Lernstand: Lernziele (einzeln) und Objectives (OKR-Klammer) ----

/**
 * Metrik eines Lernziels. Achtung auf die Richtung: `MaxWeakItems` ist ein **Höchstwert** („nicht mehr als
 * N wackelige Wörter"), alle anderen sind Mindestwerte in Prozent.
 */
export type LearnGoalMetric = "AvgMastery" | "Coverage" | "MasteredPercent" | "MaxWeakItems";

/** Wie ein Lernziel/eine Etappe gerade steht; live aus dem Lernstand berechnet, nie gespeichert. */
export type GoalStatus = "open" | "achieved" | "overdue";

/** Ein ausgewertetes Lernziel auf einem Katalog-Scope (Fach, optional Kapitel, optional Übung). */
export interface LearnGoal {
  id: number;
  childId: number;
  subjectId: number;
  chapterId: number | null;
  exerciseId: number | null;
  /** Menschenlesbarer Scope-Text vom Server (z. B. „Englisch · Unit 1"). */
  scope: string;
  metric: LearnGoalMetric;
  targetValue: number;
  currentValue: number;
  progressPercent: number;
  dueDate: string | null;
  status: GoalStatus;
  title: string | null;
  createdAt: string;
}

export interface CreateLearnGoalRequest {
  subjectId: number;
  chapterId?: number | null;
  exerciseId?: number | null;
  metric: LearnGoalMetric;
  targetValue: number;
  dueDate?: string | null;
  title?: string | null;
}

/** Nur gesetzte Felder ändern sich; der Scope bleibt fix (zum Umhängen neu anlegen). */
export interface UpdateLearnGoalRequest {
  metric?: LearnGoalMetric;
  targetValue?: number;
  dueDate?: string | null;
  title?: string | null;
}

/**
 * Art eines Objectives – sie bestimmt die **Währung** der Belohnung: `Committed` ist verbindlich und zahlt
 * Münzen (real einlösbar), `Stretch` ist ein Dehnungsziel und zahlt Gems (Skins).
 */
export type ObjectiveKind = "Committed" | "Stretch";

/** Metrik einer Etappe; `ClassTestGrade` ist die Note ×10 als Höchstwert (20 = „mindestens 2,0"). */
export type KeyResultMetric = "AvgMastery" | "MasteredPercent" | "MaxWeakItems" | "ClassTestGrade";

/** Eine ausgewertete Etappe eines Objectives. */
export interface KeyResult {
  id: number;
  objectiveId: number;
  subjectId: number;
  chapterId: number | null;
  exerciseId: number | null;
  scope: string;
  metric: KeyResultMetric;
  targetValue: number;
  currentValue: number;
  progressPercent: number;
  status: GoalStatus;
  title: string | null;
}

/** Ein Objective mit Etappen und Roll-up; `rewarded` = die Belohnung ist bereits geflossen. */
export interface Objective {
  id: number;
  childId: number;
  title: string;
  motivation: string | null;
  kind: ObjectiveKind;
  start: string | null;
  dueDate: string | null;
  active: boolean;
  rewardOnComplete: number;
  rewardPerKeyResult: number;
  achievedCount: number;
  totalCount: number;
  progressPercent: number;
  status: GoalStatus;
  rewarded: boolean;
  keyResults: KeyResult[];
  createdAt: string;
}

export interface CreateKeyResultRequest {
  subjectId: number;
  chapterId?: number | null;
  exerciseId?: number | null;
  metric: KeyResultMetric;
  targetValue: number;
  title?: string | null;
}

export interface CreateObjectiveRequest {
  title: string;
  motivation?: string | null;
  kind: ObjectiveKind;
  start?: string | null;
  dueDate?: string | null;
  rewardOnComplete: number;
  rewardPerKeyResult: number;
  keyResults?: CreateKeyResultRequest[];
}

export interface UpdateObjectiveRequest {
  title?: string | null;
  motivation?: string | null;
  kind?: ObjectiveKind;
  start?: string | null;
  dueDate?: string | null;
  active?: boolean;
  rewardOnComplete?: number;
  rewardPerKeyResult?: number;
}

/** Teil-Update einer Etappe; der Scope bleibt fix. */
export interface UpdateKeyResultRequest {
  metric?: KeyResultMetric;
  targetValue?: number;
  title?: string | null;
}

// ---- Katalog: Fächer, Kapitel, Übungssuche ----

export interface SubjectResponse {
  id: number;
  name: string;
  createdAt: string;
  chaptersCount: number;
}

export interface ChapterResponse {
  id: number;
  subjectId: number;
  name: string;
  orderIndex: number;
  exercisesCount: number;
}

/** Vollständige Sicht einer Übung inkl. roher Config + Metadaten (zum Anzeigen/Bearbeiten). */
export interface ExerciseDetail {
  id: number;
  chapterId: number;
  chapterName: string;
  subjectId: number;
  subjectName: string;
  type: string;
  title: string;
  /** Freier Beschreibungstext (optional) – hilft beim Erkennen der Übung im Lehrplan-Bau. */
  description: string | null;
  orderIndex: number;
  rewardPoints: number;
  gradeMin: number | null;
  gradeMax: number | null;
  schoolTypes: string;
  source: string | null;
  categoryId: number | null;
  categoryName: string | null;
  defaultStage: number | null;
  defaultItemCount: number | null;
  /** Übungs-Standard für Leitner-Kasten (Position erbt ihn, kann übersteuern). */
  defaultUseLeitner: boolean;
  /** Übungs-Standard „nur getippte Tests" (Position erbt ihn, kann übersteuern). */
  defaultRequireTypedTest: boolean;
  /** Autor der Übung (Vater); null = geseedete System-Übung. */
  authorFatherId: number | null;
  authorName: string | null;
  /** Darf der anfragende Vater die Übung **ändern** (Owner oder Write-Grant)? */
  isOwn: boolean;
  /** Darf er sie **verwalten** (löschen, Rechte vergeben, Sichtbarkeit umschalten)? Nur der Owner. */
  isOwner: boolean;
  /** Für alle Väter zuweisbar? Umschalten ist ein Owner-Recht. */
  executePublic: boolean;
  /** Bonus-Vorschlag, den Lehrplan-Positionen erben; null = Verfahrens-Standard. */
  suggestedBonus: SuggestedBonus | null;
  config: unknown;
}

/**
 * Bonus-Vorschlag einer Übung. Er gehört an die Übung, weil er inhaltsabhängig ist (kurze Vokabeln
 * vertragen straffere Zeitfenster als lange Sätze); die Position erbt ihn und darf übersteuern.
 */
export interface SuggestedBonus {
  comboThreshold: number;
  comboBonusPoints: number;
  speedThresholdSeconds: number;
  speedBonusPoints: number;
  newContentPoints: number;
}

/** Wo eine Übung verwendet wird (nur eigene Kinder). */
export interface PlanUsage { planId: number; planTitle: string; childId: number; childName: string; }
export interface ClassTestUsage { id: number; title: string; childId: number; childName: string; }
export interface ExerciseUsage { plans: PlanUsage[]; classTests: ClassTestUsage[]; }

// ---- Testmodus („Ausprobieren"): Vater spielt eine Übung nebenwirkungsfrei durch ----

/** Eine im Testmodus vorgelegte Aufgabe. `reveal` ist nur bei Selbsteinschätzung gesetzt (Lösung aufgedeckt). */
export interface ExercisePreviewItem {
  itemIndex: number;
  prompt: string;
  /** Nur bei Lückentexten: die {{n}}-Nummer der Lücke. */
  gapIndex: number | null;
  hint: string | null;
  /** Nur bei Vokabel-Buchstabenkästchen: Länge der Lösung. */
  answerLength: number | null;
  /** Bei Selbsteinschätzung die Lösung, bei getippten Stufen null. */
  reveal: string | null;
  /** Nur bei Multiple-Choice: die Auswahlmöglichkeiten (Lösung + Ablenker, gemischt). */
  choices: string[] | null;
  /** Nur bei der Hör-Stufe: Aussprache-Audioquelle der Vokabel (Wort-Text wird dann ausgeblendet). */
  audioUrl: string | null;
}
/** Eine im Testmodus umschaltbare Abfrageform (Stufenwert + Anzeigename). */
export interface ExercisePreviewStage { value: number; label: string; }
/**
 * Spielbarer Zustand einer Übung im Testmodus. `typed` = Antwort wird getippt (sonst Selbsteinschätzung);
 * `stages` = die für diesen Übungstyp durchprobierbaren Abfrageformen (leer, wenn nur eine sinnvoll ist).
 */
export interface ExercisePreviewData {
  type: string; stage: number; typed: boolean; stages: ExercisePreviewStage[]; items: ExercisePreviewItem[];
}
/** Eine Antwort im Testmodus: getippt (`givenAnswer`) oder Selbsteinschätzung (`wasKnown`). */
export interface ExercisePreviewAnswer { itemIndex: number; givenAnswer?: string | null; wasKnown?: boolean | null; }
/** Einzelauswertung im Testmodus (die Lösung `expected` wird hier immer offengelegt). */
export interface ExercisePreviewOutcome {
  itemIndex: number; prompt: string; expected: string; givenAnswer: string | null; wasCorrect: boolean;
}
/** Gesamtergebnis eines Testmodus-Durchlaufs. */
export interface ExercisePreviewResult {
  total: number; correct: number; scorePercent: number; items: ExercisePreviewOutcome[];
}

/** Partielle Vokabel-Änderung (nur gesetzte Felder). */
export interface UpdateVocabularyDto {
  version?: string;
  sourceLanguage?: string;
  targetLanguage?: string;
  word?: string;
  translation?: string;
  partOfSpeech?: PartOfSpeech;
  noun?: NounInfo | null;
  verb?: VerbInfo | null;
  /** Key der Grundform; "" hebt die Verknüpfung auf. */
  baseFormKey?: string | null;
  baseFormRelation?: string | null;
  pronunciationAudioUrl?: string | null;
}

/** Schlanke Trefferzeile der Übungssuche (Metadaten-Filter über den Katalog). */
export interface ExerciseSummary {
  id: number;
  chapterId: number;
  subjectId: number;
  type: string;
  title: string;
  /** Freier Beschreibungstext (optional). */
  description: string | null;
  gradeMin: number | null;
  gradeMax: number | null;
  schoolTypes: string;
  source: string | null;
  categoryId: number | null;
  categoryName: string | null;
  /** Übungs-Standards für Leitner/getippte Tests (Position erbt sie beim Hinzufügen). */
  defaultStage: number | null;
  defaultItemCount: number | null;
  defaultUseLeitner: boolean;
  defaultRequireTypedTest: boolean;
  /** Autor der Übung (Vater); null = geseedete System-Übung. Grundlage der „von …"-Attribution. */
  authorFatherId: number | null;
  authorName: string | null;
  /** Gehört die Übung dem anfragenden Vater? Nur dann darf er sie ändern/löschen. */
  isOwn: boolean;
}

/** Server-paginierte Liste: eine Seite plus Gesamtzahl (kommt aus dem X-Total-Count-Header). */
export interface Paged<T> {
  items: T[];
  total: number;
}

export type SortDir = "asc" | "desc";
/** Erlaubte Sortierschlüssel des Übungskatalogs (Server-Whitelist in ExerciseCatalogController). */
export type ExerciseSortKey = "title" | "type" | "grade" | "source" | "created";
/** Erlaubte Sortierschlüssel des Vokabel-Stores (Server-Whitelist in VocabularyStoreController). */
export type VocabSortKey = "key" | "word" | "translation" | "pos" | "created";

export interface ExerciseSearchParams {
  subjectId?: number;
  chapterId?: number;
  grade?: number;
  schoolType?: SchoolType;
  categoryId?: number;
  type?: string;
  search?: string;
  /** Nur eigene Übungen des Vaters (Verwaltung statt Entdeckung der geteilten Bibliothek). */
  mineOnly?: boolean;
  /** Sortierschlüssel + Richtung (Server-Whitelist); Paginierung per skip/take. */
  sort?: ExerciseSortKey;
  dir?: SortDir;
  skip?: number;
  take?: number;
}

/** Suchparameter des Vokabel-Stores (Filter + Sortierung + Paginierung). */
export interface VocabularySearchParams {
  search?: string;
  sourceLanguage?: string;
  targetLanguage?: string;
  partOfSpeech?: PartOfSpeech;
  tags?: string[];
  matchAll?: boolean;
  sort?: VocabSortKey;
  dir?: SortDir;
  skip?: number;
  take?: number;
}

/** Fachabhängige Übungs-Art ("Kategorie") zur Vorfilterung. */
export interface CategoryResponse {
  id: number;
  subjectId: number;
  name: string;
  createdAt: string;
}

export interface VocabularyResponse {
  id: number;
  key: string;
  version: string;
  sourceLanguage: string;
  targetLanguage: string;
  word: string;
  translation: string;
  partOfSpeech: PartOfSpeech;
  /** Substantiv-Details (nur bei Wortart Noun sinnvoll gesetzt). */
  noun: NounInfo | null;
  /** Verb-Details (nur bei Wortart Verb sinnvoll gesetzt). */
  verb: VerbInfo | null;
  /** Id der Grundform (go→went→gone); null = eigenständig/Grundform. */
  baseFormId: number | null;
  /** Key der Grundform (zur Anzeige/Bearbeitung). */
  baseFormKey: string | null;
  baseFormRelation: string | null;
  pronunciationAudioUrl: string | null;
  /** Globale, kindneutrale Schlagworte (Namen) – vgl. VocabTagResponse. */
  tags: string[];
  createdAt: string;
}

/** Globaler, kindneutraler Vokabel-Tag (learn/vocabulary/tags). */
export interface VocabTagResponse {
  id: number;
  name: string;
  color: string | null;
  vocabCount: number;
  createdAt: string;
}

/**
 * Kind-skopierter Tag (api/v1/creator/tags) – markiert Übungen UND Vokabeln als für ein Kind relevant.
 * Nicht verwechseln mit dem globalen VocabTagResponse.
 */
export interface ChildTagResponse {
  id: number;
  childId: number;
  name: string;
  color: string | null;
  /** "Vater" | "Sohn" – wer den Tag angelegt hat. */
  createdBy: string;
  exerciseCount: number;
  vocabularyCount: number;
  createdAt: string;
}

/** Ergebnis eines einzelnen Batch-Elements (POST /learn/vocabulary/batch). */
export interface VocabBatchResult {
  index: number;
  /** "created" | "existing" | "error". */
  status: string;
  id: number | null;
  key: string | null;
  error: string | null;
}

export interface CreateVocabularyDto {
  /** Optional – fehlt er, generiert der Server einen eindeutigen Slug ("einfache" Eingabe). */
  key?: string;
  sourceLanguage: string;
  targetLanguage: string;
  word: string;
  translation: string;
  /** Optional – Default Other ("einfache" Eingabe). */
  partOfSpeech?: PartOfSpeech;
  version?: string;
  noun?: NounInfo | null;
  verb?: VerbInfo | null;
  baseFormKey?: string | null;
  baseFormRelation?: string | null;
  pronunciationAudioUrl?: string | null;
}

// ---- Katalog: Übungen anlegen (Authoring) ----

/**
 * Übungstypen, für die diese UI einen Editor hat. Deckungsgleich mit den Server-Typen
 * (`ExerciseTypeKeys`) – das **Routen-Segment und der Anzeigename kommen aber aus dem Typ-Manifest**
 * (`GET creator/exercise-types`), nicht aus einer Tabelle hier: sie driften sonst auseinander, und der
 * Server weicht durchaus ab (Aufsatz liegt unter `essays`, nicht `essay`).
 */
export type ExerciseTypeKey =
  | "Vocabulary" | "Arithmetic" | "ArithmeticDrill" | "Cloze" | "Matching" | "List" | "Birkenbihl"
  | "Reading" | "Listening" | "Essay" | "Grammar" | "Translation";

/**
 * Selbstbeschreibung eines Übungstyps vom Server. Das Frontend liest sie einmal und verdrahtet daraus
 * Routen und Beschriftungen; die Editor-/Render-Komponente bleibt handgebaut je Typ (aus JSON allein
 * lässt sich kein Formular erzeugen, das die Fachlichkeit trifft).
 */
export interface ExerciseTypeManifest {
  type: string;
  /** Deutscher Anzeigename. */
  label: string;
  /** Id der Render-Komponente; mehrere Typen dürfen sie teilen (Arithmetic + ArithmeticDrill). */
  renderer: string;
  schemaVersion: number;
  /** Routen-Segment der Autoren-CRUD unter `.../chapters/{chapterId}/{authoringRoute}`. */
  authoringRoute: string;
  checkMode: ExerciseCheckMode;
  playRoute: string | null;
  method: string | null;
  /** Fähigkeiten, auf die ein Renderer reagieren kann (`audio`, `wordBank`, `rubric` …). */
  capabilities: string[];
}

/** Rechenarten des Rechen-Drills (der Server wählt je Aufgabe zufällig eine der erlaubten). */
export type ArithmeticOperation = "Addition" | "Subtraction" | "Multiplication" | "Division";

/**
 * Gemeinsame Nutzlast zum Anlegen einer Übung (spiegelt ExercisePayload&lt;TConfig&gt; im Backend).
 * <c>config</c> ist typ-spezifisch – der Server interpretiert es je Routen-Segment.
 */
export interface CreateExercisePayload {
  title: string;
  description?: string | null;
  orderIndex: number;
  rewardPoints: number;
  config: unknown;
  gradeMin?: number | null;
  gradeMax?: number | null;
  schoolTypes?: string;
  source?: string | null;
  categoryId?: number | null;
  defaultUseLeitner?: boolean;
  defaultRequireTypedTest?: boolean;
  /** Standard-Abfrageform (TestStage-Wert) – v. a. für Vokabeln (z. B. Multiple-Choice = 6). */
  defaultStage?: number | null;
  /** Empfohlene Anzahl genutzter Inhalte je Position; null = alle. */
  defaultItemCount?: number | null;
  /**
   * Bonus-Vorschlag. Beim **Ändern** unbedingt mitschicken: der Server ersetzt die Übung vollständig,
   * ein Weglassen würde den Vorschlag löschen.
   */
  suggestedBonus?: SuggestedBonus | null;
  /**
   * Für alle Väter zuweisbar? Beim **Ändern** den vorhandenen Wert mitschicken – der Server-Default ist
   * `true`, und ein Umschalten verlangt Owner-Rechte (sonst 403 für einen Write-Grantee).
   */
  executePublic?: boolean;
}

// ---- Vokabel-Items einer Übung (eigene Ebene, stabil identifiziert) ----

/** Ein positioniertes Vokabelpaar einer Übung; Front/Back kommen live aus dem Store. */
export interface VocabItemResponse {
  id: number;
  orderIndex: number;
  vocabularyId: number;
  front: string;
  back: string;
  hint: string | null;
}

/** Anlegen/Ändern eines Items: per Store-Id **oder** inline (`front`/`back` werden dann angelegt/gefunden). */
export interface VocabItemInput {
  vocabularyId?: number | null;
  front?: string | null;
  back?: string | null;
  hint?: string | null;
  orderIndex?: number | null;
}

// ---- Lehrpläne ----

/**
 * Lehrplan = reiner Container aus referenzierten Katalog-Übungen (Positionen). Ziele, Punkte, Stufen und
 * Leitner-Einstellungen leben an der jeweiligen {@link PositionResponse}, nicht mehr am Plan.
 */
export interface PlanResponse {
  id: number;
  childId: number;
  title: string;
  description: string | null;
  subjectId: number | null;
  startDate: string;
  endDate: string;
  active: boolean;
  positionCount: number;
  /**
   * Server-autoritative Affordance: Ob dies der eine, aktuell spielbare Plan des Kindes ist
   * (aktiv + heute in Laufzeit). Statt die „ein aktiver Plan"-Regel im Client nachzubilden.
   */
  isPlayable: boolean;
}

export interface CreatePlanDto {
  childId: number;
  title: string;
  description?: string | null;
  subjectId?: number | null;
  startDate?: string;
  durationDays: number;
}

// ---- Lehrplan-Positionen (neues, verfahrens-gemischtes Modell) ----

/** Ziel-Rhythmus einer Position: kein Pflichtziel / Tagesziel / Wochenziel. */
export type GoalCadence = "None" | "Daily" | "Weekly";
/** Umfang der Inhaltsauswahl einer Position aus dem Übungs-Pool. */
export type ItemScope = "All" | "New" | "Old";
/** Server-Reihenfolge, in der fällige Inhalte ausgespielt werden. */
export type PracticeOrder = "WeakestFirst" | "Serial" | "Random" | "NewestWeighted";

/** Stufen-Fahrplan-Eintrag (Tag → Stufe) einer Leitner-Position. */
export interface StageStep { dayNumber: number; stage: number; }

/** Eine Position eines Lehrplans: Verweis auf eine Katalog-Übung + eigene Ziele/Punkte/Leitner. */
export interface PositionResponse {
  id: number;
  studyPlanId: number;
  exerciseId: number;
  exerciseTitle: string;
  exerciseType: string;
  order: number;
  stage: number | null;
  itemCount: number | null;
  scope: ItemScope;
  cadence: GoalCadence;
  orderStrategy: PracticeOrder;
  goalThreshold: number | null;
  requireTypedTest: boolean;
  useLeitner: boolean;
  maxBox: number;
  boxIntervalDays: number[] | null;
  stageSchedule: StageStep[] | null;
  pointsGoalMet: number;
  /** Münz-Malus bei gerissener Pflicht-Periode (0 = kein Malus). Nur bei Cadence Tag/Woche wirksam. */
  penaltyCoins: number;
  newContentPoints: number;
  comboThreshold: number;
  comboBonusPoints: number;
  speedThresholdSeconds: number;
  speedBonusPoints: number;
}

/** Tagesstand eines Kindes im Vater-Dashboard (aggregiert über seine aktiven Lehrpläne). */
export interface ChildDay {
  childId: number;
  name: string;
  activePlans: number;
  goalsTotal: number;
  goalsMet: number;
  pointsToday: number;
  dutyDone: boolean;
  practiced: boolean;
}

/** Kindübergreifender Tagesüberblick des Vaters („wer hat heute was geschafft?"). */
export interface ChildrenDashboard {
  date: string;
  children: ChildDay[];
}

/** Eine Report-Zeile: wie gut ein einzelner Inhalt der Position „sitzt". */
export interface ItemReport {
  itemIndex: number;
  prompt: string;
  answer: string;
  introduced: boolean;
  box: number;
  masteryPercent: number;
  reviewCount: number;
  dueOn: string | null;
  lastReviewedAt: string | null;
  testsSeen: number;
  testsCorrect: number;
}

/** Lern-Report einer Position: „welche Vokabel sitzt/sitzt nicht" (Box/Beherrschung + Test-Trefferquote). */
export interface PositionReport {
  positionId: number;
  exerciseId: number;
  exerciseTitle: string;
  exerciseType: string;
  maxBox: number;
  totalItems: number;
  introducedItems: number;
  masteredItems: number;
  items: ItemReport[];
}

/** Anlegen einer Position. Leere Felder erben den Vorschlag der Übung (Hybrid-Prinzip). */
export interface CreatePositionDto {
  exerciseId: number;
  order?: number;
  stage?: number | null;
  itemCount?: number | null;
  scope?: ItemScope;
  cadence?: GoalCadence;
  orderStrategy?: PracticeOrder;
  goalThreshold?: number | null;
  requireTypedTest?: boolean;
  useLeitner?: boolean;
  maxBox?: number;
  pointsGoalMet?: number;
  /** Münz-Malus bei gerissener Pflicht-Periode (0 = kein Malus). */
  penaltyCoins?: number;
  /*
   * Bonus-Werte sind `null`-fähig, und der Unterschied trägt Bedeutung: `null` heißt „Bonus-Vorschlag der
   * Übung übernehmen" (Server: `dto.X ?? exercise.SuggestedBonus?.X ?? Default`), eine Zahl überschreibt ihn.
   */
  newContentPoints?: number | null;
  comboThreshold?: number | null;
  comboBonusPoints?: number | null;
  speedThresholdSeconds?: number | null;
  speedBonusPoints?: number | null;
}

/** Partielle Änderung einer Position (nur gesetzte Felder). */
export type UpdatePositionDto = Omit<Partial<CreatePositionDto>, "exerciseId"> & {
  boxIntervalDays?: number[] | null;
  stageSchedule?: StageStep[] | null;
};

// ---- Tagesmission / Fortschritt (über Positionen) ----

/** Prüf-/Spieloberfläche eines Übungstyps (aus dem Typ-Manifest). */
export type ExerciseCheckMode = "None" | "StudyPlanTest" | "CatalogCheck" | "CatalogGenerateCheck";

/** Status einer Position für einen Tag – steuert, welche Aktion der Sohn-Client anbietet. */
export interface PositionStatus {
  positionId: number;
  exerciseId: number;
  exerciseTitle: string;
  exerciseType: string;
  renderer: string;
  order: number;
  cadence: GoalCadence;
  checkMode: ExerciseCheckMode;
  useLeitner: boolean;
  testable: boolean;
  goalMet: boolean;
  dueCount: number;
  poolSize: number;
  pointsGoalMet: number;
}

/** Tages-Rollup eines Lehrplans über seine Positionen. */
export interface DayOverview {
  day: string;
  dutyDone: boolean;
  goalsTotal: number;
  goalsMet: number;
  pointsAwarded: number;
  outstanding: string[];
  positions: PositionStatus[];
}

/** Tagesmission des Sohns bzw. Ein-Blick-Status eines Plans. */
export interface OverviewResponse {
  planId: number;
  title: string;
  startDate: string;
  endDate: string;
  active: boolean;
  currentStreak: number;
  today: DayOverview;
}

/** Ein Tag im Verlauf (Vater-Auswertung). */
export interface ProgressDay {
  day: string;
  dutyDone: boolean;
  goalsTotal: number;
  goalsMet: number;
  pointsAwarded: number;
}

export interface ProgressResponse {
  planId: number;
  startDate: string;
  endDate: string;
  daysComplete: number;
  totalDays: number;
  totalPoints: number;
  currentStreak: number;
  days: ProgressDay[];
}

// ---- Positions-Üben (Leitner) ----

export type PlayMode = "Info" | "Lern";

export interface PositionSession {
  id: number;
  planId: number;
  positionId: number;
  day: string;
  startedAt: string;
  endedAt: string | null;
  activeSeconds: number;
  reviewCount: number;
  /** Ausspiel-Modus: `Info` = freies Üben ohne Feedback, `Lern` = server-geführt (Cursor). */
  mode: PlayMode;
  /** Aktuelle Cursor-Position im Lern-Modus. */
  cursor: number;
  /** Anzahl Karten in der eingefrorenen Reihenfolge. */
  total: number;
}

/**
 * Eine Übungskarte einer Position. `reveal` ist bei Anzeige-/Selbsteinschätzungs-Stufen die aufgedeckte
 * Lösung (Flip-Karte); bei getippten Stufen ist es `null` (Eingabefeld). `answerLength` nur bei
 * Vokabel-Buchstabenkästchen, `hint` nur bei getippten Stufen.
 */
export interface PracticeCard {
  itemIndex: number;
  stage: number;
  type: string;
  prompt: string;
  hint: string | null;
  answerLength: number | null;
  reveal: string | null;
  /** Nur bei Multiple-Choice: die Auswahlmöglichkeiten (Lösung + Ablenker, gemischt). */
  choices: string[] | null;
  /** Nur bei der Hör-Stufe: Aussprache-Audioquelle der Vokabel (Wort-Text wird dann ausgeblendet). */
  audioUrl: string | null;
  /**
   * Das für dieses Kind ausgewählte Bild. Nur auf nicht-getippten Stufen gesetzt – auf getippten
   * verriete ein Motiv die Lösung, deshalb liefert der Server dort weder URL noch Alt-Text.
   */
  imageUrl: string | null;
  imageAlt: string | null;
}

/** Antwort zu einer Übungskarte: getippt (`givenAnswer`) oder Selbsteinschätzung (`wasKnown`). */
export interface ReviewInput {
  itemIndex: number;
  givenAnswer?: string | null;
  wasKnown?: boolean | null;
}

export interface ReviewOutcome {
  wasCorrect: boolean;
  expected: string;
  awarded: number;
  box: number;
  dueOn: string | null;
  combo: number;
  comboBonus: number;
  speedBonus: number;
  /** Lern-Modus: die nächste Karte, direkt mitgeliefert (server-geführter Cursor); null am Ende. */
  next: PracticeCard | null;
  /** Lern-Modus: true, wenn der Lauf zu Ende ist (Cursor am Ende der eingefrorenen Reihenfolge). */
  done: boolean;
}

// ---- Missionen & Auszeichnungen (Gamification) ----

export type ProgressMetric =
  | "NewWords" | "CorrectReviews" | "TestsPassed" | "MinutesPracticed" | "DaysComplete" | "StreakDays";
export type MissionPeriod = "Daily" | "Weekly" | "OneOff";

export interface MissionStatus {
  id: number;
  title: string;
  metric: ProgressMetric;
  period: MissionPeriod;
  target: number;
  current: number;
  completed: boolean;
  rewardPoints: number;
}

export interface AchievementStatus {
  id: number;
  title: string;
  icon: string | null;
  metric: ProgressMetric;
  threshold: number;
  current: number;
  earned: boolean;
  earnedAt: string | null;
  rewardPoints: number;
}

// ---- Vater: Missionen & Auszeichnungen verwalten (Definitionen) ----

/** Missions-Definition zur Verwaltung durch den Vater. */
export interface MissionDef {
  id: number;
  title: string;
  metric: ProgressMetric;
  target: number;
  period: MissionPeriod;
  rewardPoints: number;
  active: boolean;
}
export interface CreateMissionDto {
  title: string;
  metric: ProgressMetric;
  target: number;
  period: MissionPeriod;
  rewardPoints: number;
}

/** Auszeichnungs-Definition zur Verwaltung durch den Vater. */
export interface AchievementDef {
  id: number;
  title: string;
  icon: string | null;
  metric: ProgressMetric;
  threshold: number;
  rewardPoints: number;
  active: boolean;
}
export interface CreateAchievementDto {
  title: string;
  icon: string | null;
  metric: ProgressMetric;
  threshold: number;
  rewardPoints: number;
}

// Hinweis: Das frühere Angebots-System (Reward/Redemption/OfferPeriod) wurde entfernt – der
// Familien-Shop (siehe unten) ist der einzige Münz-Ausgabeweg.

// ---- Familien-Shop (einziger Münz-Ausgabeweg) ----
// Enums werden serverseitig als Strings serialisiert (JsonStringEnumConverter).

/** Maßeinheit eines Shop-Artikels (z. B. Minuten Fernsehen, Gramm Süßigkeiten). */
export type UnitType = "Stueck" | "Minute" | "Stunde" | "Gramm" | "Mal";
/** Art der Belohnung, die ein Artikel repräsentiert (kategorisiert + bebildert). */
export type ActionType = "Sonstiges" | "TV" | "Zocken" | "Suessigkeit" | "Ausflug";
/** Automatische Auffüll-Regel eines Angebots. */
export type ShopRefillKind = "None" | "Once" | "Daily" | "TwiceDaily" | "Weekly";
export type ShopPurchaseStatus = "Owned" | "Cancelled";
export type ActivationRequestStatus = "Pending" | "Approved" | "Rejected";
/** Wochentag (C# DayOfWeek, string-serialisiert) – nur für wöchentliches Auffüllen relevant. */
export type DayOfWeek =
  | "Sunday" | "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday";

/** Katalog-Artikel des Vaters: die *Art* der Belohnung (Preis/Bestand liegen an den Angeboten). */
export interface ShopArticle {
  id: number;
  articleNumber: string;
  title: string;
  description: string;
  unitType: UnitType;
  actionType: ActionType;
  createdAt: string;
}
export interface CreateShopArticleDto {
  articleNumber: string;
  title: string;
  description?: string | null;
  unitType: UnitType;
  actionType: ActionType;
}
export interface UpdateShopArticleDto {
  articleNumber?: string;
  title?: string;
  description?: string | null;
  unitType?: UnitType;
  actionType?: ActionType;
}

/** Ein konkretes Angebot zu einem Artikel: Preis (Coin/Gem), Menge je Kauf und Bestand. */
export interface ShopListing {
  id: number;
  shopArticleId: number;
  articleNumber: string;
  articleTitle: string;
  title: string;
  description: string;
  coinPrice: number;
  gemPrice: number;
  unitsPerPurchase: number;
  active: boolean;
  currentStock: number;
  maxStock: number;
  refillKind: ShopRefillKind;
  refillAtUtc: string | null;
  refillDayOfWeek: DayOfWeek | null;
  lastRefilledAtUtc: string | null;
  createdAt: string;
}
export interface CreateShopListingDto {
  title?: string | null;
  description?: string | null;
  coinPrice: number;
  gemPrice: number;
  unitsPerPurchase: number;
  currentStock: number;
  maxStock: number;
  refillKind?: ShopRefillKind;
  refillAtUtc?: string | null;
  refillDayOfWeek?: DayOfWeek | null;
}
export interface UpdateShopListingDto {
  title?: string | null;
  description?: string | null;
  coinPrice?: number;
  gemPrice?: number;
  unitsPerPurchase?: number;
  active?: boolean;
  currentStock?: number;
  maxStock?: number;
  refillKind?: ShopRefillKind;
  refillAtUtc?: string | null;
  refillDayOfWeek?: DayOfWeek | null;
}

/** Aggregierter Inventar-Eintrag eines Kindes: Artikel-Typ → verfügbare Gesamtmenge. */
export interface InventoryItem {
  shopArticleId: number;
  articleNumber: string;
  title: string;
  unitType: UnitType;
  actionType: ActionType;
  quantity: number;
}

/** Kaufbuchung eines Kindes (Vater-Sicht, mit Stornier-Affordance). */
export interface ShopPurchase {
  id: number;
  childId: number;
  shopListingId: number | null;
  articleNumber: string;
  title: string;
  description: string;
  coinPrice: number;
  gemPrice: number;
  unitsPerPurchase: number;
  status: ShopPurchaseStatus;
  purchasedAt: string;
  closedAt: string | null;
  canCancel: boolean;
}

/** Aktivierungsanfrage eines Kindes (Vater-Sicht, mit Genehmigen/Ablehnen-Affordance). */
export interface ActivationRequest {
  id: number;
  childId: number;
  shopArticleId: number | null;
  articleTitle: string;
  unitType: UnitType;
  actionType: ActionType;
  requestedQuantity: number;
  status: ActivationRequestStatus;
  requestedAt: string;
  closedAt: string | null;
  canApprove: boolean;
  canReject: boolean;
}

// ---- Familien-Shop: Sohn-Sicht ----

/** Ein kaufbares Angebot aus Sohn-Sicht (`affordable` = reicht das aktuelle Guthaben?). */
export interface ShopAvailableListing {
  id: number;
  shopArticleId: number;
  articleNumber: string;
  articleTitle: string;
  unitType: UnitType;
  actionType: ActionType;
  title: string;
  description: string;
  coinPrice: number;
  gemPrice: number;
  unitsPerPurchase: number;
  currentStock: number;
  affordable: boolean;
}
/** Eigene Kaufbuchung aus Sohn-Sicht (Kassenbuch). */
export interface MyShopPurchase {
  id: number;
  shopListingId: number | null;
  articleNumber: string;
  title: string;
  coinPrice: number;
  gemPrice: number;
  unitsPerPurchase: number;
  status: ShopPurchaseStatus;
  purchasedAt: string;
  closedAt: string | null;
}
/** Eigene Aktivierungsanfrage aus Sohn-Sicht. */
export interface MyActivation {
  id: number;
  shopArticleId: number | null;
  articleTitle: string;
  unitType: UnitType;
  actionType: ActionType;
  requestedQuantity: number;
  status: ActivationRequestStatus;
  requestedAt: string;
  closedAt: string | null;
}
/** Gebündelte Shop-Sicht des Sohns (Salden + kaufbare Angebote + Inventar + Kaufhistorie). */
export interface ShopView {
  coins: number;
  gems: number;
  available: ShopAvailableListing[];
  inventory: InventoryItem[];
  purchases: MyShopPurchase[];
}

// ---- Vater: Klassenarbeiten ----

export type KlassenarbeitStatus = "Planned" | "Written" | "Cancelled";

export interface TagRef { id: number; name: string; color: string | null; }

/** Kurzform einer Übung aus dem Katalog (für Zuweisung/Üben). */
export interface ExerciseBrief {
  id: number;
  chapterId: number;
  chapterName: string;
  subjectId: number | null;
  subjectName: string;
  type: string;
  title: string;
  rewardPoints: number;
  config: unknown;
}

export interface KlassenarbeitResponse {
  id: number;
  childId: number;
  subjectId: number | null;
  subjectName: string | null;
  title: string;
  topic: string | null;
  scheduledDate: string;
  status: KlassenarbeitStatus;
  grade: number | null;
  gradeComment: string | null;
  directExerciseCount: number;
  tags: TagRef[];
  createdAt: string;
}

export interface KlassenarbeitDetail {
  klassenarbeit: KlassenarbeitResponse;
  assignedExercises: ExerciseBrief[];
}

export interface CreateKlassenarbeitDto {
  childId: number;
  title: string;
  topic?: string | null;
  subjectId?: number | null;
  scheduledDate: string;
  grade?: number | null;
}

export interface UpdateKlassenarbeitDto {
  title?: string;
  topic?: string | null;
  subjectId?: number;
  scheduledDate?: string;
  status?: KlassenarbeitStatus;
  grade?: number | null;
  clearGrade?: boolean;
  gradeComment?: string | null;
}

export interface KlassenarbeitPractice {
  klassenarbeitId: number;
  title: string;
  scheduledDate: string;
  daysUntil: number;
  exercises: ExerciseBrief[];
}

export interface KlassenarbeitRepeat {
  minBadGrade: number;
  sources: KlassenarbeitResponse[];
  exercises: ExerciseBrief[];
}

/** Partielle Lehrplan-Änderung durch den Vater (Datumsfelder als "YYYY-MM-DD"). */
export interface UpdatePlanDto {
  title?: string;
  description?: string | null;
  subjectId?: number | null;
  startDate?: string;
  endDate?: string;
  active?: boolean;
  /** Umzuweisung an ein anderes eigenes Kind. */
  childId?: number;
}

// ---- Positions-Test (Abschlusstest einer Übung) ----

/**
 * Eine im Positions-Test vorgelegte Aufgabe. `reveal` = aufgedeckte Lösung bei Anzeige-/Selbsteinschätzung,
 * `null` bei getippten Stufen; `answerLength` nur bei Vokabel-Buchstabenkästchen, `hint` nur getippt.
 */
export interface TestItem {
  itemIndex: number;
  prompt: string;
  stage: number;
  reveal: string | null;
  answerLength: number | null;
  hint: string | null;
  /** Nur bei Multiple-Choice: die Auswahlmöglichkeiten (Lösung + Ablenker, gemischt). */
  choices: string[] | null;
  /** Nur bei der Hör-Stufe: Aussprache-Audioquelle der Vokabel (Wort-Text wird dann ausgeblendet). */
  audioUrl: string | null;
  /** Wie bei der Übungskarte: nur auf nicht-getippten Stufen gesetzt. */
  imageUrl: string | null;
  imageAlt: string | null;
}

/**
 * Antwort des Test-Starts. Der Klausur-Modus ist strikt server-getrieben: es kommen KEINE Aufgaben im Bulk,
 * nur die Metadaten. Die Fragen holt der Client einzeln über `nextTest` (kein Zurück).
 */
export interface TestAttemptResponse {
  attemptId: number;
  planId: number;
  positionId: number;
  day: string;
  stage: number;
  totalItems: number;
}

/** Die nächste Prüfungsfrage (oder `done`), server-geführt über den Attempt-Cursor. */
export interface TestNextResponse {
  item: TestItem | null;
  done: boolean;
  cursor: number;
  total: number;
}

/** Bestätigung einer abgegebenen Prüfungsantwort – bewusst OHNE Korrektheit (Feedback erst beim Abschluss). */
export interface TestAnswerAck {
  done: boolean;
  cursor: number;
  total: number;
}

export interface AnswerDto {
  itemIndex: number;
  givenAnswer?: string | null;
  wasKnown?: boolean | null;
}

export interface ItemOutcome {
  itemIndex: number;
  prompt: string;
  expected: string;
  givenAnswer: string | null;
  wasCorrect: boolean;
}

export interface TestSubmitResponse {
  attemptId: number;
  stage: number;
  totalItems: number;
  correctItems: number;
  scorePercent: number;
  passed: boolean;
  passPercent: number;
  items: ItemOutcome[];
}

// ---- Sohn-Wallet ----

export type PointKind =
  | "Base" | "Manual" | "Minutes" | "Test" | "DayComplete" | "Goal"
  | "Combo" | "Speed" | "Duration" | "Mission" | "Achievement" | "SkinPurchase" | "Reward"
  | "ManualGems" | "GoalPenalty";

/** Die beiden Währungen der App (Münzen fürs echte Leben, Gems für Kosmetik). */
export type Currency = "Coins" | "Gems";

export interface WalletEntry {
  id: number;
  amount: number;
  kind: PointKind;
  reason: string;
  createdAt: string;
}

/** Reiner Kontostand des Sohns (GET me/points). Die Buchungen liegen unter me/points/entries. */
export interface WalletBalance {
  childId: number;
  coins: number;
  gems: number;
}

/** Kombinierte Konto-Sicht des Vaters (GET children/{id}/points): Salden + eingebettete Buchungen. */
export interface Wallet {
  childId: number;
  coins: number;
  gems: number;
  entries: WalletEntry[];
}

// ---- Sohn-Skins (server-autoritativer Besitz) ----

/** Skin-Zustand des Kindes vom Server: Gem-Stand, ausgerüsteter und freigeschaltete Skins. */
export interface SkinState {
  gems: number;
  selected: string;
  owned: string[];
}
