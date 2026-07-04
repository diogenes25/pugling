// Skins/Charaktere für den Sohn. Besitz & Auswahl sind server-autoritativ (siehe `api.skins`/
// `api.purchaseSkin`/`api.equipSkin`): Der Kauf bucht echte Münzen ab und persistiert am Kind,
// gilt also geräteübergreifend. Diese Datei liefert nur den *visuellen* Katalog (Emoji/Farbe/Kosten
// zur Anzeige); die Kosten-Wahrheit fürs Abbuchen liegt serverseitig (SkinCatalog).

export interface Skin {
  id: string;
  name: string;
  emoji: string;
  /** Farbverlauf für Avatar/Badge (CSS). */
  gradient: string;
  /** Freischaltkosten in Münzen; 0 = von Anfang an frei (nur Anzeige – Server ist Quelle der Wahrheit). */
  cost: number;
  blurb: string;
}

export const SKINS: Skin[] = [
  { id: "pug", name: "Pug", emoji: "🐶", gradient: "linear-gradient(160deg,#e8c496,#b98a54)", cost: 0, blurb: "Dein treuer Starter." },
  { id: "fox", name: "Fuchs", emoji: "🦊", gradient: "linear-gradient(160deg,#ff9e2c,#c23a1d)", cost: 300, blurb: "Schnell und schlau." },
  { id: "dragon", name: "Drache", emoji: "🐉", gradient: "linear-gradient(160deg,#3ce85c,#1f9c3a)", cost: 800, blurb: "Legendär – für echte Meister." },
  { id: "robot", name: "Robo", emoji: "🤖", gradient: "linear-gradient(160deg,#26d9ff,#2a7bff)", cost: 1200, blurb: "Rechnet mit dir." },
  { id: "ninja", name: "Ninja", emoji: "🥷", gradient: "linear-gradient(160deg,#b14bff,#6d28d9)", cost: 2000, blurb: "Lautlos zur Bestnote." },
];

/** Gratis-Starter – Fallback, solange der Server-Zustand noch nicht geladen ist. */
export const DEFAULT_SKIN: Skin = SKINS[0];

/** Skin per ID (Fallback: Starter), damit UI und Server-Zustand über die ID zusammenfinden. */
export function skinById(skinId: string | undefined): Skin {
  return SKINS.find((s) => s.id === skinId) ?? DEFAULT_SKIN;
}
