---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Birkenbihl decoding null 500]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: true
grund: ""
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
