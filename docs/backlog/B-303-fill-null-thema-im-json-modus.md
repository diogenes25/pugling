---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [topics null im JSON-Modus, NullReferenceException in Clamp]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review zu B-300/B-301 in Lauf F, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22]
wartet_auf: ""
---

# B-303 · Ein `null` in `topics` aus dem JSON-Modus ließe `fill` roh abstürzen

`UnitNotesFiller.Clamp` ruft `t.Trim()` auf jedes Thema. Im Schema-Modus verhindert der Elementtyp `string` ein
`null`; im JSON-Modus-Rückfall (`DraftAsync`) ist offen, ob der M.E.AI-Deserializer `"topics": [null]` durchlässt.
Wenn ja: `NullReferenceException`, roher Absturz statt stillem Weglassen. Vom Prüfer gelesen, **nicht gemessen**
(`unverifiziert`) – der erste Schritt ist ein Test mit einem `FakeChatClient`, der `{"topics":[null]}` antwortet.

**Behebung (Vorschlag):** `null`-Themen vor dem `Trim` herausfiltern – ein leeres Thema wird heute schon
weggelassen, ein fehlendes gehört dazu.

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-300/B-301 (Lauf F, Teil 2, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 3b (🟢, außerhalb des Diffs): eigene Story, weil das Sprint-Ziel ohne sie erfüllt ist.
