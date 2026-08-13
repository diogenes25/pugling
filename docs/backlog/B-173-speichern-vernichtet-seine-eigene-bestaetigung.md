---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Gespeichert fuer einen Frame, Zeile faellt aus dem Filter, Speichern sieht wie Loeschen aus]
status: idee
prio: P2
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

# B-173 · Ein erfolgreiches Speichern vernichtet seine eigene Bestätigung

Die Bestätigung lebt **in** der Zeile, die sie bestätigt. Ändert das Speichern einen Wert, nach dem gefiltert
oder sortiert wird, verlässt die Zeile die Liste — und nimmt „Gespeichert." mit. Das Speichern sieht dann aus
wie ein Löschen.

## Behauptung (aus der Nachschau, von mir nicht nachgeprüft)

`frontend/src/vater/VaterLehrwerke.tsx:291` (`onSaved()` → `:213` → `:116` `list.reload`) läuft gegen eine
**gefilterte** Abfrage (`:63-68`, Abhängigkeiten `[applied, publisherId, schoolTypes, grade]`). Der
`StatusBanner` und das offene Formular liegen **innerhalb** der Zeile (`:209-217`, Banner `:373`).

**Klickfolge:**

1. Verlag-Filter (`:91`) auf „Klett" stellen.
2. Bei der Reihe „Access" „Reihe bearbeiten", Verlag auf „– keine Angabe –", **Speichern**.
3. Server setzt `publisherId = null`. `list.reload()` läuft mit `publisherId=<Klett>` → die Zeile fällt aus
   dem Ergebnis, `SeriesRow` wird unmontiert.

**Ergebnis:** „Gespeichert." erscheint für einen Frame und ist weg, das Formular ist weg — und weil die Liste
jetzt leer ist, steht dort „Noch kein Lehrwerk. Lege unten eines an." (`:119-121`). Die Reihe existiert, sie
hat nur den Filter verlassen. Der Nutzer hat keinen Hinweis, ob es geklappt hat.

Dieselbe Kette bei aktivem Schulart-Filter (`:95`) plus geänderter Schulart, und bei Suche nach dem
Verlagsnamen (`:87`) plus entferntem Verlag.

**Abgeschwächte Variante ohne Filter:** Die Liste ist `OrderBy(s => s.Name)`
(`backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:93`). B-123 macht den Namen erstmals
über die Oberfläche veränderlich — ein Umbenennen sortiert die Zeile um und trägt Banner samt Formular aus
dem Blickfeld.

**Die zweite Hälfte:** „Noch kein Lehrwerk." zieht „es gibt keine" und „keine passt zum Filter" zusammen —
dieselbe Familie, und hier mit einer **falschen Handlungsanweisung** („Lege unten eines an."), obwohl die
Reihe schon existiert.

## Offene Punkte

1. Reproduzieren, bevor gebaut wird. Empfehlung: Die Filter-Variante ist der klarste Fall und in drei Klicks
   erreichbar.
2. Bestätigung nach oben oder Formular zuerst schließen? Empfehlung: **Bestätigung auf Seitenebene** — das
   Muster steht in derselben Datei schon (`NewSeries`, `:741`), und ein „erst schließen, dann melden" hängt
   weiter an der Lebensdauer der Zeile.
3. Gehört die leere Liste in dieselbe Story? Empfehlung: **ja** — sie ist die Hälfte, die den Schaden von
   „unklar" auf „irreführend" hebt, und beides sitzt in denselben zwanzig Zeilen.
4. Trifft es weitere Listen mit zeileninterner Bestätigung? Empfehlung: nachsehen und die Zahl in die
   Schätzung nehmen, statt sie zu vermuten.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-123. Bleibt `unverifiziert`: Die Fundorte sind benannt,
  gefahren habe ich die Klickfolge nicht.
