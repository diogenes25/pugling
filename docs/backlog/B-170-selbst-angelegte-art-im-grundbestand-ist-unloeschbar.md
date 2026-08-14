---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/frontend, rolle/creator]
aliases: [unloeschbare Art, Grammtik bleibt fuer immer, B-157 nahm eine Faehigkeit weg]
status: ausformuliert
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-157 und B-154
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-157]
wartet_auf: "eine Schema-Entscheidung am Tag: CreatedByAdultId oder nicht (die Kette wird dabei neu gefaltet)"
---

# B-170 · Eine selbst angelegte Art im Grundbestands-Fach ist unlöschbar — und der Seitentext verspricht das Gegenteil

B-157 hat das Eigentum am Fach auf seine Arten ausgedehnt und das **Anlegen** bewusst frei gelassen. Beides
ist begründet. Ihre Schnittmenge hat niemand gewogen: Wer eine Art in ein Fach ohne Eigentümer legt — und
das sind für einen neuen Nutzer **alle vier** Fächer —, kann sie danach **nie wieder** entfernen. Niemand
kann es.

**Vor `3b65475` war genau dieses `DELETE` ein `204`.** Die Story hat also im Namen des Schutzes eine
Fähigkeit entfernt, die ein normaler Nutzer für seine einzigen Fächer braucht.

## Ist-Stand am Code (selbst nachgeprüft)

| Schritt | Ergebnis | Fundort |
|---|---|---|
| Art in ein Fach ohne Eigentümer anlegen | **201**, ohne Prüfung (Absicht, Entscheidung 2) | `ExerciseCategoriesController.cs:73-78` |
| Sie umbenennen | **403 `not_owner`** | `ExerciseCategoriesController.cs:111` |
| Sie löschen | **403 `not_owner`** | `ExerciseCategoriesController.cs:143` |
| Das Fach löschen (Fluchtweg) | **403** | `SubjectsController.cs:120-123` |
| Als Admin eingreifen | **kein Ventil** — `grep IsAdmin` in beiden Controllern: nichts | — |

Ein zweiter Schreibpfad auf `ExerciseCategories` existiert nicht. Beide Hälften sind je getestet
(`FachEigentumTests.cs:194` pinnt das freie Anlegen, `:215` das Gesperrtsein für jeden) — nur die
Schnittmenge nicht, darum blieb die Suite grün.

**Der Kontrast ist im Repo belegt:** `ExerciseControllerBase.cs:117,126` reicht `User.IsAdmin()` bewusst
durch, ausdrücklich „to edit orphaned (ownerless) exercises in an emergency". Die Übung hat das Ventil, die
Art nicht.

## Fehlerszenario

Auf `/vater/katalog` das Fach „Englisch" wählen und bei „Neue Art" **„Grammtik"** tippen (Tippfehler).
Ein Klick, `201`, Banner „Art angelegt." — und „Grammtik" steht ab jetzt **dauerhaft** in der
Vorfilter-Liste, an der der Planbau hängt (`VaterWizard.tsx:133`, `ExerciseFilterBar.tsx:44`).
Reparierbar nur durch Wegwerfen der Datenbank.

## Die zweite Hälfte: der Bildschirm widerspricht sich zwei Absätze weit

`frontend/src/vater/VaterKatalog.tsx:26-27` sagt:

> Fächer und Arten sind der **gemeinsame** Rahmen aller Übungen – auch der von anderen Vätern. Hier legst du
> neue an; umbenennen und löschen kannst du, was du selbst angelegt hast.

`frontend/src/vater/CatalogAdmin.tsx:183-184` sagt zwei Absätze darunter:

> Diese Arten gehören zum Fach „Englisch" aus dem Grundbestand – ergänzen darfst du sie, umbenennen und
> löschen kann sie **niemand**.

Die Karte hat recht, der Vorspann nicht. **Attribution, ehrlich:** Der Satz stammt aus B-154 und war dort
nur *zu streng* (Arten waren damals bedingungslos änderbar) — also harmlos. Falsch **in der gefährlichen
Richtung** (Versprechen > Server) wurde er mit B-157, und B-157 hat `VaterKatalog.tsx` nicht angefasst
(`git show 3b65475 --stat`).

Und der bittere Teil: der Kommentar direkt über diesem Satz (`VaterKatalog.tsx:22-24`) begründet ihn damit,
dass im Rollengang zu B-154 „eine Zeile über der Karte weiter etwas versprach, was die Karte darunter
verweigert" — der Satz wurde also *gegen genau diese Fehlerklasse* geschrieben und ist ihr wieder verfallen.
Kein Test deckt Vorspann-Prosa, und ein Rollengang liest sie als „stimmt ja".

