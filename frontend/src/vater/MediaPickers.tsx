import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
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
 *
 * Der Ladezustand kommt aus `useAsync`, dem **lesenden** Primitiv. Vorher stand hier `useAction` – das
 * schreibende: es hielt zwar denselben Zustand, verwirft seit der Wiedereintritts-Sperre aber eine zweite
 * Aktion. Für ein Speichern ist das richtig, für eine Suche wäre es „ich suche neu, es passiert nichts".
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
  /*
   * Was **abgeschickt** wurde – und wie oft. `useAsync` löst über seine Deps aus, und ein Objekt je
   * Absenden macht dieselbe Suche wiederholbar: mit einem bloßen String wäre „Suchen" bei unverändertem
   * Text keine Dep-Änderung und täte nichts. `null` heißt „noch nicht gesucht" (kein Aufruf beim Anzeigen).
   */
  const [query, setQuery] = useState<{ text: string; nr: number } | null>(null);
  const found = useAsync<MediaAssetResponse[] | null>(
    () => (query === null ? Promise.resolve(null) : api.media({ search: query.text || undefined, take })),
    [query],
  );
  const hits = found.data;
  // `useAsync` startet mit `loading: true`, bevor überhaupt gesucht wurde – ohne `query` ist das kein Laden.
  const searching = found.loading && query !== null;

  function find(e: React.FormEvent) {
    e.preventDefault();
    setQuery((q) => ({ text: search.trim(), nr: (q?.nr ?? 0) + 1 }));
  }

  return (
    <>
      <form className="row" style={{ gap: 8, marginTop: 10 }} onSubmit={find}>
        <input aria-label="Bild suchen" autoComplete="off" value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Bild in der Bibliothek suchen" style={{ maxWidth: 280 }} />
        <button type="submit" className="btn ghost small" style={{ width: "auto" }} disabled={searching}>
          {searching ? "Suche…" : "Suchen"}
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
                  disabled: busy || already || searching,
                  onClick: () => onPick(a.id),
                }} />
              );
            })}
          </div>
        ))}
      {/*
        Über `StatusBanner`, nicht als bedingter Kasten: die Live-Region muss **dauerhaft** im DOM stehen,
        sonst entstehen Region und Text gleichzeitig und die Ansage bleibt aus (begründet in
        `StatusBanner.tsx`, gepinnt von seinem Test).
      */}
      <StatusBanner message={found.error ? { ok: false, text: found.error } : null} />
    </>
  );
}
