---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/lehrplan, rolle/supervisor]
aliases: [Gescheiterte Katalogsuche liest sich als leerer Katalog]
status: abgenommen
prio: P2
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
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

## Entscheidungen

1. **Beides: Fehler anzeigen *und* den Leer-Satz schärfen.** `exercises.error` bekommt ein eigenes
   `banner err`; der Satz „Keine passenden Übungen im Katalog" greift nur noch, wenn die Abfrage
   tatsächlich gelaufen ist. *Begründung:* Ein Banner allein ließe den falschen Satz daneben stehen — die
   Seite sagte dann gleichzeitig „das ging schief" und „der Katalog ist leer, lege welche an".
   *Kosten:* ein dritter sichtbarer Zustand im Trefferbereich (Fehler / leer / Zeilen) statt zwei.
2. **Ein „Erneut suchen"-Knopf gehört dazu, nicht in eine Folge-Story.** *Begründung:* Der Ist-Stand
   belegt, dass die intuitive Bewegung (über den Stepper zurück und wieder vor) **keine** neue Abfrage
   auslöst; ohne Knopf stünde die neue Fehlermeldung über einem Bildschirm, aus dem der Vater nur mit F5
   herauskommt — und das kostet den ganzen Assistenten, es persistiert kein Zustand. Eine Meldung ohne
   Ausweg ist eine schlechtere Lage als die heutige, nicht eine bessere. *Kosten:* Diese Story trägt
   damit ein Bedienelement, das anderswo (40 weitere Stellen, siehe B-174) noch fehlt — die Asymmetrie
   ist gewollt und begründet: hier ist der Ausweg nachweislich versperrt, dort nicht.
   **Abgrenzung:** Der *allgemeine* Wiederholmechanismus über alle Lesewege ist
   [B-183](B-183-gescheitertes-lesen-ohne-erneuten-versuch.md) und dort als `Wunsch` gesperrt. Diese
   Story baut **einen** Knopf an **einer** Stelle, keine Infrastruktur.
3. **Der Trefferbereich wird als eigener exportierter Baustein herausgezogen.** *Begründung:* Der
   Fehlerfall verlangt einen scheiternden Abruf. `VaterWizard` ist eine ganze Seite am Netz, und kein Test
   dieses Frontends mockt `../lib/api` (`CatalogAdmin.test.tsx:6-9`) — der Zustand wäre auf Bausteinebene
   sonst gar nicht erreichbar und im E2E nur mit abgeschaltetem Server. Vorbild ist derselbe Schnitt, den
   B-154 an `CategoryRows` gemacht hat. *Kosten:* Ein Baustein mehr in der Datei und sechs bis acht
   Props an seiner Grenze; dafür ist die Regel testbar statt behauptet.
4. **Die Trefferzahl wird während einer laufenden Abfrage nicht behauptet.** Sie hängt am selben
   `zeilenVeraltet` wie die Kästchen und der „Alle wählen"-Knopf. *Begründung:* Eine Wahrheit, drei
   Steuerelemente — dieselbe Begründung, mit der B-169 das Gate an den Knopf genommen hat, statt ein
   zweites einzuführen. *Kosten:* keine; der Schlüsselvergleich existiert bereits (`:199-200`).
5. **`art` bleibt `Defekt`** (offener Punkt 3 der Ausformulierung, Empfehlung bestätigt). *Begründung:*
   Die Meldung ist nicht unschön, sondern unwahr und handlungsleitend — sie schickt den Vater ins
   Anlege-Formular, während der Katalog voll ist. *Kosten:* keine.

## Schätzung

- **Größe: M.** Anker: eine Datei, ein herausgezogener Baustein mit Props, drei Renderzweige, ein neues
  Bedienelement, dazu ein neuer Testblock mit vier bis fünf Fällen. Größer als B-174 (S) genau um den
  Baustein-Schnitt und den Knopf.
- **Migration: nein**, **Vertragsbruch: nein** — alles Zustände, die `useAsync` schon liefert.
- **Testweg:** neue Datei `frontend/src/vater/VaterWizard.test.tsx` gegen den herausgezogenen Baustein
  (`npm run test` unter `frontend/`). Fälle: (a) Fehler → Banner sichtbar, Leer-Satz **nicht**,
  (b) Fehler → „Erneut suchen" ruft `reload`, (c) echte Leermenge → alter Satz unverändert,
  (d) veraltete Zeilen → Trefferzahl wird nicht behauptet. **Kein E2E:** der Fall verlangt einen
  scheiternden Abruf, den ein Browserlauf nur mit abgeschaltetem Server erzeugt — laut `pm-loop` Step 6
  die ausdrücklich argumentierte Ausnahme, nicht eine Lücke.

## Offene Punkte (aus der Ausformulierung — hier beantwortet)

1. ~~Nur den Fehler anzeigen, oder auch den Leer-Satz schärfen?~~ → **Entscheidung 1** (Empfehlung bestätigt). Ursprünglich: Empfehlung beides. `exercises.error` in
   ein `banner err` (das Muster steht direkt daneben, `:434`), und der Leer-Satz greift erst, wenn
   `exercises.data !== null`.
