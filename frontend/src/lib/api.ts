import type {
  AchievementDef, AchievementStatus, AnswerDto, ChapterResponse, ChildResponse, CreateAchievementDto,
  CreateChildDto, CreateExercisePayload, CreateKlassenarbeitDto, CreateMissionDto, CreatePlanDto, CreateRewardDto, CreateVocabularyDto,
  ExerciseDetail, ExercisePreviewAnswer, ExercisePreviewData, ExercisePreviewResult,
  ExerciseSearchParams, ExerciseSummary, ExerciseUsage, KlassenarbeitDetail, KlassenarbeitPractice, KlassenarbeitRepeat,
  KlassenarbeitResponse, KlassenarbeitStatus, LoginResponse, MissionDef, MissionStatus, PlanResponse,
  CreatePositionDto, PositionResponse, UpdatePositionDto, OverviewResponse, PositionSession, PracticeCard,
  ProgressResponse, RedemptionDef, ReviewInput, ReviewOutcome, RewardDef, RewardRedemptionStatus, RewardsView,
  SkinState, SubjectResponse,
  TestAttemptResponse, TestSubmitResponse, UpdateKlassenarbeitDto, UpdatePlanDto, UpdateVocabularyDto,
  VocabBatchResult, VocabularyResponse, VocabTagResponse, ChildTagResponse, Wallet,
} from "./types";

const TOKEN_KEY = "pugling.token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}
export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

/**
 * Fehler mit HTTP-Status, damit die UI 401 (Session weg) von 4xx (Eingabe) trennen kann.
 * `traceId` (aus dem RFC-7807-Body) korreliert die Meldung mit den Server-Logs – im Supportfall
 * kann der Nutzer diese Referenz nennen.
 */
export class ApiError extends Error {
  constructor(public status: number, message: string, public traceId?: string) {
    super(message);
    this.name = "ApiError";
  }
}

/** Menschlich lesbare Fehlermeldung inkl. Trace-Referenz, wo vorhanden. */
export function errorMessage(e: unknown): string {
  if (e instanceof ApiError)
    return e.traceId ? `${e.message} (Ref: ${e.traceId})` : e.message;
  return e instanceof Error ? e.message : String(e);
}

async function http<T>(url: string, method = "GET", body?: unknown): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(url, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!res.ok) {
    const raw = await res.text().catch(() => "");
    // Die API antwortet einheitlich als application/problem+json (RFC 7807): detail/title als
    // Klartext, traceId zur Korrelation. Bei Nicht-JSON den Rohtext behalten.
    let message = raw || `${res.status} ${res.statusText}`;
    let traceId: string | undefined;
    if (res.headers.get("content-type")?.includes("json") && raw) {
      try {
        const problem = JSON.parse(raw) as { detail?: string; title?: string; traceId?: string };
        message = problem.detail || problem.title || message;
        traceId = problem.traceId;
      } catch {
        /* kein valides JSON – Rohtext behalten */
      }
    }
    throw new ApiError(res.status, message, traceId);
  }
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

// Alle Routen liegen unter dem API-Versionssegment (Backend: ApiRoutes.V1 = "api/v{version}").
// Zentral hier gehalten, damit ein künftiger v2-Umzug nur eine Stelle betrifft.
const V1 = "/api/v1";

