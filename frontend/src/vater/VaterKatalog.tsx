import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { CatalogAdmin } from "./CatalogAdmin";
import type { SubjectResponse } from "../lib/types";

/**
 * Der Katalog als eigener Bereich: **Fach → Art**. Übungen selbst hängen seit B-106 an einer
 * Lehrwerk-Unit (`/vater/lehrwerke`), nicht mehr an einem Kapitel des Fachs.
 *
 * Er lag eingeklappt auf der Übungen-Seite und war darum kaum zu finden (Anmerkung 12). Der Katalog ist
 * die *Behälter-Hierarchie* der Übungen und wird von **allen Vätern geteilt** – das macht ihn zu einem
 * eigenen Ort, nicht zu einem Beiwerk des Anlege-Formulars.
 *
 * Die Fächer lädt dieser Bildschirm selbst; vorher kamen sie als Prop von der Übungen-Seite.
 */
export function VaterKatalog() {
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  return (
    <>
      <h2 className="h-section">Katalog</h2>
      <p className="sub">
        Fächer und Arten sind der <strong>gemeinsame</strong> Rahmen aller Übungen – auch der von anderen
        Vätern. Hier legst du sie an, benennst sie um und löschst sie.
      </p>
      {subjects.error && <div className="banner err">{subjects.error}</div>}
      {/*
        Der Platzhalter gilt nur fürs **erste** Laden. `onCatalogChanged` löst nach jeder Änderung ein
        `reload` aus, und das setzt `loading` erneut auf `true` (useAsync.ts:27) – würde `CatalogAdmin`
        dabei gegen „Lade…" getauscht, verlöre es beim Umbenennen sein eigenes `useState`: die Fach-Auswahl
        springt auf „– wählen –" zurück und die Erfolgsmeldung erscheint nie. Darum bleibt es montiert.
      */}
      {subjects.loading && subjects.data === null
        ? <div className="loading">Lade…</div>
        : <CatalogAdmin subjects={subjects.data ?? []} onCatalogChanged={subjects.reload} />}
    </>
  );
}
