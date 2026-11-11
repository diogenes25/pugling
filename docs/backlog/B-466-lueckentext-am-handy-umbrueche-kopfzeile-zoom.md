---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Cloze mobil, Tagestest-Kopf, 16px]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
unverifiziert: true
entgangen_bei: []
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

Regressionstest: eine E2E im Handy-Viewport, die prüft, dass Satz 2 unter Satz 1 beginnt und der Knopf
„Später weiter“ ganz im Viewport liegt.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
