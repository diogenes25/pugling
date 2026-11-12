---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Cloze mobil, Tagestest-Kopf, 16px]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
entgangen_bei: []
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-466 · Lückentext am Handy: Zeilenumbrüche verschwinden, „Später weiter“ ist abgeschnitten, das Eingabefeld zoomt

Gesehen bei 375×812 in `/sohn/test/{positionId}` an Übung 19 („Question tags“):

1. **Zeilenumbrüche fehlen.** Der Text trägt zehn nummerierte Sätze, je in einer Zeile (`\n`). Die Karte setzt
   sie als einen Absatz: „… the drums, ? 2. We aren't late, … 3. You can swim, …“. Der Container hat
   `white-space: normal`. Bei zehn Lücken findet das Kind die gesuchte kaum.
2. **Die Kopfzeile läuft aus dem Bild.** „Tagestest · Frage 1 / 10 · Selbstcheck · Später weiter“ passt nicht in
   375 px; der Knopf „Später weiter“ ist rechts abgeschnitten.
3. **Eingabefelder haben 15 px Schrift** (`index.css:258`, gemessen am Lösungsfeld). iOS-Safari zoomt bei
   unter 16 px beim Antippen in die Seite. Nicht am Gerät geprüft (kein iPhone im Prüfgang).

Nebenbefund derselben Ansicht: Das Etikett sagt „Selbstcheck“, das Feld verlangt eine getippte Lösung.

## User Story

Als **Sohn** möchte ich einen Lückentext am Handy Satz für Satz lesen, den Ausweg „Später weiter“ sehen und ins
Feld tippen können, ohne dass die Seite heranzoomt, **damit** ich die gefragte Lücke finde und die Runde nicht
an der Bedienung scheitert.

## Ist-Stand am Code

- `ClozePrompt` ([ClozePrompt.tsx](../../frontend/src/components/ClozePrompt.tsx)) setzt Textteile als `<span>` in
  ein `div.cloze-prompt`; für `.cloze-prompt` gab es nur `.fcard .cloze-prompt` (Schriftgröße), kein `white-space`
  → `\n` wird zum Leerzeichen. Dasselbe Bauteil zeigt den Text in Übung **und** Test.
- Kopfzeile des Tests ([SohnTest.tsx](../../frontend/src/sohn/SohnTest.tsx), `div.row` mit Titel und rechter
  Pillen-Gruppe): `.row` ist `display: flex` ohne `flex-wrap` ([index.css:82](../../frontend/src/index.css)).
  Die Übungsrunde ([SohnPractice.tsx](../../frontend/src/sohn/SohnPractice.tsx)) hat dieselbe Bauart.
- Lösungsfelder: Inline-Style `fontSize: 15` in `SohnTest.tsx` und `SohnPractice.tsx`, im Shop (Menge) ebenso;
  `.field input/select/textarea` in `index.css` mit `font-size: 15px`.
- Nebenbefund: seit B-476 zeigt die Pille die Stufe **dieser Frage** (`stageLabel(item.stage)`), beim Lückentext
  also „Tippen“ – erledigt, nicht Teil dieser Story.

## Die echte Lücke

Drei CSS-Lücken in der Arcade, die erst am schmalen Gerät auffallen; keine E2E lief bisher bei 375 px gegen einen
Lückentext oder gegen das längste Stufen-Etikett.

## Offene Punkte

1. ~~Nur der Test oder auch die Übungsrunde?~~ → Entscheidung 1.
2. ~~16 px nur im Sohn-Feld oder überall?~~ → Entscheidung 2.
3. ~~Ist Punkt 2 nach B-476 noch reproduzierbar?~~ → gemessen: beim Lückentext (Etikett „Tippen“, 3 Fragen) passt
   die Zeile, weil die Pillen in sich umbrechen; mit „Selbstcheck“ und „Frage 1 / 10“ (Vokabeln, Stufe 2) endet
   „Später weiter“ bei **403,5 px** – ja, reproduzierbar.

## Entscheidungen

1. **Beide Rundenköpfe (Test und Übung)** bekommen dieselbe Klasse `play-head`, die umbrechen darf; die Pillen
   brechen als Ganzes um, nicht in sich. Begründung: dieselbe Bauart, derselbe Fehler. Kosten: am Handy steht der
   Kopf in bis zu drei Zeilen statt einer.
2. **16 px für jedes Eingabefeld** (`.field …` global plus die drei Inline-Styles der Arcade). Begründung: der Zoom
   trifft jedes Feld, auch im Vater-Web am iPhone; 1 px mehr kostet am Desktop nichts. Kosten: Formulare im
   Vater-Web werden minimal größer.
