import type {
  AchievementDef, AchievementStatus, AnswerDto, CategoryResponse, ChapterResponse, ChildResponse, CreateAchievementDto,
  CreateChildDto, CreateExercisePayload, CreateKlassenarbeitDto, CreateMissionDto, CreatePlanDto, CreateVocabularyDto,
  ExerciseDetail, ExercisePreviewAnswer, ExercisePreviewData, ExercisePreviewResult,
  ExerciseSearchParams, ExerciseSummary, ExerciseUsage, KlassenarbeitDetail, KlassenarbeitPractice, KlassenarbeitRepeat,
  KlassenarbeitResponse, KlassenarbeitStatus, LoginResponse, MissionDef, MissionStatus, PlanResponse,
  ChildrenDashboard, CreatePositionDto, PositionResponse, PositionReport, UpdatePositionDto, OverviewResponse, PositionSession, PracticeCard,
  ProgressResponse, ReviewInput, ReviewOutcome,
  SkinState, SubjectResponse,
  TestAttemptResponse, TestNextResponse, TestAnswerAck, TestSubmitResponse, UpdateKlassenarbeitDto, UpdatePlanDto, UpdateVocabularyDto,
  VocabBatchResult, VocabularyResponse, VocabTagResponse, ChildTagResponse, Wallet, WalletBalance, WalletEntry,
  Paged, VocabularySearchParams,
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
  constructor(public status: number, message: string, public traceId?: string, public code?: string) {
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

// Ein Request inkl. Token + einheitlicher RFC-7807-Fehlerbehandlung; liefert die rohe Response,
// damit sowohl der Body-Parser (`http`) als auch der paginierte Helfer (`httpPaged`, liest zusätzlich
// den `X-Total-Count`-Header) dieselbe Logik teilen.
async function request(url: string, method: string, body?: unknown): Promise<Response> {
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
    let code: string | undefined;
    if (res.headers.get("content-type")?.includes("json") && raw) {
      try {
        const problem = JSON.parse(raw) as { detail?: string; title?: string; traceId?: string; code?: string };
        message = problem.detail || problem.title || message;
        traceId = problem.traceId;
        code = problem.code;
      } catch {
        /* kein valides JSON – Rohtext behalten */
      }
    }
    throw new ApiError(res.status, message, traceId, code);
  }
  return res;
}

async function http<T>(url: string, method = "GET", body?: unknown): Promise<T> {
  const res = await request(url, method, body);
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/**
 * Wie {@link http}, aber für Server-paginierte Listen: Das Backend paginiert per `skip`/`take` und
 * schreibt die Gesamtzahl in den `X-Total-Count`-Header (nicht in den Body). Liefert die Seite plus
 * `total`; fehlt der Header, fällt `total` auf die Seitenlänge zurück.
 */
/** Liest die Gesamtzahl aus dem `X-Total-Count`-Header; fehlt/leer, fällt sie auf `fallback` zurück. */
function totalFrom(res: Response, fallback: number): number {
  const header = res.headers.get("X-Total-Count");
  return header != null && header !== "" ? Number(header) : fallback;
}

async function httpPaged<T>(url: string): Promise<Paged<T>> {
  const res = await request(url, "GET");
  const text = await res.text();
  const items = (text ? JSON.parse(text) : []) as T[];
  return { items, total: totalFrom(res, items.length) };
}

/** Hängt Paginierung/Sortierung an eine URLSearchParams an (nur gesetzte Werte). */
function appendPaging(q: URLSearchParams, p: { sort?: string; dir?: string; skip?: number; take?: number }) {
  if (p.sort) q.set("sort", p.sort);
  if (p.dir) q.set("dir", p.dir);
  if (p.skip != null) q.set("skip", String(p.skip));
  if (p.take != null) q.set("take", String(p.take));
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
  children: () => http<ChildResponse[]>(`${V1}/supervisor/children`),
  createChild: (dto: CreateChildDto) => http<ChildResponse>(`${V1}/supervisor/children`, "POST", dto),
  updateChild: (childId: number, dto: Partial<CreateChildDto>) =>
    http<ChildResponse>(`${V1}/supervisor/children/${childId}`, "PATCH", dto),

  // ---- Vater: Katalog (Fächer, Kapitel, Übungssuche über Metadaten) ----
  subjects: () => http<SubjectResponse[]>(`${V1}/creator/subjects`),
  createSubject: (name: string) => http<SubjectResponse>(`${V1}/creator/subjects`, "POST", { name }),
  chapters: (subjectId: number) =>
    http<ChapterResponse[]>(`${V1}/creator/subjects/${subjectId}/chapters`),
  createChapter: (subjectId: number, name: string, orderIndex: number) =>
    http<ChapterResponse>(`${V1}/creator/subjects/${subjectId}/chapters`, "POST", { name, orderIndex }),
  // Fachabhängige Arten ("Kategorien") – zur Vorfilterung im Katalog/Planbau.
  categories: (subjectId: number) =>
    http<CategoryResponse[]>(`${V1}/creator/subjects/${subjectId}/categories`),
  // Übung eines Typs im Kapitel anlegen. Das Routen-Segment (vocabulary/arithmetic/…) bestimmt den Typ.
  createExercise: (subjectId: number, chapterId: number, typeRoute: string, payload: CreateExercisePayload) =>
    http<ExerciseSummary>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}`, "POST", payload),
  // Typ-übergreifender Detail-Abruf (mit Config) + „wo verwendet".
  getExercise: (id: number) => http<ExerciseDetail>(`${V1}/creator/exercises/${id}`),
  exerciseUsage: (id: number) => http<ExerciseUsage>(`${V1}/creator/exercises/${id}/usage`),
  // Testmodus: eine Übung nebenwirkungsfrei durchspielen (keine Punkte/kein Fortschritt) und bewerten lassen.
  previewExercise: (id: number, stage?: number) =>
    http<ExercisePreviewData>(`${V1}/creator/exercises/${id}/preview${stage != null ? `?stage=${stage}` : ""}`),
  checkPreviewExercise: (id: number, answers: ExercisePreviewAnswer[], stage?: number) =>
    http<ExercisePreviewResult>(`${V1}/creator/exercises/${id}/preview/check`, "POST", { answers, stage }),
  // Ersetzen (PUT) bzw. Löschen laufen über die per-Typ-Route.
  updateExercise: (subjectId: number, chapterId: number, typeRoute: string, id: number, payload: CreateExercisePayload) =>
    http<ExerciseSummary>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}/${id}`, "PUT", payload),
  deleteExercise: (subjectId: number, chapterId: number, typeRoute: string, id: number) =>
    http<void>(`${V1}/creator/subjects/${subjectId}/chapters/${chapterId}/${typeRoute}/${id}`, "DELETE"),
  searchExercises: (p: ExerciseSearchParams = {}) => {
    const q = new URLSearchParams();
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
    if (p.chapterId != null) q.set("chapterId", String(p.chapterId));
    if (p.grade != null) q.set("grade", String(p.grade));
    if (p.schoolType && p.schoolType !== "None") q.set("schoolType", p.schoolType);
    if (p.categoryId != null) q.set("categoryId", String(p.categoryId));
    if (p.type) q.set("type", p.type);
    if (p.search) q.set("search", p.search);
    if (p.mineOnly) q.set("mineOnly", "true");
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<ExerciseSummary>(`${V1}/creator/exercises${qs ? `?${qs}` : ""}`);
  },

  // ---- Vater: Vokabel-Store ----
  // Optional nach Sprachpaar, Wortart und Tags filtern (Store zeigt dann nur die passenden Einträge).
  vocabulary: (p: VocabularySearchParams = {}) => {
    const q = new URLSearchParams();
    if (p.search) q.set("search", p.search);
    if (p.sourceLanguage) q.set("sourceLanguage", p.sourceLanguage);
    if (p.targetLanguage) q.set("targetLanguage", p.targetLanguage);
    if (p.partOfSpeech) q.set("partOfSpeech", p.partOfSpeech);
    for (const t of p.tags ?? []) q.append("tag", t);
    if (p.matchAll) q.set("matchAll", "true");
    appendPaging(q, p);
    const qs = q.toString();
    return httpPaged<VocabularyResponse>(`${V1}/creator/vocabulary${qs ? `?${qs}` : ""}`);
  },
  createVocabulary: (dto: CreateVocabularyDto) =>
    http<VocabularyResponse>(`${V1}/creator/vocabulary`, "POST", dto),
  // Viele Paare in einem Aufruf (idempotent) – für die zeilenweise Paar-Eingabe.
  createVocabularyBatch: (items: CreateVocabularyDto[]) =>
    http<VocabBatchResult[]>(`${V1}/creator/vocabulary/batch`, "POST", items),
  updateVocabulary: (id: number, patch: UpdateVocabularyDto) =>
    http<VocabularyResponse>(`${V1}/creator/vocabulary/${id}`, "PATCH", patch),
  deleteVocabulary: (id: number) => http<void>(`${V1}/creator/vocabulary/${id}`, "DELETE"),

  // ---- Globale (kindneutrale) Vokabel-Tags ----
  vocabTags: () => http<VocabTagResponse[]>(`${V1}/creator/vocabulary/tags`),
  // Verknüpft eine Vokabel mit Tag-Namen (create-if-missing); liefert die aktuellen Tags der Vokabel.
  attachVocabTags: (vocabId: number, tags: string[]) =>
    http<VocabTagResponse[]>(`${V1}/creator/vocabulary/${vocabId}/tags`, "POST", { tags }),
  detachVocabTag: (vocabId: number, tagId: number) =>
    http<void>(`${V1}/creator/vocabulary/${vocabId}/tags/${tagId}`, "DELETE"),

  // ---- Kind-skopierte Tags (auch an Vokabeln) ----
  childTags: (childId: number) => http<ChildTagResponse[]>(`${V1}/creator/tags?childId=${childId}`),
  createChildTag: (dto: { childId: number; name: string; color?: string | null }) =>
    http<ChildTagResponse>(`${V1}/creator/tags`, "POST", dto),
  tagsForVocabulary: (vocabId: number, childId: number) =>
    http<ChildTagResponse[]>(`${V1}/creator/tags/for-vocabulary/${vocabId}?childId=${childId}`),
  tagVocabulary: (tagId: number, vocabularyIds: number[]) =>
    http<ChildTagResponse>(`${V1}/creator/tags/${tagId}/vocabulary`, "POST", { vocabularyIds }),
  untagVocabulary: (tagId: number, vocabId: number) =>
    http<void>(`${V1}/creator/tags/${tagId}/vocabulary/${vocabId}`, "DELETE"),

  // ---- Lehrpläne (reiner Container; Ziele/Punkte je Position) ----
  plans: (childId?: number) =>
    http<PlanResponse[]>(`${V1}/supervisor/study-plans${childId ? `?childId=${childId}` : ""}`),
  plan: (planId: number) => http<PlanResponse>(`${V1}/supervisor/study-plans/${planId}`),
  createPlan: (dto: CreatePlanDto) => http<PlanResponse>(`${V1}/supervisor/study-plans`, "POST", dto),
  // Lehrplan nachträglich umbenennen/verlängern/deaktivieren (Inhalte laufen über Positionen).
  updatePlan: (planId: number, dto: UpdatePlanDto) =>
    http<PlanResponse>(`${V1}/supervisor/study-plans/${planId}`, "PATCH", dto),
  // Lehrplan samt Positionen/Fortschritt löschen (Kaskade); die Katalog-Übungen bleiben erhalten.
  deletePlan: (planId: number) => http<void>(`${V1}/supervisor/study-plans/${planId}`, "DELETE"),

  // ---- Lehrplan-Positionen (Plan = Container aus Katalog-Übungen) ----
  positions: (planId: number) =>
    http<PositionResponse[]>(`${V1}/supervisor/study-plans/${planId}/positions`),
  addPosition: (planId: number, dto: CreatePositionDto) =>
    http<PositionResponse>(`${V1}/supervisor/study-plans/${planId}/positions`, "POST", dto),
  updatePosition: (planId: number, positionId: number, dto: UpdatePositionDto) =>
    http<PositionResponse>(`${V1}/supervisor/study-plans/${planId}/positions/${positionId}`, "PATCH", dto),
  deletePosition: (planId: number, positionId: number) =>
    http<void>(`${V1}/supervisor/study-plans/${planId}/positions/${positionId}`, "DELETE"),
  // Lern-Report der Position: je Inhalt Box/Beherrschung + Test-Trefferquote („sitzt/sitzt nicht").
  positionReport: (planId: number, positionId: number) =>
    http<PositionReport>(`${V1}/student/study-plans/${planId}/positions/${positionId}/report`),

  // ---- Vater: kindübergreifender Tagesüberblick ----
  childrenDaily: (date?: string) =>
    http<ChildrenDashboard>(`${V1}/supervisor/children/daily-overview${date ? `?date=${date}` : ""}`),

  // ---- Tagesmission (Sohn) / Verlauf (Vater) über Positionen ----
  overview: (planId: number) => http<OverviewResponse>(`${V1}/student/study-plans/${planId}/overview`),
  overviewProgress: (planId: number) => http<ProgressResponse>(`${V1}/student/study-plans/${planId}/overview/progress`),

  // ---- Sohn: Position üben (Leitner) ----
  startSession: (planId: number, positionId: number) =>
    http<PositionSession>(`${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions`, "POST", {}),
  heartbeat: (planId: number, positionId: number, sessionId: number, seconds: number, active: boolean) =>
    http<PositionSession>(
      `${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/heartbeat`, "POST", { seconds, active }),
  cards: (planId: number, positionId: number, sessionId: number) =>
    http<PracticeCard[]>(`${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/cards`),
  // Der Server bewertet serverseitig: das Frontend liefert nur die Antwort (getippt) bzw. bei
  // Anzeige-/Selbsteinschätzungs-Stufen das WasKnown-Flag; die Stufe erzwingt der Server.
  review: (planId: number, positionId: number, sessionId: number, dto: ReviewInput) =>
    http<ReviewOutcome | undefined>(
      `${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/review`, "POST", dto),
  endSession: (planId: number, positionId: number, sessionId: number) =>
    http<PositionSession>(
      `${V1}/student/study-plans/${planId}/positions/${positionId}/practice-sessions/${sessionId}/end`, "POST", {}),

  // ---- Sohn: Position testen (Abschlusstest = Klausur, strikt server-getrieben) ----
  // Der Start liefert nur Metadaten; die Fragen kommen einzeln über nextTest, beantwortet wird über
  // answerTest (ohne Korrektheit – Feedback erst beim Abschluss), submitTest wertet aus.
  startTest: (planId: number, positionId: number) =>
    http<TestAttemptResponse>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests`, "POST", {}),
  nextTest: (planId: number, positionId: number, attemptId: number) =>
    http<TestNextResponse>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests/${attemptId}/next`),
  answerTest: (planId: number, positionId: number, attemptId: number, dto: AnswerDto) =>
    http<TestAnswerAck>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests/${attemptId}/answer`, "POST", dto),
  submitTest: (planId: number, positionId: number, attemptId: number, answers: AnswerDto[] = []) =>
    http<TestSubmitResponse>(`${V1}/student/study-plans/${planId}/positions/${positionId}/tests/${attemptId}/submit`, "POST", { answers }),

  // ---- Sohn: Wallet ----
  // Kontostand (Salden) und Buchungsverlauf sind getrennt: Salden als Einzelwerte, Buchungen server-paginiert.
  wallet: () => http<WalletBalance>(`${V1}/student/me/points`),
  walletEntries: (opts: { skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    appendPaging(q, opts);
    const qs = q.toString();
    return httpPaged<WalletEntry>(`${V1}/student/me/points/entries${qs ? `?${qs}` : ""}`);
  },

  // ---- Sohn: Missionen & Auszeichnungen ----
  missions: () => http<MissionStatus[]>(`${V1}/student/me/missions`),
  achievements: () => http<AchievementStatus[]>(`${V1}/student/me/achievements`),

  // ---- Sohn: Skins (Besitz server-autoritativ; Kauf bucht Münzen ab) ----
  skins: () => http<SkinState>(`${V1}/student/me/skins`),
  purchaseSkin: (skinId: string) => http<SkinState>(`${V1}/student/me/skins/${skinId}/purchase`, "POST", {}),
  equipSkin: (skinId: string) => http<SkinState>(`${V1}/student/me/skins/${skinId}/equip`, "POST", {}),

  // ---- Vater: Missionen (Belohnungsziele) je Kind verwalten ----
  missionsFor: (childId: number) => http<MissionDef[]>(`${V1}/supervisor/children/${childId}/missions`),
  createMission: (childId: number, dto: CreateMissionDto) =>
    http<MissionDef>(`${V1}/supervisor/children/${childId}/missions`, "POST", dto),
  updateMission: (childId: number, missionId: number, dto: Partial<MissionDef>) =>
    http<MissionDef>(`${V1}/supervisor/children/${childId}/missions/${missionId}`, "PATCH", dto),
  deleteMission: (childId: number, missionId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/missions/${missionId}`, "DELETE"),

  // ---- Vater: Auszeichnungen (Badges) je Kind verwalten ----
  achievementsFor: (childId: number) => http<AchievementDef[]>(`${V1}/supervisor/children/${childId}/achievements`),
  createAchievement: (childId: number, dto: CreateAchievementDto) =>
    http<AchievementDef>(`${V1}/supervisor/children/${childId}/achievements`, "POST", dto),
  updateAchievement: (childId: number, achievementId: number, dto: Partial<AchievementDef>) =>
    http<AchievementDef>(`${V1}/supervisor/children/${childId}/achievements/${achievementId}`, "PATCH", dto),
  deleteAchievement: (childId: number, achievementId: number) =>
    http<void>(`${V1}/supervisor/children/${childId}/achievements/${achievementId}`, "DELETE"),

  // ---- Vater: Konto-Übersicht (Punktestand + Buchungsverlauf je Kind) ----
  // Der Buchungsverlauf ist server-paginiert (Einträge in der Hülle + X-Total-Count); die Salden sind
  // über ALLE Zeilen berechnet, bleiben also über die Seiten stabil.
  childPoints: async (childId: number, opts: { skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams();
    appendPaging(q, opts);
    const qs = q.toString();
    const res = await request(`${V1}/supervisor/children/${childId}/points${qs ? `?${qs}` : ""}`, "GET");
    const text = await res.text();
    const body = (text ? JSON.parse(text) : { coins: 0, gems: 0, entries: [] }) as Wallet;
    return { coins: body.coins, gems: body.gems, items: body.entries, total: totalFrom(res, body.entries.length) };
  },

  // ---- Vater: Klassenarbeiten (planen, Übungen zuweisen, benoten, üben/wiederholen) ----
  classTests: (childId: number, opts: { status?: KlassenarbeitStatus; skip?: number; take?: number } = {}) => {
    const q = new URLSearchParams({ childId: String(childId) });
    if (opts.status) q.set("status", opts.status);
    appendPaging(q, opts);
    return httpPaged<KlassenarbeitResponse>(`${V1}/supervisor/class-tests?${q.toString()}`);
  },
  classTest: (id: number) => http<KlassenarbeitDetail>(`${V1}/supervisor/class-tests/${id}`),
  createClassTest: (dto: CreateKlassenarbeitDto) =>
    http<KlassenarbeitDetail>(`${V1}/supervisor/class-tests`, "POST", dto),
  updateClassTest: (id: number, dto: UpdateKlassenarbeitDto) =>
    http<KlassenarbeitResponse>(`${V1}/supervisor/class-tests/${id}`, "PATCH", dto),
  deleteClassTest: (id: number) => http<void>(`${V1}/supervisor/class-tests/${id}`, "DELETE"),
  assignClassTestExercises: (id: number, exerciseIds: number[]) =>
    http<KlassenarbeitDetail>(`${V1}/supervisor/class-tests/${id}/exercises`, "POST", { exerciseIds }),
  unassignClassTestExercise: (id: number, exerciseId: number) =>
    http<void>(`${V1}/supervisor/class-tests/${id}/exercises/${exerciseId}`, "DELETE"),
  classTestPractice: (id: number) => http<KlassenarbeitPractice>(`${V1}/supervisor/class-tests/${id}/practice`),
  classTestRepeat: (childId: number, minBadGrade?: number) => {
    const q = new URLSearchParams({ childId: String(childId) });
    if (minBadGrade != null) q.set("minBadGrade", String(minBadGrade));
    return http<KlassenarbeitRepeat>(`${V1}/supervisor/class-tests/repeat?${q.toString()}`);
  },
};
