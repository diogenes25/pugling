/**
 * Anmerkungen beim Testen – der Fehler-Ringpuffer.
 *
 * Für eine Bug-Anmerkung ist „was ging kurz vorher schief?" der wertvollste Kontext: Er erspart das
 * Nachstellen. Der Puffer hält deshalb die letzten Fehlschläge vor, damit das Widget sie mitschicken kann.
 *
 * **Sicherheitsregel (verbindlich):** Es werden ausschließlich **Metadaten** gespeichert – Methode,
 * Pfad, Status, Fehler-`code`, Zeitstempel. **Keine** Request-/Response-Bodies, **keine** Header,
 * **keine** Tokens und **keine Query-Werte**. Der Login-Request trägt die PIN im Body; ein roher
 * Mitschnitt legte sie im Klartext in die Datenbank und trüge sie über den Export ins Repo.
 * Die Funktionen unten nehmen darum bewusst nur Einzelwerte entgegen und niemals ein Request-Objekt –
 * so kann ein Body strukturell gar nicht erst hineingeraten.
 */

/** Ein Eintrag im Ringpuffer. Bewusst flach und rein deskriptiv – siehe Sicherheitsregel oben. */
export type RemarkErrorEntry = {
  /** Woher der Fehler kam: abgewiesene Antwort, gescheiterte Verbindung oder ein JS-Fehler. */
  kind: "http" | "network" | "js";
  /** HTTP-Methode (nur bei `http`/`network`). */
  method?: string;
  /** Pfad **ohne Query** (nur bei `http`/`network`). */
  path?: string;
  /** HTTP-Status (nur bei `http`). */
  status?: number;
  /** Maschinenlesbarer Fehler-Code aus den ProblemDetails (nur bei `http`). */
  code?: string;
  /**
   * Kurze Fehlermeldung – nur bei `network`/`js`, wo sie die eigentliche Information ist. Auf
   * {@link MAX_MESSAGE} Zeichen gekürzt; Nutzereingaben stehen hier nicht drin.
   */
  message?: string;
  /** Zeitpunkt (ISO-8601, UTC). */
  at: string;
};

/** So viele Fehler werden vorgehalten – genug für „was lief kurz vorher schief", ohne den Body aufzublähen. */
export const RING_SIZE = 10;

/** Obergrenze für Meldungstexte, damit ein Stacktrace-artiger Text den Puffer nicht sprengt. */
export const MAX_MESSAGE = 200;

let ring: RemarkErrorEntry[] = [];

function push(entry: RemarkErrorEntry): void {
  ring.push(entry);
  if (ring.length > RING_SIZE) ring = ring.slice(-RING_SIZE);
}

/**
 * Reduziert eine URL auf den Pfad. Die Query fällt **absichtlich** weg: Sie trägt IDs, Filter und
 * Suchbegriffe, also potenziell Inhaltliches, das in einem Fehlerprotokoll nichts zu suchen hat.
 */
function pathOf(url: string): string {
  try {
    return new URL(url, typeof location !== "undefined" ? location.origin : "http://localhost").pathname;
  } catch {
    // Unparsbare URL: alles ab dem ersten `?` abschneiden, damit auch hier keine Query durchrutscht.
    return url.split("?")[0];
  }
}

function shorten(text: string): string {
  const clean = text.trim();
  return clean.length > MAX_MESSAGE ? `${clean.slice(0, MAX_MESSAGE)}…` : clean;
}

/** Eine vom Server abgewiesene Anfrage festhalten (4xx/5xx). */
export function recordHttpError(method: string, url: string, status: number, code?: string): void {
  push({ kind: "http", method, path: pathOf(url), status, code, at: new Date().toISOString() });
}

/** Eine Anfrage festhalten, die den Server gar nicht erreicht hat (offline, DNS, abgebrochen). */
export function recordNetworkError(method: string, url: string, message: string): void {
  push({ kind: "network", method, path: pathOf(url), message: shorten(message), at: new Date().toISOString() });
}

/** Einen JS-Fehler festhalten. Hier ist die Meldung die eigentliche Information, deshalb wird sie (gekürzt) übernommen. */
export function recordJsError(message: string): void {
  push({ kind: "js", message: shorten(message), at: new Date().toISOString() });
}

/** Die aktuell gepufferten Fehler, älteste zuerst. Kopie – der Aufrufer kann den Puffer nicht verändern. */
export function recentErrors(): RemarkErrorEntry[] {
  return [...ring];
}

/** Puffer leeren (nach dem Absenden einer Anmerkung und in Tests). */
export function clearRecentErrors(): void {
  ring = [];
}

/**
 * Die gepufferten Fehler als JSON für das Feld `recentErrorsJson`; `null`, wenn nichts anliegt
 * (dann soll die Spalte leer bleiben statt ein `[]` zu tragen).
 */
export function recentErrorsJson(): string | null {
  return ring.length > 0 ? JSON.stringify(ring) : null;
}

let installed = false;

/**
 * Hängt die globalen JS-Fehlerlauscher ein (einmalig, mehrfacher Aufruf ist ein No-op).
 * Bewusst hart abgesichert: Der Puffer ist Beiwerk – er darf nie eine Seite kaputt machen.
 */
export function installGlobalErrorCapture(): void {
  if (installed || typeof window === "undefined") return;
  installed = true;

  window.addEventListener("error", (e) => {
    try {
      recordJsError(e.message || "Unbekannter Fehler");
    } catch {
      /* Protokollieren darf nie stören. */
    }
  });

  window.addEventListener("unhandledrejection", (e) => {
    try {
      const reason: unknown = e.reason;
      recordJsError(reason instanceof Error ? reason.message : String(reason));
    } catch {
      /* Protokollieren darf nie stören. */
    }
  });
}