3. **`white-space: pre-line` an `.cloze-prompt`**, nicht am Container der Karte: so behält jeder andere Text sein
   Verhalten, Mehrfach-Leerzeichen bleiben zusammengefasst.

## Akzeptanzkriterien

1. Bei 375 × 812 beginnt im Tagestest Satz 2 eines zeilenweise geschriebenen Lückentexts unter Satz 1.
2. „Später weiter“ liegt ganz im Fenster – auch im schlechtesten Fall „Frage 1 / 10 · Selbstcheck“; die Seite
   scrollt nicht seitlich.
3. Das Lösungsfeld im Tagestest hat mindestens 16 px Schrift.

## Schätzung

- **Größe S**, `wo: frontend`, keine Migration, kein Vertragsbruch.
- **Risiko:** andere E2E, die sich auf die einzeilige Kopfzeile verlassen (Position der Pillen). Volle E2E-Suite.
- **Angriffsplan:** E2E-Probe `e2e/lueckentext-am-handy.spec.ts` (zwei Fälle) rot messen, dann CSS/Klassen.
- **Testweg:** die neue Spec, volle E2E-Suite, Vitest; der Zoom selbst ist am iPhone zu prüfen (Gerätevorbehalt).

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — ausformuliert, gegrillt, geschätzt im Backlog-Lauf (art Defekt, autonom): Ist-Stand am Code,
  drei Entscheidungen; Punkt 2 nach B-476 nachgemessen (403,5 px im schlechtesten Fall).
- **2026-11-11** — rote Probe vor dem Fix: Fall 1 rot (Satz 2 bei 199 px, erwartet ≥ 220 px), Fall 3 rot (15 px,
  erwartet ≥ 16), Kopfzeile mit „Selbstcheck“ rot (rechter Rand 403,5 px, erwartet ≤ 375). Der Kopf mit „Tippen“ und
  drei Fragen war schon vorher grün – er steht nur als Zeuge in Fall 1, belegt wird AK 2 vom zweiten Test.
- **2026-11-12** — gebaut: `.cloze-prompt { white-space: pre-line }`, Klasse `play-head` an den Rundenköpfen von Test
  und Übung, 16 px an `.field …` und den drei Inline-Feldern der Arcade (Test, Übung, Shop-Menge). Volle E2E am
  Zwischenstand 137 passed, Vitest 742.
- **2026-11-12** — `frontend-reviewer`: kein 🔴. 🟡 die Kopfzeilen-Prüfung im ersten Test sicherte nur den
  Ausgangszustand (war vor dem Fix grün) – **Fund 2 des Sprints (zählt)**, aus dem ersten Test entfernt, Kopfkommentar
  ordnet AK 2 dem zweiten zu. 🟡 die Frontend-Angleichung an B-457 (`normalizeAnswer`) war ungetestet – **Fund 3
  (zählt)**, `normalizeAnswer`/`choicesMissAnswer` exportiert und in `exerciseConfig.test.ts` belegt. 🟢 ohne Zählung
  behoben: kürzere Sätze in der Spec (die Zeilenprobe war nur dank Segoe UI rot, mit anderer Schrift in der Linux-CI
  bräche „2.“ von selbst um); CSS-Blöcke neben ihre Geschwister verlegt. 🟢 bewusst nicht: `pre-line` im Rückfall-Zweig
  von `ClozePrompt` (er rendert die Fragen *jedes* Übungstyps – über diese Story hinaus); Tab/NBSP-Abweichung der
  Editor-Warnung; die Textarea des Anmerkungs-Widgets (nur DEV).
- **2026-11-12** — Mutanten nach der letzten Änderung (`mutant-probe.py`, `abbe8b5 + Arbeitsbaum`, 01:15):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | B466-M1-ohne-pre-line | index.css:189 | red | red |
  | B466-M2-Kopf-ohne-Umbruch | SohnTest.tsx:187 | red | red |
  | B466-M3-Feld-15px | SohnTest.tsx:252 | red | red |

  Grundlinie nach dem Zurückspielen grün. **Gerätevorbehalt:** ob iOS-Safari bei 16 px wirklich nicht mehr zoomt,
  misst nur ein iPhone (Chromium misst nur die Schriftgröße).
- **2026-11-12** — **abgenommen.** Commit `bc2c18a`. Rollengang (Sprint 31) als E2E am frisch gestarteten Server: `lueckentext-am-handy.spec.ts` (2 Tests, 375 × 812) in der vollen Suite, 137 passed. Gerätevorbehalt Zoom bleibt (iPhone), steht in der Liste für den Menschen.
