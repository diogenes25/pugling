---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [pointsFrom current heisst zweierlei, Punkte der vorigen Uebung wandern mit]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-01 zu B-45
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-45]
wartet_auf: ""
---

# B-191 · Die Punkte-Vorbelegung trägt den Wert der zuvor angeklickten Übung mit

Die Fehlerfamilie dieses Repos, wörtlich: **eine Bedingung, die zwei Situationen zusammenzieht.**

## User Story

Als **Vater** möchte ich, dass die vorgeschlagene Zielbelohnung zu der Übung gehört, die ich gerade
gewählt habe, damit ich meinem Kind nicht versehentlich das Fünffache für die falsche Aufgabe zusage.

## Ist-Stand am Code

`PlanPositions.tsx:184-186` — `pointsFrom(ex, current)` liefert `current` zurück, wenn die Übung keine
Empfehlung trägt. **`current` steht dabei für zwei verschiedene Dinge:**

- „Der Vater hat diese Zahl selbst getippt" — der Fall, für den die Regel gedacht ist.
- „Die **vorher angeklickte** Übung hat diese Zahl empfohlen" — der Fall, den niemand gewogen hat.

**Ablauf, an dem es sichtbar wird:**

1. Übung A anklicken (`rewardPoints = 100`) → das Feld steht auf 100.
2. Übung B anklicken (`rewardPoints = 0`, also **keine** Empfehlung) → `pointsFrom` gibt `current` = 100
   zurück, das Feld bleibt auf 100.
3. Hinzufügen → die Position zu B trägt **100 Münzen** Zielbelohnung, die weder B empfiehlt noch jemand
   getippt hat. Der Server allein hätte 20 gesetzt.

Der Kommentar `PlanPositions.tsx:177-179` und die Testkonstante `GETIPPT` in `PlanPositions.test.ts:146`
schreiben die Annahme „selbst getippt" fest — und **können die beiden Fälle nicht unterscheiden**. Genau
darum ist die Suite grün.

## Die echte Lücke

Es fehlt der Zustand, der „getippt" von „geerbt" trennt. Dasselbe Muster wie `Testable` als Typ- statt
Tages-Aussage (B-114) oder „leer" für „nichts gekauft" und „Laden gescheitert" (B-111).

## Was das Nachlesen ergänzt hat (2026-09-01, zweiter Nachtlauf)

**Das ist ein verfehltes Akzeptanzkriterium, nicht nur ein Nebeneffekt.** B-45s eigenes AK 4 verlangt
wörtlich, das Feld werde *„mit dem `RewardPoints`-Wert der gewählten Übung vorbelegt (bzw. `20`, wenn die
Übung `0` trägt)"*. Der Code setzt statt `20` aber `current`. Das Kriterium wurde abgenommen, ohne erfüllt
zu sein — und der Test konnte es nicht messen, weil er nur **eine** Übung anklickt.

**Die spätere Verfeinerung widerspricht dem Kriterium nicht, sie braucht nur einen Zustand.** Der Kommentar
`PlanPositions.tsx:441-443` begründet den `current`-Zweig damit, dass eine *selbst getippte* Zahl stehen
bleiben soll. Das ist richtig und soll bleiben — es fehlt allein die Unterscheidung, ob sie getippt wurde.

## Entscheidungen

