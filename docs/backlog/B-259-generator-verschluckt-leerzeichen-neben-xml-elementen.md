---
tags: [typ/story, status/idee, bereich/backend, bereich/doku]
aliases: [Leerzeichen neben c und b verschluckt, list-Ende klebt am Folgetext]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu Sprint 8 (vierter Nachtlauf 2026-10-23, Nit 5)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-259 · Der XML-Doc-Generator verschluckt Leerzeichen zwischen zwei Elementen

Gefunden beim Review von Sprint 8 (B-147/B-138), aber älter als dessen Diff: zwischen zwei aufeinanderfolgenden
Elementen geht das Leerzeichen verloren – `<b>PATCH semantics:</b> <c>null</c>` wird zu
``**PATCH semantics:**`null` `` (`UpdateMyAccountDto`), und ein `</list>` klebt am Folgetext
(`used?**Children** are…`, Quelle `ExerciseCatalogDtos.cs:68-70`). Der `OpenApiProseNormalizer` kann das nicht
reparieren: das Leerzeichen ist schon weg, wenn er läuft. Zu klären beim Ausformulieren: wie viele Stellen es
gibt (ein Prüfskript über `docs/openapi/v1.json`), ob ein Umformulieren der Quelle genügt (Text statt zweier
Elemente in Folge) oder ob ein Tor die Form in der Quelle verbieten soll. **Bewusst nicht in Sprint 8
mitgenommen:** außerhalb des Diffs, und das Sprint-Ziel (keine Einrückung, keine rohen Tags) ist ohne es erfüllt.

## Verlauf

- **2026-10-23** — angelegt aus dem Review von Sprint 8 im vierten Nachtlauf; ungeprüft.