export const api = {
  // ---- Auth ----
  loginFather: (fatherId: number, pin: string) =>
    http<LoginResponse>(`${V1}/auth/father`, "POST", { fatherId, pin }),
  loginChild: (childId: number, pin: string) =>
    http<LoginResponse>(`${V1}/auth/child`, "POST", { childId, pin }),

  // ---- Vater: Kinder (der Vater ergibt sich serverseitig aus dem JWT) ----
  children: () => http<ChildResponse[]>(`${V1}/children`),
  createChild: (dto: CreateChildDto) => http<ChildResponse>(`${V1}/children`, "POST", dto),
  updateChild: (childId: number, dto: Partial<CreateChildDto>) =>
    http<ChildResponse>(`${V1}/children/${childId}`, "PATCH", dto),

  // ---- Vater: Katalog (Fächer, Kapitel, Übungssuche über Metadaten) ----
  subjects: () => http<SubjectResponse[]>(`${V1}/learn/subjects`),
  createSubject: (name: string) => http<SubjectResponse>(`${V1}/learn/subjects`, "POST", { name }),
  chapters: (subjectId: number) =>
    http<ChapterResponse[]>(`${V1}/learn/subjects/${subjectId}/chapters`),
  createChapter: (subjectId: number, name: string, orderIndex: number) =>
    http<ChapterResponse>(`${V1}/learn/subjects/${subjectId}/chapters`, "POST", { name, orderIndex }),
  // Übung eines Typs im Kapitel anlegen. Das Routen-Segment (vocabulary/arithmetic/…) bestimmt den Typ.
  createExercise: (subjectId: number, chapterId: number, typeRoute: string, payload: CreateExercisePayload) =>
    http<ExerciseSummary>(`${V1}/learn/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}`, "POST", payload),
  // Typ-übergreifender Detail-Abruf (mit Config) + „wo verwendet".
  getExercise: (id: number) => http<ExerciseDetail>(`${V1}/learn/exercises/${id}`),
  exerciseUsage: (id: number) => http<ExerciseUsage>(`${V1}/learn/exercises/${id}/usage`),
  // Testmodus: eine Übung nebenwirkungsfrei durchspielen (keine Punkte/kein Fortschritt) und bewerten lassen.
  previewExercise: (id: number) => http<ExercisePreviewData>(`${V1}/learn/exercises/${id}/preview`),
  checkPreviewExercise: (id: number, answers: ExercisePreviewAnswer[]) =>
    http<ExercisePreviewResult>(`${V1}/learn/exercises/${id}/preview/check`, "POST", { answers }),
  // Ersetzen (PUT) bzw. Löschen laufen über die per-Typ-Route.
  updateExercise: (subjectId: number, chapterId: number, typeRoute: string, id: number, payload: CreateExercisePayload) =>
    http<ExerciseSummary>(`${V1}/learn/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}/${id}`, "PUT", payload),
  deleteExercise: (subjectId: number, chapterId: number, typeRoute: string, id: number) =>
    http<void>(`${V1}/learn/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}/${id}`, "DELETE"),
  searchExercises: (p: ExerciseSearchParams = {}) => {
    const q = new URLSearchParams();
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
    if (p.grade != null) q.set("grade", String(p.grade));
    if (p.schoolType && p.schoolType !== "None") q.set("schoolType", p.schoolType);
    if (p.categoryId != null) q.set("categoryId", String(p.categoryId));
    if (p.type) q.set("type", p.type);
    if (p.search) q.set("search", p.search);
    if (p.mineOnly) q.set("mineOnly", "true");
    const qs = q.toString();
    return http<ExerciseSummary[]>(`${V1}/learn/exercises${qs ? `?${qs}` : ""}`);
  },

  // ---- Vater: Vokabel-Store ----
  // Optional nach Sprachpaar filtern (Store zeigt dann nur die gewählte Kombination).
  vocabulary: (search?: string, opts?: { sourceLanguage?: string; targetLanguage?: string }) => {
    const q = new URLSearchParams();
    if (search) q.set("search", search);
    if (opts?.sourceLanguage) q.set("sourceLanguage", opts.sourceLanguage);
    if (opts?.targetLanguage) q.set("targetLanguage", opts.targetLanguage);
    const qs = q.toString();
    return http<VocabularyResponse[]>(`${V1}/learn/vocabulary${qs ? `?${qs}` : ""}`);
  },
  createVocabulary: (dto: CreateVocabularyDto) =>
    http<VocabularyResponse>(`${V1}/learn/vocabulary`, "POST", dto),
  // Viele Paare in einem Aufruf (idempotent) – für die zeilenweise Paar-Eingabe.
  createVocabularyBatch: (items: CreateVocabularyDto[]) =>
    http<VocabBatchResult[]>(`${V1}/learn/vocabulary/batch`, "POST", items),
  updateVocabulary: (id: number, patch: UpdateVocabularyDto) =>
    http<VocabularyResponse>(`${V1}/learn/vocabulary/${id}`, "PATCH", patch),
  deleteVocabulary: (id: number) => http<void>(`${V1}/learn/vocabulary/${id}`, "DELETE"),

  // ---- Globale (kindneutrale) Vokabel-Tags ----
  vocabTags: () => http<VocabTagResponse[]>(`${V1}/learn/vocabulary/tags`),
  // Verknüpft eine Vokabel mit Tag-Namen (create-if-missing); liefert die aktuellen Tags der Vokabel.
  attachVocabTags: (vocabId: number, tags: string[]) =>
    http<VocabTagResponse[]>(`${V1}/learn/vocabulary/${vocabId}/tags`, "POST", { tags }),
  detachVocabTag: (vocabId: number, tagId: number) =>
    http<void>(`${V1}/learn/vocabulary/${vocabId}/tags/${tagId}`, "DELETE"),

  // ---- Kind-skopierte Tags (auch an Vokabeln) ----
  childTags: (childId: number) => http<ChildTagResponse[]>(`${V1}/tags?childId=${childId}`),
  createChildTag: (dto: { childId: number; name: string; color?: string | null }) =>
    http<ChildTagResponse>(`${V1}/tags`, "POST", dto),
  tagsForVocabulary: (vocabId: number, childId: number) =>
    http<ChildTagResponse[]>(`${V1}/tags/for-vocabulary/${vocabId}?childId=${childId}`),
  tagVocabulary: (tagId: number, vocabularyIds: number[]) =>
    http<ChildTagResponse>(`${V1}/tags/${tagId}/vocabulary`, "POST", { vocabularyIds }),
  untagVocabulary: (tagId: number, vocabId: number) =>
    http<void>(`${V1}/tags/${tagId}/vocabulary/${vocabId}`, "DELETE"),

  // ---- Lehrpläne (reiner Container; Ziele/Punkte je Position) ----
  plans: (childId?: number) =>
    http<PlanResponse[]>(`${V1}/study-plans${childId ? `?childId=${childId}` : ""}`),
  plan: (planId: number) => http<PlanResponse>(`${V1}/study-plans/${planId}`),
  createPlan: (dto: CreatePlanDto) => http<PlanResponse>(`${V1}/study-plans`, "POST", dto),
  // Lehrplan nachträglich umbenennen/verlängern/deaktivieren (Inhalte laufen über Positionen).
  updatePlan: (planId: number, dto: UpdatePlanDto) =>
    http<PlanResponse>(`${V1}/study-plans/${planId}`, "PATCH", dto),

  // ---- Lehrplan-Positionen (Plan = Container aus Katalog-Übungen) ----
  positions: (planId: number) =>
    http<PositionResponse[]>(`${V1}/study-plans/${planId}/positions`),
  addPosition: (planId: number, dto: CreatePositionDto) =>
    http<PositionResponse>(`${V1}/study-plans/${planId}/positions`, "POST", dto),
  updatePosition: (planId: number, positionId: number, dto: UpdatePositionDto) =>
    http<PositionResponse>(`${V1}/study-plans/${planId}/positions/${positionId}`, "PATCH", dto),
  deletePosition: (planId: number, positionId: number) =>
    http<void>(`${V1}/study-plans/${planId}/positions/${positionId}`, "DELETE"),

  // ---- Tagesmission (Sohn) / Verlauf (Vater) über Positionen ----
  overview: (planId: number) => http<OverviewResponse>(`${V1}/study-plans/${planId}/overview`),
  overviewProgress: (planId: number) => http<ProgressResponse>(`${V1}/study-plans/${planId}/overview/progress`),

  // ---- Sohn: Position üben (Leitner) ----
  startSession: (planId: number, positionId: number) =>
    http<PositionSession>(`${V1}/study-plans/${planId}/positions/${positionId}/practice-sessions`, "POST", {}),
  heartbeat: (planId: number, positionId: number, sessionId: number, seconds: number, active: boolean) =>
    http<PositionSession>(
      `${V1}/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/heartbeat`, "POST", { seconds, active }),
  cards: (planId: number, positionId: number, sessionId: number) =>
    http<PracticeCard[]>(`${V1}/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/cards`),
  // Der Server bewertet serverseitig: das Frontend liefert nur die Antwort (getippt) bzw. bei
  // Anzeige-/Selbsteinschätzungs-Stufen das WasKnown-Flag; die Stufe erzwingt der Server.
  review: (planId: number, positionId: number, sessionId: number, dto: ReviewInput) =>
    http<ReviewOutcome>(
      `${V1}/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/review`, "POST", dto),
  endSession: (planId: number, positionId: number, sessionId: number) =>
    http<PositionSession>(
      `${V1}/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/end`, "POST", {}),

  // ---- Sohn: Position testen (Abschlusstest einer Übung) ----
  startTest: (planId: number, positionId: number) =>
    http<TestAttemptResponse>(`${V1}/study-plans/${planId}/positions/${positionId}/tests`, "POST", {}),
  submitTest: (planId: number, positionId: number, attemptId: number, answers: AnswerDto[]) =>
    http<TestSubmitResponse>(`${V1}/study-plans/${planId}/positions/${positionId}/tests/${attemptId}/submit`, "POST", { answers }),

  // ---- Sohn: Wallet ----
  wallet: () => http<Wallet>(`${V1}/me/points`),

  // ---- Sohn: Missionen & Auszeichnungen ----
  missions: () => http<MissionStatus[]>(`${V1}/me/missions`),
  achievements: () => http<AchievementStatus[]>(`${V1}/me/achievements`),

  // ---- Sohn: Skins (Besitz server-autoritativ; Kauf bucht Münzen ab) ----
  skins: () => http<SkinState>(`${V1}/me/skins`),
  purchaseSkin: (skinId: string) => http<SkinState>(`${V1}/me/skins/${skinId}/purchase`, "POST", {}),
  equipSkin: (skinId: string) => http<SkinState>(`${V1}/me/skins/${skinId}/equip`, "POST", {}),

  // ---- Vater: Missionen (Belohnungsziele) je Kind verwalten ----
  missionsFor: (childId: number) => http<MissionDef[]>(`${V1}/children/${childId}/missions`),
  createMission: (childId: number, dto: CreateMissionDto) =>
    http<MissionDef>(`${V1}/children/${childId}/missions`, "POST", dto),
  updateMission: (childId: number, missionId: number, dto: Partial<MissionDef>) =>
    http<MissionDef>(`${V1}/children/${childId}/missions/${missionId}`, "PATCH", dto),
  deleteMission: (childId: number, missionId: number) =>
    http<void>(`${V1}/children/${childId}/missions/${missionId}`, "DELETE"),

  // ---- Vater: Auszeichnungen (Badges) je Kind verwalten ----
  achievementsFor: (childId: number) => http<AchievementDef[]>(`${V1}/children/${childId}/achievements`),
  createAchievement: (childId: number, dto: CreateAchievementDto) =>
    http<AchievementDef>(`${V1}/children/${childId}/achievements`, "POST", dto),
  updateAchievement: (childId: number, achievementId: number, dto: Partial<AchievementDef>) =>
    http<AchievementDef>(`${V1}/children/${childId}/achievements/${achievementId}`, "PATCH", dto),
  deleteAchievement: (childId: number, achievementId: number) =>
    http<void>(`${V1}/children/${childId}/achievements/${achievementId}`, "DELETE"),

  // ---- Vater: Konto-Übersicht (Punktestand + Buchungsverlauf je Kind) ----
  childAccount: (childId: number) => http<Wallet>(`${V1}/children/${childId}/points`),

  // ---- Vater: Angebote (kaufbare reale Belohnungen) verwalten ----
  rewardsFor: (childId: number) => http<RewardDef[]>(`${V1}/children/${childId}/rewards`),
  createReward: (childId: number, dto: CreateRewardDto) =>
    http<RewardDef>(`${V1}/children/${childId}/rewards`, "POST", dto),
  updateReward: (childId: number, rewardId: number, dto: Partial<RewardDef>) =>
    http<RewardDef>(`${V1}/children/${childId}/rewards/${rewardId}`, "PATCH", dto),
  deleteReward: (childId: number, rewardId: number) =>
    http<void>(`${V1}/children/${childId}/rewards/${rewardId}`, "DELETE"),

  // ---- Vater: Käufe ansehen und erfüllen/stornieren ----
  redemptionsFor: (childId: number, status?: RewardRedemptionStatus) => {
    const q = status ? `?status=${status}` : "";
    return http<RedemptionDef[]>(`${V1}/children/${childId}/rewards/redemptions${q}`);
  },
  fulfillRedemption: (childId: number, redemptionId: number) =>
    http<RedemptionDef>(`${V1}/children/${childId}/rewards/redemptions/${redemptionId}/fulfill`, "POST", {}),
  cancelRedemption: (childId: number, redemptionId: number) =>
    http<RedemptionDef>(`${V1}/children/${childId}/rewards/redemptions/${redemptionId}/cancel`, "POST", {}),

  // ---- Sohn: Angebote ansehen und direkt kaufen (Münzen sofort weg, Vater erfüllt später) ----
  myRewards: () => http<RewardsView>(`${V1}/me/rewards`),
  purchaseReward: (rewardId: number) => http<RewardsView>(`${V1}/me/rewards/${rewardId}/purchase`, "POST", {}),

  // ---- Vater: Klassenarbeiten (planen, Übungen zuweisen, benoten, üben/wiederholen) ----
  classTests: (childId: number, status?: KlassenarbeitStatus) => {
    const q = new URLSearchParams({ childId: String(childId) });
    if (status) q.set("status", status);
    return http<KlassenarbeitResponse[]>(`${V1}/class-tests?${q.toString()}`);
  },
  classTest: (id: number) => http<KlassenarbeitDetail>(`${V1}/class-tests/${id}`),
  createClassTest: (dto: CreateKlassenarbeitDto) =>
    http<KlassenarbeitDetail>(`${V1}/class-tests`, "POST", dto),
  updateClassTest: (id: number, dto: UpdateKlassenarbeitDto) =>
    http<KlassenarbeitResponse>(`${V1}/class-tests/${id}`, "PATCH", dto),
  deleteClassTest: (id: number) => http<void>(`${V1}/class-tests/${id}`, "DELETE"),
  assignClassTestExercises: (id: number, exerciseIds: number[]) =>
    http<KlassenarbeitDetail>(`${V1}/class-tests/${id}/exercises`, "POST", { exerciseIds }),
  unassignClassTestExercise: (id: number, exerciseId: number) =>
    http<void>(`${V1}/class-tests/${id}/exercises/${exerciseId}`, "DELETE"),
  classTestPractice: (id: number) => http<KlassenarbeitPractice>(`${V1}/class-tests/${id}/practice`),
  classTestRepeat: (childId: number, minBadGrade?: number) => {
    const q = new URLSearchParams({ childId: String(childId) });
    if (minBadGrade != null) q.set("minBadGrade", String(minBadGrade));
    return http<KlassenarbeitRepeat>(`${V1}/class-tests/repeat?${q.toString()}`);
  },
};
