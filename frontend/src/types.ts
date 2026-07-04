export interface User {
  id: number;
  name: string;
  role: number; // 0 = Vater, 1 = Sohn
}

export interface Topic {
  id: number;
  name: string;
  cardCount: number;
}

export interface VocabCard {
  id: number;
  topicId: number;
  front: string;
  back: string;
  box: number;
  dueAt: string;
  reviewCount: number;
}

export interface LearningSession {
  id: number;
  userId: number;
  topicId: number;
  startedAt: string;
  endedAt: string | null;
  activeSeconds: number;
  idleSeconds: number;
  interactionCount: number;
  cardsReviewed: number;
  newCards: number;
  pointsEarned: number;
}

export interface PointsTransaction {
  id: number;
  userId: number;
  amount: number;
  reason: string;
  createdAt: string;
}

export interface RewardOffer {
  id: number;
  userId: number;
  title: string;
  costPoints: number;
  status: number; // 0 offen, 1 angenommen, 2 abgelehnt
  createdAt: string;
}

export interface TimeSlotRule {
  id: number;
  name: string;
  startTime: string;
  endTime: string;
  multiplier: number;
}
