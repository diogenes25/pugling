import { memo, useMemo } from "react";
import { prefersReducedMotion } from "../lib/ui";

export type DadAction =
  | "thumbs-up"
  | "thumbs-up-alt"
  | "facepalm"
  | "clock"
  | "punch"
  | "boxing-win"
  | "angry"
  | "become-angry"
  | "become-monster"
  | "starts-fight"
  | "boxing-complete"
  | "applause"
  | "avatar"
  | "still-thumbs-up"
  | "still-facepalm";

export type DadSize = "xs" | "sm" | "md" | "lg" | "hero";

export interface LearningDadProps {
  action?: DadAction;
  size?: DadSize | number;
  /** 'webp' nutzt transparentes animiertes WebP (Standard), 'video' HTML5 Video */
  mode?: "webp" | "video" | "still";
  /** Optionale Sprechblase / Beschriftung */
  caption?: string;
  subCaption?: string;
  variant?: "cutout" | "card" | "badge" | "circle";
  className?: string;
  style?: React.CSSProperties;
  loop?: boolean;
  alt?: string;
  /** Changes per trigger so a one-shot clip restarts from frame 0 instead of showing its last frame. */
  playKey?: string | number;
  onClick?: () => void;
}

/** Height/width of the sprite: the boxing clips are cropped nearly square, the others are portrait. */
const ASPECT: Partial<Record<DadAction, number>> = {
  punch: 0.9,
  "boxing-win": 0.9,
  "boxing-complete": 0.9,
  // Cropped from a tall 590x1080 panel.
  "become-angry": 1.83,
  "become-monster": 1.83,
  "starts-fight": 1.83,
  applause: 1.48,
};

const PIXEL_SIZES: Record<DadSize, { width: number; height: number }> = {
  xs: { width: 44, height: 60 },
  sm: { width: 70, height: 96 },
  md: { width: 120, height: 165 },
  lg: { width: 180, height: 248 },
  hero: { width: 260, height: 358 },
};

type Asset = { webp: string; webm?: string; mp4?: string; still: string; alt: string };

const ASSETS: Record<DadAction, Asset> = {
  "thumbs-up": {
    webp: "/character/dad/dad_thumbs_up.webp",
    webm: "/character/dad/dad_thumbs_up.webm",
    mp4: "/character/dad/dad_thumbs_up.mp4",
    still: "/character/dad/dad_thumbs_up_still.png",
    alt: "Learning Dad zeigt Daumen hoch",
  },
  "thumbs-up-alt": {
    webp: "/character/dad/dad_thumbs_up_alt.webp",
    webm: "/character/dad/dad_thumbs_up.webm",
    mp4: "/character/dad/dad_thumbs_up.mp4",
    still: "/character/dad/dad_thumbs_up_still.png",
    alt: "Learning Dad freut sich",
  },
  facepalm: {
    webp: "/character/dad/dad_facepalm.webp",
    webm: "/character/dad/dad_facepalm.webm",
    mp4: "/character/dad/dad_facepalm.mp4",
    still: "/character/dad/dad_facepalm_still.png",
    alt: "Learning Dad schüttelt verständnisvoll den Kopf",
  },
  clock: {
    webp: "/character/dad/dad_clock.webp",
    webm: "/character/dad/dad_clock.mp4",
    mp4: "/character/dad/dad_clock.mp4",
    still: "/character/dad/dad_clock.webp",
    alt: "Learning Dad zeigt auf die Uhr",
  },
  // One-shot clips (WebP loop=1): jab+cross "you hit it" and the boxing win. Remounted per trigger (see `playKey`).
  punch: {
    webp: "/character/dad/dad_punch.webp",
    still: "/character/dad/dad_punch_still.png",
    alt: "Learning Dad boxt – Treffer!",
  },
  "boxing-win": {
    webp: "/character/dad/dad_boxing_win.webp",
    still: "/character/dad/dad_boxing_win_still.png",
    alt: "Learning Dad boxt und gewinnt",
  },
  angry: {
    webp: "/character/dad/dad_angry.webp",
    still: "/character/dad/dad_angry_still.png",
    alt: "Learning Dad ist wütend",
  },
  // Missed-goals sequence: calm -> furious -> monster -> picks a fight (each plays once).
  "become-angry": {
    webp: "/character/dad/dad_become_angry.webp",
    still: "/character/dad/dad_become_angry_still.png",
    alt: "Learning Dad wird wütend",
  },
  "become-monster": {
    webp: "/character/dad/dad_become_monster.webp",
    still: "/character/dad/dad_become_monster_still.png",
    alt: "Learning Dad verwandelt sich in ein Monster",
  },
  "starts-fight": {
    webp: "/character/dad/dad_starts_fight.webp",
    still: "/character/dad/dad_starts_fight_still.png",
    alt: "Learning Dad fordert zum Kampf heraus",
  },
  "boxing-complete": {
    webp: "/character/dad/dad_boxing_complete.webp",
    still: "/character/dad/dad_boxing_complete_still.png",
    alt: "Learning Dad boxt eine Serie",
  },
  applause: {
    webp: "/character/dad/dad_applause.webp",
    still: "/character/dad/dad_applause_still.png",
    alt: "Learning Dad klatscht Beifall",
  },
  avatar: {
    webp: "/character/dad/dad_avatar.png",
    webm: "/character/dad/dad_avatar.png",
    mp4: "/character/dad/dad_avatar.png",
    still: "/character/dad/dad_avatar.png",
    alt: "Learning Dad Profil",
  },
  "still-thumbs-up": {
    webp: "/character/dad/dad_thumbs_up_still.png",
    webm: "/character/dad/dad_thumbs_up.webm",
    mp4: "/character/dad/dad_thumbs_up.mp4",
    still: "/character/dad/dad_thumbs_up_still.png",
    alt: "Learning Dad Daumen hoch",
  },
  "still-facepalm": {
    webp: "/character/dad/dad_facepalm_still.png",
    webm: "/character/dad/dad_facepalm.webm",
    mp4: "/character/dad/dad_facepalm.mp4",
    still: "/character/dad/dad_facepalm_still.png",
    alt: "Learning Dad Kopfschütteln",
  },
};

