---
tags: [typ/story, status/idee, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Beinahe-Treffer, Tippfehler-Toleranz]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: B-421
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-422 · Prüfauftrag: soll ein Tippfehler („Beinahe-Treffer") verziehen werden – und wie?

Abgetrennt von [B-421](B-421-antwort-toleranz-artikel.md), das nur den Artikel verzeiht. Offen bleibt die
zweite Fairness-Frage: „Strase" statt „Straße", „Geschaft" statt „Geschäft". Duolingo wertet einen Buchstaben
Abstand als richtig mit Hinweis („Achte auf die Akzente"); `AnswerGrader.cs:14-15` kennt nur exakte
Gleichheit nach `Normalize`. **Produktfrage**, nur im Dialog: ein Beinahe-Treffer ist bei Vokabeln für ein
Kind in Klasse 5 Fairness, bei Rechtschreibübungen aber genau das, was geprüft werden soll. Empfehlung des
PM: Levenshtein-Abstand 1 ab fünf Zeichen Lösungslänge, **nur im Üben** als „fast richtig" gewertet (voller
Treffer, aber Reveal mit Markierung), **im Test** weiterhin exakt – und als Opt-out je Übung wie
`articleRequired`. Zu klären ist außerdem, ob Akzente/Umlaute (é/e, ä/ae) eine eigene, immer erlaubte
Normalisierung bekommen.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  abgetrennt von B-421 als eigene Wertentscheidung.
