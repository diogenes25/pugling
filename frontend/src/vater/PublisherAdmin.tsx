import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { PublisherResponse } from "../lib/types";

/**
 * Das Verlags-Vokabular – wie das Schlagwort-Vokabular in InterestTagAdmin eine geteilte Taxonomie, die
 * sonst nebenbei entsteht (beim Anlegen einer Reihe). Hier lässt sich ein Tippfehler ("Coernelsen")
 * korrigieren oder eine Dublette löschen (B-63: ohne diese Fläche kann eine falsch angelegte Zeile nie
 * wieder zusammengeführt werden).
 */
export function PublisherAdmin({ onChanged }: { onChanged: () => void }) {
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const [open, setOpen] = useState(false);
  const publishers = useAsync<PublisherResponse[]>(() => api.publishers(applied || undefined), [applied]);
  const action = useAction();

  if (!open) {
    return (
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto", alignSelf: "flex-start" }}
        onClick={() => setOpen(true)}>🏷️ Verlags-Vokabular verwalten</button>
    );
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
                  onSave={(name) => action.run(() => api.updatePublisher(p.id, { name }), "Gespeichert.")
                    .then((ok) => { if (ok) { publishers.reload(); onChanged(); } })}
                  onDelete={() => {
                    // Die Zahl ist der Punkt: Serien verlieren nur die Zuordnung (SetNull), keine Sperre nötig.
                    if (!confirmAction(
                      `Verlag „${p.name}" löschen? ${p.seriesCount} Reihe(n) verlieren nur die Zuordnung `
                      + "und bleiben nutzbar.")) return;
                    action.run(() => api.deletePublisher(p.id), "Verlag gelöscht.")
                      .then((ok) => { if (ok) { publishers.reload(); onChanged(); } });
                  }} />
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
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />
    </section>
  );
}

/** Eine Zeile; der Speichern-Knopf erscheint erst, wenn sich der Name wirklich geändert hat. */
function PublisherRow({ publisher, busy, onSave, onDelete }: {
  publisher: PublisherResponse;
  busy: boolean;
  onSave: (name: string) => void;
  onDelete: () => void;
}) {
  const [name, setName] = useState(publisher.name);
  const dirty = name.trim() !== publisher.name;

  return (
    <tr>
      <td>
        <input aria-label={`Name von ${publisher.name}`} value={name}
          onChange={(e) => setName(e.target.value)} style={{ maxWidth: 200 }} />
        {/* Der Slug ist unveränderlich – er wird gezeigt, damit klar ist, worauf Reihen zeigen. */}
        <div className="muted" style={{ fontSize: 11 }}><code>{publisher.slug}</code></div>
      </td>
      <td className="num">{publisher.seriesCount}</td>
      <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
        {dirty && (
          <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy || !name.trim()}
            aria-label={`„${publisher.name}" speichern`} onClick={() => onSave(name.trim())}>OK</button>
        )}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
          aria-label={`Löschen: ${publisher.name}`} onClick={onDelete}>Löschen</button>
      </td>
    </tr>
  );
}
