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
function MediaLinkEditor({ heading, hint, empty, load, deps, onLink, onUnlink }: {
  heading: string;
  hint: string;
  /** Was dasteht, wenn nichts zugeordnet ist – die Folge ist je Ebene eine andere. */
  empty: string;
  load: () => Promise<MediaLinkResponse[]>;
  deps: unknown[];
  onLink: (assetId: number) => Promise<unknown>;
  onUnlink: (linkId: number) => Promise<unknown>;
}) {
  const links = useAsync<MediaLinkResponse[]>(load, deps);
  const action = useAction();

  async function mutate(fn: () => Promise<unknown>) {
    if (await action.run(fn)) links.reload();
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
      <StatusBanner message={action.message} />
    </div>
  );
}
