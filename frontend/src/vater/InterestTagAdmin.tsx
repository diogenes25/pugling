import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { INTEREST_FACETS, interestFacetLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { InterestFacet, InterestTagResponse, UpdateInterestTagDto } from "../lib/types";
import { ListenAusfall } from "../components/ListenAusfall";

/*
 * Das Schlagwort-Vokabular – die **eine** Taxonomie, aus der Bilder *und* Kinder schöpfen. Genau deshalb
 * ist sie überhaupt verwaltbar: passen die Wörter der beiden Seiten nicht zusammen, findet die Bildauswahl
 * nichts, und das Kind lernt ohne Bild.
 *
 * Schlagworte entstehen sonst nebenbei (beim Taggen eines Bildes, beim Eintragen eines Interesses) – hier
 * lassen sie sich nachziehen: Tippfehler korrigieren, Facette richtigstellen, Synonyme ergänzen. Der
 * **Slug bleibt** dabei unangetastet; er ist die Referenz, an der beide Seiten hängen.
 */
export function InterestTagAdmin() {
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  const [open, setOpen] = useState(false);
  const tags = useAsync<InterestTagResponse[]>(() => api.interestTags(applied || undefined), [applied]);
  const action = useAction();

  if (!open) {
    return (
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto", alignSelf: "flex-start" }}
        onClick={() => setOpen(true)}>🏷️ Schlagwort-Vokabular verwalten</button>
    );
  }

  return (
    <section className="card">
      <div className="row" style={{ alignItems: "center" }}>
        <h3 style={{ margin: 0 }}>Schlagwort-Vokabular</h3>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }}
          onClick={() => setOpen(false)}>Schließen</button>
      </div>
      <p className="muted" style={{ fontSize: 13 }}>
        Dieselben Schlagworte beschreiben <strong>Bilder</strong>, <strong>Interessen der Kinder</strong> und
        die <strong>Einkleidung von Übungen</strong> – nur deshalb ist die Bildauswahl berechenbar. Die
        Zähler zeigen, woran ein Wort hängt; eingekleidete Übungen sperren zusätzlich das Löschen.
        Der <strong>Bild-Prompt</strong> ist die englische Szene, die der KI-Creator für dieses Interesse in
        Vokabelbilder malt – ohne Markennamen („in a blocky voxel world" statt „Minecraft"). Ohne Prompt
        bekommt das Interesse keine eigenen Bilder. <strong>Für Kinder</strong> gibt ein Schlagwort für die
        Interessen-Auswahl der Kinder frei – neue Schlagworte (etwa aus dem Freitext eines anderen Elternteils)
        sieht dort niemand, bis du sie freigibst.
      </p>

      <form className="row" style={{ gap: 8, marginTop: 8 }}
        onSubmit={(e) => { e.preventDefault(); setApplied(search.trim()); }}>
        <input aria-label="Schlagwort suchen" value={search} onChange={(e) => setSearch(e.target.value)}
          placeholder="Schlagwort suchen…" style={{ maxWidth: 260 }} />
        <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }}>Suchen</button>
      </form>

      {/* Die Rückmeldung steht ÜBER der Tabelle: Die Liste wird bis zu 200 Zeilen lang, und ein Grund
          unterhalb davon liegt für den, der in Zeile 40 auf „Löschen" drückt, außerhalb des Sichtfelds –
          für ihn passiert dann scheinbar nichts (B-160). */}
      <StatusBanner message={action.message} style={{ marginTop: 0, marginBottom: 10 }} />

      <ListenAusfall was="Die Stichwörter" fehler={tags.error} laedt={tags.loading} onRetry={tags.reload} schluessel={applied} />
      {tags.loading && tags.data === null ? <div className="loading">Lade…</div> : (
        <div style={{ overflowX: "auto", marginTop: 8 }}>
          <table className="table">
            <thead>
              <tr>
                <th>Schlagwort</th><th>Facette</th><th>Synonyme</th><th>Bild-Prompt</th><th>Für Kinder</th>
                <th className="num">Bilder</th><th className="num">Kinder</th><th className="num">Übungen</th><th />
              </tr>
            </thead>
            <tbody>
              {tags.data?.map((t) => (
                <TagRow key={t.id} tag={t} busy={action.busy}
                  onSave={(dto) => action.run(() => api.updateInterestTag(t.id, dto), "Gespeichert.").then((ok) => { if (ok) tags.reload(); })}
                  onDelete={() => {
                    // Ein Wort, das Übungen einkleidet, weist der Server ab (B-206) - dann gar nicht erst
                    // fragen, sondern sagen warum. Ein Bestätigungsdialog, der etwas verspricht, was
                    // danach verweigert wird, ist schlimmer als keiner (B-150).
                    if (t.exerciseCount > 0) {
                      action.fail(`„${t.label}" kleidet ${t.exerciseCount} Übung(en) ein und lässt sich `
                        + "darum nicht löschen. Nimm es zuerst im Bearbeiten-Dialog dieser Übungen heraus – "
                        + "die Einkleidung ist Autoren-Arbeit und nicht wiederherstellbar.");
                      return;
                    }
                    // Die Zahlen in der Warnung sind der Punkt: ein Wort mit vielen Bildern zu löschen
                    // verkleinert die Auswahl für jedes Kind, das es mag.
                    if (!confirmAction(
                      `Schlagwort „${t.label}" löschen? Es verschwindet von ${t.mediaCount} Bild(ern) und `
                      + `aus ${t.childCount} Kind-Profil(en). Die Bilder selbst bleiben.`)) return;
                    action.run(() => api.deleteInterestTag(t.id), "Schlagwort gelöscht.")
                      .then((ok) => { if (ok) tags.reload(); });
                  }} />
              ))}
              {tags.data?.length === 0 && (
                <tr><td colSpan={9} className="muted">
                  Keine Schlagworte{applied ? " für diese Suche" : " – sie entstehen beim Taggen von Bildern und Interessen"}.
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

/** Eine Zeile; der Speichern-Knopf erscheint erst, wenn sich wirklich etwas geändert hat. */
function TagRow({ tag, busy, onSave, onDelete }: {
  tag: InterestTagResponse;
  busy: boolean;
  onSave: (dto: UpdateInterestTagDto) => void;
  onDelete: () => void;
}) {
  const [label, setLabel] = useState(tag.label);
  const [facet, setFacet] = useState<InterestFacet>(tag.facet);
  const [synonyms, setSynonyms] = useState(tag.synonyms.join(", "));
  const [imagePrompt, setImagePrompt] = useState(tag.imagePrompt ?? "");
  const [visible, setVisible] = useState(tag.visibleToChildren);

  const parsedSynonyms = synonyms.split(",").map((s) => s.trim()).filter(Boolean);
  const promptChanged = imagePrompt.trim() !== (tag.imagePrompt ?? "");
  const dirty = label.trim() !== tag.label
    || facet !== tag.facet
    || parsedSynonyms.join("|") !== tag.synonyms.join("|")
    || promptChanged
    || visible !== tag.visibleToChildren;

  return (
    <tr>
      <td>
        <input aria-label={`Bezeichnung von ${tag.label}`} value={label}
          onChange={(e) => setLabel(e.target.value)} style={{ maxWidth: 160 }} />
        {/* Der Slug ist unveränderlich – er wird gezeigt, damit klar ist, worauf Bilder und Kinder zeigen. */}
        <div className="muted" style={{ fontSize: 11 }}><code>{tag.slug}</code></div>
      </td>
      <td>
        <select aria-label={`Facette von ${tag.label}`} value={facet}
          onChange={(e) => setFacet(e.target.value as InterestFacet)} style={{ maxWidth: 140 }}>
          {INTEREST_FACETS.map((f) => <option key={f.value} value={f.value}>{f.label}</option>)}
        </select>
      </td>
      <td>
        <input aria-label={`Synonyme von ${tag.label}`} value={synonyms}
          onChange={(e) => setSynonyms(e.target.value)} placeholder="z. B. Fußball, Soccer"
          style={{ maxWidth: 200 }} />
      </td>
      <td>
        <input aria-label={`Bild-Prompt von ${tag.label}`} value={imagePrompt} maxLength={200}
          onChange={(e) => setImagePrompt(e.target.value)} placeholder="z. B. on a soccer pitch"
          style={{ maxWidth: 220 }} />
      </td>
      <td style={{ textAlign: "center" }}>
        <input type="checkbox" aria-label={`${tag.label} für Kinder freigeben`} checked={visible}
          onChange={(e) => setVisible(e.target.checked)} />
      </td>
      <td className="num">{tag.mediaCount}</td>
      <td className="num">{tag.childCount}</td>
      {/* Die dritte Zahl, seit B-46 im Vertrag und bis B-206 nirgends gezeigt. Sie ist die einzige, die
          das Löschen sperrt - sie ungenannt zu lassen machte die Sperre unerklärlich. */}
      <td className="num">{tag.exerciseCount}</td>
      <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
        {dirty && (
          <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy || !label.trim()}
            aria-label={`${interestFacetLabel(facet)} „${tag.label}" speichern`}
            onClick={() => onSave({
              label: label.trim(), facet, synonyms: parsedSynonyms,
              // Die Freigabe ist wie der Prompt eine globale Entscheidung: nur bei Änderung mitschicken, sonst setzte
              // eine Label-Korrektur aus einem älteren Stand die Entscheidung eines anderen Creators zurück.
              ...(visible !== tag.visibleToChildren ? { visibleToChildren: visible } : {}),
              // Nur bei Änderung mitschicken: das Vokabular ist global, und eine Label-Korrektur aus einem älteren
              // Stand soll den Prompt eines anderen Creators nicht überschreiben. Geleert heißt „entfernen" – null
              // allein hieße „unverändert" (PATCH-Semantik), darum der Schalter.
              ...(!promptChanged ? {} : imagePrompt.trim() ? { imagePrompt: imagePrompt.trim() } : { clearImagePrompt: true }),
            })}>OK</button>
        )}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
          aria-label={`Löschen: ${tag.label}`} onClick={onDelete}>Löschen</button>
      </td>
    </tr>
  );
}