2. ~~Gilt dasselbe an weiteren `useAsync`-Leerzweigen?~~ → **gemessen am 2026-09-01: 41 von 105 Aufrufstellen rendern ihren Fehler nicht.** Die Zahl liegt samt Klassifizierung bei [B-174](B-174-arten-liste-verschweigt-ihren-fehler.md); ein Wächter wird daraus **nicht** abgeleitet, weil Options-Lieferant und Hauptinhalt nicht dieselbe Klasse sind. Ursprünglich: zu messen, bevor eine Regel daraus wird — B-116 hat
   dieselbe Frage für den Ladezustand gestellt und drei Stellen gefunden. Empfehlung: erst zählen, dann
   entscheiden, ob ein Wächter trennscharf formulierbar ist.
3. ~~Ist es ein `Defekt` oder `Aufräumen`?~~ → **Entscheidung 5**. Ursprünglich: Empfehlung `Defekt` — die Meldung ist nicht unschön, sie ist
   unwahr und führt zu einer Handlung.

## Akzeptanzkriterien

1. Scheitert die Suche, sieht der Vater den Fehler und **nicht** „Keine passenden Übungen im Katalog".
2. Findet die Suche wirklich nichts, bleibt der bisherige Satz mit seiner Anweisung.
3. Während eine neue Suche lädt, behauptet die Trefferzahl nicht die Zahl der vorigen.
4. Aus dem Fehlerzustand führt ein sichtbarer Weg heraus, der ohne F5 auskommt.
5. Ein Test deckt die Fälle 1–4; die rote Probe nennt erwartete und gemessene Zahl.

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
- 2026-09-01 · **Ist-Stand nachgemessen, Zeilennummern korrigiert** (Nachtlauf). Die Datei ist seit B-161/
  B-169 gewachsen: Der Leer-Zweig steht heute auf `VaterWizard.tsx:561-563` (nicht `:434-437`), die
  Trefferzahl auf `:546` (nicht `:479`), der Ladeplatzhalter auf `:561` (nicht `:489`). **Der Befund selbst
  hält unverändert:** `exercises.error` kommt in der ganzen Datei nicht vor, und der Leer-Zweig hängt
  weiterhin allein an `filteredExercises.length === 0`.
- 2026-09-01 · **`ausformuliert` → `gegrillt`** (Nachtlauf, Freigabe 1 — `art: Defekt` ist autonom
  grillbar). Fünf Entscheidungen mit Begründung und Kosten; alle drei offenen Punkte sind beantwortet,
  durchgestrichen statt gelöscht. Ein Akzeptanzkriterium ist **hinzugekommen** (Nr. 4, der Ausweg): Der
  Entwurf der Ausformulierung hätte eine Meldung ohne Ausweg zugelassen und damit die Lage verschlechtert.
- 2026-09-01 · **`gegrillt` → `geschaetzt`** (Nachtlauf). M, Testweg als benannte Datei mit vier Fällen;
  das Fehlen eines E2E ist argumentiert, nicht ausgelassen.
- 2026-09-01 · **`geschaetzt` → `in-arbeit`** (Nachtlauf, Sprint 1, Commit `e479d83`).
  `WizardTrefferbereich` herausgezogen; Fehlerbanner mit `role="alert"`, „Erneut suchen", geschärfter
  Leer-Zweig, und die Trefferzahl hängt am selben `zeilenVeraltet` wie Kästchen und Knopf.
  **Rote Probe (Baustein): erwartet 3 von 5 rot, gemessen `3 failed | 2 passed`** — der Baustein wurde
  dafür vorübergehend auf sein Vorher-Verhalten zurückgedreht; zwei der fünf Fälle sind Gegenproben, die
  auch vorher grün sein mussten. **Rote Probe (E2E): erwartet 1 von 2 rot, gemessen
  `1 failed | 1 passed`** — und das erst im **zweiten** Anlauf: das Routen-Muster des ersten
  (`creator/exercises/search`) traf den Endpunkt nicht, der Fall fiel also aus dem *falschen* Grund
  (`creator/exercises?…` ist richtig). Ein rot gelaufener Test ist kein Beleg, solange nicht feststeht,
  woran er fiel.
- 2026-09-01 · **`in-arbeit` → `abgenommen`** (Nachtlauf, Sprint 1). Belege:
  - **Reviewer:** `frontend-reviewer` in **zwei** Runden. Runde 1 setzte einen berechtigten Blocker
    (Meldungsführung), Runde 2 meldete **keinen Blocker mehr**; ihre drei kleineren Funde sind in
    `6688ff5` eingearbeitet.
  - **Rollengang:** maschinell geführt — voller Playwright-Lauf **42/42 grün**, echter Chromium gegen
    echten Server mit Wegwerf-DB. Freigabe 6 greift nicht (keine Browser-Verbindung in dieser Sitzung);
    der E2E-Weg ist der dokumentierte **stärkere** Beleg, kein Ersatz.
  - **Zahlen:** `npx tsc -b` sauber · `npm run test` **326/326** in 38 Dateien.
  - **Commits:** `e479d83` (Bau) und `6688ff5` (Nachprüfung des Reviewers).
  - **Was ein Mensch noch ansehen muss:** nur bei B-151/B-173 — siehe dort.
