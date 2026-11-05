---
tags: [typ/story, status/abgenommen, bereich/auth, rolle/supervisor]
aliases: [Name vor PIN ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#retrospektive
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-372]
---

# B-379 · Die Vorrangfolge „leerer Name vor zu kurzer PIN" an `auth/me` ist ungetestet

B-372 behauptet in Entscheidung 2: „ein leerer Name mit zu kurzer PIN [antwortet] weiter mit dem Namen". Beide Fälle
antworten `400 validation_error` und unterscheiden sich nur im `detail`. Der Mutant „PIN-Prüfung vor der Namensprüfung"
(`AuthController.cs:165` hinter `:170`) läuft durch jeden Test – gefunden in der Nachschau zu `46e3871` (frischer
`pugling-reviewer`, 🟡). Reparatur: ein Fall in `AccountSelfServiceTests` mit `{ name = "  ", pin = "12" }`, der
`detail == "Name must not be empty."` zusichert.

## User Story

Als **Entwickler** möchte ich, dass jede Vorrangfolge, die eine Entscheidung behauptet, einen Test hat.

## Ist-Stand am Code (Stand `2361974`)

- `backend/Pugling.Api/Controllers/AuthController.cs:165-170`: Name → E-Mail → PIN. `AccountSelfServiceTests` trug nur
  „E-Mail vor PIN".

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. Ein Fall mit leerem Namen und zu kurzer PIN; zugesichert wird das `detail`, weil Status und `code` beider Zweige gleich sind.

## Akzeptanzkriterien

1. Der Mutant „Namensprüfung hinter die PIN-Prüfung" (E-Mail bleibt vorn) fällt **nur** durch den neuen Fall.

## Schätzung

`XS`, `wo: backend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf S, Teil 3 (Sprint 1 der Sitzung 2026-11-05).
- **2026-11-05** — gebaut in Sprint 2 der Sitzung 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `AccountSelfServiceTests.cs:131`. Rote Probe: Namensprüfung hinter die PIN-Prüfung (E-Mail bleibt vorn) → erwartet 1 failed, gemessen 1 failed von 10 – nur der neue Fall; der breitere Mutant „PIN ganz vorn" fällt durch ihn und den B-372-Fall (2 von 18).
- **2026-11-05** — **abgenommen.** Review `pugling-reviewer`: keine 🔴, vier 🟡 – zwei mit Test-Änderung behoben (Zähler Sprint 2: 2 von 5: Controller-Reihenfolge der Attrappe ungetestet, Altklartext-Pfad ohne Zeitprobe), zwei nur Text (veralteter Kommentar `CreatorAgentTests`, „403" in `docs/REST/Creator.http`) ohne Zählung. Volle Backend-Suite 1076/1076 vor den Review-Fixes, danach die betroffenen Klassen grün (Zahlen im Protokoll). Rollengang: kein eigener Weg (reine Testlücke, kein Verhalten geändert). Commit: siehe nächster Eintrag.
