---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/lehrplan, rolle/supervisor]
aliases: [Gescheiterte Katalogsuche liest sich als leerer Katalog]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau im Nachtlauf 2026-08-12 (Nebenfund zu B-18)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-162 · Scheitert die Übungssuche im Assistenten, behauptet er einen leeren Katalog

Dieselbe Klasse wie [B-111](B-111-verlauf-luegt-im-fehlerfall.md), eine Fläche weiter: ein Fehler beim Laden
wird als „nichts da" ausgegeben — und hier mit einer ausdrücklichen, falschen Handlungsanweisung.

## User Story

Als *Vater* möchte ich bei einer gescheiterten Suche erfahren, dass sie gescheitert ist, damit ich nicht
Übungen anlege, die längst existieren.

## Ist-Stand am Code

- `frontend/src/vater/VaterWizard.tsx:434-437`: Der Zweig `filteredExercises.length === 0` zeigt „**Keine
  passenden Übungen im Katalog. Lege welche unter ‚Übungen' an**".
- `exercises.error` wird in der ganzen Datei **nirgends** gerendert (nachgezählt über `VaterWizard.tsx`) —
  anders als `setError`/`error` für die Assistenten-eigenen Fehler, die es gibt.
- `useAsync` (`frontend/src/lib/useAsync.ts:29-36`) setzt bei einem Fehler nur `error` und `loading = false`;
  `data` bleibt **unangetastet**.
  - **Korrigiert am 2026-08-14** (gefunden beim Review von [B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md)):
    Der frühere Satz hier lautete „lässt `data` auf `null`" und stimmt **nur für die erste Ladung**. Ab der
    zweiten Abfrage bleibt die **alte Seite** stehen, und dann greift der Leer-Zweig gar nicht — der Vater
    sieht stattdessen die Treffer des vorigen Filters. Damit hat diese Story zwei verschiedene Bildschirme zu
    behandeln, nicht einen.
- **Der zweite ist der schwerere:** Scheitert die Abfrage, sind die gezeigten Zeilen veraltet, also zu Recht
  gesperrt — der Effekt auf `[exercises.data]` läuft aber nie (`data` ändert seine Referenz nicht), die Sperre
  bleibt für die **eingestellten** Kriterien also endgültig. Der Vater sieht die alte Liste, alle Kästchen
  tot, keine Meldung, und „Weiter" antwortet „Bitte mindestens eine Übung wählen."
  - **Präzisiert am 2026-08-14** (Nachschau zu B-169; meine erste Fassung hier sagte „kein Wiederholen" und
    war **zu stark**): Es gibt drei Auswege — die getippte Suche wieder **leeren** (dann ist `filterKey`
    wieder gleich dem Seitenschlüssel und beides sofort bedienbar), **irgendeine weitere** Kriterienänderung,
    die gelingt (neues `data` → Effekt → frische Zeilen), und **F5** (heilt, kostet aber den ganzen
    Assistenten, es persistiert kein Zustand).
  - **Wirkungslos ist ausgerechnet die intuitive Bewegung:** über den Stepper zurück auf Schritt 1/2 und
    wieder „Weiter" — die Abhängigkeiten von `useAsync` ändern sich dabei nicht, es fliegt **keine** neue
    Abfrage, die Sperre bleibt. **Folge für diese Story: ein Banner allein genügt nicht.** Es braucht ein
    „Erneut suchen" (`exercises.reload()`) — sonst steht die Meldung da und der einzige Weg heraus ist einer,
    den der Nutzer nicht als solchen erkennt.
  - Nebenbei ein Beleg für B-169s Entscheidung 2: Mit der dort **verworfenen** Variante
    (`loading && data !== null`) hätte ein `reload()` das Gate fälschlich geschlossen — mit dem
    Schlüsselvergleich nicht. Der „Erneut suchen"-Knopf ist also nur deshalb baubar, weil dort nicht die
    billigere Bedingung genommen wurde.
- Der Satz ist nicht bloß unpräzise, sondern **handlungsleitend falsch**: er schickt den Vater ins
  Anlege-Formular, während der Katalog voll ist und nur die Abfrage scheiterte.

**Eine zweite Stelle derselben Familie im selben Bildschirm** (gefunden vom `frontend-reviewer` zu B-161,
außerhalb dessen Diffs): Nach einem Filterwechsel bleibt die **alte** Trefferliste stehen, solange die neue
lädt — der Platzhalter greift korrekt nur bei `data === null` (`VaterWizard.tsx:489`), aber „N passende
Übungen" (`:479`) nennt in diesem Moment weiter die **alte** Zahl. Seit B-161 steht daneben der Hinweis
„Auswahl zurückgesetzt", der also einen Reset über einer Liste behauptet, die noch die vorige ist. Von
B-161s Diff nicht verschlechtert (die leer werdenden Kästchen sind sogar ein Signal), aber dieselbe Wurzel:
ein Zustand, der „aktuell" und „von vorhin" nicht trennt.

