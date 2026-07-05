/**
 * Mehrsinniges Erfolgs-Feedback für die Sohn-Arcade: kurzer Ton (WebAudio, synthetisiert – keine
 * Asset-Dateien, offline-tauglich) plus Haptik (`navigator.vibrate`). Bewusst hart abgesichert:
 * fehlt `AudioContext`/`vibrate` oder ist stummgeschaltet, passiert einfach nichts – niemals ein
 * Wurf, sonst bräche der Headless-E2E-Lauf. Der Ton begleitet die visuelle Feier (`celebrate`),
 * ist also an dieselben Stufen gekoppelt.
 */
import type { CelebrationTier } from "../components/Celebration";
import { prefersReducedMotion } from "./ui";

const MUTE_KEY = "pugling.muted";

// Modul-weiter Mute-Stand, aus localStorage vorgeladen; die HUD-Umschaltung hält ihn per setMuted synchron.
let muted = ((): boolean => {
  try {
    return localStorage.getItem(MUTE_KEY) === "1";
  } catch {
    return false;
  }
})();

export function isMuted(): boolean {
  return muted;
}

export function setMuted(value: boolean): void {
  muted = value;
  try {
    localStorage.setItem(MUTE_KEY, value ? "1" : "0");
  } catch {
    /* localStorage kann fehlen (Privatmodus) – Mute gilt dann nur für diese Sitzung. */
  }
}

// AudioContext erst bei erster Nutzung erzeugen: mobile Browser erlauben Audio nur nach einer
// Nutzer-Geste, und der erste Erfolg (Treffer/Abgabe) ist genau so eine.
type AnyWindow = Window & { webkitAudioContext?: typeof AudioContext };
let ctx: AudioContext | null = null;

function audioContext(): AudioContext | null {
  if (typeof window === "undefined") return null;
  if (ctx) return ctx;
  const Ctor = window.AudioContext ?? (window as AnyWindow).webkitAudioContext;
  if (!Ctor) return null;
  try {
    ctx = new Ctor();
  } catch {
    ctx = null;
  }
  return ctx;
}

// Aufsteigende Notenfolgen (Hz) je Stufe – klein = kurzer Blip, groß = triumphaler Akkord.
const TONES: Record<CelebrationTier, number[]> = {
  small: [880],
  medium: [660, 990],
  big: [523.25, 659.25, 783.99, 1046.5], // C-Dur-Arpeggio C5–E5–G5–C6
};

const VIBRATION: Record<CelebrationTier, number[]> = {
  small: [15],
  medium: [20, 40, 30],
  big: [30, 50, 40, 50, 70],
};

function playTone(ac: AudioContext, freq: number, startAt: number, length: number): void {
  const osc = ac.createOscillator();
  const gain = ac.createGain();
  osc.type = "triangle"; // weicher als eine reine Sinuskurve, spielzeug-freundlich
  osc.frequency.value = freq;
  // Kurze Attack + exponentielles Ausklingen, damit es „poppt" statt zu klicken.
  gain.gain.setValueAtTime(0.0001, startAt);
  gain.gain.exponentialRampToValueAtTime(0.22, startAt + 0.012);
  gain.gain.exponentialRampToValueAtTime(0.0001, startAt + length);
  osc.connect(gain).connect(ac.destination);
  osc.start(startAt);
  osc.stop(startAt + length + 0.02);
}

/**
 * Spielt Ton + Haptik für eine Feier-Stufe. Wird zentral von `celebrate(...)` aufgerufen, damit jeder
 * Erfolg (Combo, Sieg, Mission, Badge) automatisch klingt und vibriert.
 */
export function playCelebration(tier: CelebrationTier): void {
  if (muted) return;

  // Ton – vollständig gekapselt, jeder Fehler bleibt folgenlos.
  try {
    const ac = audioContext();
    if (ac) {
      if (ac.state === "suspended") void ac.resume().catch(() => {});
      const notes = TONES[tier];
      const step = tier === "big" ? 0.11 : 0.09;
      const length = tier === "small" ? 0.1 : 0.16;
      notes.forEach((f, i) => playTone(ac, f, ac.currentTime + i * step, length));
    }
  } catch {
    /* Audio ist Beiwerk – ein stummer Erfolg ist besser als ein geworfener. */
  }

  // Haptik – nur wo unterstützt; Desktop ignoriert es ohnehin. Bei „Bewegung reduzieren" bleibt sie
  // aus: Vibration ist spürbare Bewegung, die eine reine CSS-Regel nicht unterdrücken kann.
  try {
    if (!prefersReducedMotion() && typeof navigator !== "undefined" && typeof navigator.vibrate === "function") {
      navigator.vibrate(VIBRATION[tier]);
    }
  } catch {
    /* Vibration nicht erlaubt/unterstützt – egal. */
  }
}
