import { useCallback, useMemo, useRef, useState } from "react";
import { playCelebration } from "../lib/feedback";

export type CelebrationTier = "small" | "medium" | "big";

export interface Celebration {
  id: number;
  tier: CelebrationTier;
  emoji: string;
  title?: string;
  sub?: string;
}

const DURATION: Record<CelebrationTier, number> = { small: 750, medium: 1500, big: 2000 };

/**
 * Motivations-Animationen à la Duolingo. `celebrate(...)` triggert eine Feier, die sich nach
 * kurzer Zeit selbst wieder abräumt. Bewusst nur ein Overlay gleichzeitig (kein Stapeln).
 */
export function useCelebration() {
  const [celebration, setCelebration] = useState<Celebration | null>(null);
  const seq = useRef(0);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const celebrate = useCallback((tier: CelebrationTier, emoji: string, title?: string, sub?: string) => {
    seq.current += 1;
    const id = seq.current;
    setCelebration({ id, tier, emoji, title, sub });
    playCelebration(tier); // Ton + Haptik gehören zur Feier – zentral, damit jeder Aufrufer sie erbt.
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => setCelebration((c) => (c?.id === id ? null : c)), DURATION[tier]);
  }, []);

  return { celebration, celebrate };
}

const CONFETTI_COLORS = ["#ffc738", "#26d9ff", "#3ce85c", "#b14bff", "#ff4d6d", "#ff9e2c"];

/** Overlay-Ebene; rendert die aktuelle Feier. Liegt über allem, fängt aber keine Klicks ab. */
export function CelebrationLayer({ celebration }: { celebration: Celebration | null }) {
  // Konfetti-Stücke je Feier neu würfeln (id als Seed-Ersatz).
  const pieces = useMemo(() => {
    if (!celebration || celebration.tier === "small") return [];
    const n = celebration.tier === "big" ? 30 : 18;
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

  return (
    <div className="cel-layer" aria-hidden="true" key={celebration.id}>
      {pieces.map((p, i) => (
        <span
          key={i}
          className="confetti"
          style={{
            left: `${p.left}%`,
            width: p.w, height: p.h, background: p.color,
            // CSS-Variablen steuern die Keyframe-Bewegung.
            ["--drift" as string]: `${p.drift}px`,
            ["--rot" as string]: `${p.rot}deg`,
            animationDelay: `${p.delay}s`,
            animationDuration: `${p.dur}s`,
          }}
        />
      ))}

      {celebration.tier === "big" && (
        <div className="cel-fighter">{celebration.emoji}</div>
      )}

      {celebration.tier === "small" ? (
        <div className="cel-pop">{celebration.emoji}</div>
      ) : (
        <div className="cel-banner">
          <div className="cel-emoji">{celebration.emoji}</div>
          {celebration.title && <div className="cel-title">{celebration.title}</div>}
          {celebration.sub && <div className="cel-sub">{celebration.sub}</div>}
        </div>
      )}
    </div>
  );
}
