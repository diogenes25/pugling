---
tags: [typ/story, status/idee, bereich/backend, bereich/doku]
aliases: [falscher Absatz im OpenAPI-Text, bündig dann eingerückt]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Sprint 8 (vierter Nachtlauf 2026-10-23)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-147]
---

# B-260 · Der OpenAPI-Normalisierer setzt einen Absatz mitten in einen Satz

Gefunden in der Nachschau zu Sprint 8. `OpenApiProseNormalizer` (`backend/Pugling.Api/OpenApi/OpenApiProseNormalizer.cs`,
die Regel „eingerückte Zeile nach bündiger = neuer Absatz") greift auch, wenn ein mehrzeiliges `<summary>`/`<param>`
seinen Text **auf der Zeile des Tags** beginnt: der Generator liefert dann die erste Zeile bündig und die Folgezeilen
eingerückt – umgekehrt wie bei `<para>`. Selbst nachgemessen am geparsten `docs/openapi/v1.json`:
`UpdateMyAccountDto.pin` = „…so an account can\n\nbe deliberately deactivated…" (Quelle `AuthDtos.cs:42-43`). Laut
Prüfer drei weitere Fälle: `Gender`, `SetExerciseSharingDto.executePublic`, `UsageResponse.otherLearnersCount`
(dort reißt der Umbruch zusätzlich einen Listenpunkt). Kein Fall in `OpenApiProseNormalizerTests` deckt die Form
„bündig, dann eingerückt", und das Dokument-Tor sieht sie nicht (weder Einrückung noch Tag).

Vorschlag für die rote Probe: ein Fall `"a later\n            study plan"` → erwartet weicher Umbruch. Zu entscheiden beim
Ausformulieren: die Regel auf „vorige Zeile ebenfalls eingerückt oder erste Zeile eingerückt" schärfen, oder die
Quellen so umformulieren, dass der Text nicht auf der Tag-Zeile beginnt (dann gehört ein Quelltext-Tor dazu, siehe
Retro-Vorschlag von Sprint 8). **Nicht im laufenden Sprint behoben:** der Fund liegt im Increment von Sprint 8; nach
Limit 2 endet der Lauf an diesem Sprint-Ende.

## Verlauf

- **2026-10-23** — angelegt in der Nachschau zu Sprint 8 (vierter Nachtlauf); ein Fall selbst nachgemessen, drei vom Prüfer.
