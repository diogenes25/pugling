// Skins/Charaktere für den Sohn. Freischaltung wird vorerst lokal je Kind persistiert
// (Wallet-Deckung wird beim Kauf geprüft). Für geräteübergreifende Speicherung wäre später
// ein kleines Backend-Feld am Child nötig (z.B. SelectedSkin + UnlockedSkins) – siehe PROTOKOLL.

export interface Skin {
  id: string;
  name: string;
  emoji: string;
  /** Farbverlauf für Avatar/Badge (CSS). */
  gradient: string;
  /** Freischaltkosten in Münzen; 0 = von Anfang an frei. */
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

function keyFor(childId: number) {
  return `pugling.skins.${childId}`;
}

interface SkinState {
  selected: string;
  unlocked: string[];
}

function load(childId: number): SkinState {
  try {
    const raw = localStorage.getItem(keyFor(childId));
    if (raw) return JSON.parse(raw) as SkinState;
  } catch {
    /* ignorieren, Default nutzen */
  }
  return { selected: "pug", unlocked: ["pug"] };
}

function save(childId: number, state: SkinState) {
  localStorage.setItem(keyFor(childId), JSON.stringify(state));
}

export function getSkinState(childId: number): SkinState {
  return load(childId);
}

export function getSelectedSkin(childId: number): Skin {
  const state = load(childId);
  return SKINS.find((s) => s.id === state.selected) ?? SKINS[0];
}

export function selectSkin(childId: number, skinId: string): SkinState {
  const state = load(childId);
  if (!state.unlocked.includes(skinId)) return state;
  const next = { ...state, selected: skinId };
  save(childId, next);
  return next;
}

/** Schaltet einen Skin frei, sofern genug Münzen gedeckt sind. Gibt neuen Zustand + Erfolg zurück. */
export function unlockSkin(childId: number, skinId: string, balance: number): { state: SkinState; ok: boolean } {
  const state = load(childId);
  const skin = SKINS.find((s) => s.id === skinId);
  if (!skin || state.unlocked.includes(skinId)) return { state, ok: false };
  if (balance < skin.cost) return { state, ok: false };
  const next: SkinState = { selected: skinId, unlocked: [...state.unlocked, skinId] };
  save(childId, next);
  return { state: next, ok: true };
}
