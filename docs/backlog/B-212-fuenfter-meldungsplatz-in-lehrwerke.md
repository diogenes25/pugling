---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [rot und gruen gleichzeitig, zwei eigene StatusBanner]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-03 zu B-204 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-17: sieben Schreibpfade in VaterLehrwerke.tsx je auf ihren Meldungsplatz gezählt (alle → Seite); scrollIntoView-Bedingung :150-152 geprüft; Seitenbezug der User Story NICHT gebaut → B-224, falscher Kommentar :1031 → B-225"
---

# B-212 · Auf der Lehrwerke-Seite stehen rot und grün gleichzeitig — der Weg ist nur ein anderer

## User Story

Als **Creator** möchte ich auf der Lehrwerke-Seite nicht gleichzeitig eine Fehlermeldung und eine
Erfolgsmeldung sehen, weil ich dann nicht weiß, welche zu meiner letzten Handlung gehört.

## Ist-Stand am Code

`VaterLehrwerke.tsx:526` (`SeriesForm`) und `:820` (`UnitForm`) rendern je einen **eigenen**
`StatusBanner` aus ihrer `action.message`. Der trägt nicht nur Eingabeprüfungen, sondern auch
Server-Fehler — `SeriesForm:425` ruft `action.runFor(updateTextbookSeries)`, das etwa an einer
Namensdublette scheitert.

Der Weg: Reihe A aufklappen, Speichern scheitert (rot unter ihrem Formular) → an Reihe B „Löschen", das
gelingt → grün am Platz der Seite (`:210`). Beides steht gleichzeitig im Blickfeld, mit demselben
Argument wie beim vierten Weg: **kein Aufrufer kann einen fremden lokalen Platz räumen.**

**Was B-204 tatsächlich geschlossen hat:** den Platz in `SeriesRow` (Kommentar `:326-333`, `remove` meldet
seit `:269` nach oben). Der Kommentar behauptet darüber hinaus „**ein** Platz, und B-197s Regel gilt
konstruktiv" — nachgemessen gilt das nur für `SeriesRow`; die Datei trägt weiterhin **drei**
`action.message`-Banner (`:526`, `:820`, `:931`).

## Offene Punkte

1. ~~Beide Formulare nach oben melden lassen, oder ist ein lokaler Platz an einem Formular richtig?~~
   → Entscheidung 1.
2. ~~Den Kommentar korrigieren~~ → erledigt, und zweimal: Die erste Fassung behauptete erneut zu viel
   (Review-Fund), weil `NewSeries` noch zwei eigene Plätze trug.

## Entscheidungen

