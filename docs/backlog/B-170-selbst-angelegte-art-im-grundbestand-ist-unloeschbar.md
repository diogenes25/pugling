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

1. **Wie darf man aufräumen?** Empfehlung: `ExerciseCategory.CreatedByAdultId` (nullable, `SetNull`).
   Seed-Zeilen tragen `null` und bleiben eingefroren — B-157s Absicht unberührt; eine selbst angelegte Zeile
   in einem ownerlosen Fach darf ihr Anleger umbenennen und löschen. Das ist **keine** zweite Kopie der
   Fach-Wahrheit (B-157 Entscheidung 3 verbietet die zu Recht), sondern die Antwort auf die *andere* Frage,
   die die heutige Bedingung mitverschluckt. Kosten: eine Faltung der Migrationskette (`migration: ja`).
2. **Oder erst nur ein Ventil?** `IsOwnedBy(…) || User.IsAdmin()` in beiden Actions macht den Zustand ohne
   Schema reparierbar. Empfehlung: **als Sofortmaßnahme ja, als Lösung nein** — ein Haushalt hat keinen
   Admin, der Zustand bliebe für den Vater unreparierbar.
3. **Was gilt für eine benutzte Art?** Empfehlung: unverändert lassen — Löschen nimmt der Übung nur die
   Zuordnung (`ExerciseCategoriesController.cs:128-131`), das ist bereits entschieden und getestet.
4. Wird der Vorspann-Satz mitrepariert oder in einer eigenen Story? Empfehlung: **hier mit**, denn nach
   Punkt 1 ändert sich, was wahr ist — zwei Stories würden zwei Wahrheiten schreiben.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-157 (Server-Hälfte) und B-154 (Text-Hälfte); beide
  Hälften haben eine Wurzel und darum eine Story. Von mir gegengeprüft: `Create` ist ungegated, `Update` und
  `Delete` liefern `403`, es gibt kein `IsAdmin` in beiden Controllern, und die zwei Sätze widersprechen
  sich wörtlich.
