---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: [Richtung both ist abwechselnd]
status: idee
prio: P1
art: Wunsch
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: B-463
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-472 · Jedes Wort einer Wortliste in beide Richtungen üben

Abgespalten aus [B-463](B-463-ki-lehrplan-deckt-ein-sechstel-der-wortliste.md) (Entscheidung 3). Die
Access-Wortlisten tragen `direction: both`. Im Server heißt das **abwechselnd**: Eintrag 1 wird Englisch →
Deutsch gefragt, Eintrag 2 Deutsch → Englisch, und so fort (`ExerciseContentProvider.cs:77–82`). Jedes Wort kommt
also nur in einer Richtung dran, und die Hälfte der Wörter schreibt das Kind nie auf Englisch, obwohl der
Vokabeltest der Klasse 7 genau das abfragt. Zu entscheiden: `both` umdeuten (jedes Wort zweimal, eigener
Lernstand je Richtung) oder die Richtung an die Position legen (zwei Positionen je Liste). Beides berührt
Lernstand und Leitner-Fortschritt je Eintrag.

## Verlauf

- **2026-11-11** — angelegt (beim Ausformulieren von B-463).
