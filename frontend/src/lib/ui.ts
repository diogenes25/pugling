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

/** Was `radioGroupKeys` von einem Tastaturereignis braucht – React- und DOM-Ereignis erfüllen es beide. */
type GroupKeyEvent = {
  key: string; altKey: boolean; ctrlKey: boolean; metaKey: boolean;
  currentTarget: EventTarget | null; preventDefault(): void;
};

const RADIO_STEP: Record<string, "next" | "prev" | "first" | "last"> = {
  ArrowRight: "next", ArrowDown: "next", ArrowLeft: "prev", ArrowUp: "prev", Home: "first", End: "last",
};

/**
 * Das Tastaturmodell einer Radiogruppe aus Knöpfen (B-352, WAI-ARIA-Muster „Radio Group"): Pfeile wandern umlaufend
 * über die nicht gesperrten Optionen und wählen sie, `Home`/`End` springen an die Enden. Gehört als `onKeyDown` an den
 * `radiogroup`-Container; jede Option trägt `tabIndex={gewählt ? 0 : -1}`, damit die Gruppe EIN Tab-Stopp ist. Ohne
 * beides sagt `role="radio"` Pfeiltasten an, die nichts tun. Gewählt wird per `click()`, damit der Aufrufer nur
 * seinen `onClick` kennt. Vorausgesetzt ist, dass die gewählte Option nicht gesperrt ist – sonst hätte die Gruppe keinen
 * Tab-Stopp (alle Gruppen des Vater-Webs haben eine wählbare Vorgabe).
 */
export function radioGroupKeys(e: GroupKeyEvent): void {
  const step = RADIO_STEP[e.key];
  // Mit Alt/Strg/Cmd gehört die Taste dem Browser (Alt+Links ist „Zurück“), nicht der Gruppe.
  if (!step || e.altKey || e.ctrlKey || e.metaKey || !(e.currentTarget instanceof HTMLElement)) return;
  const radios = [...e.currentTarget.querySelectorAll<HTMLButtonElement>('[role="radio"]')].filter((r) => !r.disabled);
  const at = radios.indexOf(document.activeElement as HTMLButtonElement);
  if (radios.length === 0 || at < 0) return;
  e.preventDefault();
  const n = radios.length;
  const to = step === "first" ? 0 : step === "last" ? n - 1 : (at + (step === "next" ? 1 : -1) + n) % n;
  // Schon dort (Home auf der ersten, eine einzige freie Option): ein zweiter Klick ist nicht für jeden Aufrufer folgenlos –
  // der Assistent liest ihn als Wahl des Vaters und belegt die spät ankommende Kinderliste nicht mehr vor (B-356).
  if (to === at) return;
  radios[to].focus();
  radios[to].click();
}
