---
tags: [typ/story, status/verworfen, bereich/backend, rolle/creator]
aliases: [Birkenbihl decoding null 500]
status: verworfen
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: false
grund: "Für null-MEMBER widerlegt (implizites [Required] antwortet 400 validation_error mit dem Pfad in errors); für null-ELEMENTE einer Liste bestätigt – weiter in B-397."
ersetzt_durch: []
---

# B-391 · Ein Birkenbihl-Satz mit `"decoding": null` endet in einer 500 statt einer 400

`POST/PUT …/birkenbihl` mit einem Satz, dessen `decoding` `null` ist (ebenso `"sentences": null`), wirft eine `NullReferenceException`: seit B-387 in
`ValidateConfigAsync` (`SelectMany(s => s.Decoding)`, `backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs` um `:472`),
vorher eine Stufe später in `NormalizeConfig`. Ein Client-Fehler sollte `400 validation_error` sein, kein Serverfehler.
Zu prüfen beim Ausformulieren: ob dieselbe Lücke in anderen Configs mit verschachtelten Listen steckt (dann eine Regel, nicht eine
Stelle).

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-387 (Lauf W, Sprint 8, `pugling-reviewer`, 🟢). Älter als B-387, darum eigene Story
  und kein Fund im Zähler; am Code nicht reproduziert (`unverifiziert: true`).
- **2026-11-05** — ergänzt aus der Nachschau zu Sprint 8: auch `"sentences": null` endet in einer 500 (`SelectMany` auf `null`).
- **2026-11-05** — **verworfen** in Lauf X, Sprint 10, nach Messung. Die Tests liefen gegen den Ausgangszustand: `"sentences": null`,
  `"decoding": null`, `"items": null` (Übersetzung, Vokabelübung) und ein `null`-String antworten schon heute `400 validation_error`,
  `errors` nennt den Pfad (`Config.Sentences[0].Decoding`). Die 500 stammte aus dem Lesen des Validators, der hinter der
  Modellvalidierung gar nicht erreicht wird. Ein gebauter Reflexions-Wächter war darum doppelt und ist wieder entfernt. **Geblieben:**
  sechs Regressionsfälle (`UnknownReferenceCodeTests.cs:295`, `:307`); rote Probe: implizites `[Required]` abgeschaltet → genau diese
  sechs rot. **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu
  entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)):
  `verworfen` ist laut Stufentabelle eine Sache des Menschen – hier belegt widerlegt, darum im Auftrag gesetzt.
- **2026-11-05** — **Berichtigt in der Nachschau** (Lauf X, Retro Sprint 11; frischer `pugling-reviewer`, `git show 2312aaf`): die
  Widerlegung galt nur für `null` als **Wert eines Members**. `[Required]` greift nicht für **Elemente** einer Liste – gemessen:
  `"items":[null]` (Übersetzung), `"sentences":[null]` (Birkenbihl), `"gaps":[null]` (Lückentext) antworten **500**. Weiter in
  [B-397](B-397-null-element-in-config-liste-ist-500.md). Die Summary des Regressionstests behauptet „explicit null … is malformed
  input" zu weit – B-397 berichtigt sie mit.
