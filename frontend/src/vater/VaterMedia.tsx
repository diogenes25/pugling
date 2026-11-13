import { useRef, useState } from "react";
import { InterestTagAdmin } from "./InterestTagAdmin";
import { StatusBanner } from "../components/StatusBanner";
import { api, errorMessage } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { confirmAction, radioGroupKeys } from "../lib/ui";
import type { ContentRating, MediaAssetResponse, MediaUsage } from "../lib/types";
import { ListenAusfall } from "../components/ListenAusfall";

/**
 * Die Bild-Bibliothek: der Vorrat, aus dem die App je Kind auswählt.
 *
 * Das Leitbild ist **ein Motiv, viele Bilder** – zu „laufen" liegen nebeneinander das Einhorn im
 * Comic-Stil, Flash und ein Foto. Welches ein Kind sieht, entscheidet sein Profil; hier wird nur der
 * Vorrat gepflegt. Deshalb sind die Schlagworte kein Beiwerk: sie sind dasselbe Vokabular, aus dem die
 * Interessen der Kinder schöpfen. Ein Bild ohne Tags ist seit B-485 das neutrale Grundbild: es passt zu
 * keinem Interesse, bekommt aber jedes Kind, für das kein passenderes da ist.
 *
 * Ein Bild kommt entweder als Datei-Upload herein (der Server erzeugt die Auflösungen selbst) oder als
 * fertige URL – beides landet im selben Store.
 */
export function VaterMedia() {
  const [search, setSearch] = useState("");
  const [applied, setApplied] = useState("");
  // Der Filter auf KI-erzeugte Bilder ist der Nachsorge-Weg aus B-485: generierte Bilder gehen ohne
  // Freigabe auf die Karte, hier sichtet und löscht der Vater sie gesammelt.
  const [onlyGenerated, setOnlyGenerated] = useState(false);
  const list = useAsync<MediaAssetResponse[]>(
    () => api.media({ search: applied || undefined, origin: onlyGenerated ? "Generated" : undefined }),
    [applied, onlyGenerated]);

  return (
    <>
      <section>
        <div className="row">
          <h2 className="h-section">Bilder</h2>
        </div>
        <p className="sub">
          Zu einem Motiv gehören mehrere Darstellungen – erst die Auswahl macht die Bebilderung
          individuell. Zugeordnet werden sie bei der Vokabel (Reiter <strong>Vokabeln</strong>).
        </p>

        {/* Das Vokabular gehört hierher: es ist die Sprache, in der Bilder und Kind-Interessen
            beschrieben werden – passen die Wörter nicht zusammen, findet die Auswahl nichts. */}
        <InterestTagAdmin />

        <form
          className="row" style={{ gap: 8, marginBottom: 10 }}
          onSubmit={(e) => { e.preventDefault(); setApplied(search.trim()); }}
        >
          <input
            aria-label="Bilder durchsuchen" value={search} onChange={(e) => setSearch(e.target.value)}
            placeholder="Suche in Beschreibung oder Key" style={{ maxWidth: 320 }}
          />
          <button type="submit" className="btn ghost small" style={{ width: "auto" }}>Suchen</button>
          <label className="checkline" style={{ marginLeft: 8 }}>
            <input type="checkbox" checked={onlyGenerated} onChange={(e) => setOnlyGenerated(e.target.checked)} />
            nur KI-erzeugte
          </label>
        </form>

        <ListenAusfall was="Die Bilder" fehler={list.error} laedt={list.loading} onRetry={list.reload}
          schluessel={`${applied}|${onlyGenerated}`} />
        {list.loading && list.data === null ? <div className="loading">Lade…</div> : (
          // Beim Umschalten des Filters bleiben die alten Zeilen stehen, bis die neuen da sind – gedimmt und
          // aria-busy, damit der Wechsel nicht wie „nichts passiert" aussieht (B-116).
          <table className="table" aria-busy={list.loading} style={{ opacity: list.loading ? 0.5 : 1 }}>
            <thead><tr><th>Vorschau</th><th>Beschreibung</th><th>Schlagworte</th><th>Eignung</th><th /></tr></thead>
            <tbody>
              {list.data?.map((a) => <AssetRow key={a.id} asset={a} onChanged={list.reload} />)}
              {list.data?.length === 0 && (
                <tr><td colSpan={5} className="muted">
                  {applied || onlyGenerated ? "Keine Bilder für diesen Filter." : "Noch keine Bilder. Lege unten eines an."}
                </td></tr>
              )}
            </tbody>
          </table>
        )}
      </section>

      <NewAsset onCreated={list.reload} />
    </>
  );
}

