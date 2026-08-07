---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/qualitaet]
aliases: [Wächter AllowAnonymous braucht EnableRateLimiting]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer-Empfehlung zur Abnahme von
  [B-48](B-48-anonyme-registrierung-produktion.md) (2026-08-06) — dort nicht mitgenommen, weil B-48s Ziel
  ohne den Wächter erfüllt ist
unverifiziert: false
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

## User Story

Als **Entwickler**, der einen neuen anonymen Endpunkt hinzufügt, möchte ich, dass ein Tor mich sofort
warnt, wenn ich das Rate-Limiting vergesse — damit „anonym heißt gedrosselt" nicht von Disziplin abhängt,
sondern mechanisch erzwungen wird.

## Ist-Stand am Code

Nach [B-48](B-48-anonyme-registrierung-produktion.md) tragen alle fünf anonym erreichbaren Actions des
Backends (drei Login-, zwei Registrierungs-Endpunkte) `[EnableRateLimiting("login")]`. Gehalten wird das
aber von fünf richtig gesetzten Attributen — genau die Sorte Regel, gegen die dieses Repo sonst
mechanische Tore stellt (`ConventionGuardTests`, `SchemaGuardTests`). Der sechste anonyme Endpunkt
vergisst sie lautlos, und gemerkt würde es erst an einer öffentlichen Instanz.

## Entscheidungen

1. **Ausnahmeliste vorgesehen, aber leer.** Muster `OwnershipExceptions` in derselben Datei übernommen:
   alle fünf heutigen `[AllowAnonymous]`-Actions sind Schreibzugriffe (drei Logins, zwei
   Registrierungen) und brauchen die Bremse — kein Eintrag heute nötig. Kosten: keine, reine
   Vorsichtsstruktur für morgen. Ein anonymer Lese-Endpunkt existiert heute nicht — sollte einer
   entstehen, trägt er seinen Ausnahme-Eintrag mit Begründung, wie es die übrigen Ausnahmelisten des
   Repos verlangen.

## Schätzung

`groesse: XS`, `wo: backend`, `migration: nein`, `vertragsbruch: nein` (reiner Test-Code). Angriffsplan:
ein neuer reflexiver Test `ConventionGuardTests.Anonyme_Actions_Tragen_EnableRateLimiting`, direkt nach
dem bestehenden Ownership-Filter-Wächter, mit Selbstschutzschwelle (mindestens 5 Treffer erwartet, sonst
bricht der Test mit eigener Meldung ab statt leer grün zu laufen). Testweg: rote Probe durch Entfernen
eines `[EnableRateLimiting]`-Attributs — siehe „Verlauf" für die tatsächliche Umsetzung und die
Messzahlen.

## Akzeptanzkriterien

1. Ein neuer reflexiver Test in `ConventionGuardTests.cs` verlangt für jede Action mit `[AllowAnonymous]`
   (Klassen- oder Methodenebene) ein `[EnableRateLimiting]` (Klassen- oder Methodenebene).
2. Selbstschutz: der Test bricht mit einer eigenen Meldung ab, wenn die Reflexion zu wenige Treffer findet
   (heute exakt 5), statt leer und grün durchzulaufen.
3. Leere Ausnahmeliste mit dokumentierter Begründung (kein Eintrag heute, klare Regel für morgen).

## Verlauf

- **2026-08-07** — ausformuliert, gegrillt, geschätzt (**XS**, `wo: backend`) in einem Zug: der
  Review-Vorschlag war bereits vollständig (Muster, Ort, geschätzte Zeilenzahl); die einzige offene Frage
  (Ausnahmeliste ja/nein) löst sich mit „leer, aber vorgesehen".
- **2026-08-07** — umgesetzt: `ConventionGuardTests.Anonyme_Actions_Tragen_EnableRateLimiting`, Abschnitt
  „(f) anonymous means throttled", direkt nach dem Ownership-Filter-Wächter. **Rote Probe** (testweise
  `[EnableRateLimiting("login")]` von `AuthController.LoginAdult` entfernt): Test schlägt fehl und nennt
  exakt `AuthController.LoginAdult ([AllowAnonymous] without [EnableRateLimiting])`. Attribut
  wiederhergestellt: grün. Volle Suite: **760/760 grün** (gemeinsam mit B-118 im selben Sprint gebaut).
- **2026-08-07** — `pugling-reviewer` gefahren (zusammen mit B-118): **kein Blocker.** Musterkonformität
  zum bestehenden Ownership-Guard bestätigt, Selbstschutzschwelle (`>= 5`) gegen die tatsächlich
  vorhandenen fünf Fundstellen nachgezählt. Ein 🟢-Nice-to-have (Abschnitts-Buchstabe „(f)" ist in der
  Datei nicht eindeutig, vorbestehende Unschärfe der Datei, keine neue) — nicht behoben, da rein
  kosmetisch und nicht Teil dieser Story.
- **2026-08-07** — Rollengang-Ersatz: kein UI-Kandidat (reiner Test-Code). Ersatz: volle Suite plus
  Reviewer plus der gezielte rot→grün-Beleg oben.
- **2026-08-07** — `abgenommen`.
