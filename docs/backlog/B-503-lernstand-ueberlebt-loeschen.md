---
tags: [typ/story, status/idee, bereich/lernen, rolle/supervisor]
aliases: [ItemProgress SetNull, Lernstand geht beim Löschen verloren]
status: idee
prio: P2
art: Frage
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Sprint 45, aus B-502)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-503 · Prüfauftrag: soll der Lernstand der Kinder das Löschen einer Übung oder Vokabel überleben?

Seit B-499/B-502 sagen die Lösch-Fragen (Übung, Unit, Reihe, Vokabel aus einer Übung) ehrlich, dass der Lernstand der
Kinder an den betroffenen Einträgen mitgeht: `ExerciseItem` → `ItemProgress` ist Cascade (`PuglingDbContext.cs:682`),
die Historie (`ItemReviewEvent.ItemId`) dagegen `SetNull`. Die Söhne lernen echt auf der App (seit 2026-11-11).

Zu entscheiden (im Dialog, `art: Frage`):

1. Soll `ItemProgress` beim Löschen erhalten bleiben (`SetNull` wie die Historie, oder an die `VocabularyId` gehängt) –
   der Lernstand je Wort ist ohnehin plan-übergreifend gedacht?
2. Oder bleibt das Löschen hart, und die Frage vor dem Löschen genügt?

Kosten von 1: eine Migration (Kette verlängern), Folgen für `by-word` und die Lernstand-Sichten, Waisen-Zeilen.

## Verlauf

- **2026-11-12** — angelegt (Sprint 45, aus dem Review von Sprint 44 / B-502 Punkt 2).
