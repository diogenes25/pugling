// DTOs der Pugling-API (Ausschnitt für den Vokabel-Durchstich).
// Enums werden serverseitig als Strings serialisiert (JsonStringEnumConverter).

export type Role = "Vater" | "Sohn";

export type LearningMethod = "Vocabulary" | "Cloze" | "Matching";
export type TestStage = "ShowBoth" | "SelfAssess" | "LetterBoxes" | "FreeText" | "Audio";
export type LessonDayMode = "New" | "Review";
export type PartOfSpeech = "Noun" | "Verb" | "Adjective" | "Adverb" | "Other";

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
  createdAt: string;
  pointsBalance: number;
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

export interface ReviewOutcome {
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
  | "Combo" | "Speed" | "Duration" | "Mission" | "Achievement";

export interface WalletEntry {
  id: number;
  amount: number;
  kind: PointKind;
  reason: string;
  createdAt: string;
}

export interface Wallet {
  childId: number;
  balance: number;
  entries: WalletEntry[];
}
