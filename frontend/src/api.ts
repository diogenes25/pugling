import type {
  LearningSession, PointsTransaction, RewardOffer, TimeSlotRule, Topic, User, VocabCard,
} from "./types";

async function http<T>(url: string, method = "GET", body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method,
    headers: body ? { "Content-Type": "application/json" } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(await res.text());
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  users: () => http<User[]>("/api/settings/users"),
  topics: () => http<Topic[]>("/api/vocab/topics"),
  dueCards: (topicId: number) => http<VocabCard[]>(`/api/vocab/topics/${topicId}/due`),

  startSession: (userId: number, topicId: number) =>
    http<LearningSession>("/api/sessions/start", "POST", { userId, topicId }),
  heartbeat: (sessionId: number, activeSeconds: number, idleSeconds: number, interactions: number) =>
    http<void>(`/api/sessions/${sessionId}/heartbeat`, "POST", { activeSeconds, idleSeconds, interactions }),
  review: (sessionId: number, cardId: number, correct: boolean) =>
    http<{ awarded: number; box: number }>(`/api/sessions/${sessionId}/review`, "POST", { cardId, correct }),
  endSession: (sessionId: number) =>
    http<LearningSession>(`/api/sessions/${sessionId}/end`, "POST"),
  sessions: (userId: number) => http<LearningSession[]>(`/api/sessions?userId=${userId}`),

  balance: (userId: number) => http<{ balance: number }>(`/api/points/balance/${userId}`),
  transactions: (userId: number) => http<PointsTransaction[]>(`/api/points/transactions/${userId}`),
  offers: () => http<RewardOffer[]>("/api/points/offers"),
  createOffer: (userId: number, title: string, costPoints: number) =>
    http<RewardOffer>("/api/points/offers", "POST", { userId, title, costPoints }),
  decideOffer: (id: number, accept: boolean) =>
    http<RewardOffer>(`/api/points/offers/${id}/decide`, "POST", { accept }),

  timeslots: () => http<TimeSlotRule[]>("/api/settings/timeslots"),
};
