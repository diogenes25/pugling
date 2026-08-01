---
tags: [typ/story, status/idee, bereich/qualitaet, bereich/api]
aliases: [ProblemDetails required extensions]
status: idee
prio: P3
art: Defekt
quelle: docs/backlog/B-42-openapi-typen-generieren.md
unverifiziert: true
---

# B-56 · `ProblemDetails` fordert im Schema ein Feld, das es nicht beschreibt

Im OpenAPI-Dokument trägt das Schema `ProblemDetails` genau **ein** Pflichtfeld – `extensions` –, und dieses
Feld steht **nicht** unter `properties` (dort liegen `type`, `title`, `status`, `detail`, `instance`, `code`).
Nachgesehen am 2026-08-01 in `docs/openapi/v1.json`. Ein Generator, der das ernst nimmt, erzeugt daraus einen
Typ mit einem verpflichtenden Feld, das die API nie sendet – und `ProblemDetails` ist der Fehlertyp
**jedes** Endpunkts.

Vermutete Ursache: `Program.cs` rechnet `schema.Required` aus der Nullability der `JsonTypeInfo` neu; bei
`ProblemDetails` (kein Record, sondern die ASP.NET-Klasse mit `IDictionary<string, object?> Extensions`)
greift diese Rechnung offenbar anders als bei den eigenen Vertrags-Records.

Der Befund ist **vorbestehend** und fiel erst auf, als das Dokument mit
[B-42](B-42-openapi-typen-generieren.md) eingecheckt wurde – ein Nebengewinn des Tors: Merkwürdigkeiten im
Vertrag sind jetzt sichtbar, statt nur ausgeliefert zu werden.

**Zu prüfen beim Ausformulieren:** Betrifft es nur `ProblemDetails` oder alle Nicht-Record-Typen? Ist
`extensions` überhaupt gewollt im Vertrag (das `code`-Feld daneben ist es ausdrücklich)? Und beißt es
wirklich – oder ignoriert `openapi-typescript` ein `required` ohne zugehörige Property? Letzteres entscheidet,
ob das vor oder nach [B-42](B-42-openapi-typen-generieren.md) Schritt 2 gehört.
