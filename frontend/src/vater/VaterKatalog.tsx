import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { CatalogAdmin } from "./CatalogAdmin";
import type { SubjectResponse } from "../lib/types";
import { ListenAusfall } from "../components/ListenAusfall";

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
      {/* Vierte Fassung dieses Satzes, und die Kette ist der Grund, ihn hier zu erklären statt ihn nur zu
          ändern: B-154 versprach „was du selbst angelegt hast" (damals zu streng, harmlos), B-157 machte
          ihn dadurch falsch in der GEFÄHRLICHEN Richtung (Versprechen > Server), B-178 setzte ihn auf die
          Wahrheit von damals („nur wer das Fach angelegt hat"), und B-170 gibt der Art einen eigenen
          Eigentümer. Jetzt nennt er BEIDE Träger — und ist damit erstmals so genau wie der Server.
          Fach und Art getrennt zu nennen ist nicht Pedanterie: für einen neuen Nutzer sind alle Fächer
          mitgeliefert, „nur wer das Fach angelegt hat" hieße für ihn also weiterhin „niemand".
          „Die mitgelieferten kannst du nur ergänzen" statt „kann niemand ändern": Letzteres war um genau
          das Admin-Ventil zu streng (B-178), und ein Versprechen, das weniger zusagt als der Server tut,
          ist derselbe Bruch wie eines, das mehr zusagt. */}
      <p className="sub">
        Fächer und Arten sind der <strong>gemeinsame</strong> Rahmen aller Übungen – auch der von anderen
        Vätern. Neue anlegen darf jeder; umbenennen und löschen nur, wer das <strong>Fach</strong> oder
        die <strong>Art</strong> selbst angelegt hat – die mitgelieferten kannst du nur ergänzen.
      </p>
      <ListenAusfall was="Die Fächer" fehler={subjects.error} laedt={subjects.loading} onRetry={subjects.reload} />
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
