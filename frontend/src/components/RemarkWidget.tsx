/**
 * Das Erfassungs-Widget für Test-Anmerkungen.
 *
 * **Reibungsarmut ist die Existenzberechtigung.** Wer beim Testen etwas bemerkt, tippt heute eine Zeile in
 * ein Textdokument – das dauert Sekunden. Ein Widget, das erst ein Thema aus einem Pulldown verlangt, ist
 * *langsamer* als das und wird nach zwei Wochen nicht mehr benutzt. Deshalb: Tastenkürzel, ein Feld,
 * Enter. Die Kategorie ist ein optionaler Klick, kein Pflichtfeld – einordnen kann der Skill später aus
 * dem Text.
 *
 * Das Widget ist **Eingang plus Lesesicht**, kein Chat: Die Liste zeigt Antworten an, bietet aber kein
 * Antwortfeld. Rückfragen stellst du in Claude Code, wo du ohnehin stehst.
 */
import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "../lib/api";
import { clearRecentErrors } from "../lib/remarks";
import { useRemarkSnapshot } from "../lib/remarkContext";
import { useAction } from "../lib/useAction";
import type { Remark, RemarkCategory } from "../lib/types";
import { StatusBanner } from "./StatusBanner";

/** Ein Klick genügt – aber keiner ist auch in Ordnung. */
const QUICK_CATEGORIES: { value: RemarkCategory; label: string }[] = [
  { value: "Bug", label: "🐞 Bug" },
  { value: "Ui", label: "🎨 UI" },
  { value: "Code", label: "⚙️ Code" },
  { value: "Content", label: "📚 Inhalt" },
  { value: "Idea", label: "💡 Idee" },
  { value: "Question", label: "❓ Frage" },
];

const STATUS_LABEL: Record<string, string> = {
  Open: "offen",
  Planned: "eingeplant",
  Done: "erledigt",
  Rejected: "verworfen",
};

/**
 * Abschalter für den E2E-Lauf. Playwright startet `npm run dev`, also ist `import.meta.env.DEV` dort
 * **wahr** und das Widget liefe mit – es könnte Klicks abfangen oder mit Tastatureingaben kollidieren.
 * Der Schalter sitzt bewusst im `localStorage` (browser-seitig) und nicht in einer Env-Variablen: Bei
 * `reuseExistingServer` verwendet Playwright einen schon laufenden Dev-Server, dessen Env es nicht setzt.
 */
const OFF_KEY = "pugling.remarks.off";

function isDisabled(): boolean {
  try {
    return localStorage.getItem(OFF_KEY) === "1";
  } catch {
    return false;
  }
}

/**
 * Abstand zum unteren Rand. Die Sohn-Arcade braucht mehr: Dort klebt `.sohn-nav` unten, und ein Widget
 * bei 12px läge darüber und finge die Klicks auf die Navigation ab. 96px ist dieselbe Höhe, auf die auch
 * `.toast` ausweicht – ein Wert, der sich dort schon bewährt hat.
 */
