---
tags: [typ/story, status/idee, bereich/training, rolle/supervisor, lerntechnik/vokabeln]
aliases: [falsche Platzhalter-Art falsch geschrieben]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/backlog/B-990-eigene-platzhalter-kuerzel-des-kindes.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-994 · Eine Antwort mit der falschen Platzhalter-Art erscheint als „falsch geschrieben“

Seit B-990 vergleicht „falsch geschrieben“ (`AnswerGrader.IsMisspelled`) nach der Kürzel-Abbildung. „turn to smth“ für
„(to) turn to sb.“ wird zu „turn to sth“ gegen „turn to sb“ – zwei Zeichen, also „falsch geschrieben“. Nach B-990
Entscheidung 3 kennt das Kind die Wendung dann nicht ganz; der Report des Vaters zeigt einen Schreibfehler statt einer
Lücke. Nur die Anzeige, die Wertung bleibt falsch. Vorher war der Abstand 3 („smth“ gegen „sb“). Beim Rollengang von
B-990 an der Kopie gesehen; bewusst nicht im Sprint, weil das Sprint-Ziel ohne ihn erfüllt ist.

## Verlauf

- **2026-11-25** — angelegt aus dem Rollengang von B-990 (Vorschau an Übung 15).
