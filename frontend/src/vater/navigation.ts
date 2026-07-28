/*
 * Die Informationsarchitektur des Vater-Webs als **Daten**, nicht als JSX.
 *
 * Ein Vater-Konto trägt zwei Rollen (Creator + Supervisor, siehe docs/grundprinzip.md), und das UI zeigte
 * beide gleichzeitig: „Belohnungen" stand neben „Lehrwerke". Wer sein Kind steuern wollte, sah
 * Autorenwerkzeug; wer Inhalte baute, sah Münz-Verwaltung. Die Gruppierung der Nav hat das gemildert,
 * aber nicht getrennt.
 *
 * Darum drei **Perspektiven**. Sie sind keine Rechte (ein Vater darf alles), sondern eine Antwort auf
 * „woran arbeite ich gerade":
 *
 *   Betreuen  – Wie läuft es bei meinem Kind, was steuere ich?   (Supervisor)
 *   Zuweisen  – Welches Kind lernt welchen Stoff, mit welcher Pflicht?
 *   Erstellen – Welcher Stoff existiert überhaupt?               (Creator / Lehrer)
 *
 * „Zuweisen" ist bewusst **eigenständig** und nicht Teil von „Betreuen": es ist die einzige Stelle, an
 * der beide Rollen sich treffen – es verbraucht den Katalog des Creators und erzeugt die Pflicht des
 * Supervisors. Und es hat ein eigenes Vokabular (Position, Rhythmus, Bestehensgrenze, Münz-Malus), das
 * die anderen zwei nie brauchen.
 *
 * Die aktive Perspektive wird aus dem **Pfad** abgeleitet (siehe `perspectiveOfPath`), nicht in einem
 * State gehalten: sonst öffnete ein Lesezeichen die richtige Seite in der falschen Perspektive, und die
 * Navigation zeigte etwas anderes als der Inhalt. Aus demselben Grund wandert keine Route – die Pfade
 * bleiben, was sie sind.
 */

/** Ein Eintrag der Bereichs-Navigation. */
export interface NavEntry {
  /** Zielpfad (absolut, damit er ohne Kontext stimmt). */
  to: string;
  /** Beschriftung inklusive Symbol – jeder Eintrag trägt eines, sonst wirkt die Reihe unfertig. */
  label: string;
  /** Nur der Startseiten-Eintrag: exakter Abgleich, sonst wäre er auf jeder Unterseite aktiv. */
  end?: boolean;
}

/** Schlüssel einer Perspektive; zugleich der Wert für `aria-current`-Entscheidungen. */
export type PerspectiveKey = "betreuen" | "zuweisen" | "erstellen";

/** Eine Perspektive: Startseite, Umschalter-Beschriftung und ihre Bereiche. */
export interface Perspective {
  key: PerspectiveKey;
  /** Kurzname im Umschalter. */
  label: string;
  /** Symbol im Umschalter. */
  icon: string;
  /** Ein Satz, der die Perspektive erklärt – steht als `title` am Umschalter und auf der Startseite. */
  purpose: string;
  /** Startseite; das Ziel des Umschalters. */
  home: string;
  entries: NavEntry[];
}

export const PERSPECTIVES: Perspective[] = [
  {
    key: "betreuen",
    label: "Betreuen",
    icon: "👀",
    purpose: "Wie läuft es bei deinem Kind – und was steuerst du daran?",
    home: "/vater",
    entries: [
      { to: "/vater", label: "🏠 Kinder & Heute", end: true },
      { to: "/vater/class-tests", label: "📝 Klassenarbeiten" },
      { to: "/vater/rewards", label: "🏆 Belohnungen" },
      { to: "/vater/shop", label: "🛒 Shop" },
      { to: "/vater/konto", label: "💰 Kontostand" },
    ],
  },
  {
    key: "zuweisen",
    label: "Zuweisen",
    icon: "🎯",
    purpose: "Welches Kind lernt welchen Stoff – mit welcher Pflicht?",
    home: "/vater/plaene",
    entries: [
      { to: "/vater/plaene", label: "🗂️ Lehrpläne", end: true },
      { to: "/vater/wizard", label: "🧭 Assistent" },
    ],
  },
  {
    key: "erstellen",
    label: "Erstellen",
    icon: "✏️",
    purpose: "Der Stoff selbst: Übungen und die Bausteine, aus denen sie entstehen.",
    home: "/vater/inhalte",
    entries: [
      { to: "/vater/inhalte", label: "🏠 Werkstatt", end: true },
      { to: "/vater/exercises", label: "📚 Übungen" },
      { to: "/vater/vocab", label: "🔤 Vokabeln" },
      { to: "/vater/lueckentexte", label: "📄 Lückentexte" },
      { to: "/vater/katalog", label: "🗂️ Katalog" },
      { to: "/vater/lehrwerke", label: "📕 Lehrwerke" },
      { to: "/vater/fachlehrer", label: "🎓 Fachlehrer" },
      { to: "/vater/media", label: "🖼️ Bilder" },
    ],
  },
];

/**
 * Pfad-Präfix → Perspektive. Nur Seiten, die **nicht** schon als Nav-Eintrag auftauchen, brauchen hier
 * einen Eintrag – die Unterseiten. Ohne sie stünde die Navigation beim Öffnen einer Plan-Seite auf
 * „Betreuen", und der Vater müsste raten, wo er ist.
 */
