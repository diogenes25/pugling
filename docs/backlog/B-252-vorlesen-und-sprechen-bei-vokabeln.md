---
tags: [typ/story, status/idee, bereich/frontend, bereich/vokabeln, rolle/student]
aliases: [Vokabel vorlesen, Spracheingabe]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-252 · Vokabeln vorlesen lassen und optional per Stimme beantworten

Knowunity hat Sprach-Wiederholung im Plan und einen Sprachmodus. Bei uns gibt es Ton nur, wo ein Creator
Audio hochlädt (Übungstyp Hörverstehen); eine Vokabelkarte kann ihre Aussprache nicht hören lassen. Die
Idee: ein Lautsprecher-Knopf an der Vokabelkarte liest das Wort über die Browser-Sprachausgabe
(`speechSynthesis`) in der Zielsprache vor – reine Frontend-Arbeit, keine Medien, kein Vertrag. Als
zweiter Schritt optional eine Spracheingabe als Antwortweg. Vorher den Klang-Vorbehalt aus
[B-31](B-31-geraete-vorbehalt-klang.md) prüfen. Anti-Cheat-Frage: Vorlesen darf auf der Abfrageseite
nur das Gefragte sprechen, nie die Lösung – sonst ist es ein Lösungskanal am Server vorbei.

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 5 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **die Grundannahme stimmt nicht** – Vokabeln haben schon Aussprache-Audio (`PronunciationAudioUrl`, `ExerciseContentResolver.cs:100`; `AudioButton` an der Karte, `SohnPractice.tsx:295`) und eine Hörstufe, in der der Server bewusst keinen Text schickt (`AudioButton.tsx:8-10`). Browser-TTS bräuchte den Text am Client und wäre in der Hörstufe ein Lösungskanal. Übrig bleibt: TTS als Rückfall für Vokabeln **ohne** Aufnahme und nie in der Hörstufe – das ist die Frage beim Ausformulieren.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-0 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Browser-Sprachausgabe **nur als Rückfall** für Vokabeln **ohne** Aufnahme und **nie** in der Hörstufe (sonst Lösungskanal am Server vorbei). Beim Ausformulieren gilt das als Rahmen; offen bleiben die Spracheingabe als zweiter Schritt und wie der Server ‚hat keine Aufnahme‘ meldet.
