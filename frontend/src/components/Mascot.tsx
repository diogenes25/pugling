import type { Skin } from "../lib/skins";

type Mood = "happy" | "hyped" | "sleepy";

/**
 * Anime-Begleiter als Inline-SVG. Der ausgewählte Skin bestimmt Farbe/Emoji-Abzeichen;
 * die Stimmung (mood) spiegelt Streak/Tagesmission wider.
 */
export function Mascot({ skin, mood = "happy", size = 120 }: { skin: Skin; mood?: Mood; size?: number }) {
  const eyes =
    mood === "sleepy"
      ? `<path d="M40 58 q7 6 14 0" stroke="#2a2140" stroke-width="4" fill="none" stroke-linecap="round"/>
         <path d="M78 58 q7 6 14 0" stroke="#2a2140" stroke-width="4" fill="none" stroke-linecap="round"/>`
      : `<circle cx="47" cy="60" r="8" fill="#2a2140"/><circle cx="85" cy="60" r="8" fill="#2a2140"/>
         <circle cx="50" cy="57" r="2.6" fill="#fff"/><circle cx="88" cy="57" r="2.6" fill="#fff"/>`;
  const blush = mood === "hyped" ? "#ffb347" : mood === "sleepy" ? "#9aa0d8" : "#ff8fb0";
  const spark =
    mood === "hyped"
      ? `<path d="M112 30 l3 8 8 3 -8 3 -3 8 -3 -8 -8 -3 8 -3z" fill="#ffc738"/>
         <path d="M18 40 l2 5 5 2 -5 2 -2 5 -2 -5 -5 -2 5 -2z" fill="#26d9ff"/>`
      : "";

  const svg = `<svg viewBox="0 0 132 132" xmlns="http://www.w3.org/2000/svg" style="display:block;filter:drop-shadow(0 10px 14px rgba(4,6,26,.5))">
    <ellipse cx="66" cy="120" rx="30" ry="6" fill="rgba(0,0,0,.25)"/>
    <path d="M28 44 q-14 -6 -12 14 q2 14 16 10z" fill="#c99a63"/>
    <path d="M104 44 q14 -6 12 14 q-2 14 -16 10z" fill="#c99a63"/>
    <ellipse cx="66" cy="66" rx="46" ry="42" fill="#e8c496"/>
    <ellipse cx="66" cy="66" rx="46" ry="42" fill="none" stroke="#b98a54" stroke-width="3"/>
    <path d="M40 72 q26 26 52 0 q-4 22 -26 22 q-22 0 -26 -22z" fill="#4a3a26"/>
    <ellipse cx="66" cy="78" rx="13" ry="10" fill="#2a2140"/>
    ${eyes}
    <circle cx="34" cy="78" r="7" fill="${blush}" opacity=".55"/>
    <circle cx="98" cy="78" r="7" fill="${blush}" opacity=".55"/>
    ${spark}
  </svg>`;

  // Nicht-Pug-Skins: großes Emoji auf Skin-Farbverlauf (leichtgewichtig, keine 5 SVGs nötig).
  if (skin.id !== "pug") {
    return (
      <div
        aria-label={skin.name}
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

  return (
    <div
      role="img"
      aria-label={skin.name}
      style={{ width: size, height: size, margin: "0 auto" }}
      dangerouslySetInnerHTML={{ __html: svg }}
    />
  );
}
