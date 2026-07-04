import type {
  AchievementStatus, AnswerDto, ChapterResponse, ChildResponse, CreateChildDto, CreatePlanDto,
  CreateVocabularyDto, ExerciseSearchParams, ExerciseSummary, LoginResponse, MissionStatus,
  PlanResponse, ProgressResponse, ReviewInput, ReviewOutcome, SessionResponse, SubjectResponse,
  TestAttemptResponse, TestSubmitResponse, TodayResponse, VocabularyResponse, Wallet,
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
  chapters: (subjectId: number) =>
    http<ChapterResponse[]>(`${V1}/learn/subjects/${subjectId}/chapters`),
  searchExercises: (p: ExerciseSearchParams = {}) => {
    const q = new URLSearchParams();
    if (p.subjectId != null) q.set("subjectId", String(p.subjectId));
    if (p.grade != null) q.set("grade", String(p.grade));
    if (p.schoolType && p.schoolType !== "None") q.set("schoolType", p.schoolType);
    if (p.categoryId != null) q.set("categoryId", String(p.categoryId));
    if (p.type) q.set("type", p.type);
    if (p.search) q.set("search", p.search);
    const qs = q.toString();
    return http<ExerciseSummary[]>(`${V1}/learn/exercises${qs ? `?${qs}` : ""}`);
  },

  // ---- Vater: Vokabel-Store ----
  vocabulary: (search?: string) =>
    http<VocabularyResponse[]>(`${V1}/learn/vocabulary${search ? `?search=${encodeURIComponent(search)}` : ""}`),
  createVocabulary: (dto: CreateVocabularyDto) =>
    http<VocabularyResponse>(`${V1}/learn/vocabulary`, "POST", dto),

  // ---- Lehrpläne ----
  plans: (childId?: number) =>
    http<PlanResponse[]>(`${V1}/study-plans${childId ? `?childId=${childId}` : ""}`),
  plan: (planId: number) => http<PlanResponse>(`${V1}/study-plans/${planId}`),
  createPlan: (dto: CreatePlanDto) => http<PlanResponse>(`${V1}/study-plans`, "POST", dto),
  today: (planId: number) => http<TodayResponse>(`${V1}/study-plans/${planId}/today`),
  progress: (planId: number) => http<ProgressResponse>(`${V1}/study-plans/${planId}/progress`),

  // ---- Sohn: Übungssession (Leitner) ----
  startSession: (planId: number) =>
    http<SessionResponse>(`${V1}/study-plans/${planId}/practice-sessions`, "POST", {}),
  heartbeat: (planId: number, sessionId: number, seconds: number, active: boolean) =>
    http<import("./types").DayProgress>(
      `${V1}/study-plans/${planId}/practice-sessions/${sessionId}/heartbeat`, "POST", { seconds, active }),
  // Der Server bewertet serverseitig: das Frontend liefert nur die Antwort (getippt/Lücken) bzw. bei
  // Anzeige-/Selbsteinschätzungs-Stufen das WasKnown-Flag; die Stufe leitet der Server aus dem Fahrplan ab.
  review: (planId: number, sessionId: number, dto: ReviewInput) =>
    http<ReviewOutcome>(
      `${V1}/study-plans/${planId}/practice-sessions/${sessionId}/review`, "POST", dto),
  endSession: (planId: number, sessionId: number) =>
    http<import("./types").DayProgress>(
      `${V1}/study-plans/${planId}/practice-sessions/${sessionId}/end`, "POST", {}),

  // ---- Sohn: Vokabel-Test ----
  startTest: (planId: number) =>
    http<TestAttemptResponse>(`${V1}/study-plans/${planId}/tests`, "POST", {}),
  submitTest: (planId: number, attemptId: number, answers: AnswerDto[]) =>
    http<TestSubmitResponse>(`${V1}/study-plans/${planId}/tests/${attemptId}/submit`, "POST", { answers }),

  // ---- Sohn: Wallet ----
  wallet: () => http<Wallet>(`${V1}/me/points`),

  // ---- Sohn: Missionen & Auszeichnungen ----
  missions: () => http<MissionStatus[]>(`${V1}/me/missions`),
  achievements: () => http<AchievementStatus[]>(`${V1}/me/achievements`),
};