## Offene Punkte

1. ~~Wie darf man aufräumen?~~ → **bleibt offen, und das ist der Kern dieser Story.** Siehe Entscheidung 1.
2. ~~Oder erst nur ein Ventil?~~ → Entscheidung 2 (in eine eigene Story ausgelagert).
3. ~~Was gilt für eine benutzte Art?~~ → Entscheidung 3.
4. ~~Wird der Vorspann-Satz mitrepariert?~~ → Entscheidung 2. Die Empfehlung „hier mit" ist **entfallen**;
   ihre Begründung hing an Punkt 1.

## Entscheidungen

1. **Der eigentliche Fix bleibt `ExerciseCategory.CreatedByAdultId` — und wird am Tag entschieden, nicht
   nachts.** Vom Nutzer so entschieden (Nachtlauf 2026-08-14): Die Migrationskette wird bei jeder
   Schemaänderung **neu gefaltet**, `SchemaGuardTests` erzwingt Länge 1, und der Snapshot-Diff *ist* die
   Abnahme — die kann unbeaufsichtigt niemand beurteilen. *Kosten:* Der Vater kann seinen Tippfehler bis
   dahin nicht wegräumen. Das ist der Preis, und er ist ausgesprochen statt weggelassen.
2. **Diese Story wird geteilt.** Was heute Nacht baubar ist, liegt in
   [B-178](B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md): das Admin-Ventil (vom Nutzer
   freigegeben) **und** der Vorspann-Satz, der die *heutige* Wahrheit sagen muss.
   **Begründung, und sie widerspricht der ursprünglichen Empfehlung dieser Story:** Punkt 4 empfahl, den Satz
   hier mitzunehmen, „denn nach Punkt 1 ändert sich, was wahr ist". Punkt 1 kommt aber nicht — also ändert
   sich für den Vater **nichts**, und der Satz muss etwas anderes sagen als nach dem Schema-Fix. Ihn hier zu
   lassen hieße, ihn zweimal zu schreiben. *Kosten:* Zwei Stories über einen Zustand; der Zusammenhang hängt
   an den Querverweisen. Dafür schreibt keine der beiden eine Wahrheit hin, die gerade nicht gilt.
3. **Eine benutzte Art bleibt unverändert.** Löschen nimmt der Übung nur die Zuordnung
   (`ExerciseCategoriesController.cs:128-131`), das ist entschieden und seit B-171 auch tragend getestet.
   *Kosten:* keine — die Regel wird von diesem Vorhaben nicht berührt.

## Was diese Story nach der Teilung noch ist

Genau eine Frage: **Darf eine selbst angelegte Art in einem Fach ohne Eigentümer von ihrem Anleger entfernt
werden, und trägt die DB dafür eine Spalte?** Alles andere ist in B-178 abgeflossen. Der Ist-Stand oben
bleibt gültig und ist der Beleg dafür, dass die Frage nicht akademisch ist: ein Tippfehler steht heute
dauerhaft in der Planbau-Vorfilterung.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-157 (Server-Hälfte) und B-154 (Text-Hälfte); beide
  Hälften haben eine Wurzel und darum eine Story. Von mir gegengeprüft: `Create` ist ungegated, `Update` und
  `Delete` liefern `403`, es gibt kein `IsAdmin` in beiden Controllern, und die zwei Sätze widersprechen
  sich wörtlich.
- 2026-08-14 · `ausformuliert` bleibt, **Story geteilt** (Nachtlauf, Freigabe 3 erlaubt das Teilen
  ausdrücklich). Der Nutzer hat die Schemaänderung für diesen Lauf **ausgeschlossen**; damit fällt der Kern
  dieser Story auf den Tag, und `wartet_auf` benennt das. Was heute Nacht baubar ist, steht in
  [B-178](B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md).
  **Die eigene Empfehlung dieser Story wurde dabei widerlegt:** Punkt 4 wollte den Vorspann-Satz hier
  mitnehmen, weil „sich nach Punkt 1 ändert, was wahr ist". Ohne Punkt 1 ändert sich für den Vater nichts —
  der Satz muss also die heutige Wahrheit sagen, und die ist eine andere. Ihn hier zu lassen hätte bedeutet,
  ihn zweimal zu schreiben.
