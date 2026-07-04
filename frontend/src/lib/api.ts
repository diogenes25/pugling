import type {
  AnswerDto, ChildResponse, CreatePlanDto, CreateVocabularyDto, LoginResponse,
  PlanResponse, ProgressResponse, ReviewOutcome, SessionResponse, TestAttemptResponse,
  TestSubmitResponse, TodayResponse, VocabularyResponse, Wallet,
} from "./types";

const TOKEN_KEY = "pugling.token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}
export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

/** Fehler mit HTTP-Status, damit die UI 401 (Session weg) von 4xx (Eingabe) trennen kann. */
export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = "ApiError";
  }
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
    const text = await res.text().catch(() => "");
    throw new ApiError(res.status, text || `${res.status} ${res.statusText}`);
  }
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  // ---- Auth ----
  loginFather: (fatherId: number, pin: string) =>
    http<LoginResponse>("/api/auth/father", "POST", { fatherId, pin }),
  loginChild: (childId: number, pin: string) =>
    http<LoginResponse>("/api/auth/child", "POST", { childId, pin }),

  // ---- Vater: Kinder ----
  children: (fatherId: number) =>
    http<ChildResponse[]>(`/api/fathers/${fatherId}/children`),
  createChild: (fatherId: number, name: string, pin: string, birthYear?: number) =>
    http<ChildResponse>(`/api/fathers/${fatherId}/children`, "POST", { name, pin, birthYear }),

  // ---- Vater: Vokabel-Store ----
  vocabulary: (search?: string) =>
    http<VocabularyResponse[]>(`/api/learn/vocabulary${search ? `?search=${encodeURIComponent(search)}` : ""}`),
  createVocabulary: (dto: CreateVocabularyDto) =>
    http<VocabularyResponse>("/api/learn/vocabulary", "POST", dto),

  // ---- Lehrpläne ----
  plans: (childId?: number) =>
    http<PlanResponse[]>(`/api/study-plans${childId ? `?childId=${childId}` : ""}`),
  plan: (planId: number) => http<PlanResponse>(`/api/study-plans/${planId}`),
  createPlan: (dto: CreatePlanDto) => http<PlanResponse>("/api/study-plans", "POST", dto),
  today: (planId: number) => http<TodayResponse>(`/api/study-plans/${planId}/today`),
  progress: (planId: number) => http<ProgressResponse>(`/api/study-plans/${planId}/progress`),

  // ---- Sohn: Übungssession (Leitner) ----
  startSession: (planId: number) =>
    http<SessionResponse>(`/api/study-plans/${planId}/practice-sessions`, "POST", {}),
  heartbeat: (planId: number, sessionId: number, seconds: number, active: boolean) =>
    http<import("./types").DayProgress>(
      `/api/study-plans/${planId}/practice-sessions/${sessionId}/heartbeat`, "POST", { seconds, active }),
  review: (planId: number, sessionId: number, contentId: number, stage: number, wasCorrect: boolean) =>
    http<ReviewOutcome>(
      `/api/study-plans/${planId}/practice-sessions/${sessionId}/review`, "POST", { contentId, stage, wasCorrect }),
  endSession: (planId: number, sessionId: number) =>
    http<import("./types").DayProgress>(
      `/api/study-plans/${planId}/practice-sessions/${sessionId}/end`, "POST", {}),

  // ---- Sohn: Vokabel-Test ----
  startTest: (planId: number) =>
    http<TestAttemptResponse>(`/api/study-plans/${planId}/tests`, "POST", {}),
  submitTest: (planId: number, attemptId: number, answers: AnswerDto[]) =>
    http<TestSubmitResponse>(`/api/study-plans/${planId}/tests/${attemptId}/submit`, "POST", { answers }),

  // ---- Sohn: Wallet ----
  wallet: () => http<Wallet>("/api/me/points"),
};