export function RemarkWidget({ bottomOffset = 12 }: { bottomOffset?: number } = {}) {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const [category, setCategory] = useState<RemarkCategory>("Unspecified");
  const [lastId, setLastId] = useState<number | null>(null);
  const [mine, setMine] = useState<Remark[] | null>(null);
  const [showList, setShowList] = useState(false);
  const action = useAction();
  const snapshot = useRemarkSnapshot();
  const inputRef = useRef<HTMLTextAreaElement | null>(null);
  // Schützt den abgeschickten POST, nicht nur den State – siehe `submit()`.
  const inFlight = useRef(false);

  // Alt+A öffnet und schließt. Bewusst mit Modifier: ein nackter Buchstabe würde jede Eingabe in einem
  // Formularfeld kapern.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.altKey && !e.ctrlKey && !e.metaKey && e.key.toLowerCase() === "a") {
        e.preventDefault();
        setOpen((v) => !v);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  const loadMine = useCallback(async () => {
    try {
      setMine((await api.myRemarks(10)).items);
    } catch {
      // Die Liste ist Beiwerk – scheitert sie, bleibt das Erfassen trotzdem benutzbar.
      setMine([]);
    }
  }, []);

  useEffect(() => {
    if (showList && mine === null) void loadMine();
  }, [showList, mine, loadMine]);

  async function submit() {
    const value = text.trim();
    if (!value) {
      action.fail("Bitte etwas eintragen.");
      return;
    }

    // Ref statt `action.busy`: Der State ist beim nächsten Tastendruck noch nicht aktualisiert. Bei
    // gedrückt gehaltener Enter-Taste (Auto-Repeat) gingen sonst mehrere POSTs mit demselben Text raus,
    // bevor die erste Antwort da ist – der Knopf ist über `disabled` geschützt, die Tastatur wäre es
    // nicht, und genau die ist hier der Hauptweg.
    if (inFlight.current) return;
    inFlight.current = true;

    const ctx = snapshot();
    let created: Remark | null = null;
    try {
      created = await action.runFor(() =>
        api.createRemark({
          text: value,
          category: category === "Unspecified" ? undefined : category,
          context: {
            route: ctx.route,
            appArea: ctx.appArea,
            childId: ctx.childId ?? null,
            exerciseId: ctx.exerciseId ?? null,
            studyPlanId: ctx.studyPlanId ?? null,
            planPositionId: ctx.planPositionId ?? null,
            contextJson: ctx.contextJson,
            recentErrorsJson: ctx.recentErrorsJson,
          },
        }),
      );
    } finally {
      // Auch nach einem Fehlschlag freigeben – sonst wäre das Widget nach einem Netzwerkfehler tot.
      inFlight.current = false;
    }

    if (!created) return;

    setLastId(created.id);
    setText("");
    setCategory("Unspecified");
    // Der Puffer hat seinen Zweck erfüllt – sonst schleppte die nächste Anmerkung dieselben Fehler mit.
    clearRecentErrors();
    // Die Liste ist jetzt veraltet; beim nächsten Aufklappen frisch holen.
    setMine(null);
  }

  function onKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    // Enter sendet, Shift+Enter macht einen Absatz – ein Feld, ein Tastendruck.
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      // Auto-Repeat der gedrückt gehaltenen Taste ignorieren; das Ref-Gate in `submit()` fängt den Rest.
      if (e.repeat) return;
      void submit();
    }
    if (e.key === "Escape") setOpen(false);
  }

  if (isDisabled()) return null;

  if (!open) {
    return (
      <button
        type="button"
        className="btn ghost small"
        style={{ ...launcher, bottom: bottomOffset }}
        onClick={() => setOpen(true)}
        title="Anmerkung erfassen (Alt+A)"
        aria-label="Anmerkung erfassen"
      >
        📝
      </button>
    );
  }

  return (
    // Kein Backdrop und kein `role="dialog"`: Das Widget soll die Seite *nicht* blockieren – man notiert
    // ja gerade etwas über das, was dahinter steht, und will weiterklicken können.
    <section style={{ ...panel, bottom: bottomOffset }} aria-label="Anmerkung erfassen">
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <strong style={{ fontSize: 14 }}>📝 Anmerkung</strong>
        <span className="muted" style={{ fontSize: 12 }}>Alt+A</span>
        <span style={{ flex: 1 }} />
        <button type="button" className="btn ghost small" style={{ width: "auto" }}
          onClick={() => setShowList((v) => !v)} aria-expanded={showList}>
          {showList ? "Liste zu" : "Meine"}
        </button>
        <button type="button" className="btn ghost small" style={{ width: "auto" }}
          onClick={() => setOpen(false)} aria-label="Schließen">✕</button>
      </div>

      <textarea
        ref={inputRef}
        value={text}
        onChange={(e) => setText(e.target.value)}
        onKeyDown={onKeyDown}
        rows={3}
        placeholder="Was ist dir aufgefallen? (Enter sendet)"
        aria-label="Text der Anmerkung"
        style={{ width: "100%", resize: "vertical" }}
      />

      <div style={{ display: "flex", flexWrap: "wrap", gap: 4 }}>
        {QUICK_CATEGORIES.map((c) => (
          <button
            key={c.value}
            type="button"
            className="btn ghost small"
            style={{ width: "auto", opacity: category === c.value ? 1 : 0.55 }}
            aria-pressed={category === c.value}
            // Nochmal klicken hebt die Wahl wieder auf – die Kategorie bleibt freiwillig.
            onClick={() => setCategory((prev) => (prev === c.value ? "Unspecified" : c.value))}
          >
            {c.label}
          </button>
        ))}
      </div>

      <button type="button" className="btn" onClick={() => void submit()} disabled={action.busy}>
        {action.busy ? "Speichert …" : "Speichern"}
      </button>

      <StatusBanner message={action.message} />

      {lastId !== null && (
        // Die Id ist der eigentliche Ertrag: Damit gehst du zu Claude Code.
        <div className="banner ok" style={{ marginTop: 8 }}>
          Gespeichert als <b>#{lastId}</b> — in Claude Code: „Beantworte die Frage {lastId}."
        </div>
      )}

      {showList && (
        <div style={{ maxHeight: 220, overflowY: "auto", display: "grid", gap: 6 }}>
          {mine === null && <span className="muted" style={{ fontSize: 13 }}>Lädt …</span>}
          {mine?.length === 0 && <span className="muted" style={{ fontSize: 13 }}>Noch nichts erfasst.</span>}
          {mine?.map((r) => (
            <article key={r.id} className="card" style={{ padding: 8 }}>
              <div style={{ display: "flex", gap: 6, alignItems: "baseline" }}>
                <b style={{ fontSize: 13 }}>#{r.id}</b>
                <span className="muted" style={{ fontSize: 12 }}>{STATUS_LABEL[r.status] ?? r.status}</span>
                {r.category !== "Unspecified" && (
                  <span className="muted" style={{ fontSize: 12 }}>· {r.category}</span>
                )}
              </div>
              <div style={{ fontSize: 13 }}>{r.text}</div>
              {r.answer && (
                // Lesesicht, kein Antwortfeld – hier begänne sonst der Weg zum Messenger.
                <div className="muted" style={{ fontSize: 12, marginTop: 4, whiteSpace: "pre-wrap" }}>
                  ↳ {r.answer}
                </div>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

// zIndex unter der Feier-Ebene (`.cel-layer`, 80) wäre zu wenig – das Widget muss anklickbar bleiben,
// während oben Konfetti läuft. 900 liegt darüber, aber unter dem Modal-Backdrop (1000): Ein offener
// Dialog soll das Widget verdecken, nicht umgekehrt.
const launcher: React.CSSProperties = {
  position: "fixed", right: 12, zIndex: 900,
  width: "auto", padding: "8px 10px", opacity: 0.75,
};

const panel: React.CSSProperties = {
  position: "fixed", right: 12, zIndex: 900,
  width: "min(360px, calc(100vw - 24px))",
  display: "flex", flexDirection: "column", gap: 8,
  background: "linear-gradient(180deg, var(--card-hi), var(--card))",
  border: "1.5px solid var(--stroke)", borderRadius: 18, padding: 12,
  boxShadow: "0 18px 40px -18px rgba(0,0,0,.7)",
};
