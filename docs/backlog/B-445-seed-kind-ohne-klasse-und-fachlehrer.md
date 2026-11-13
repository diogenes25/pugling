---
tags: [typ/story, status/idee, bereich/seed, rolle/supervisor]
aliases: [Seed-Kind ohne Klasse und Fachlehrer]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-445 · Seed: „Sohn" hat weder Klasse noch Schulart, und Herr Schmidt trägt kein Fachlehrer-Profil

Teil 2 von B-429. Im Kind-Hub steht für das Seed-Kind „Kein Profil passt zu Sohn – prüfe Klasse und Schulart"
(`Seed.cs` `SeedAdmin`: `Child` ohne `Grade`/`SchoolType`), und der Lehrer-Seed legt kein `CreatorProfile` an.

**Warum nicht im selben Sprint wie das Buch:** Klasse und Schulart des Kindes filtern den Katalog serverseitig. Die E2E
`frontend/e2e/assistent.spec.ts` (Kommentar über `waehleSohnUndEnglisch`) braucht genau deshalb ein Kind **ohne** Klasse:
„Begrüßungen" ist Klasse 5–6, „Vocabulary: The environment" 8–10 – mit gesetzter Klasse verschwände eine der beiden Zeilen.
Dazu laufen viele Integrationstests auf Kind 1. Das ist kein Seed-Aufräumen mehr, sondern ein Umbau der Testannahmen,
der eigene Entscheidungen braucht (Klasse 5 am Seed-Kind und die Specs auf ein eigenes Kind umziehen? Oder ein zweites
Seed-Kind mit Klasse für den Fachlehrer-Treffer?).

## Befund am Code (2026-11-13): was die echte DB davon träfe

`Seed.RunAsync` läuft bei **jedem** Start, auch gegen die echte `pugling.db` der Söhne (`Seed.cs:28-31`).
`SeedAdmin` kehrt bei vorhandenen Erwachsenen sofort zurück (`Seed.cs:1088`) – Klasse/Schulart am Seed-Kind erreichten die
echte DB also **nie**, nur frische DBs (Tests, E2E). `SeedTeacherLibrary` (`Seed.cs:516`) hat dagegen keine solche Sperre
am Anfang, sondern prüft je Schritt – ein dort ergänztes `CreatorProfile` für Herrn Schmidt landete beim nächsten Start
**auch in der echten DB**. Für die Entscheidung heißt das: Teil „Seed-Kind" ist ein reiner Test-/Demo-Umbau, Teil
„Fachlehrer-Profil" ändert die Daten der Familie und braucht dieselbe Idempotenz-Prüfung wie die übrigen Schritte dort.

## Verlauf

- **2026-11-09** — abgeteilt aus [B-429](B-429-seed-familie-widerspricht-dem-katalog.md) beim Grillen in Lauf AG (Sprint 27). Abgeteilt, weil die Klasse am Seed-Kind den Katalog in mehreren Specs filtert (Begründung oben).
- **2026-11-13** — Befund (Tageslauf, Restzeit): welcher der zwei Teile die echte DB erreicht – nur das Fachlehrer-Profil.
