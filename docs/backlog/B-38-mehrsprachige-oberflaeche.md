---
tags: [typ/story, status/idee, bereich/frontend, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Mehrsprachigkeit, i18n, Oberflächensprache, Deutsch Englisch Französisch]
status: idee
prio: P3
art: Wunsch
quelle: Nutzer, Sitzung 2026-07-31
unverifiziert: true
---

# B-38 · Mehrsprachige Oberfläche (Deutsch, Englisch, Französisch)

Die App spricht heute ausschließlich Deutsch: `frontend/index.html` steht hart auf `lang="de"`, es gibt
**keine** i18n-Bibliothek, und jeder Oberflächentext liegt als deutsches Literal im TSX bzw. in
`src/lib/fieldHelp.ts`. Die Idee: die Oberfläche in **Deutsch, Englisch und Französisch** anbieten, mit
diesen drei als erster Ausbaustufe.

## User Story

> Als **Nutzer** (Vater, Lehrer oder Kind) möchte ich die App **in meiner Sprache** bedienen — Deutsch,
> Englisch oder Französisch —, damit ich sie überhaupt verstehe und nicht an der Sprache der Oberfläche
> scheitere, statt am Lernstoff.

Feiner geschnitten, weil die drei Rollen unterschiedliche Gründe haben:

> Als **Vater** möchte ich die Verwaltung in meiner Muttersprache bedienen, damit ich beim Zuweisen von
> Pflichten und beim Deuten des Lernstands keine Fachbegriffe in einer Fremdsprache raten muss.
>
> Als **Lehrer** möchte ich Material in der Sprache meines Kollegiums pflegen, damit ein geteilter Katalog
> über Sprachgrenzen hinweg brauchbar ist.
>
> Als **Kind, das Französisch lernt**, möchte ich meine Arcade **auf Französisch** stellen können, damit die
> Sprache im Alltag vorkommt und nicht nur in der Übung — Immersion als Lerneffekt, nicht als Einstellung.

## Warum das nicht bloß „Texte austauschen" ist

Zwei Unterscheidungen entscheiden über den Umfang, und beide sind in diesem Produkt besonders:

1. **Oberflächensprache ≠ Lernsprache.** Das ist eine Sprachlern-App: „Französisch" ist heute schon ein
   *Inhalt* (Vokabeln tragen Sprachcodes, vgl. [B-17](B-17-birkenbihl-sprachcodes.md)). Französisch als
   *Bediensprache* ist etwas völlig anderes und muss sauber getrennt bleiben, sonst stellt das Umschalten der
   Menüsprache versehentlich den Lernstoff um.
2. **Ein Teil der Oberflächensprache kommt heute aus dem Backend.** Die Übungstyp-Anzeigenamen liefert das
   Server-Manifest (`"Leseverständnis"`, `"Hörverständnis"` … in
   [BuiltInExerciseTypes.cs](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — bewusst, damit
   es keine dritte Kopie im Frontend gibt. Übersetzt werden muss also **beidseitig**, oder das Manifest
   liefert künftig Schlüssel statt Beschriftungen. Das ist die eigentliche Architekturfrage dieser Story.

## Ungeprüft, beim Ausformulieren zu belegen

- **Wie groß ist der Textkorpus?** Anzahl deutscher Literale im Frontend, dazu `fieldHelp.ts` (die
  Feld-Erklärungen sind lange Fließtexte, nicht Labels).
- **Welche Texte sind Nutzerdaten und dürfen NICHT übersetzt werden?** Missions- und Auszeichnungstitel
  gibt der Vater selbst ein, ebenso Shop-Artikel, Plan- und Übungstitel. Eine i18n-Schicht, die darüber
  läuft, wäre falsch.
- **Wo lebt die Sprachwahl?** Am Konto, am Profil oder je Rolle? Der Fall „Vater auf Deutsch, Kind auf
  Französisch" **im selben Haushalt** ist der Normalfall, nicht die Ausnahme — das spricht gegen eine
  globale Einstellung. Ob daraus ein Feld an `Adult`/`Child` wird (Migration) oder eine reine
  Client-Einstellung genügt, hängt daran, ob der Server je lokalisierte Texte ausliefern soll.
- **Was ist mit den server-seitig deutschen Texten?** Das ist [B-30](B-30-i18n-rest.md) (Ledger-`Reason`,
  Content-Platzhalter). Deren offene Frage — „Englisch wäre für das Kind womöglich falsch" — wird durch
  diese Story **beantwortet**: nicht nach Englisch übersetzen, sondern lokalisieren. B-30 ist damit
  vermutlich ein Teil hiervon und nicht eigenständig; beim Ausformulieren entscheiden.
- **Plural, Zahlen, Datum.** „1 Münze / 2 Münzen", „vor 3 Tagen", Dezimaltrennzeichen. Französisch braucht
  andere Pluralregeln als Deutsch; eine selbstgebaute Ersetzung reicht dafür nicht.
- **Barrierefreiheit:** `<html lang>` muss mit der Wahl wandern, sonst liest der Screenreader französischen
  Text mit deutscher Aussprache vor.
- **Ist das eine Story oder ein Programm?** Vermutlich Letzteres (Infrastruktur · Vater-Web · Sohn-Arcade ·
  Server-Texte · Übersetzungspflege). Dann geht diese Id nach der Grill-Runde auf `verworfen` mit
  `grund: geteilt`.
- **Nicht betroffen:** die deutschen Inline-Kommentare und die Markdown-Doku. Das ist Entwickler-Text, kein
  Produkt (vgl. [B-08](B-08-xml-docs-englisch.md), [docs/translate.md](../translate.md)).

## Verlauf

- **2026-07-31** — vom Nutzer direkt aufgenommen (ungeprüft). Die User Story ist auf Wunsch schon
  formuliert; das macht die Story **nicht** `ausformuliert` — dafür fehlen der belegte Ist-Stand am Code
  (Textkorpus, Manifest-Kopplung) und die Akzeptanzkriterien.
