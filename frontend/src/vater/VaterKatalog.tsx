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
      {/* „Anlegen" gilt uneingeschränkt, „umbenennen und löschen" nicht mehr: seit B-13 darf das nur der
          Eigentümer, und ein Fach aus dem Grundbestand niemand. Der Satz sagt das, weil er sonst genau
          das verspricht, was die Karte darunter dann verweigert (gefunden im Rollengang zu B-154). */}
      {/* B-178: Der Satz stand hier zwei Stories lang falsch – er versprach für Arten „was du selbst
          angelegt hast", während der Server seit B-157 für jeden `403 not_owner` liefert, sobald das Fach
          keinen Eigentümer hat (und das sind die vier geseedeten). Das Recht hängt am **Fach**, nicht daran,
          wer die Art angelegt hat. Sobald B-170 entschieden ist, ändert sich das – dann gehört der Satz
          erneut angefasst, und lieber das als heute eine Zusage, die der Server bricht. */}
      <p className="sub">
        Fächer und Arten sind der <strong>gemeinsame</strong> Rahmen aller Übungen – auch der von anderen
        Vätern. Neue anlegen darf jeder; umbenennen und löschen nur, wer das <strong>Fach</strong> angelegt
        hat – bei den mitgelieferten Fächern also niemand.
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