## Die echte Lücke

`filteredExercises.length === 0` trägt zwei Bedeutungen: „die Suche lief und fand nichts" und „die Suche lief
nicht". Genau die Fehlerfamilie, die dieses Repo mehrfach bezahlt hat (B-111, B-116). Der Unterschied zu
B-111: dort log die App über einen Verlauf, hier über den **Katalog** — und gibt eine Anweisung dazu.

Die zweite Stelle oben ist dieselbe Lücke in ihrer *Ladezustands*-Ausprägung: nicht „leer heißt zweierlei",
sondern „die Zahl gehört zu einer anderen Abfrage als die Meldung daneben".

## Offene Punkte

1. **Nur den Fehler anzeigen, oder auch den Leer-Satz schärfen?** Empfehlung: beides. `exercises.error` in
   ein `banner err` (das Muster steht direkt daneben, `:434`), und der Leer-Satz greift erst, wenn
   `exercises.data !== null`.
2. **Gilt dasselbe an weiteren `useAsync`-Leerzweigen?** Zu messen, bevor eine Regel daraus wird — B-116 hat
   dieselbe Frage für den Ladezustand gestellt und drei Stellen gefunden. Empfehlung: erst zählen, dann
   entscheiden, ob ein Wächter trennscharf formulierbar ist.
3. **Ist es ein `Defekt` oder `Aufräumen`?** Empfehlung: `Defekt` — die Meldung ist nicht unschön, sie ist
   unwahr und führt zu einer Handlung.

## Akzeptanzkriterien (Entwurf)

1. Scheitert die Suche, sieht der Vater den Fehler und **nicht** „Keine passenden Übungen im Katalog".
2. Findet die Suche wirklich nichts, bleibt der bisherige Satz mit seiner Anweisung.
3. Während eine neue Suche lädt, behauptet die Trefferzahl nicht die Zahl der vorigen.
4. Ein Test deckt die Fälle; die rote Probe belegt, dass er den heutigen Stand fängt.

## Verlauf

- **2026-08-12** — angelegt aus der **Nachschau** des Nachtlaufs (Nebenfund neben
  [B-161](B-161-alle-waehlen-macht-die-auswahl-unsichtbar.md)). **`entgangen_bei` bleibt leer, und das ist
  keine Nachlässigkeit:** der Block liegt außerhalb des Diffs von B-18 (der fasst nur die Zeile darüber und
  `TruncationHint` an) und außerhalb der drei anderen nachgeschauten Stories — er ist also **keiner**
  Abnahme entgangen, sondern älter. **Bewusst nicht mit B-161 zusammengelegt**, obwohl beide in derselben
  Datei liegen: B-161 trägt ein `entgangen_bei`, dieses hier nicht, und ein gemeinsamer Eintrag würde die
  Wirkungszahl des Bereichs verfälschen.
- **2026-08-12** — zweite Fundstelle derselben Familie ergänzt (Ladezustand: alte Trefferzahl neben neuer
  Meldung), aus dem `frontend-reviewer`-Befund zu B-161. Sie liegt ebenfalls **außerhalb** jedes Diffs dieses
  Laufs, darum bleibt `entgangen_bei` leer. Bewusst hier statt als eigene Story: dieselbe Datei, dieselbe
  Wurzel und in einem Schnitt zu beheben — anders als bei B-161, wo die Trennung nötig war, weil dort ein
  `entgangen_bei` daranhängt.
- 2026-08-14 · **Ist-Stand korrigiert und `prio` P3 → P2**, beides aus dem Review von B-169. Die Analyse
  behauptete, `useAsync` setze `data` bei einem Fehler auf `null` — das gilt nur für die erste Ladung. Damit
  ist der Befund dieser Story nicht „ein unwahrer Satz", sondern ein **bedienungsloser Bildschirm**: seit
  B-169 sind die veralteten Zeilen gesperrt (richtig), und ohne Fehlermeldung gibt es aus dem Zustand keinen
  Ausweg. Das Gate darf dafür **nicht** aufgeweicht werden — die Zeilen *sind* veraltet.
- 2026-08-14 · Ist-Stand **präzisiert** aus der Nachschau zu B-169: Meine Formulierung „Dauerzustand, kein
  Wiederholen" war zu stark — es gibt drei Auswege. Der Befund ist damit nicht kleiner, sondern **schärfer**:
  Ausgerechnet die Bewegung, die ein Nutzer versucht (über den Stepper zurück und wieder vor), ist die
  wirkungslose. Damit steht auch fest, was diese Story braucht: nicht nur ein Banner, sondern ein „Erneut
  suchen". `prio` bleibt P2.