1. **Nicht „nur der Erfolg reist", sondern: alles, was vom Server kommt.** Am Formular bleibt allein, was
   der Bildschirm **selbst** festgestellt hat — „Der Name der Reihe fehlt.", „Nichts geändert.". Der
   Schnitt läuft damit an etwas Ablesbarem: Bei einer Eingabeprüfung ist *nichts passiert*, sie kann ihr
   Formular also gar nicht überdauern; jede Server-Antwort gehört zu einer Handlung, die die Zeile aus der
   Filterung werfen oder das Formular schließen kann. Die erwogene Ausnahme („ein Formular hat einen
   eigenen Fokusbereich") trägt nicht: Der Fehler war von **keiner** anderen Handlung mehr erreichbar, und
   genau das war der Defekt. **Kosten:** ein geteilter Weg `nachObenMelden` statt vier handgeschriebener
   Kaskaden — und ein neuer, gemessener Nachteil, siehe Entscheidung 3.
2. **Auch `NewSeries` verliert seine zwei Plätze** (aus der Review-Runde). Das Formular schließt nie, seine
   Bestätigung ginge also nicht verloren — aber ein zweiter Platz erzeugt denselben Widerspruch von der
   anderen Seite: grün „… steht im Katalog" hier, während ein gescheitertes Unit-Speichern sein Rot in den
   Platz der Seite legt. **Ein** Platz ohne Ausnahme ist die Regel, die man im Kopf behält. **Kosten:**
   `nachObenMelden` musste `okText` auch als Funktion annehmen — beide Meldungen nennen den Namen, den der
   **Server** zurückgibt (gleicher Slug = gleiche Reihe).
3. **Der Fehler wird notfalls ins Sichtfeld geholt.** Der Seitenplatz steht unter der Tabelle, und die
   Liste hat kein Paging: Bei dreißig Reihen liegt ein Fehler aus Zeile 3 außerhalb des Bildschirms — der
   Knopf entgraut, sonst passiert sichtbar nichts. Das ist der stumme Pfad aus B-200, neu erzeugt durch
   Entscheidung 1. `scrollIntoView({ block: "nearest" })`, **nur bei `ok === false`**: Eine Bestätigung,
   die man nicht sieht, ist verschmerzbar; ein Fehler nicht. **Kosten:** ein `ref` und ein Effekt; und die
   E2E kann es nicht prüfen — Playwrights `toBeVisible()` verlangt eine Bounding-Box, nicht den Viewport.
4. **`onMeldung` ist bei `SeriesForm` jetzt Pflicht** (aus der Review-Runde). Der optionale Zweig war
   produktiv unerreichbar und trug als einzige ungeprüfte Verzweigung genau die Semantik weiter, die diese
   Story abschafft. Dieselbe Begründung stand längst an `UnitForm` und `TextbookForm`: „ein `?`-Prop kann
   man weglassen, und genau daran ist diese Stelle durchgerutscht". **Kosten:** vier Aufrufstellen im
   Bausteintest nachgezogen.

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| `SeriesForm` meldet wieder lokal (Empfänger weg) | 1 | **1**, an Zeile 122 — der Zeile, die den Fall trägt (Nachschau 2026-10-17: vorher „115" notiert; `:115` prüft die Abwesenheit der Zeile und fällt bei zurückgenommener Reparatur nicht) |

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-204 (Nachtlauf). `entgangen_bei` bleibt **leer**: die beiden
  Banner sind älter als B-204s Diff, und dessen Ziel (der Platz in `SeriesRow`) ist erfüllt. Zuzurechnen
  ist B-204 nur der zu weit gefasste Kommentar.
- **2026-09-05** — **ausformuliert → abgenommen** im Sprint „Eine Rückmeldung gehört an einen Platz, der sie überlebt — und es darf nur einer
  sein" (B-187/B-200/B-212 gemeinsam). Autonom gegrillt unter Freigabe 1 (`art: Defekt`).
  **Rollengang:** `e2e/ein-meldungsplatz.spec.ts` fährt die Creator-Rolle im echten Browser gegen einen
  echten Server. Kein Browser-Handgang möglich (keine Chrome-Anbindung), damit greift der dokumentierte
  Ersatz. Belege: `tsc -b` sauber, `vitest run` **368/368** grün, `playwright test` **63/63** grün.
  `frontend-reviewer` gelaufen, **kein Blocker**; seine acht Funde mit Code- oder Testwirkung sind
  eingearbeitet (Protokoll `docs/pm-sitzung-2026-09-05.md`). Commit `3d763fe`.
- **2026-10-17** — **Nachschau** (Nachtlauf, Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`): Schnitt für alle sieben Schreibpfade der Datei nachgerechnet – hält. **Zwei Funde:** die User Story verspricht die Seite, gebaut ist die Datei – `PublisherAdmin.tsx:80` trägt einen eigenen `StatusBanner` ([B-224](B-224-verlagsbaustein-traegt-zweiten-meldungsplatz.md)); der neue Kommentar `:1031` behauptete eine Knopf-Sperre, die nur der Verlags-Knopf hat, darunter ein stummer Pfad ([B-225](B-225-reihe-anlegen-schweigt-bei-leerem-namen.md)). Zeilenangabe der roten Probe korrigiert (122 statt 115); die Zahl der Banner steht in der Story uneinheitlich („drei"/„vier").
