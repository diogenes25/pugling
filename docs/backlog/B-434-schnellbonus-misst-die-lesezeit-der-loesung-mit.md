---
tags: [typ/story, status/verworfen, bereich/training, bereich/reward, rolle/student]
aliases: [Schnellbonus nach Fehler, Lesezeit zählt als Antwortzeit]
status: verworfen
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-nacht.md
unverifiziert: false
grund: "zusammengelegt mit B-530 – dieselbe Messstelle (elapsedSeconds ab der vorigen Review, PositionPracticeController.cs:418-420); B-530 ist die allgemeinere Fassung (seit B-526 jede Antwort mit Blatt, nicht nur Fehler)"
ersetzt_durch: [B-530]
---

# B-434 · Nach einem Fehler zählt die Lesezeit der Lösung als Antwortzeit der nächsten Karte

Seit B-420 hält eine falsch getippte Antwort die Karte an, bis das Kind „Weiter" drückt. Der Schnellbonus misst die Zeit
seit dem **vorigen Review** (`PositionPracticeController.Review`, `elapsedSeconds`), nicht seit dem Erscheinen der Karte.
Wer die Lösung in Ruhe liest, verliert damit den Schnellbonus auf der nächsten Karte – die Tempo-Leiste startet aber je
Karte neu und verspricht das Gegenteil.

Vom `frontend-reviewer` in Lauf AE als 🟡 gemeldet, ohne Frontend-Reparatur: die Messung liegt beim Server. Die Frage für
den Dialog: Zeit ab dem Ausspielen der Karte messen (der Server müsste wissen, wann `next` sie geliefert hat), oder den
Effekt bewusst hinnehmen (der Bonus belohnt Tempo über die ganze Runde)?

## Verlauf

- **2026-11-08** — angelegt aus dem Review von B-420 (Lauf AE, Sprint 22).
- **2026-11-13** — `verworfen` zugunsten von [B-530](B-530-antwortzeit-ab-sichtbarer-karte.md): Backlog-Abgleich gegen den Code. Die Frage hier (Zeit ab dem Ausspielen messen oder hinnehmen) wird dort beantwortet.
