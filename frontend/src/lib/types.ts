// DTOs der Pugling-API (Ausschnitt für den Vokabel-Durchstich).
// Enums werden serverseitig als Strings serialisiert (JsonStringEnumConverter).

export type Role = "Vater" | "Sohn";

export type PartOfSpeech = "Noun" | "Verb" | "Adjective" | "Adverb" | "Other";

/**
 * Schularten – serverseitig ein [Flags]-Enum. Einzelwerte für Auswahl/Filter; der Server kann
 * bei einem Kind auch eine Kombination als kommaseparierten String liefern ("Realschule, Gymnasium").
 */
export type SchoolType =
  | "None" | "Grundschule" | "Hauptschule" | "Realschule" | "Gymnasium" | "Gesamtschule" | "Berufsschule";

export interface LoginResponse {
  token: string;
  role: Role;
  id: number;
  name: string;
  expiresAt: string;
}

// ---- Vater: Kinder & Vokabel-Store ----

export interface ChildResponse {
  id: number;
  fatherId: number;
  name: string;
  birthYear: number | null;
  grade: number | null;
  schoolType: string;
  createdAt: string;
  coins: number;
  gems: number;
}

export interface CreateChildDto {
  name: string;
  pin?: string;
  birthYear?: number | null;
  grade?: number | null;
  schoolType?: SchoolType;
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
  /** Autor der Übung (Vater); null = geseedete System-Übung. */
  authorFatherId: number | null;
  authorName: string | null;
  /** Gehört die Übung dem anfragenden Vater? Nur dann darf er sie ändern/löschen. */
  isOwn: boolean;
  config: unknown;
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
}
/** Spielbarer Zustand einer Übung im Testmodus. `typed` = Antwort wird getippt (sonst Selbsteinschätzung). */
export interface ExercisePreviewData { stage: number; typed: boolean; items: ExercisePreviewItem[]; }
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
  pronunciationAudioUrl?: string | null;
}

/** Schlanke Trefferzeile der Übungssuche (Metadaten-Filter über den Katalog). */
export interface ExerciseSummary {
  id: number;
  chapterId: number;
  subjectId: number;
  type: string;
  title: string;
  gradeMin: number | null;
  gradeMax: number | null;
  schoolTypes: string;
  source: string | null;
  categoryId: number | null;
  categoryName: string | null;
  /** Autor der Übung (Vater); null = geseedete System-Übung. Grundlage der „von …"-Attribution. */
  authorFatherId: number | null;
  authorName: string | null;
  /** Gehört die Übung dem anfragenden Vater? Nur dann darf er sie ändern/löschen. */
  isOwn: boolean;
}

export interface ExerciseSearchParams {
  subjectId?: number;
  grade?: number;
  schoolType?: SchoolType;
  categoryId?: number;
  type?: string;
  search?: string;
  /** Nur eigene Übungen des Vaters (Verwaltung statt Entdeckung der geteilten Bibliothek). */
  mineOnly?: boolean;
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
 * Kind-skopierter Tag (api/v1/tags) – markiert Übungen UND Vokabeln als für ein Kind relevant.
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
  pronunciationAudioUrl?: string | null;
}

// ---- Katalog: Übungen anlegen (Authoring) ----

/** Übungstypen, die die Vater-UI anlegen kann (Route-Segment siehe TYPE_ROUTE in VaterExercises). */
export type ExerciseTypeKey =
  | "Vocabulary" | "Arithmetic" | "Cloze" | "Matching" | "List" | "Birkenbihl";

/**
 * Gemeinsame Nutzlast zum Anlegen einer Übung (spiegelt ExercisePayload&lt;TConfig&gt; im Backend).
 * <c>config</c> ist typ-spezifisch – der Server interpretiert es je Routen-Segment.
 */
export interface CreateExercisePayload {
  title: string;
  orderIndex: number;
  rewardPoints: number;
  config: unknown;
  gradeMin?: number | null;
  gradeMax?: number | null;
  schoolTypes?: string;
  source?: string | null;
  categoryId?: number | null;
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
  subjectId: number | null;
  startDate: string;
  endDate: string;
  active: boolean;
  positionCount: number;
}

export interface CreatePlanDto {
  childId: number;
  title: string;
  subjectId?: number | null;
  startDate?: string;
  durationDays: number;
}

