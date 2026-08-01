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
Mit erledigt würde die aus B-41 zurückgestellte Frage, ob ein `/health`-Endpunkt dazugehört – ohne ihn muss
sich ein Smoke an einer fachlichen Route festmachen.

**Ungeprüft:** ob sich das überhaupt lohnt, solange `deploy-azure.yml` **stillgelegt** ist (`on:`-Block
auskommentiert, der Azure-Schlüssel ist als [B-33](B-33-azure-publish-profile.md) bewusst verworfen) – dann
prüft der Job einen Weg, den niemand geht. Ebenso offen: eigener Workflow oder Schritt im vorhandenen; und
ob `/health` Produktivcode werden soll.

## Verlauf

- **2026-07-31** — angelegt bei der Teilung von B-41 (Grillen der vier Test-Stories).
