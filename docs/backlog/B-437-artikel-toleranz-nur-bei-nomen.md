---
tags: [typ/story, status/idee, bereich/training, rolle/student, rolle/creator, lerntechnik/vokabeln]
aliases: [Artikel-Toleranz bei Nicht-Nomen, a few ist nicht few]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-437 · Die Artikel-Toleranz greift auch dort, wo das erste Wort kein Artikel eines Nomens ist

Seit B-421 darf ein führender Artikel der Antwortsprache fehlen. Die Regel prüft nur das **erste Wort** gegen die
Artikel-Tabelle (`StageMechanics.WithoutLeadingArticle`), nicht die Wortart. Bei Nicht-Nomen ändert das die Bedeutung:
„a few" (einige) nimmt „few" (wenige) an, „a little" nimmt „little", „un peu" nimmt „peu"; französisch „des" ist auch
Teilungsartikel.

Vom `pugling-reviewer` in Lauf AE (Sprint 23) als 🟡 gemeldet. Entscheidung 1 von B-421 deckt den Wortlaut („Artikel als
eigenes Wort"), darum kein Code-Defekt und im Nachtlauf nicht entschieden. Der günstige Weg des Reviewers: die Toleranz in
`ExerciseContentResolver` nur setzen, wenn die Store-Vokabel ein Nomen ist oder keine Wortart trägt (die `Vocabulary` ist
dort schon geladen). Die Frage für den Dialog: so eng – oder bewusst so weit, weil die Fälle selten sind und das Reveal den
Artikel ohnehin zeigt?

## Verlauf

- **2026-11-08** — angelegt aus dem Review von B-421 (Lauf AE, Sprint 23).
