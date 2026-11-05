---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/sicherheit, rolle/creator]
aliases: [profiles/match 404 gegen 403, Kind-Id-Abfrage per Match]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: ja
quelle: Review zu B-308/B-309 in Lauf F, Teil 5 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-310 · `GET creator/profiles/match` verrät per 404 gegen 403, ob es eine Kind-Id gibt

`CreatorProfilesController.Match` antwortet **404**, wenn das Kind nicht existiert, und **403**, wenn es existiert,
der Aufrufer es aber nicht betreut. Jeder Creator kann so durchprobieren, welche Kind-Ids es gibt – der Kommentar
am Endpunkt nennt genau diese Sorte Seitenkanal als Grund für die Betreuungsprüfung. Der Prüfer verweist auf eine
Repo-Regel „gibt es nicht“ und „nicht meins“ antworten beide 404; ob und wo sie steht, ist **nicht geprüft**
(`unverifiziert`) – das ist der erste Schritt, zusammen mit der Frage, ob andere Kind-Endpunkte es genauso halten.

**Behebung (Vorschlag, wenn die Regel trägt):** beide Fälle 404; der Fang im Agenten (`ShowProfilesAsync`, B-308)
bräuchte dann nur noch `NotFound`. Achtung: Statuscode-Änderung am Vertrag – `vertragsbruch` vor dem Schätzen klären.

## User Story

Als **Familie** möchte ich, dass ein fremder Creator über das Profil-Matching nicht herausfinden kann, welche Kind-Ids es
gibt.

## Ist-Stand am Code (vor dem Bau, Stand `2361974`)

- `backend/Pugling.Api/Controllers/Creator/CreatorProfilesController.cs:75-77`: erst `db.Children.AnyAsync` → 404, dann
  `SupervisorOwnsChildAsync` → 403 `forbidden`.
- Die Repo-Regel trägt: `ChildOwnershipFilter.cs:21` antwortet bei fehlender Betreuung 404 „Child not found." – für jedes
  Kind, ob es existiert oder nicht. Der Match-Endpunkt war die Ausnahme, weil er unter der Creator-Route liegt und den
  Filter nicht nutzt.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Eine Prüfung, eine Antwort:** `SupervisorOwnsChildAsync` allein, sonst `404 not_found` „Child not found.". Eine
   Betreuung existiert nur für ein existierendes Kind, die Existenzabfrage entfällt.
2. **Vertragsbruch angenommen:** 403 fällt aus dem Dokument. Vor der Publikation ändert v1 frei (`backend/CLAUDE.md`);
   der einzige Aufrufer außerhalb des Servers, der Agent, fängt jetzt nur noch 404 (`AgentCommands.ShowProfilesAsync`).
   Das Frontend ruft den Endpunkt nur für betreute Kinder.

## Akzeptanzkriterien

1. Ein fremdes und ein unbekanntes Kind bekommen denselben Status, denselben `code` und dasselbe `detail`.
2. Der betreuende Creator bekommt weiter seine Treffer.

## Schätzung

`XS`, `wo: backend`, `migration: nein`, `vertragsbruch: ja` (Statuscode). Testweg: `CreatorProfileTests`, `PuglingClientTests`.

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-308/B-309 (Lauf F, Teil 5, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  🟡 außerhalb des Diffs; eigene Story, weil das Sprint-Ziel ohne sie erfüllt ist.
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Sprint 2 der Sitzung 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `CreatorProfileTests.cs:357` (Status, `code` und `detail` beider Fälle gleich), AK 2 → `PuglingClientTests` (Treffer des betreuenden Kontos). Rote Probe: `ApiErrors.NotFound` → `ApiErrors.Forbidden` am Endpunkt → erwartet 2 failed, gemessen 2 failed von 43 (genau diese beiden Fälle). `docs/openapi/v1.json` vom Testlauf neu geschrieben (403 fällt weg).
- **2026-11-05** — Review `pugling-reviewer`: keine 🔴, vier 🟡 – zwei mit Test-Änderung behoben (Zähler Sprint 2: 2 von 5: Controller-Reihenfolge der Attrappe ungetestet, Altklartext-Pfad ohne Zeitprobe), zwei nur Text (veralteter Kommentar `CreatorAgentTests`, „403" in `docs/REST/Creator.http`) ohne Zählung. Volle Backend-Suite 1076/1076 vor den Review-Fixes, danach die betroffenen Klassen grün (Zahlen im Protokoll). **abgenommen.** Rollengang: der Creator-Weg „Fachlehrer zum Kind finden" ist im `PuglingClientTests`-Durchstich gegen den echten Server gefahren (betreutes Kind: Treffer; fremdes: 404), der Agent-Weg in `CreatorAgentTests.Profile_fuer_*`. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `b32460e`.
