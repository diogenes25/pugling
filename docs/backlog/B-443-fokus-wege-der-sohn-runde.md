---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Fokus-Wege der Sohn-Runde]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-439]
---

# B-443 · Nach „Jetzt abfragen" fällt der Fokus auf `body`, und drei Wege ins Rundenende hält kein Test

Gefunden in der Nachschau zu Sprint 24 (Commit `62bb35d`, Lauf AF, frischer `frontend-reviewer`), alle 🟢:

1. **Fokus nach „Jetzt abfragen" auf `body`** (älter, aus B-419): `startNextRound` (`frontend/src/sohn/SohnPractice.tsx:163`) baut den
   Knopf ab (`loading`), danach `loading → front`; der Effekt (`:156`) springt nur bei `wrong → front` ins Antwortfeld. Derselbe
   Fehler, den B-439 fürs Rundenende behoben hat.
2. **Drei Wege ins Rundenende ohne Test:** nach der Kennenlern-Runde (`displayOnly` → „Weiter"), nach der Runde hinter „Jetzt
   abfragen", nach Selbsteinschätzung auf der letzten Karte. Heute gehalten über dieselbe Bedingung (`:158`), aber eine Verengung auf
   eine Positivliste bliebe grün.
3. `prevPhase.current !== "done"` (`:158`) ist toter Code – der Effekt läuft nur bei Phasenwechsel.
4. `index.css:370-372`: der Kommentar zum `.banner`-Fehlerkasten steht jetzt über der neuen `.vtitle`-Regel.
5. `e2e/bilder.spec.ts:204-209`: fehlt die bebilderte Vokabel ganz, zeigt die Fehlermeldung auf den Zähler statt aufs Bild.

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 24 (Retro Sprint 25, Lauf AF). Nur 🟢, darum kein Limit 2.
