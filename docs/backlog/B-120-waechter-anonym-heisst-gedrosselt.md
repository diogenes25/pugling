---
tags: [typ/story, status/idee, bereich/backend, bereich/qualitaet]
aliases: [Wächter AllowAnonymous braucht EnableRateLimiting]
status: idee
prio: P3
art: Aufräumen
quelle: pugling-reviewer-Empfehlung zur Abnahme von
  [B-48](B-48-anonyme-registrierung-produktion.md) (2026-08-06) — dort nicht mitgenommen, weil B-48s Ziel
  ohne den Wächter erfüllt ist
unverifiziert: true
---

# B-120 · „Anonym heißt gedrosselt" hängt an Disziplin, nicht an einem Tor

Nach [B-48](B-48-anonyme-registrierung-produktion.md) tragen **alle fünf** anonym erreichbaren Actions des
Backends (drei Login-, zwei Registrierungs-Endpunkte) `[EnableRateLimiting("login")]`. Gehalten wird das
aber von fünf richtig gesetzten Attributen — genau die Sorte Regel, gegen die dieses Repo sonst
mechanische Tore stellt (`ConventionGuardTests`, `SchemaGuardTests`). Der sechste anonyme Endpunkt vergisst
sie lautlos, und gemerkt würde es erst an einer öffentlichen Instanz.

Vorschlag aus dem Review: ein Wächter neben
`ConventionGuardTests.Actions_Unter_ChildId_Oder_PlanId_Tragen_Den_Ownership_Filter`
(`backend/Pugling.Api.Tests/ConventionGuardTests.cs:163`), der reflexiv über alle Actions läuft und für
jede mit `AllowAnonymousAttribute` ein `EnableRateLimitingAttribute` verlangt — geschätzt zehn Zeilen.
Das macht aus B-48s Ergebnis eine Regel: der nächste anonyme Endpunkt stellt das Tor erst rot, und das
ist der Zweck.

Beim Ausformulieren zu klären, ob es eine Ausnahmeliste braucht (ein anonymer **Lese**-Endpunkt wäre
denkbar und nicht zwingend drosselungsbedürftig) — und falls ja, ob sie wie die übrigen Ausnahmelisten des
Repos eine Begründung je Eintrag verlangt.
