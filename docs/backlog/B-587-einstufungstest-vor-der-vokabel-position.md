---
tags: [typ/story, status/idee, bereich/training, rolle/student, rolle/supervisor]
aliases: [Einstufungstest, Vortest, bekannte Wörter überspringen]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/sofatutor-vergleich-plan.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-587 · Ein Einstufungstest vor einer Vokabel-Position lässt sicher gewusste Wörter die Einführung überspringen

sofatutor beginnt mit einem kurzen Einstufungs-Quiz, aus dem der Lernplan entsteht. Pugling führt jedes
Wort einer Position neu ein, in Buchreihenfolge und begrenzt durch die Tagesdosis
(`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:201`–`215`,
`backend/Pugling.Api/Models/PlanPositionEntities.cs:40`), auch wenn das Kind es aus früheren Units oder
aus dem Unterricht längst kann. Bei einem Vier-Wochen-Plan aus einer Unit
([B-580](B-580-plan-aus-unit.md)) verbraucht das Bekannte die Tagesdosis, und das Neue kommt später –
für Friedrich, der Erfolge braucht ([B-535](B-535-belohnungen-erreichen-friedrich-nicht.md)), ist das
Langeweile statt Fortschritt, für Theodor ein Grund, die Access-Grundwörter wegzuklicken.

Die Idee: beim Start der Position (oder auf Wunsch des Vaters) ein einmaliger Test über den Pool,
**getippt** und serverseitig bewertet wie der Positions-Test. Sicher gewusste Wörter gelten als
eingeführt und starten in einem höheren Leitner-Kasten; sie belasten die Tagesdosis nicht, kommen aber
in der Wiederholung wieder. Falsch Getipptes bleibt in der normalen Einführung. Der Vater sieht das
Ergebnis als Startpunkt.

Zu klären beim Ausformulieren:

- **Anti-Cheat:** Nur getippte Antworten zählen (keine Selbsteinschätzung), einmal je Position, und ein
  übersprungenes Wort darf die Pflicht nicht billiger machen – es kommt in der Wiederholung wieder.
- **Startkasten** (2? 3?) und ob der Test auch den plan-übergreifenden Stand (`ItemProgress`,
  `backend/Pugling.Api/Services/Shared/ItemProgressService.cs:60`) befüllt.
- **Umfang:** ganzer Pool oder eine Stichprobe mit Abbruch nach N Fehlern, damit der Test nicht länger
  ist als eine Übungsrunde.
- **Münzen:** keine für den Test selbst (sonst lohnt Raten), aber das Überspringen soll sich als
  Fortschritt anfühlen.
- Verhältnis zu B-04 (Tagesdosis) und [B-419](B-419-erstbegegnung-neue-karte-erst-zeigen.md)
  (Erstbegegnung zeigt erst, dann fragt): der Einstufungstest fragt bewusst kalt, aber zählt nicht als
  Fehlversuch.

## Verlauf

- **2026-11-15** — angelegt (Quelle: sofatutor-Vergleich, Etappe 2 des Übernahmeplans; Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-547 auf B-587: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-557). Inhalt unverändert.
