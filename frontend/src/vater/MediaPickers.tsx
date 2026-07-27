import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import type { MediaAssetResponse, MediaPurpose } from "../lib/types";

/*
 * Die geteilten Bausteine der drei Bild-Zuordnungen (Vokabel, Übungs-Item, Übungs-Titelbild). Sie
 * unterscheiden sich nur in den Endpunkten – die Kachel und die Bibliothekssuche sind identisch, und drei
 * Kopien liefen zwangsläufig auseinander (eine zeigte die Karten-, eine die Thumb-Auflösung).
 */

/**
 * URL eines Assets in der gewünschten Rolle, mit Rückfall auf die erste vorhandene Variante.
 * `undefined` heißt: das Asset hat noch **keine** Datei – dann gehört dorthin kein `<img>`, sondern ein
 * Hinweis. Ein `<img>` ohne `src` zeigt sonst das kaputte Bildsymbol des Browsers.
 */
export const variantUrl = (a: MediaAssetResponse, purpose: MediaPurpose = "Card"): string | undefined =>
  (a.variants.find((v) => v.purpose === purpose) ?? a.variants[0])?.url;

/** Ein Knopf unter der Kachel („Entfernen", „+ Zuordnen") – optional, denn ohne Schreibrecht gibt es keinen. */
export interface ThumbAction {
  label: string;
  disabled?: boolean;
  onClick: () => void;
}

/**
 * Eine Bildkachel mit Beschreibung und optionalem Knopf. `width`/`height` stehen als Attribute am `<img>`
 * (nicht nur im Stil), damit der Platz vor dem Laden reserviert ist und die Liste nicht springt.
 */
export function AssetThumb({ asset, size = 96, purpose = "Card", action }: {
  asset: MediaAssetResponse;
  size?: number;
  purpose?: MediaPurpose;
  action?: ThumbAction;
}) {
  const url = variantUrl(asset, purpose);
  return (
    <figure style={{ margin: 0, textAlign: "center", maxWidth: size + 14 }}>
      {url
        ? <img src={url} alt={asset.description} width={size} height={size} loading="lazy"
            style={{ width: size, height: size, objectFit: "cover", borderRadius: 8 }} />
        : <span className="pill mag">keine Datei</span>}
      <figcaption className="muted" style={{ fontSize: 12, overflowWrap: "anywhere" }}>{asset.description}</figcaption>
      {action && (
        /*
         * Der zugängliche Name beginnt mit der **sichtbaren** Beschriftung und nennt dann das Bild: bei
         * zwölf Treffern heißt „+ Zuordnen" allein nichts, und wer den Knopf per Sprache anspricht, sagt
         * genau das, was er liest (WCAG „Label in Name").
         */
        <button type="button" className="btn ghost small" style={{ width: "auto" }}
          aria-label={`${action.label}: ${asset.description}`}
          disabled={action.disabled} onClick={action.onClick}>
          {action.label}
        </button>
      )}
    </figure>
  );
}

/**
 * Die Bibliothekssuche: Treffer zeigen und einen davon zuordnen. Gesucht wird **auf Absenden**, nicht bei
 * jedem Tastendruck – die Bibliothek kann groß sein, und ein Treffer, der unter dem Finger wegwandert,
 * lässt sich nicht anklicken.
 */
export function MediaSearch({ busy, linkedIds, onPick, take = 12 }: {
  /** Sperrt die Treffer, solange die *äußere* Zuordnung läuft. */
  busy: boolean;
  /** Schon zugeordnete Assets – sie bleiben sichtbar, aber nicht wählbar. */
  linkedIds: Set<number>;
  onPick: (assetId: number) => void;
  take?: number;
}) {
  const [search, setSearch] = useState("");
  const [hits, setHits] = useState<MediaAssetResponse[] | null>(null);
  const action = useAction();

  async function find(e: React.FormEvent) {
    e.preventDefault();
    await action.run(async () => setHits(await api.media({ search: search.trim() || undefined, take })));
  }

  return (
    <>
      <form className="row" style={{ gap: 8, marginTop: 10 }} onSubmit={find}>
        <input aria-label="Bild suchen" autoComplete="off" value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Bild in der Bibliothek suchen" style={{ maxWidth: 280 }} />
        <button type="submit" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Suche…" : "Suchen"}
        </button>
      </form>

      {hits && (hits.length === 0
        ? <p className="muted" style={{ marginTop: 8 }}>Nichts gefunden – neue Bilder legst du unter „Bilder" an.</p>
        : (
          <div className="row" style={{ gap: 10, flexWrap: "wrap", marginTop: 8 }}>
            {hits.map((a) => {
              const already = linkedIds.has(a.id);
              return (
                <AssetThumb key={a.id} asset={a} purpose="Thumb" action={{
                  label: already ? "schon dabei" : "+ Zuordnen",
                  disabled: busy || already || action.busy,
                  onClick: () => onPick(a.id),
                }} />
              );
            })}
          </div>
        ))}
      <StatusBanner message={action.message} />
    </>
  );
}
