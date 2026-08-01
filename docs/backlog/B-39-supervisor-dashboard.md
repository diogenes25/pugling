---
tags: [typ/story, status/idee, bereich/frontend, bereich/auswertung, rolle/supervisor]
aliases: [Supervisor-Dashboard, Eltern-Dashboard, Fortschritts-Dashboard, Tag Woche Monat]
status: idee
prio: P2
art: Wunsch
quelle: Nutzer, Sitzung 2026-07-31
unverifiziert: true
---

# B-39 · Supervisor-Dashboard über die Kinder

Ein Dashboard, das dem Supervisor Fortschritte, Klausuren und Tätigkeiten seiner Kinder **anschaulich,
übersichtlich und informativ** zeigt — statt als Tabellen und Listen, wie das Vater-Web es heute tut. Mit
drei Zeitachsen-Ansichten (**täglich, wöchentlich, monatlich**), den **Zusammenhängen von Übungen zu einer
Klausur**, und durchgehend vom Groben ins Genaue: jede verdichtete Ansicht lässt sich aufklappen, bis man
beim einzelnen Wort steht.

## Warum das vermutlich eine Darstellungs-Story ist

Ungeprüft im Detail, aber strukturell schon sichtbar: **die Daten liegen weitgehend vor.** Das ist die
wichtigste Vorabinformation, weil sie den Charakter der Story bestimmt.

- **Drei Auswertungs-Blickwinkel existieren** (siehe [endpunkt-beziehungen.md](../endpunkt-beziehungen.md) §3):
  pro Position (`…/positions/{id}/report`), kind-zentrisch flach (`…/vocabulary-progress`) und
  kind-zentrisch **hierarchisch** (`…/learn/subjects/…/items`).
- **Der Drill-down gibt es schon — aber auf der falschen Achse.** Die hierarchische Sicht geht
  Fach → Kapitel → Übung → Item, also entlang des **Katalogs**. Diese Idee verlangt ihn zusätzlich entlang
  der **Zeit** (Monat → Woche → Tag → Sitzung). Das ist die eigentliche Lücke.
- **Übung ↔ Klausur ist bereits modelliert:**
  `Klassenarbeit --< KlassenarbeitTag >-- Tag --< ExerciseTag >-- Exercise`
  ([KlassenarbeitEntities.cs](../../backend/Pugling.Api/Models/KlassenarbeitEntities.cs)). Der
  Zusammenhang muss also **gezeigt**, nicht erfunden werden.
- **Ein Kinder-Dashboard existiert rudimentär** (Supervisor-Übersicht mit `goalsTotal`/`dutyDone` je Kind,
  abgedeckt von `ChildrenDashboardTests`), und `…/overview/progress` liefert einen Tages-Verlauf mit Paging,
  Sortierung und Filtern.

## Ungeprüft, beim Ausformulieren zu belegen

- **„Monatlich" gibt es nirgends.** `GoalCadence` kennt nur `Daily`/`Weekly`, und die Malus-Abrechnung
  rechnet in genau diesen Perioden. Ist die Monatsansicht eine reine **Aggregation** über Wochen (dann rein
  lesend, keine Schemafrage), oder soll sie eine eigene Zielperiode werden (dann Migration und Auswirkung
  auf Pflicht und Malus)? Die erste Lesart ist vermutlich gemeint und deutlich billiger.
- **Was heißt „Tätigkeiten"?** Kandidaten: Sitzungen mit Übungszeit (`PracticeSession.ActiveSeconds`),
  Klausur-Versuche, Shop-Käufe und Einlösungen, erreichte Missionen. Eine gemeinsame **Zeitleiste** über
  diese Ereignisse wäre neu — sie liegen heute in getrennten Tabellen ohne einen Endpunkt, der sie
  chronologisch zusammenführt.
- **Ein Kind oder alle?** Die Formulierung sagt „seine Kinder" *und* „des Kindes". Sind das zwei Ansichten
  (Haushalts-Überblick vs. Kind-Detail), und ist die Haushaltssicht die gröbste Stufe des Drill-downs?
- **Wie „anschaulich"?** Es gibt **keine** Chart-Bibliothek im Frontend. Also entweder handgebautes SVG
  (kein neues Paket, volle Kontrolle, mehr Aufwand) oder eine Abhängigkeit — die im `--legacy-peer-deps`-
  Umfeld dieses Projekts eine eigene Fußangel ist (vgl. [B-25](B-25-vite-pwa-peer-konflikt.md)). Dazu die
  Pflicht aus `lib/ui.ts`: `prefersReducedMotion` respektieren, und Farbe darf nie die einzige
  Information tragen.
- **Wie viele neue Endpunkte braucht es wirklich?** Wenn die Daten vorliegen, ist die Versuchung groß, im
  Frontend fünf Abfragen zu verrechnen. Nach der API-First-Regel gehört ein verdichtetes Dashboard aber als
  **Aggregat in den Server** — sonst entsteht die Rechenlogik doppelt (Web und später PWA).
- **Abgrenzung zu den großen Zielen.** `Objective`/`KeyResult` messen schon den Lernstand gegen einen
  Zielwert und werden live berechnet. Das Dashboard soll sie vermutlich **anzeigen**, nicht ersetzen.
- **Ist das eine Story oder ein Programm?** Zeitachsen-Aggregat, Tätigkeits-Zeitleiste, Klausur-Sicht und
  Visualisierung sind vier trennbare Brocken. Beim Grillen entscheiden, ob geteilt wird.

## Verlauf

- **2026-07-31** — vom Nutzer direkt aufgenommen (ungeprüft, keine Recherche). Vorab nur geklärt, dass es
  keine Duplikat-Story gibt und dass Übung↔Klausur schon modelliert ist.
