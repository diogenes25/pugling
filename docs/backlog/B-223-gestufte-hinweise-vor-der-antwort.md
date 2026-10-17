---
tags: [typ/story, status/idee, bereich/training, bereich/backend, rolle/student, rolle/creator]
aliases: [Gestufte Hinweise, Hint-Stufen, Hilfe vor der Antwort]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-223 · Gestufte Hinweise vor der Antwort, die Punkte kosten

Ein Vokabel-Item trägt einen statischen `Hint`, der mit der Karte ausgespielt wird; danach kennt der
Student-Pfad nur Antwort und Reveal. Khan Academy gibt über Khanmigo Strategiehinweise, ohne die Lösung
zu verraten. Die Idee: ein gestufter Hinweisweg am Item (etwa Anfangsbuchstabe, dann Wortlänge, dann
ein weiterer Buchstabe), den das Kind aktiv anfordert und der die Review-Punkte der Karte mindert. Der
Server muss den Hinweis so herausgeben, dass die Lösungsfeld-Regel (kein `Answer`/`Solution` an den
Studenten) unverletzt bleibt, und die Minderung muss im `ScoringService` nachvollziehbar sein.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 6 des Übernahmeplans).