/**
 * Learning Dad – Der humorvolle, sportliche Vater und Identifikationsfigur von Puckling.
 *
 * Zeigt lebendige Animationen für:
 * - "thumbs-up": Erfolg, gelöste Aufgaben, Daumen hoch, Siegesmoment
 * - "facepalm": Fehler, falsche Antwort, aufmunterndes Kopfschütteln („Kein Problem – das packen wir!“)
 * - "clock": Zeit läuft ab, Erinnerung
 * - "avatar": Runder Avatar für Header/HUD/Badges
 */
export const LearningDad = memo(function LearningDad({
  action = "thumbs-up",
  size = "md",
  mode = "webp",
  caption,
  subCaption,
  variant = "cutout",
  className = "",
  style,
  loop = true,
  alt,
  playKey,
  onClick,
}: LearningDadProps) {
  const reducedMotion = prefersReducedMotion();
  // Clips without a video encode (punch/boxing-win/angry) fall back from "video" to the animated WebP.
  const hasVideo = Boolean(ASSETS[action]?.webm);
  const effectiveMode = reducedMotion || action.startsWith("still-") ? "still" : mode === "video" && !hasVideo ? "webp" : mode;

  const dims = useMemo(() => {
    const aspect = ASPECT[action] ?? 1.38;
    if (typeof size === "number") {
      // 1 : 1.38 Seitenverhältnis für Ganzkörper/Halbkörper
      return { width: size, height: Math.round(size * aspect) };
    }
    const base = PIXEL_SIZES[size] ?? PIXEL_SIZES.md;
    // Same width as the portrait sprites, height follows the clip's own aspect.
    return { width: base.width, height: Math.round(base.width * aspect) };
  }, [size, action]);

  const asset = ASSETS[action] ?? ASSETS["thumbs-up"];
  const description = alt ?? asset.alt;

  const isCircle = variant === "circle" || action === "avatar";

  const containerStyle: React.CSSProperties = {
    position: "relative",
    display: "inline-flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    ...style,
  };

  const figureStyle: React.CSSProperties = isCircle
    ? {
        width: typeof size === "number" ? size : dims.width,
        height: typeof size === "number" ? size : dims.width,
        borderRadius: "50%",
        overflow: "hidden",
        border: "3px solid var(--gold)",
        boxShadow: "0 4px 14px rgba(0,0,0,0.4), 0 0 16px rgba(255,199,56,0.3)",
        background: "radial-gradient(circle at 50% 35%, #2a3568, #0e1236)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }
    : variant === "card"
    ? {
        width: dims.width + 24,
        padding: "12px 10px 8px",
        borderRadius: "18px",
        background: "linear-gradient(180deg, rgba(34,39,84,0.95), rgba(16,20,54,0.98))",
        border: "2px solid var(--stroke)",
        boxShadow: "0 10px 24px rgba(0,0,0,0.5), 0 0 20px rgba(38,217,255,0.15)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
      }
    : {
        width: dims.width,
        height: dims.height,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      };

  return (
    <div
      className={`dad-character-wrap dad-${action} dad-${variant} ${className}`}
      style={containerStyle}
      onClick={onClick}
    >
      {/* Decorative, deliberately no live region: every caller either sits under aria-hidden (celebration layer, lesson
          sheet) or shows the caption from mount, and the screen it sits on announces through its own region (B-134).
          The sub-caption stands on its own - a small celebration passes only "+N coins". */}
      {(caption || subCaption) && (
        <div className="dad-bubble">
          {caption && <span className="dad-bubble-text">{caption}</span>}
          {subCaption && <span className="dad-bubble-sub">{subCaption}</span>}
        </div>
      )}

      <div className="dad-figure-box" style={figureStyle}>
        {effectiveMode === "video" ? (
          <video
            autoPlay
            loop={loop}
            muted
            playsInline
            aria-label={description}
            style={{
              width: "100%",
              height: "100%",
              objectFit: isCircle ? "cover" : "contain",
              filter: "drop-shadow(0 6px 12px rgba(0,0,0,0.45))",
            }}
          >
            {asset.webm && <source src={asset.webm} type="video/webm" />}
            {asset.mp4 && <source src={asset.mp4} type="video/mp4" />}
            <img src={asset.still} alt={description} />
          </video>
        ) : (
          <img
            key={playKey}
            src={effectiveMode === "still" ? asset.still : asset.webp}
            alt={description}
            loading="eager"
            style={{
              width: "100%",
              height: "100%",
              objectFit: isCircle ? "cover" : "contain",
              filter: isCircle ? undefined : "drop-shadow(0 8px 16px rgba(0,0,0,0.48))",
              pointerEvents: "none",
            }}
          />
        )}
      </div>
    </div>
  );
});
