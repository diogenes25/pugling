---
tags: [typ/story, status/idee, bereich/katalog, bereich/vokabeln, rolle/creator]
aliases: [Foto zu Vokabeln, Vorlage zu Übung]
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

# B-251 · Ein Foto der Vokabelliste aus dem Buch landet im Vokabelspeicher

Knowunity macht aus hochgeladenen PDFs, Folien, Fotos oder Handschrift Karteikarten und Quizfragen. Unser
KI-Creator (`backend/Pugling.Agent.Creator`) erzeugt Übungen aus Briefing und Lehrwerks-Unit, nicht aus
einer Vorlage – wer die Vokabelliste einer Unit erfassen will, tippt sie ab. Die Idee: ein neuer Modus
des Konsolen-Agenten nimmt ein Foto oder PDF (Vokabelliste, Arbeitsblatt), extrahiert Wort/Übersetzung
und legt die Einträge über die Creator-API im Vokabelspeicher an, mit Vorschau vor dem Anlegen. Bleibt
in der Creator-Ebene: kein LLM im Backend, kein Zugriff des Kindes, die Anti-Cheat-Lage ändert sich
nicht. Zu klären: ob das lokale Ollama-Modell Bilder lesen kann (Vision-Modell) oder ob eine
OCR-Vorstufe nötig ist, und wie Dubletten gegen den bestehenden Store aufgelöst werden.

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 4 des Übernahmeplans).
