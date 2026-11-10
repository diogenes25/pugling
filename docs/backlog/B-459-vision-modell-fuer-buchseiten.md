---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Vision-Modell zu schwach]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-375
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-459 · Fotos echter Buchseiten brauchen ein stärkeres Vision-Modell als gemma4:e2b

Im Rollengang von B-375 (2026-11-10) mit 53 Fotos eines echten Lehrwerks hat `gemma4:e2b` die Zeile des
Inhaltsverzeichnisses zu Unit 2 gelesen. Die Themen stimmten, die Grammatik war falsch („Zustandssätze“
statt will-future und conditional 1/2), und Themen landeten ohne Übersetzung als Wörter. Eine selbst
gerenderte, groß gesetzte Testseite las dasselbe Modell richtig. Vermutlich überfordern dicht bedruckte,
hochauflösende Doppelseiten ein Modell dieser Größe (verkleinert gelesen). Für das echte Lehrwerk hat
deshalb Opus die Seiten ausgewertet, nicht der Agent. Zu klären: ob ein größeres lokales Vision-Modell
(z. B. `llama3.2-vision` oder ein Qwen-VL) reicht, ob der `PageReader` große Seiten in Kacheln zerlegen
soll, und welche Empfehlung README und Skill nennen.

## Verlauf

- **2026-11-10** — angelegt (Rollengang von B-375).
