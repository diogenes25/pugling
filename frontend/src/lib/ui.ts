/**
 * Kleine, framework-freie UI-Helfer, die quer über Vater- und Sohn-Screens gebraucht werden.
 * Bewusst ohne React-Abhängigkeit, damit sie auch in Event-Handlern und Services nutzbar sind.
 */

/**
 * Bestätigt eine schwer umkehrbare Aktion (Löschen, Stornieren, Münzen/Gems ausgeben). Kapselt
 * `window.confirm`, damit die Absicht am Aufrufort lesbar ist und wir später gegen einen hübscheren
 * Dialog tauschen können, ohne jeden Aufrufer anzufassen. In Headless-Umgebungen ohne `confirm`
 * (E2E/SSR) wird die Aktion durchgelassen (`true`), statt zu werfen.
 */
export function confirmAction(message: string): boolean {
  if (typeof window === "undefined" || typeof window.confirm !== "function") return true;
  return window.confirm(message);
}

/**
 * Respektiert die System-Einstellung „Bewegung reduzieren". Wir gaten Konfetti/Fly-by und Haptik
 * daran – CSS blendet die Overlay-Ebene zwar aus, kann aber Ton/Vibration und das Aufbauen der
 * Konfetti-Daten nicht verhindern.
 */
export function prefersReducedMotion(): boolean {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") return false;
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}
