import { useId, useState } from "react";
import { api } from "../lib/api";
import { nachObenMelden } from "../lib/nachObenMelden";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import type { ActionMessage } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { PublisherResponse } from "../lib/types";

/**
 * Das Verlags-Vokabular – wie das Schlagwort-Vokabular in InterestTagAdmin eine geteilte Taxonomie, die
 * sonst nebenbei entsteht (beim Anlegen einer Reihe). Hier lässt sich ein Tippfehler ("Coernelsen")
 * korrigieren oder eine Dublette löschen (B-63: ohne diese Fläche kann eine falsch angelegte Zeile nie
 * wieder zusammengeführt werden).
 *
 * Meldungen gehen an den Platz der **Seite** (B-224): Dieser Baustein war nach B-212 der letzte Platz der
 * Lehrwerke-Seite, an dem noch **Server**-Meldungen standen – ein grünes „Gespeichert." hier unten stand
 * neben dem grünen „… steht im Katalog" der Seite, und keines konnte das andere räumen. (Die lokalen Banner
 * von `SeriesForm` und `UnitForm` tragen nur noch Eingabeprüfung und „Nichts geändert.") Nur der Lesefehler
 * der Liste bleibt lokal, wie bei jedem Lesefehler der Seite.
 */
export function PublisherAdmin({ onChanged, onMeldung }: {
  onChanged: () => void;
  /** Setzt bzw. räumt den Meldungsplatz der Seite — Pflicht, aus demselben Grund wie bei `TextbookForm` (B-197). */
  onMeldung: (m: ActionMessage | null) => void;
}) {
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const [open, setOpen] = useState(false);
  const publishers = useAsync<PublisherResponse[]>(() => api.publishers(applied || undefined), [applied]);
  const action = useAction();

  if (!open) {
    return (
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto", alignSelf: "flex-start" }}
        // Öffnen räumt den Platz der Seite (B-197) – eine stehengebliebene Bestätigung gehört zu einer
        // Handlung, die mit dem Aufklappen abgeschlossen ist.
        onClick={() => { onMeldung(null); setOpen(true); }}>🏷️ Verlags-Vokabular verwalten</button>
    );
  }

  async function speichern(p: PublisherResponse, name: string) {
    onMeldung(null);
    const ok = await nachObenMelden(action, onMeldung, () => api.updatePublisher(p.id, { name }), "Gespeichert.");
    if (ok !== null) { publishers.reload(); onChanged(); }
  }

  async function loeschen(p: PublisherResponse) {
    // Nur noch die EIGENEN Reihen nennen: seit B-127 sperrt der Server, sobald eine fremde oder herrenlose
    // daran hängt, und der Knopf ist dann gar nicht erst anklickbar. Der alte Satz („N Reihe(n) verlieren
    // nur die Zuordnung") versprach das Gegenteil und führte in einen 409, den der Vater erst nach dem
    // Bestätigen sah.
    const own = p.seriesCount - p.foreignSeriesCount;
    if (!confirmAction(
      `Verlag „${p.name}" löschen? ${own} eigene Reihe(n) verlieren nur die Zuordnung `
      + "und bleiben nutzbar.")) return;
    onMeldung(null);
    const ok = await nachObenMelden(action, onMeldung, () => api.deletePublisher(p.id), "Verlag gelöscht.");
    if (ok !== null) { publishers.reload(); onChanged(); }
  }

  return (
    <section className="card">
      <div className="row" style={{ alignItems: "center" }}>
        <h3 style={{ margin: 0 }}>Verlags-Vokabular</h3>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
          onClick={() => setOpen(false)}>Schließen</button>
      </div>
      <p className="muted" style={{ fontSize: 13 }}>
        Eine Zeile je Verlag – der Slug macht das Anlegen idempotent, hier lässt sich ein Tippfehler
        korrigieren, statt eine zweite Schreibweise stehen zu lassen.
      </p>

      <form className="row" style={{ gap: 8, marginTop: 8 }}
        onSubmit={(e) => { e.preventDefault(); setApplied(search.trim()); }}>
        <input aria-label="Verlag suchen" value={search} onChange={(e) => setSearch(e.target.value)}
          placeholder="Verlag suchen…" style={{ maxWidth: 260 }} />
        <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }}>Suchen</button>
      </form>

      {publishers.error && <div className="banner err">{publishers.error}</div>}
      {publishers.loading && publishers.data === null ? <div className="loading">Lade…</div> : (
        <div style={{ overflowX: "auto", marginTop: 8 }}>
          <table className="table">
            <thead><tr><th>Verlag</th><th className="num">Reihen</th><th /></tr></thead>
            <tbody>
              {publishers.data?.map((p) => (
                <PublisherRow key={p.id} publisher={p} busy={action.busy}
                  onSave={(name) => { void speichern(p, name); }}
                  onDelete={() => { void loeschen(p); }} />
              ))}
              {publishers.data?.length === 0 && (
                <tr><td colSpan={3} className="muted">
                  Keine Verlage{applied ? " für diese Suche" : " – sie entstehen beim Anlegen einer Reihe"}.
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

/** Eine Zeile; der Speichern-Knopf erscheint erst, wenn sich der Name wirklich geändert hat. */
export function PublisherRow({ publisher, busy, onSave, onDelete }: {
  publisher: PublisherResponse;
  busy: boolean;
  onSave: (name: string) => void;
  onDelete: () => void;
}) {
  const uid = useId();
  const [name, setName] = useState(publisher.name);
  const dirty = name.trim() !== publisher.name;
  const locked = publisher.foreignSeriesCount > 0;
  const grundId = `${uid}-gesperrt`;

  return (
    <tr>
      <td>
        <input aria-label={`Name von ${publisher.name}`} value={name}
          onChange={(e) => setName(e.target.value)} style={{ maxWidth: 200 }} />
        {/* Der Slug ist unveränderlich – er wird gezeigt, damit klar ist, worauf Reihen zeigen. */}
        <div className="muted" style={{ fontSize: 11 }}><code>{publisher.slug}</code></div>
      </td>
      <td className="num">
        {publisher.seriesCount}
        {/* Die Zahl stand hier schon; was fehlte, war ihre **Folge**. „davon 2 fremd" ist eine Tatsache,
            aus der niemand „also nicht löschbar" ableiten muss – und genau das war der Teil, den B-150 in
            ein `title` gelegt hatte, das nie erscheint. */}
        {locked && (
          <div id={grundId} className="muted" style={{ fontSize: 11 }}>
            davon {publisher.foreignSeriesCount} fremd – darum nicht löschbar
          </div>
        )}
      </td>
      <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
        {dirty && (
          <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy || !name.trim()}
            aria-label={`„${publisher.name}" speichern`} onClick={() => onSave(name.trim())}>OK</button>
        )}
        {/*
          * Gesperrt statt anklickbar: Der Server antwortet hier mit 409, und im geseedeten Katalog ist das
          * ein Dauerzustand (die herrenlose „Green Line 1" an „Klett"). Ein Knopf, der nie funktionieren
          * kann, gehört nicht angeboten.
          *
          * **`aria-disabled` für die fachliche Sperre, echtes `disabled` nur für „läuft gerade"** (B-160).
          * Ein `disabled` Form-Control ist nicht fokussierbar und empfängt in Chromium und WebKit keine
          * Pointer-Events: Der `title` wurde darum nie zum Tooltip, und wer per Tab durch die Tabelle geht,
          * traf den Knopf nie und erfuhr nichts von der Sperre. Die Konvention „jeder mutierende Knopf
          * trägt `disabled={busy}`" (frontend/CLAUDE.md) bleibt unangetastet – ihr Grund (Playwrights
          * Wartepunkt auf eine laufende Aktion) gilt dem Vorgang, nicht einem Dauerzustand.
          *
          * Der Preis steht direkt darunter: Ohne echtes `disabled` hält der Browser den Klick nicht mehr
          * auf, also muss der Handler es selbst tun – sonst liefe der Creator wieder in den 409.
          */}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
          aria-disabled={locked}
          aria-describedby={locked ? grundId : undefined}
          title={locked
            ? "Gesperrt: An diesem Verlag hängen Reihen, die dir nicht gehören – fremde oder herrenlose "
              + "aus dem geteilten Katalog. Löschen würde auch deren Zuordnung entfernen."
            : undefined}
          aria-label={`Löschen: ${publisher.name}`}
          onClick={() => { if (!locked) onDelete(); }}>Löschen</button>
      </td>
    </tr>
  );
}
