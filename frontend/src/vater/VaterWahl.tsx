import type { PerspectiveKey } from "./navigation";

/** Die zwei Antworten des Wahlschritts: als Vater betreuen oder als Creator Stoff bauen. */
export type WahlKey = Extract<PerspectiveKey, "betreuen" | "erstellen">;

const OPTIONEN: { key: WahlKey; icon: string; titel: string; zweck: string }[] = [
  { key: "betreuen", icon: "👀", titel: "Vater", zweck: "Kinder, Tagesziel und Shop – geht auch am Handy." },
  { key: "erstellen", icon: "✏️", titel: "Creator", zweck: "Übungen erstellen und ansehen – nur am Rechner." },
];

/**
 * Der Wahlschritt nach der PIN (B-663): ein Konto mit beiden Rollen sagt, ob es jetzt als Vater oder als Creator arbeitet.
 * Bisher entschied das allein der zuletzt geklickte Umschalter – an einem neuen Gerät landete der Vater dann im Betreuen
 * und sah „Erstellen" daneben. Vorbelegt ist die gemerkte Wahl: der Knopf hat den Fokus, Enter nimmt ihn.
 *
 * Nur eine Oberflächenwahl, keine Rechte: das Token trägt weiter beide Rollen, der Umschalter bleibt erreichbar.
 */
export function VaterWahl({ vorbelegt, name, onWahl, onAbmelden }: {
  vorbelegt: WahlKey; name: string; onWahl: (key: WahlKey) => void; onAbmelden: () => void;
}) {
  return (
    <div className="app-vater" style={{ justifyContent: "center", alignItems: "center" }}>
      <section className="card" aria-labelledby="vater-wahl-titel"
        style={{ width: 420, maxWidth: "90vw", display: "flex", flexDirection: "column", gap: 14 }}>
        <h1 id="vater-wahl-titel" style={{ fontSize: 22, margin: 0 }}>Hallo {name} – woran arbeitest du jetzt?</h1>
        {OPTIONEN.map((o) => (
          <button key={o.key} type="button"
            className={`btn ${o.key === vorbelegt ? "" : "ghost"}`}
            // Vorbelegt heißt: Fokus auf der gemerkten Wahl. Ein einzelner Knopf mit Fokus ist ohne Zusatzschritt mit
            // Enter bestätigt – eine Radiogruppe mit eigenem „Weiter" wäre ein Klick mehr für dieselbe Antwort.
            autoFocus={o.key === vorbelegt}
            // Der Name ist nur die Antwort; der Zweck-Satz hängt als Beschreibung daran, sonst hieße der Knopf den ganzen Satz.
            aria-label={`Als ${o.titel}`}
            aria-describedby={`vater-wahl-${o.key}`}
            onClick={() => onWahl(o.key)}
            style={{ textAlign: "left", display: "flex", flexDirection: "column", alignItems: "flex-start", gap: 2 }}>
            <span><span aria-hidden="true">{o.icon}</span> Als {o.titel}</span>
            <span id={`vater-wahl-${o.key}`} className="muted" style={{ fontSize: 13, fontWeight: 400 }}>{o.zweck}</span>
          </button>
        ))}
        {/* Wer am falschen Konto gelandet ist, muss nicht erst eine Seite wählen, um wieder herauszukommen. */}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", alignSelf: "flex-start" }}
          onClick={onAbmelden}>Abmelden</button>
      </section>
    </div>
  );
}
