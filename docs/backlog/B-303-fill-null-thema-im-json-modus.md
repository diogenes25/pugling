---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [topics null im JSON-Modus, NullReferenceException in Clamp]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review zu B-300/B-301 in Lauf F, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22]
wartet_auf: ""
nachgeschaut: "2026-10-27"
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
- **2026-10-27** — in Lauf F, Teil 3 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS) und gebaut. **Gemessen:** `Fill_Laesst_Ein_Null_Thema_Weg_Statt_Abzustuerzen` mit
  `{"topics":["Growing up",null,"  "]}` → rote Probe **1 failed** (`NullReferenceException`, der Deserializer lässt das
  `null` durch). Fix: `t?.Trim() ?? ""` vor dem Leer-Filter → grün; Mutante = alte Zeile (ohne `?? ""` kompiliert sie
  mit Nullable-Warnungen als Fehler gar nicht – das Tor liegt dann beim Compiler). Review: `Assert.Empty(Dropped)`
  hält die Absicht „ein fehlendes Thema ist kein zu nennender Verlust“ (Mutante: `null` als `"<null>"` melden) →
  im Kommentar benannt. 🟡 außerhalb des Diffs: dieselbe Klasse in den Übungsstrategien →
  [B-306](B-306-null-element-in-uebungsstrategien.md). Suite **979/979 `exit=0`**. **`abgenommen`.**
- **2026-10-27** — Nachschau (Lauf F, Teil 5, frischer `pugling-reviewer` am Commit `193f4f6`): alte Zeile `t.Trim()`
  als Mutante → `NullReferenceException`; die Behauptung „ohne `?? ""` kompiliert es nicht“ geprüft (CS8602 unter
  `TreatWarningsAsErrors`); `UnitNotesDraft` hat nur eine Liste, `Grammar`/`VocabularyNotes` laufen über das
  null-sichere `Text()`; ein Nicht-String-Element endet über `TryGetResult` in der klaren `AgentException`. Kein
  Entgleiten. 🟢 Testkommentar „trims before it filters“ träfe auch eine korrekte Alternative → präzisiert.
