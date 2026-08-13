---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [leeres Fach-Select loescht, subjects.data ?? [], clearSubject ohne Absicht]
status: idee
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-123
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-123]
---

# B-172 · Die Fachauswahl löscht das Fach, wenn ihre eigenen Optionen nicht geladen sind

`?? []` zieht „noch nicht geladen" und „das ist die vollständige Liste" zusammen. Vor B-123 war das harmlos —
das Fach-Auswahlfeld gab es nur beim **Anlegen**, und „keine Auswahl" hieß dort „ohne Fach anlegen". B-123
hat aus demselben Feld ein **löschendes** Bedienelement gemacht.

## Behauptung (aus der Nachschau, von mir nicht nachgeprüft)

- `frontend/src/vater/VaterLehrwerke.tsx:114` reicht `subjects.data ?? []` und `publishers.data ?? []` durch.
- `useAsync` lässt `data` bei einem Fehler auf `null` und hat keinen erneuten Versuch
  (`frontend/src/lib/useAsync.ts:31-35`).
- Die Seite zeigt **nur** `list.error` (`:107`); `subjects.error` und `publishers.error` werden nirgends
  gerendert.

**Szenario:**

1. `GET creator/subjects` scheitert einmal (Server-Neustart, Offline-Blip in der PWA), `GET textbook-series`
   nicht. **Kein Fehler ist sichtbar.**
2. „Reihe bearbeiten" bei einer Reihe mit Fach „Englisch". Das Fach-Feld (`:314-329`) rendert **nur**
   „– keine Angabe –"; der Wert `"1"` hat keine Option, das Feld steht leer da. Es sieht aus wie
   „Fach fehlt".
3. Der Nutzer greift ins Feld, um das Fach zu **setzen**, und wählt die einzige Option.
4. `subjectPatch("1", "")` → `{ clearSubject: true }` (`frontend/src/vater/subjectField.ts:56`) → der Server
   räumt Id **und** gespeicherten Namen (`TextbookSeriesController.cs:194`).

**Ergebnis:** „Gespeichert." — und die **geteilte** Reihe hat ihr Fach verloren. Folgeschaden: sie kann keine
Übungen mehr tragen (`series_without_subject`, `VaterLehrwerke.tsx:397`), und „Englisch" ist auch als
Rückfall-Name weg, also nicht rekonstruierbar. Dieselbe Kette für den Verlag (`clearPublisherId`), mit
geringerem Schaden.

Die Ironie steht im Kopf der geteilten Datei selbst (`subjectField.ts:8-9`): kaputt wird es „bei einem Feld,
dessen **Ladezustand das Formular nicht abbilden kann**". Eine leere Options-Liste ist genau das — nur für
ein *Katalog*-Fach, und diesen Fall hat B-143 nicht mitgedacht.

**Warum keine Testebene das sah:** `seriesPatch`/`subjectPatch` kennen die Options-Liste nicht, und
`frontend/src/vater/SeriesForm.test.tsx:34` übergibt immer ein gefülltes `SUBJECTS`.

## Offene Punkte

1. Trifft die Kette wirklich zu? Empfehlung: **erst reproduzieren** — das Netz-Panel im Browser auf
   „offline" für den einen Abruf, oder ein Komponentenfall mit `subjects={[]}` bei gesetztem `subjectId`.
   Solange das nicht gelaufen ist, bleibt `unverifiziert`.
2. Sperren oder Fehler zeigen? Empfehlung: **beides** — `subjects.error`/`publishers.error` als
   `banner err` rendern (Muster `VaterKatalog.tsx:29`) **und** das Feld sperren, solange
   `subjects.data === null`. Ein Fehler allein lässt das löschende Feld bedienbar.
3. Gilt dasselbe an weiteren Stellen? Empfehlung: **nachsehen, nicht annehmen** — `?? []` an einem Feld, das
   einen `Clear…`-Schalter speist, ist das Muster; das ist greppbar und die Zahl gehört in die Schätzung.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-123. Bleibt `unverifiziert`: Die Fundorte sind benannt,
  aber ich habe die Kette nicht selbst gefahren — und eine Kette mit vier Gliedern ist genau die Sorte
  Behauptung, die man nicht abschreibt.
