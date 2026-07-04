// DTOs der Pugling-API (Ausschnitt für den Vokabel-Durchstich).
// Enums werden serverseitig als Strings serialisiert (JsonStringEnumConverter).

export type Role = "Vater" | "Sohn";

export type LearningMethod = "Vocabulary" | "Cloze" | "Matching";
export type TestStage = "ShowBoth" | "SelfAssess" | "LetterBoxes" | "FreeText" | "Audio";
export type LessonDayMode = "New" | "Review";
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
}

export interface ExerciseSearchParams {
  subjectId?: number;
  grade?: number;
  schoolType?: SchoolType;
  categoryId?: number;
  type?: string;
  search?: string;
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
  createdAt: string;
}

export interface CreateVocabularyDto {
  key: string;
  sourceLanguage: string;
  targetLanguage: string;
  word: string;
  translation: string;
  partOfSpeech: PartOfSpeech;
}

// ---- Lehrpläne ----

export interface PlanItemResponse {
  id: number;
  order: number;
  kind: LearningMethod;
  contentId: number;
  ref: string;
  label: string;
  detail: string;
  introducedAt: string | null;
  box: number;
  dueOn: string | null;
}

export interface PlanResponse {
  id: number;
  childId: number;
  method: LearningMethod;
  title: string;
  subjectId: number | null;
  newItemsPerLesson: number;
  startDate: string;
  endDate: string;
  dailyMinutesRequired: number;
  dailyTestRequired: boolean;
  dailyTestPassPercent: number;
  defaultStage: number;
  requireTypedTest: boolean;
  useLeitner: boolean;
  maxBox: number;
  pointsMinutesMet: number;
  pointsTestPassed: number;
  pointsDayCompleteBonus: number;
  comboThreshold: number;
  comboBonusPoints: number;
  active: boolean;
  items: PlanItemResponse[];
}

export interface CreatePlanDto {
  childId: number;
  title: string;
  method?: LearningMethod;
  subjectId?: number | null;
  newItemsPerLesson?: number;
  startDate?: string;
  durationDays: number;
  contentKeys?: string[];
  dailyMinutesRequired?: number;
  dailyTestPassPercent?: number;
  defaultStage?: number;
  requireTypedTest?: boolean;
  useLeitner?: boolean;
  maxBox?: number;
  pointsMinutesMet?: number;
  pointsTestPassed?: number;
  pointsDayCompleteBonus?: number;
  comboThreshold?: number;
  comboBonusPoints?: number;
}

// ---- Tagesstatus / Fortschritt ----

export interface DayProgress {
  day: string;
  minutesPracticed: number;
  minutesMet: boolean;
  testAttempts: number;
  bestScorePercent: number | null;
  testPassed: boolean;
  dayComplete: boolean;
  pointsAwarded: number;
  outstanding: string[];
}

export interface ItemStat {
  contentId: number;
  ref: string;
  label: string;
  detail: string;
  timesReviewed: number;
  reviewCorrect: number;
  timesTested: number;
  testCorrect: number;
  masteryPercent: number;
  lastSeen: string | null;
  box: number;
  dueOn: string | null;
}

export interface TodayResponse {
  planId: number;
  method: LearningMethod;
  day: string;
  dutyDone: boolean;
  recommendedStage: number;
  mode: LessonDayMode | null;
  isPreparationDay: boolean;
  scheduleReason: string;
  currentStreak: number;
  progress: DayProgress;
  dueItems: PlanItemResponse[];
  weakItems: ItemStat[];
}

export interface ProgressResponse {
  planId: number;
  startDate: string;
  endDate: string;
  daysComplete: number;
  totalDays: number;
  totalPoints: number;
  currentStreak: number;
  days: DayProgress[];
}

// ---- Übungssession (Leitner) ----

export interface SessionResponse {
  id: number;
  planId: number;
  day: string;
  startedAt: string;
  endedAt: string | null;
  activeSeconds: number;
  reviewCount: number;
}

/** Antwort einer einzelnen Lücke eines Lückentexts (positionsbezogen). */
export interface GapAnswerInput {
  gapIndex: number;
  givenAnswer: string | null;
}

/**
 * Vom Kind abgegebene Antwort zu einer Übungskarte. Getippte Vokabel-/Zuordnungs-Stufen liefern
 * `givenAnswer`, Lückentexte `gaps`, reine Anzeige-/Selbsteinschätzungs-Stufen `wasKnown`.
 * Die Stufe bestimmt der Server aus dem Fahrplan – nie das Frontend.
 */
export interface ReviewInput {
  contentId: number;
  givenAnswer?: string | null;
  gaps?: GapAnswerInput[];
  wasKnown?: boolean;
}

export interface ReviewOutcome {
  wasCorrect: boolean;
  expected: string | null;
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
  startDate?: string;
  endDate?: string;
  newItemsPerLesson?: number;
  dailyMinutesRequired?: number;
  dailyTestPassPercent?: number;
  active?: boolean;
}

// ---- Vokabel-Test ----

export interface TestItem {
  vocabularyId: number;
  prompt: string;
  stage: TestStage;
  translation: string | null;
  answerLength: number | null;
  audioUrl: string | null;
}

export interface TestAttemptResponse {
  attemptId: number;
  planId: number;
  day: string;
  stage: TestStage;
  totalItems: number;
  items: TestItem[];
}

export interface AnswerDto {
  vocabularyId: number;
  givenAnswer?: string | null;
  wasKnown?: boolean | null;
}

export interface ItemOutcome {
  vocabularyId: number;
  word: string;
  expectedTranslation: string;
  givenAnswer: string | null;
  wasCorrect: boolean;
  hintsUsed: number;
}

export interface TestSubmitResponse {
  attemptId: number;
  stage: TestStage;
  totalItems: number;
  correctItems: number;
  scorePercent: number;
  passed: boolean;
  dayProgress: DayProgress;
  items: ItemOutcome[];
}

// ---- Sohn-Wallet ----

export type PointKind =
  | "Base" | "Manual" | "Minutes" | "Test" | "DayComplete"
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
