import { useCallback, useMemo, useRef, useState } from "react";
import { playCelebration } from "../lib/feedback";
import { prefersReducedMotion } from "../lib/ui";
import { LearningDad, type DadAction } from "./LearningDad";

export type CelebrationTier = "small" | "medium" | "big" | "encourage";

export interface Celebration {
  id: number;
  tier: CelebrationTier;
  emoji: string;
  title?: string;
  sub?: string;
  dadAction?: DadAction;
}

const DURATION: Record<CelebrationTier, number> = {
  small: 900,
  medium: 1800,
  big: 2500,
  encourage: 2000,
};

/** Clips longer than their tier's default: the overlay must not cut a punch mid-swing. */
const DURATION_BY_ACTION: Partial<Record<DadAction, number>> = {
  punch: 2600,
  "boxing-win": 4600,
  angry: 2600,
  "become-angry": 5200,
  "become-monster": 10200,
  "starts-fight": 10200,
  "boxing-complete": 8200,
  applause: 4000,
};

/**
 * Motivations-Animationen à la Duolingo mit dem Puckling-Repräsentanten „Learning Dad".
 * `celebrate(...)` triggert eine Erfolgsfeier mit Daumen-hoch-Animation und Konfetti.
 * `encourage(...)` triggert eine aufmunternde Reaktion mit Kopfschütteln/Facepalm bei Fehlern.
 */
export function useCelebration() {
  const [celebration, setCelebration] = useState<Celebration | null>(null);
  const seq = useRef(0);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const celebrate = useCallback(
    (
      tier: CelebrationTier,
      emoji: string,
      title?: string,
      sub?: string,
      dadAction: DadAction = "thumbs-up"
    ) => {
      seq.current += 1;
      const id = seq.current;
      setCelebration({ id, tier, emoji, title, sub, dadAction });
      playCelebration(tier); // Ton + Haptik gehören zur Feier – zentral, damit jeder Aufrufer sie erbt.
      if (timer.current) clearTimeout(timer.current);
      timer.current = setTimeout(
        () => setCelebration((c) => (c?.id === id ? null : c)),
        Math.max(DURATION[tier], DURATION_BY_ACTION[dadAction] ?? 0)
      );
    },
    []
  );

  const encourage = useCallback(
    (
      title?: string,
      sub?: string,
      dadAction: DadAction = "facepalm"
    ) => {
      seq.current += 1;
      const id = seq.current;
      setCelebration({
        id,
        tier: "encourage",
        emoji: "💪",
        title: title ?? "Kein Problem!",
        sub: sub ?? "Das kriegen wir hin!",
        dadAction,
      });
      playCelebration("encourage");
      if (timer.current) clearTimeout(timer.current);
      timer.current = setTimeout(
        () => setCelebration((c) => (c?.id === id ? null : c)),
        Math.max(DURATION.encourage, DURATION_BY_ACTION[dadAction] ?? 0)
      );
    },
    []
  );

  return { celebration, celebrate, encourage };
}

const CONFETTI_COLORS = ["#ffc738", "#26d9ff", "#3ce85c", "#b14bff", "#ff4d6d", "#ff9e2c"];

/** Overlay-Ebene; rendert die aktuelle Feier. Liegt über allem, fängt aber keine Klicks ab. */
export function CelebrationLayer({ celebration }: { celebration: Celebration | null }) {
  // Konfetti-Stücke je Feier neu würfeln (id als Seed-Ersatz).
  const pieces = useMemo(() => {
    // Bei „Bewegung reduzieren" oder bei reiner Aufmunterung kein Feuerwerk-Konfetti
    if (!celebration || celebration.tier === "small" || celebration.tier === "encourage" || prefersReducedMotion()) {
      return [];
    }
    const n = celebration.tier === "big" ? 34 : 20;
    return Array.from({ length: n }, (_, i) => ({
      left: Math.random() * 100,
      delay: Math.random() * 0.15,
      dur: 0.9 + Math.random() * 0.7,
      rot: Math.random() * 360,
      drift: (Math.random() - 0.5) * 80,
      color: CONFETTI_COLORS[i % CONFETTI_COLORS.length],
      w: 6 + Math.random() * 6,
      h: 8 + Math.random() * 8,
    }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [celebration?.id]);

  if (!celebration) return null;

  const action: DadAction = celebration.dadAction ?? (celebration.tier === "encourage" ? "facepalm" : "thumbs-up");

  return (
    <div className={`cel-layer cel-${celebration.tier}`} aria-hidden="true" key={celebration.id}>
      {pieces.map((p, i) => (
        <span
          key={i}
          className="confetti"
          style={{
            left: `${p.left}%`,
            width: p.w,
            height: p.h,
            background: p.color,
            ["--drift" as string]: `${p.drift}px`,
            ["--rot" as string]: `${p.rot}deg`,
            animationDelay: `${p.delay}s`,
            animationDuration: `${p.dur}s`,
          }}
        />
      ))}

      {celebration.tier === "encourage" ? (
        <div className="dad-stage dad-encourage-stage">
          <LearningDad
            playKey={celebration.id}
            action={action}
            size="hero"
            caption={celebration.title ?? "Kein Problem!"}
            subCaption={celebration.sub}
          />
        </div>
      ) : celebration.tier === "big" ? (
        <div className="dad-stage dad-hero-stage">
          {celebration.emoji && <div className="cel-fighter">{celebration.emoji}</div>}
          <LearningDad
            playKey={celebration.id}
            action={action}
            size="hero"
            caption={celebration.title ?? "JA! SUPER GEMACHT!"}
            subCaption={celebration.sub}
          />
        </div>
      ) : celebration.tier === "small" ? (
        <div className="dad-stage dad-pop-stage">
          <LearningDad
            playKey={celebration.id}
            action={action}
            size="md"
            subCaption={celebration.sub}
          />
        </div>
      ) : (
        <div className="dad-stage dad-banner-stage">
          <LearningDad
            playKey={celebration.id}
            action={action}
            size="lg"
            caption={celebration.title}
            subCaption={celebration.sub}
          />
        </div>
      )}
    </div>
  );
}