/** Die Kartengröße ist das, was das Kind später sieht – als Vorschau am passendsten. */
function cardUrl(asset: MediaAssetResponse): string | null {
  return (asset.variants.find((v) => v.purpose === "Card") ?? asset.variants[0])?.url ?? null;
}

function AssetRow({ asset, onChanged }: { asset: MediaAssetResponse; onChanged: () => void }) {
  const [usage, setUsage] = useState<MediaUsage[] | null>(null);
  const [tagInput, setTagInput] = useState("");
  const action = useAction();
  const url = cardUrl(asset);

  async function remove() {
    // Vor dem Löschen zeigen, was verloren geht – die API sperrt bewusst nicht (ein fehlendes Bild
    // hinterlässt keine Lücke im Inhalt, es schrumpft nur die Auswahl).
    const where = usage ?? await api.mediaUsage(asset.id).catch(() => []);
    const hint = where.length > 0 ? `\n\nZugeordnet an ${where.length} Stelle(n) – die Zuordnung geht mit.` : "";
    if (!confirmAction(`„${asset.description}" wirklich löschen?${hint}`)) return;
    if (await action.run(() => api.deleteMedia(asset.id))) onChanged();
  }

  async function addTags(e: React.FormEvent) {
    e.preventDefault();
    const tags = tagInput.split(",").map((t) => t.trim()).filter(Boolean);
    if (tags.length === 0) return;
    if (!await action.run(() => api.tagMedia(asset.id, tags))) return;
    setTagInput("");
    onChanged();
  }

  return (
    <>
      <tr>
        <td>
          {url
            ? <img src={url} alt={asset.description} style={{ width: 64, height: 64, objectFit: "cover", borderRadius: 8 }} />
            : <span className="pill mag">keine Datei</span>}
        </td>
        <td>
          {asset.description}
          {asset.origin === "Generated" && (
            <span className="pill" style={{ marginLeft: 6 }} title="KI-erzeugtes Bild">KI</span>
          )}
          <div className="muted" style={{ fontFamily: "monospace", fontSize: 12 }}>{asset.key}</div>
          {asset.origin === "Generated" && asset.source && (
            <div className="muted" style={{ fontSize: 12 }}>{asset.source}</div>
          )}
        </td>
        <td>
          {/* Ein Bild ohne Schlagwort ist seit B-485 das neutrale Grundbild, das jedes Kind ohne passenderes
              bekommt – für ein KI-Bild Absicht, für ein hochgeladenes meist ein Versäumnis. */}
          {asset.tags.length === 0
            ? asset.origin === "Generated"
              ? <span className="pill">neutral</span>
              : <span className="pill mag">ohne Schlagwort</span>
            : asset.tags.map((t) => <span key={t} className="pill" style={{ marginRight: 4 }}>{t}</span>)}
        </td>
        <td>{asset.rating === "Everyone" ? <span className="pill lime">für alle</span> : <span className="pill mag">{asset.rating}</span>}</td>
        <td className="row" style={{ gap: 6, justifyContent: "flex-end" }}>
          <button
            type="button" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy}
            onClick={async () => setUsage(usage ? null : await api.mediaUsage(asset.id))}
          >Wo benutzt?</button>
          <button type="button" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy} onClick={remove}>Löschen</button>
        </td>
      </tr>
      {(usage || action.message) && (
        <tr>
          <td colSpan={5}>
            <StatusBanner message={action.message} style={{ marginTop: 0 }} />
            {usage && (usage.length === 0
              ? <p className="muted">Noch nirgends zugeordnet – so sieht es kein Kind.</p>
              : <ul className="muted">{usage.map((u) => <li key={`${u.carrier}-${u.carrierId}`}>{u.carrier}: {u.label}</li>)}</ul>)}
            <form className="row" style={{ gap: 8, marginTop: 6 }} onSubmit={addTags}>
              <input
                aria-label={`Schlagworte für ${asset.description}`} value={tagInput}
                onChange={(e) => setTagInput(e.target.value)} placeholder="Schlagworte, kommagetrennt"
                style={{ maxWidth: 320 }}
              />
              <button type="submit" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy}>Ergänzen</button>
            </form>
          </td>
        </tr>
      )}
    </>
  );
}

/**
 * Bild hinzufügen – per Datei oder per URL.
 *
 * Der Upload ist der Normalfall: der Server skaliert selbst auf Thumbnail/Karte/Groß und ermittelt eine
 * Platzhalterfarbe, der Vater braucht also kein Grafikprogramm. Die URL-Eingabe bleibt daneben, weil
 * Stock-Bilder oft schon irgendwo liegen und ein Download-und-wieder-hochladen unnötig wäre.
 */
