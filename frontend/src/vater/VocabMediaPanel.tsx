import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { MediaAssetResponse, MediaLinkResponse } from "../lib/types";

/**
 * Die Bilder einer Store-Vokabel. Die Zuordnung hier ist die **Regel**: sie wirkt in jeder Übung, die
 * dieses Wort nutzt – nicht nur in einer.
 *
 * Mehrere Bilder sind ausdrücklich erwünscht und nicht etwa ein Versehen: aus ihnen wählt der Server je
 * Kind das passende. Ein einzelnes Bild bedeutet, dass alle Kinder dasselbe sehen.
 */
export function VocabMediaPanel({ vocabularyId, word }: { vocabularyId: number; word: string }) {
  const links = useAsync<MediaLinkResponse[]>(() => api.vocabularyMedia(vocabularyId), [vocabularyId]);
  const [search, setSearch] = useState("");
  const [hits, setHits] = useState<MediaAssetResponse[] | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function find(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true); setErr(null);
    try { setHits(await api.media({ search: search.trim() || undefined, take: 12 })); }
    catch (e) { setErr(errorMessage(e)); }
    finally { setBusy(false); }
  }

  async function link(assetId: number) {
    setBusy(true); setErr(null);
    try { await api.linkVocabularyMedia(vocabularyId, assetId); links.reload(); }
    catch (e) { setErr(errorMessage(e)); }
    finally { setBusy(false); }
  }

  async function unlink(linkId: number) {
    setBusy(true); setErr(null);
    try { await api.unlinkVocabularyMedia(vocabularyId, linkId); links.reload(); }
    catch (e) { setErr(errorMessage(e)); }
    finally { setBusy(false); }
  }

  const linkedIds = new Set((links.data ?? []).map((l) => l.asset.id));

  return (
    <div style={{ padding: "8px 0" }}>
      <h4 className="h-section" style={{ fontSize: "1rem" }}>Bilder für „{word}"</h4>
      {err && <div className="banner err">{err}</div>}

      {links.loading ? <div className="loading">Lade…</div> : (
        (links.data ?? []).length === 0
          ? <p className="muted">Noch kein Bild – diese Vokabel wird ohne Bild gelernt.</p>
          : (
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
              {links.data!.map((l) => (
                <figure key={l.id} style={{ margin: 0, textAlign: "center", maxWidth: 110 }}>
                  <img
                    src={(l.asset.variants.find((v) => v.purpose === "Card") ?? l.asset.variants[0])?.url}
                    alt={l.asset.description}
                    style={{ width: 96, height: 96, objectFit: "cover", borderRadius: 8 }}
                  />
                  <figcaption className="muted" style={{ fontSize: 12 }}>{l.asset.description}</figcaption>
                  <button
                    type="button" className="btn ghost small" style={{ width: "auto" }} disabled={busy}
                    aria-label={`${l.asset.description} entfernen`} onClick={() => unlink(l.id)}
                  >Entfernen</button>
                </figure>
              ))}
            </div>
          )
      )}

      <form className="row" style={{ gap: 8, marginTop: 10 }} onSubmit={find}>
        <input
          aria-label="Bild suchen" value={search} onChange={(e) => setSearch(e.target.value)}
          placeholder="Bild in der Bibliothek suchen" style={{ maxWidth: 280 }}
        />
        <button type="submit" className="btn ghost small" style={{ width: "auto" }} disabled={busy}>Suchen</button>
      </form>

      {hits && (
        hits.length === 0
          ? <p className="muted" style={{ marginTop: 8 }}>Nichts gefunden – neue Bilder legst du unter „Bilder" an.</p>
          : (
            <div className="row" style={{ gap: 10, flexWrap: "wrap", marginTop: 8 }}>
              {hits.map((a) => (
                <figure key={a.id} style={{ margin: 0, textAlign: "center", maxWidth: 110 }}>
                  <img
                    src={(a.variants.find((v) => v.purpose === "Card") ?? a.variants[0])?.url}
                    alt={a.description}
                    style={{ width: 96, height: 96, objectFit: "cover", borderRadius: 8, opacity: linkedIds.has(a.id) ? 0.4 : 1 }}
                  />
                  <figcaption className="muted" style={{ fontSize: 12 }}>{a.description}</figcaption>
                  <button
                    type="button" className="btn ghost small" style={{ width: "auto" }}
                    disabled={busy || linkedIds.has(a.id)}
                    onClick={() => link(a.id)}
                  >{linkedIds.has(a.id) ? "schon dabei" : "+ Zuordnen"}</button>
                </figure>
              ))}
            </div>
          )
      )}
    </div>
  );
}