// Präfixe stehen **ohne** Schlussstrich: der Abgleich hängt ihn selbst an. Mit „/vater/plan/" hätte er
// nach „/vater/plan//" gesucht und nie getroffen.
const EXTRA_ROUTES: [prefix: string, key: PerspectiveKey][] = [
  // Kindbezogenes gehört zum Betreuen: Stammdaten, Lernstand, Ziele.
  ["/vater/kind", "betreuen"],
  // Ein Plan und sein Anlegen sind das Zuweisen selbst. Beißt sich nicht mit „/vater/plaene": das trennt
  // sich schon am fünften Zeichen, und der Abgleich verlangt exakte Gleichheit oder einen Schrägstrich.
  ["/vater/plan", "zuweisen"],
];

/**
 * Die Perspektive zu einem Pfad. Längstes Präfix gewinnt, damit `/vater/exercises/neu` bei „Erstellen"
 * landet und nicht über einen kürzeren Treffer woanders. Unbekanntes (z. B. `/vater/profil`) fällt auf
 * „Betreuen" zurück – die Startperspektive des Vaters; sein Konto gehört zu keiner Werkbank.
 */
export function perspectiveOfPath(pathname: string): PerspectiveKey {
  const candidates: [string, PerspectiveKey][] = [
    ...PERSPECTIVES.flatMap((p) => p.entries.map((e) => [e.to, p.key] as [string, PerspectiveKey])),
    ...EXTRA_ROUTES,
  ];
  let best: [string, PerspectiveKey] | null = null;
  for (const [prefix, key] of candidates) {
    // `/vater` selbst ist Präfix von allem – es darf nur exakt treffen, sonst gewinnt es nie das Rennen,
    // aber es verfälschte die Länge der Bestenliste.
    const hit = prefix === "/vater" ? pathname === "/vater" : pathname === prefix || pathname.startsWith(`${prefix}/`);
    if (hit && (best === null || prefix.length > best[0].length)) best = [prefix, key];
  }
  return best?.[1] ?? "betreuen";
}

/** Die Perspektive zu ihrem Schlüssel (nie `undefined`: der Schlüssel kommt aus dieser Datei). */
export const perspective = (key: PerspectiveKey): Perspective =>
  PERSPECTIVES.find((p) => p.key === key)!;

/**
 * Seiten, die zu **keiner** Perspektive gehören: das eigene Konto und das Entwicklungswerkzeug. Sie sind
 * für jedes Konto erreichbar und dürfen von der Perspektiven-Schranke nicht weggeleitet werden – ein Lehrer
 * käme sonst nicht an sein Profil, weil `perspectiveOfPath` für Unbekanntes auf „Betreuen" zurückfällt.
 */
const NEUTRAL_PREFIXES = ["/vater/profil", "/vater/anmerkungen"];

/** Gehört der Pfad zu keiner Perspektive (Konto, Entwicklungswerkzeug)? */
export const isNeutralPath = (pathname: string): boolean =>
  NEUTRAL_PREFIXES.some((p) => pathname === p || pathname.startsWith(`${p}/`));

/**
 * Welche Perspektiven ein Konto überhaupt hat.
 *
 * Ein **Lehrer-Konto** (`Creator`) betreut kein Kind – Betreuen und Zuweisen wären für ihn leere Räume, und
 * die dahinterliegenden Endpunkte weisen ihn ohnehin ab. Er sieht darum nur die Werkstatt; bei einer
 * einzigen Perspektive entfällt der Umschalter ganz, denn ein Schalter mit einer Stellung ist Dekoration.
 *
 * Das ist **keine** Rechteprüfung: die sitzt im Server. Hier geht es darum, niemandem Türen zu zeigen, die
 * für ihn verschlossen sind.
 */
export function perspectivesFor(role: "Supervisor" | "Creator"): Perspective[] {
  return role === "Creator" ? PERSPECTIVES.filter((p) => p.key === "erstellen") : PERSPECTIVES;
}

/**
 * Die Startseite eines Kontos: der Vater beginnt beim Betreuen, der Lehrer in seiner Werkstatt.
 * Gebraucht an zwei Stellen – nach dem Anmelden und wenn ein Lehrer `/vater` direkt aufruft.
 */
export const homeFor = (role: "Supervisor" | "Creator"): string =>
  perspective(role === "Creator" ? "erstellen" : "betreuen").home;

const STORAGE_KEY = "pugling.vater.perspective";

/**
 * Hält die **bewusst gewählte** Perspektive fest – gesetzt nur beim Klick auf den Umschalter, nicht bei
 * jeder Navigation. Genau darum darf die Anmeldung später dorthin führen: der Wert ist eine Entscheidung
 * des Nutzers, keine Nebenwirkung seines Weges. Ein Lehrer landet so in seiner Werkstatt statt jedes Mal
 * in der Vater-Sicht.
 */
export function rememberPerspective(key: PerspectiveKey): void {
  try { localStorage.setItem(STORAGE_KEY, key); } catch { /* privater Modus: dann eben nicht merken */ }
}

/**
 * Die gemerkte Perspektive – oder `null`. Der gelesene Wert wird **geprüft**: ein alter oder von Hand
 * verbogener Eintrag darf nicht in einer Navigation auf `undefined` enden.
 */
export function rememberedPerspective(): PerspectiveKey | null {
  let raw: string | null = null;
  try { raw = localStorage.getItem(STORAGE_KEY); } catch { return null; }
  return PERSPECTIVES.some((p) => p.key === raw) ? (raw as PerspectiveKey) : null;
}
