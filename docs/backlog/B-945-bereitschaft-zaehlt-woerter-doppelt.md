---
tags: [typ/story, status/idee, bereich/klassenarbeit, rolle/student]
aliases: []
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 133 in der Retro von Sprint 135 (docs/pm-sitzung-2026-11-24.md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-751]
wartet_auf: ""
---

# B-945 · Die Bereitschaft „x von y Wörtern sicher“ zählt ein Wort doppelt, das in zwei Übungen des Bündels steht

`ChildClassTestsController.Readiness` (Commit 65f4d2a7) zählt als Nenner alle `ExerciseItem`s der Vokabel-Übungen im Bündel und als
Zähler die `ItemProgress`-Zeilen je `ItemId`. Die Arcade-Karte beschriftet das als „x von y **Wörtern** sicher“ (`classTest.secureWords`).
Steht dieselbe Store-Vokabel in zwei Übungen des Bündels (Unit-Übung plus Wiederholung, oder Tag-Übung neben direkter), zählt sie im
Nenner zweimal, und ihr Lernstand je Item kann sich unterscheiden: das Kind sieht mehr Wörter, als die Arbeit hat. AK 2 von B-751
spricht von „Items“, die Oberfläche von „Wörtern“ – die Abnahme prüfte gegen den AK, nicht gegen den Satz, den das Kind liest.
Unverifiziert: ob echte Bündel überlappen, ist an der echten DB nicht gemessen. Vorschlag zum Prüfen: nach `VocabularyId` falten
(sicher = sicher in mindestens bzw. in allen Items des Worts – zu entscheiden) oder die Beschriftung auf „Karten“ ändern.

## Verlauf

- **2026-11-24** — angelegt in der Nachschau der Retro von Sprint 135, nicht behoben.
