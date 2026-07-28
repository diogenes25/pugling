import { useRef, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { AssetThumb, MediaSearch } from "./MediaPickers";
import type { MediaLinkResponse } from "../lib/types";

/*
 * Die zwei Ebenen der Bebilderung eines Wortes – bewusst in einer Datei, weil sie nur zusammen Sinn ergeben:
 * die Zuordnung an der **Vokabel** ist die Regel (wirkt in jeder Übung), die am **Item** die Ausnahme (wirkt
 * nur in dieser einen Übung). Die Genauigkeits-Kaskade lautet **Item schlägt Vokabel**: sobald ein Item eigene
 * Bilder hat, spielt der Server ausschließlich diese aus.
 */

/**
 * Die Bilder einer Store-Vokabel. Die Zuordnung hier ist die **Regel**: sie wirkt in jeder Übung, die
 * dieses Wort nutzt – nicht nur in einer.
 *
 * Mehrere Bilder sind ausdrücklich erwünscht und nicht etwa ein Versehen: aus ihnen wählt der Server je
 * Kind das passende. Ein einzelnes Bild bedeutet, dass alle Kinder dasselbe sehen.
 */
export function VocabMediaPanel({ vocabularyId, word }: { vocabularyId: number; word: string }) {
  return (
    <MediaLinkEditor
      heading={`Bilder für „${word}"`}
      hint="Gilt in jeder Übung, die dieses Wort nutzt. Aus mehreren Bildern wählt der Server je Kind das passende."
      empty="Noch kein Bild – diese Vokabel wird ohne Bild gelernt."
      word={word}
      load={() => api.vocabularyMedia(vocabularyId)}
      deps={[vocabularyId]}
      onLink={(assetId) => api.linkVocabularyMedia(vocabularyId, assetId)}
      onUnlink={(linkId) => api.unlinkVocabularyMedia(vocabularyId, linkId)}
    />
  );
}

/**
 * Die übungslokale Übersteuerung: Bilder nur für dieses Wort **in dieser Übung**. Gedacht für den Fall, dass
 * derselbe Vokabeleintrag hier eine andere Bedeutung trägt als sonst (Homonyme) oder das Kapitel ein
 * bestimmtes Motiv verlangt – ohne die Regel an der Vokabel für alle anderen Übungen umzuwerfen.
 */
export function ExerciseItemMediaPanel({ exerciseId, itemId, word }: {
  exerciseId: number; itemId: number; word: string;
}) {
  return (
    <MediaLinkEditor
      heading={`Nur in dieser Übung: Bild für „${word}"`}
      hint={"Übersteuert die Bilder der Vokabel vollständig (Item schlägt Vokabel). Leer lassen heißt: es "
        + "gelten die Bilder der Vokabel."}
      empty="Keine Übersteuerung – es gelten die Bilder der Vokabel."
      word={word}
      load={() => api.exerciseItemMedia(exerciseId, itemId)}
      deps={[exerciseId, itemId]}
      onLink={(assetId) => api.linkExerciseItemMedia(exerciseId, itemId, assetId)}
      onUnlink={(linkId) => api.unlinkExerciseItemMedia(exerciseId, itemId, linkId)}
    />
  );
}

/**
 * Gemeinsamer Kern beider Ebenen: zugeordnete Bilder zeigen, in der Bibliothek suchen, zuordnen und lösen.
 * Sie unterscheiden sich nur in den Endpunkten und im Begleittext – zwei Kopien wären zwangsläufig
 * auseinandergelaufen.
 */
function MediaLinkEditor({ heading, hint, empty, word, load, deps, onLink, onUnlink }: {
  heading: string;
  hint: string;
  /** Was dasteht, wenn nichts zugeordnet ist – die Folge ist je Ebene eine andere. */
  empty: string;
  /** Das Wort – Vorbelegung der Bildbeschreibung beim Hochladen (sie ist zugleich der Alt-Text). */
  word: string;
  load: () => Promise<MediaLinkResponse[]>;
  deps: unknown[];
  onLink: (assetId: number) => Promise<unknown>;
  onUnlink: (linkId: number) => Promise<unknown>;
}) {
  const links = useAsync<MediaLinkResponse[]>(load, deps);
  const action = useAction();

  /** Liefert zurück, ob es geklappt hat – der Aufrufer räumt sein Formular nur dann auf. */
  async function mutate(fn: () => Promise<unknown>) {
    const ok = await action.run(fn);
    if (ok) links.reload();
    return ok;
  }

  /*
   * Hochladen **und** in einem Zug zuordnen. Ohne das war der Weg zu einem neuen Bild: Panel verlassen,
   * nach /vater/media wechseln, hochladen, zurück, suchen, zuordnen – fünf Schritte für den häufigsten Fall
   * („zu diesem Wort fehlt ein Bild"). Der Upload landet in derselben Bibliothek wie sonst; er ist hier nur
   * eine Abkürzung, keine zweite Ablage.
   */
  function uploadAndLink(file: File, description: string) {
    return mutate(async () => {
      const asset = await api.uploadMedia(file, { description });
      await onLink(asset.id);
    });
  }

  return (
    <div style={{ padding: "8px 0" }}>
      <h4 className="h-section" style={{ fontSize: "1rem" }}>{heading}</h4>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>{hint}</p>
      {links.error && <div className="banner err">{links.error}</div>}

      {links.loading ? <div className="loading">Lade…</div> : (
        (links.data ?? []).length === 0
          ? <p className="muted">{empty}</p>
          : (
            <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
              {links.data!.map((l) => (
                <AssetThumb key={l.id} asset={l.asset} action={{
                  label: "Entfernen",
                  disabled: action.busy,
                  onClick: () => mutate(() => onUnlink(l.id)),
                }} />
              ))}
            </div>
          )
      )}

      <MediaSearch busy={action.busy} linkedIds={new Set((links.data ?? []).map((l) => l.asset.id))}
        onPick={(assetId) => mutate(() => onLink(assetId))} />
      <UploadAndLink busy={action.busy} word={word} onUpload={uploadAndLink} />
      <StatusBanner message={action.message} />
    </div>
  );
}

/**
 * Ein neues Bild direkt hier hochladen. Die Beschreibung ist Pflicht, weil der Server sie als **Alt-Text**
 * weiterverwendet (`MediaAssetsController`: „Description is required (it doubles as the alt text)") – sie ist
 * mit dem Wort vorbelegt, denn genau das zeigt das Bild ja. Die Auflösungen (Thumb/Karte/Groß) erzeugt der
 * Server selbst.
 */
function UploadAndLink({ busy, word, onUpload }: {
  busy: boolean;
  word: string;
  /** Meldet zurück, ob der Upload durchging – nur dann darf das Formular geräumt werden. */
  onUpload: (file: File, description: string) => Promise<boolean>;
}) {
  const [file, setFile] = useState<File | null>(null);
  const [description, setDescription] = useState(word);
  // Ein <input type="file"> ist unkontrolliert – ohne dieses Zurücksetzen bliebe der alte Dateiname stehen.
  const fileInput = useRef<HTMLInputElement | null>(null);

  async function submit() {
    if (!file || !description.trim() || busy) return;
    // Nur bei Erfolg räumen: `useAction` fängt den Fehler ab (wirft also nicht), und wer die Auswahl
    // nach einem gescheiterten Upload wegwirft, lässt den Nutzer die Datei erneut heraussuchen –
    // während das Banner daneben „nochmal versuchen" sagt.
    if (!await onUpload(file, description.trim())) return;
    setFile(null);
    setDescription(word);
    if (fileInput.current) fileInput.current.value = "";
  }

  return (
    <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap", marginTop: 8 }}>
      <div className="field" style={{ maxWidth: 240 }}>
        <label>Neues Bild hochladen</label>
        <input ref={fileInput} type="file" accept="image/*" aria-label={`Bild für ${word} hochladen`}
          disabled={busy} onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
      </div>
      <div className="field" style={{ maxWidth: 220 }}>
        <label>Beschreibung <span className="muted">(Alt-Text)</span></label>
        <input aria-label="Bildbeschreibung" value={description} disabled={busy}
          onChange={(e) => setDescription(e.target.value)} />
      </div>
      <button type="button" className="btn" style={{ width: "auto" }}
        disabled={busy || !file || !description.trim()} onClick={() => void submit()}>
        {busy ? "Lade hoch…" : "Hochladen & zuordnen"}
      </button>
    </div>
  );
}