1. **Ein Zustand „der Vater hat die Zahl gesagt" wird eingeführt**, und die Vorbelegung greift nur, solange
   er falsch ist. *Begründung:* Er ist das Einzige, was AK 4 („bzw. 20") und die spätere Verfeinerung
   („getippt bleibt stehen") gleichzeitig erfüllbar macht; ohne ihn schließen sie sich aus, und der Code
   hat sich stillschweigend für die Verfeinerung entschieden. Das Muster ist im Repo entschieden — der
   Assistent führt `touchedFineTune` genau dafür (`VaterWizard.tsx`). *Kosten:* ein Feld mehr im
   Formularzustand, das bei jedem neuen Vorbelegungs-Feld mitgedacht werden muss.
2. **Der Zustand hängt am Feld, nicht am Formular.** *Begründung:* `touchedFineTune` im Assistenten gilt
   für einen ganzen Block; hier geht es um genau ein Feld, und die anderen vorbelegten Werte
   (`useLeitner`, `requireTypedTest`, `itemCount`) sollen weiter bei jedem Übungswechsel überschrieben
   werden — sie sind Übungs-Eigenschaften, die Zielbelohnung ist eine Vater-Aussage. *Kosten:* Das Repo
   trägt danach zwei Körnungen desselben Musters. Ausgesprochen statt vereinheitlicht, weil die gröbere
   hier falsch wäre.
3. **Der Testfall klickt zwei Übungen nacheinander.** *Begründung:* Das ist der Kern des Defekts und der
   Grund, warum er durchkam — ein Fall mit einer Übung kann ihn nicht fangen, egal wie er sonst aussieht.
   *Kosten:* keine.

## Akzeptanzkriterien

1. Übung A (Empfehlung 100) anklicken, dann Übung B (Empfehlung 0) → das Feld zeigt **20**, nicht 100.
   (Das ist B-45s AK 4, wörtlich.)
2. Eine selbst getippte Zahl überlebt einen Übungswechsel unverändert.
3. Die übrigen aus der Übung übernommenen Werte verhalten sich unverändert.
4. Ein Test klickt **zwei** Übungen nacheinander; die rote Probe nennt erwartete und gemessene Zahl.

## Schätzung

- **Größe: S.** Anker: ein Zustandsfeld, eine geänderte Signatur, ein Renderzweig, zwei bis drei Testfälle.
- **Migration: nein**, **Vertragsbruch: nein**.
- **Testweg:** `frontend/src/vater/PlanPositions.test.ts` — dort liegt schon die Konstante `GETIPPT`, deren
  Annahme dieser Defekt widerlegt. Zusätzlich der E2E aus B-192 (gemeinsamer Weg).

## Offene Punkte

1. ~~Woran erkennt man „selbst getippt"?~~ → **Entscheidung 1 und 2** (Empfehlung bestätigt, aber auf
   Feld-Körnung statt Block-Körnung präzisiert).
2. ~~Soll ein Übungswechsel eine getippte Zahl überschreiben?~~ → **Entscheidung 1**, Empfehlung
   bestätigt: nein.
3. ~~Deckt der Testweg beide Richtungen?~~ → **Entscheidung 3**, Empfehlung bestätigt.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-45 (Pflichthandlung der Retrospektive, Nachtlauf).
  Ablauf und Fundorte vom Nachschau-Agenten am Code belegt und von mir gegengelesen. `entgangen_bei:
  [B-45]` — der Code stammt aus dem Abnahme-Paar `48f06b1`/`5590ef3`.
- 2026-09-01 · **`ausformuliert` → `gegrillt` → `geschaetzt`** (zweiter Nachtlauf, Freigabe 1 — `art:
  Defekt`). Ist-Stand **selbst** am Code nachgelesen statt aus dem Agentenbericht übernommen; dabei kam
  der eigentliche Befund erst heraus: Es ist B-45s AK 4, wörtlich verfehlt. S, Testweg benannt.
- 2026-09-02 · **`in-arbeit` → `abgenommen`** (zweiter Nachtlauf, Sprint 2). Belege:
  - **Reviewer:** `pugling-reviewer` (Backend) **und** `frontend-reviewer` — `wo` verlangt bei diesem
    Sprint beide. Der Backend-Reviewer meldete **keinen Blocker**, der Frontend-Reviewer **einen**
    (siehe unten). Alle Funde sind eingearbeitet.
  - **Rollengang:** maschinell — voller Playwright-Lauf **44/44 grün**, echter Chromium gegen echten
    Server mit Wegwerf-DB.
  - **Zahlen:** `dotnet build` grün · `dotnet test -c Release` **838/838** · `npx tsc -b` sauber ·
    `npm run test` **327/327**.
  - **Commits:** `c3cdec3` (Bau) und der Abnahme-Commit dieses Sprints.
  - **Der Blocker betraf genau diese Story, und er wiegt:** Der `frontend-reviewer` hat nachgewiesen, dass
    `punkteGetippt` in **keinem** Testlauf jemals `true` war — man konnte das Flag durch das Literal
    `false` ersetzen, und alles blieb grün. Der Kern der Reparatur lag außerhalb jeder Zusicherung. Die
    rote Probe dazu ist nachgeholt und gemessen: `Expected "77", Received "60"`.
  - **Zweiter Fund derselben Runde:** Das Flag überlebte das Hinzufügen einer Position — Position 2..n
    trugen die Zahl, die der Vater für Position 1 getippt hatte, und **keine** weitere Übung belegte mehr
    vor. Das war ein Rückschritt gegenüber dem Stand *vor* dieser Story und widersprach der
    ausgelieferten Feldhilfe. Behoben (`setPunkteGetippt(false)` beim Anlegen) und im E2E belegt.
