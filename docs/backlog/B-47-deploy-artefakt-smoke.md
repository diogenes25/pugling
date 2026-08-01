---
tags: [typ/story, status/idee, bereich/qualitaet, bereich/tests]
aliases: [Deploy-Artefakt-Smoke]
status: idee
prio: P3
art: Aufräumen
quelle: B-41
unverifiziert: true
---

# B-47 · Startet das veröffentlichte Artefakt überhaupt?

Abgespalten von [B-41](B-41-produktions-startup-smoke.md) (Entscheidung 1): Dessen Testklasse deckt die
Produktions-**Konfiguration** in-process ab. Ungeprüft bleibt das Produktions-**Artefakt** – der Weg, den
`deploy-azure.yml` geht: Frontend bauen → nach `wwwroot` kopieren → `dotnet publish` → starten → liefert
Kestrel die PWA über `MapFallbackToFile("index.html")` aus und antwortet die API daneben? Genau diese Kette
ist 24 Tage lang unbemerkt gescheitert (Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8`), weil kein Tor sie fuhr.
Die aus B-41 zurückgestellte `/health`-Frage ist dabei **gegenstandslos geworden**: Der Endpunkt existiert
längst (`Program.cs:498`, `AddHealthChecks().AddDbContextCheck<PuglingDbContext>()`, anonym, seit `105e2e1`
vom 2026-07-04) – nur ruft ihn kein Test auf, weil der Abdeckungs-Wächter ausschließlich Controller-Actions
zählt und ein `MapHealthChecks` in keinem Inventar steht. Ein Smoke hat damit einen fertigen Angriffspunkt
und muss sich **nicht** an einer fachlichen Route festmachen.

## Eintrittsbedingung (entschieden am 2026-08-01)

Beim Grillen des Testabdeckungs-Pakets ([testabdeckung-plan.md](../testabdeckung-plan.md)) **aus dem Paket
herausgehalten**, mit zwei festen Auflagen statt eines Fragezeichens:

1. **Gebaut wird erst, wenn der `workflow_run`-Block in `deploy-azure.yml` wieder scharf ist**
   (heute auskommentiert, `:27-33`; der Azure-Schlüssel ist als [B-33](B-33-azure-publish-profile.md)
   bewusst verworfen). Bis dahin bewachte der Job einen Weg, den niemand geht.
2. **Dann als CI-Job, nie als Test in `Pugling.sln`.** `dotnet publish` + Vite-Build + Kestrel-Start liegen
   im Minutenbereich; als xUnit-Test liefe das im Stop-Hook bei **jeder** `.cs`-Änderung mit und wäre der
   einzige Vorschlag des Pakets, der das 63-Sekunden-Tor kaputtmacht.

Offen bleibt nur die kleine Frage: eigener Workflow oder Schritt im vorhandenen.

## Verlauf

- **2026-07-31** — angelegt bei der Teilung von B-41 (Grillen der vier Test-Stories).
- **2026-08-01** — Ist-Stand korrigiert: die Annahme „es gibt keinen `/health`-Endpunkt" war **falsch**;
  gefunden beim Schätzen von B-41. Der Endpunkt ist da, nur ungetestet.
- **2026-08-01** — bleibt `idee`, aber nicht mehr unbestimmt: Eintrittsbedingung und Bauform sind entschieden
  (Paket-Grillen). Damit kostet die Story in der nächsten Sichtung keine Aufmerksamkeit mehr – sie wartet auf
  ein Ereignis, nicht auf eine Entscheidung.
