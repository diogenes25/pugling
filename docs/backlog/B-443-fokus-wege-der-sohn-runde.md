---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Fokus-Wege der Sohn-Runde]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-439]
nachgeschaut: 2026-11-13
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

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **Punkt 1:** „Jetzt abfragen" setzt eine Ref; der Wechsel Laden → Karte fokussiert danach das Antwortfeld (bzw. den
   ersten Knopf). Nur nach dieser Taste: beim ersten Laden der Seite springt keine Tastatur ungefragt auf (Handy).
2. **Punkt 3:** der tote Zweig `prevPhase.current !== "done"` entfällt (der Effekt läuft nur bei einem Phasenwechsel).
3. **Punkt 2:** zwei der drei Wege ins Rundenende tragen schon Tests (`führt nach der letzten Karte über Weiter`,
   `fokussiert das Rundenende auch nach einem Treffer`, Kennenlern-Runde in `nennt die neuen Wörter …`); der dritte
   (Selbsteinschätzung auf der letzten Karte) bleibt bewusst offen – dieselbe Bedingung, nur 🟢.
4. **Punkt 4** ist inzwischen erledigt (der Kommentar steht über `.banner`, `index.css:390`); **Punkt 5** (Meldung der
   E2E-Schleife) bleibt offen, nur 🟢.

## Akzeptanzkriterien

1. Nach „Jetzt abfragen" steht der Fokus in der ersten Karte, nicht auf `body`.

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 24 (Retro Sprint 25, Lauf AF). Nur 🟢, darum kein Limit 2.
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 52).
- **2026-11-13** — gebaut (Sprint 52). AK 1 → `SohnPractice.test.tsx` „setzt den Fokus nach Jetzt abfragen in die erste Karte"
  (T2 → rot) und – nach dem Review (🟡: nur Karten mit Eingabefeld bekamen den Fokus; Zähler 1 von 5) – „… auch auf eine
  Karte ohne Eingabefeld" (Rückfall auf die Taste der Karte, T7 → rot). 🟢 offen: nach einer gescheiterten neuen Runde
  bleibt der Fokus auf `body` (Fehlerkasten ohne `role="alert"`).
  - Verifikation: Vitest 816/816, `tsc -b` sauber; Mutanten T1–T7 7/7 rot nach der letzten Änderung (Tabelle im
    Protokoll, Sprint 52); Playwright 140/140 auf 5201/5174 (vor dem Fokus-Nachzug T7, der einen Komponententest trägt).
  - Rollengang Vater/Sohn: E2E-Suite gegen echten Server und UI; Creator Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 54/55, frischer `frontend-reviewer` an `59fc0a8`, gelesen, Vitest 68/68).
  Geprüft: Rücklesen symmetrisch zu Aufbau und Edit-Modal; Fokus nur nach „Jetzt abfragen", nie beim ersten Laden;
  Tooltip während des Ladens und im Ausfall; Wächter über alle `items.length` und `Paged<`-Dateien. Nur 🟢 →
  [B-515](B-515-nachschau-reste-sprint-52.md).
