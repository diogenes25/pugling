import type { Skin } from "../lib/skins";
import { useSkinText } from "../lib/skinText";

type Mood = "happy" | "hyped" | "sleepy";

/**
 * Anime-Begleiter als Inline-SVG. Der ausgewählte Skin bestimmt Farbe/Emoji-Abzeichen;
 * die Stimmung (mood) spiegelt Streak/Tagesmission wider.
 */
export function Mascot({ skin, mood = "happy", size = 120 }: { skin: Skin; mood?: Mood; size?: number }) {
  // Der Vorlesename folgt der Arcade-Sprache (B-304) – der Begleiter lebt nur in der Arcade.
  const name = useSkinText().name(skin);
  // Nicht-Pug-Skins: großes Emoji auf Skin-Farbverlauf (leichtgewichtig, keine 5 SVGs nötig).
  if (skin.id !== "pug") {
    return (
      <div
        aria-label={name}
        style={{
          width: size, height: size, borderRadius: size * 0.28, margin: "0 auto",
          display: "grid", placeItems: "center", background: skin.gradient,
          border: "3px solid rgba(255,255,255,.25)", fontSize: size * 0.5,
          filter: "drop-shadow(0 10px 14px rgba(4,6,26,.5))",
        }}
      >
        {skin.emoji}
      </div>
    );
  }

  // The starter skin is the Learning Dad's head; the mood picks the matching expression.
  return (
    <img
      src={`/character/dad/dad_head_${mood}.png`}
      alt={name}
      width={size}
      height={size}
      style={{
        display: "block", margin: "0 auto", borderRadius: "50%", objectFit: "cover",
        border: "3px solid var(--gold)", background: "#fff",
        filter: "drop-shadow(0 10px 14px rgba(4,6,26,.5))",
      }}
    />
  );
}