// ---- Lehrplan-Positionen (neues, verfahrens-gemischtes Modell) ----

/** Ziel-Rhythmus einer Position: kein Pflichtziel / Tagesziel / Wochenziel. */
export type GoalCadence = "None" | "Daily" | "Weekly";
/** Umfang der Inhaltsauswahl einer Position aus dem Übungs-Pool. */
export type ItemScope = "All" | "New" | "Old";

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
  goalThreshold: number | null;
  requireTypedTest: boolean;
  useLeitner: boolean;
  maxBox: number;
  boxIntervalDays: number[] | null;
  stageSchedule: StageStep[] | null;
  pointsGoalMet: number;
  newContentPoints: number;
  comboThreshold: number;
  comboBonusPoints: number;
  speedThresholdSeconds: number;
  speedBonusPoints: number;
}

/** Anlegen einer Position. Leere Felder erben den Vorschlag der Übung (Hybrid-Prinzip). */
export interface CreatePositionDto {
  exerciseId: number;
  order?: number;
  stage?: number | null;
  itemCount?: number | null;
  scope?: ItemScope;
  cadence?: GoalCadence;
  goalThreshold?: number | null;
  requireTypedTest?: boolean;
  useLeitner?: boolean;
  maxBox?: number;
  pointsGoalMet?: number;
  newContentPoints?: number;
  comboThreshold?: number;
  comboBonusPoints?: number;
  speedThresholdSeconds?: number;
  speedBonusPoints?: number;
}

/** Partielle Änderung einer Position (nur gesetzte Felder). */
export type UpdatePositionDto = Partial<CreatePositionDto>;

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

export interface PositionSession {
  id: number;
  planId: number;
  positionId: number;
  day: string;
  startedAt: string;
  endedAt: string | null;
  activeSeconds: number;
  reviewCount: number;
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

// ---- Angebote (kaufbare reale Belohnungen wie Spielzeit/Taschengeld) ----

/** Kauf-Stand im Konto: gekauft (Münzen weg), vom Vater erfüllt, oder storniert (rückerstattet). */
export type RewardRedemptionStatus = "Purchased" | "Fulfilled" | "Cancelled";
/** Wiederkehr eines Angebots – bestimmt das Kontingent-Fenster. */
export type OfferPeriod = "OneOff" | "Daily" | "Weekly" | "Monthly";

/** Angebots-Definition zur Verwaltung durch den Vater. */
export interface RewardDef {
  id: number;
  title: string;
  cost: number;
  period: OfferPeriod;
  quantity: number;
  active: boolean;
}
export interface CreateRewardDto {
  title: string;
  cost: number;
  period?: OfferPeriod;
  quantity?: number;
}

/** Kauf aus Vater-Sicht (mit Kind-Bezug). */
export interface RedemptionDef {
  id: number;
  childId: number;
  rewardId: number | null;
  title: string;
  cost: number;
  status: RewardRedemptionStatus;
  purchasedAt: string;
  fulfilledAt: string | null;
}

/** Verfügbares Angebot aus Sohn-Sicht. */
export interface RewardOffer {
  id: number;
  title: string;
  cost: number;
  period: OfferPeriod;
  quantity: number;
  remainingThisPeriod: number;
  affordable: boolean;
}

/** Eigener Kauf aus Sohn-Sicht. */
export interface MyRedemption {
  id: number;
  rewardId: number | null;
  title: string;
  cost: number;
  status: RewardRedemptionStatus;
  purchasedAt: string;
  fulfilledAt: string | null;
}

/** Angebots-Sicht des Sohns: Münzstand, verfügbare Angebote, eigene Käufe. */
export interface RewardsView {
  coins: number;
  available: RewardOffer[];
  redemptions: MyRedemption[];
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
  subjectId?: number | null;
  startDate?: string;
  endDate?: string;
  active?: boolean;
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
}

export interface TestAttemptResponse {
  attemptId: number;
  planId: number;
  positionId: number;
  day: string;
  stage: number;
  totalItems: number;
  items: TestItem[];
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
  | "Combo" | "Speed" | "Duration" | "Mission" | "Achievement" | "SkinPurchase" | "Reward";

export interface WalletEntry {
  id: number;
  amount: number;
  kind: PointKind;
  reason: string;
  createdAt: string;
}

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