function NewAsset({ onCreated }: { onCreated: () => void }) {
  const [mode, setMode] = useState<"upload" | "url">("upload");
  const [description, setDescription] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [url, setUrl] = useState("");
  const [tags, setTags] = useState("");
  const [rating, setRating] = useState<ContentRating>("Everyone");
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const fileInput = useRef<HTMLInputElement>(null);

  async function create(e: React.FormEvent) {
    e.preventDefault();
    if (!description.trim()) return;
    if (mode === "upload" ? !file : !url.trim()) return;
    setBusy(true); setMsg(null);
    try {
      if (mode === "upload") {
        await api.uploadMedia(file!, { description: description.trim(), tags: tags.trim() || undefined, rating });
      } else {
        await api.createMedia({
          description: description.trim(),
          rating,
          origin: "Stock",
          tags: tags.split(",").map((t) => t.trim()).filter(Boolean),
          // Bei einer Fremd-URL kennen wir die echten Maße nicht – der Server kann sie nicht messen,
          // ohne die Datei zu laden. Ein Kartenformat als Angabe genügt für die Auslieferung.
          variants: [{ purpose: "Card", url: url.trim(), width: 512, height: 512 }],
        });
      }
      setDescription(""); setUrl(""); setTags(""); setFile(null);
      // Ein <input type="file"> ist unkontrolliert – ohne dieses Zurücksetzen bliebe der alte Dateiname stehen.
      if (fileInput.current) fileInput.current.value = "";
      setMsg(mode === "upload" ? "Bild hochgeladen – Auflösungen erzeugt." : "Bild angelegt.");
      onCreated();
    } catch (e) { setMsg(errorMessage(e)); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <h3 className="h-section">Bild hinzufügen</h3>

      <div className="row" style={{ gap: 8, marginBottom: 8 }} role="radiogroup" aria-label="Quelle" onKeyDown={radioGroupKeys}>
        {([["upload", "📤 Datei hochladen"], ["url", "🔗 Per URL"]] as const).map(([value, label]) => (
          <button
            key={value} type="button" className={`pill toggle-pill ${mode === value ? "lime" : ""}`}
            role="radio" aria-checked={mode === value} tabIndex={mode === value ? 0 : -1} onClick={() => setMode(value)}
          >{label}</button>
        ))}
      </div>

      <form className="form-grid" style={{ alignItems: "end" }} onSubmit={create}>
        <div className="field">
          <label htmlFor="m-desc">Was ist zu sehen?</label>
          <input
            id="m-desc" value={description} onChange={(e) => setDescription(e.target.value)}
            placeholder="Ein Einhorn läuft im Comic-Stil"
          />
        </div>
        {mode === "upload" ? (
          <div className="field">
            <label htmlFor="m-file">Bilddatei</label>
            <input
              id="m-file" ref={fileInput} type="file" accept="image/*"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            />
          </div>
        ) : (
          <div className="field">
            <label htmlFor="m-url">Bild-URL</label>
            <input id="m-url" value={url} onChange={(e) => setUrl(e.target.value)} placeholder="https://…" />
          </div>
        )}
        <div className="field">
          <label htmlFor="m-tags">Schlagworte</label>
          <input id="m-tags" value={tags} onChange={(e) => setTags(e.target.value)} placeholder="Einhorn, Comic" />
        </div>
        <div className="field">
          <label htmlFor="m-rating">Eignung</label>
          <select id="m-rating" value={rating} onChange={(e) => setRating(e.target.value as ContentRating)}>
            <option value="Everyone">Für alle</option>
            <option value="Teen">Ab ca. 12</option>
            <option value="Mature">Erwachsene</option>
          </select>
        </div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>
          {busy ? (mode === "upload" ? "Lade hoch…" : "Lege an…") : "Anlegen"}
        </button>
      </form>
      <p className="sub" style={{ marginTop: 8 }}>
        Die Beschreibung ist zugleich der Alt-Text für Screenreader – also beschreiben, nicht benennen.
        Ohne Schlagworte gilt das Bild als neutral: jedes Kind ohne passenderes Bild bekommt es.
        {mode === "upload" && " Aus der hochgeladenen Datei erzeugt der Server alle Größen selbst."}
      </p>
      <div className={`live-region${msg ? " banner ok" : ""}`} style={{ marginTop: 10 }} role="status" aria-live="polite">{msg}</div>
    </section>
  );
}
