---
tags: [typ/story, status/abgenommen, bereich/backend]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-730)"
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-23"
entgangen_bei: [B-730]
wartet_auf: ""
---

# B-842 · Wochenvergleich der Münzen verrutscht in einer Umstellungswoche um eine Stunde

## Ist-Stand am Code

`backend/Pugling.Api/Services/Shared/WalletService.cs:63`: `lastWeekNow = lastWeekStart + (now - weekStart)` rechnet die verstrichene Zeit seit Montag, nicht dieselbe Wanduhrzeit der Vorwoche, wie der Kommentar verspricht. Beispiel: `now` = So 2027-03-28 12:00 MESZ → Vergleichspunkt So 2027-03-21 11:00 MEZ statt 12:00; Münzen zwischen 11 und 12 Uhr fehlen in `lastWeekSoFar` (im Oktober eine Stunde zu viel). Nur Anzeige (`/student/me/points/week`), etwa 21 Stunden zweimal im Jahr. Vorschlag: auf der Wanduhr rechnen (lokal −7 Tage, dann `ConvertTimeToUtc`), Test mit `now` an einem Umstellungssonntag nach 03:00.

## User Story

Als Sohn möchte ich in der Umstellungswoche meine Münzen gegen genau denselben Moment der Vorwoche vergleichen, damit
der Vergleich nicht um eine Stunde verrutscht.

## Die echte Lücke

Der Kommentar verspricht „dieselbe Wanduhrzeit“, die Rechnung liefert „dieselbe verstrichene Zeit seit Montag“. Beides
fällt nur in Wochen ohne Zeitumstellung zusammen.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **Auf der Wanduhr rechnen, in `LearningDay`:** neuer Helfer `SameTimeDaysEarlier(utc, days)` – lokale Zeit minus
   `days` Kalendertage, zurück nach UTC. Er gehört in `LearningDay`, weil dort die Zone lebt (B-730: eine Quelle).
   Kosten: eine Methode mehr am Dienst.
2. **Eine übersprungene Wanduhrzeit** (Frühjahr, 02:00–03:00) rückt eine Stunde vor, **eine doppelte** (Herbst) nimmt
   die Normalzeit – beide sind Randfälle von höchstens einer Stunde im Jahr; Hauptsache, es wirft nicht.

## Akzeptanzkriterien

1. Sonntag 2027-03-28 12:00 MESZ (10:00Z): eine Münze am Vorsonntag 11:30 MEZ (10:30Z) zählt in `lastWeekSoFar`.
   Rot mit der alten Rechnung (Vergleichspunkt 11:00 MEZ = 10:00Z).
2. Sonntag 2027-10-31 12:00 MEZ (11:00Z): eine Münze am Vorsonntag 12:30 MESZ (10:30Z) zählt **nicht**. Rot mit der alten
   Rechnung (Vergleichspunkt 13:00 MESZ = 11:00Z).
3. Sonntag 2027-04-04 02:30 MESZ: der Vergleichspunkt (2027-03-28 02:30, gibt es nicht) wirft nicht. Rot, wenn die
   ungültige Zeit ungeschützt nach UTC geht.

## Schätzung

XS, backend, keine Migration, kein Vertragsbruch. Angriffsplan: Tests in `TagesbegriffTests` (Berlin-Fabrik) → Helfer in
`LearningDay` → `WalletService.WeekCoinsAsync`. Testweg: Integrationstests über `/student/me/points/week`.

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-730 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt im Nachtlauf 2026-11-23-c.
- **2026-11-23** — in-arbeit (Sprint 117). Rote Probe vor dem Fix: `Vorwoche_Im_Fruehjahr_Endet_Zur_Selben_Wanduhrzeit` und
  `Vorwoche_Im_Herbst_Endet_Zur_Selben_Wanduhrzeit` erwartet rot, gemessen rot (2/2; Fruehjahr 0 statt 5, Herbst 5 statt 0);
  `Vorwoche_Mit_Uebersprungener_Wanduhrzeit_Wirft_Nicht` erwartet grün (die alte Rechnung wirft nicht), gemessen grün –
  er richtet sich gegen die neue Rechnung.
  - AK 1 → `TagesbegriffTests.Vorwoche_Im_Fruehjahr_Endet_Zur_Selben_Wanduhrzeit`, M1 (alte Rechnung in `WalletService.cs:63`) → rot.
  - AK 2 → `TagesbegriffTests.Vorwoche_Im_Herbst_Endet_Zur_Selben_Wanduhrzeit`, M1 → rot.
  - AK 3 → `TagesbegriffTests.Vorwoche_Mit_Uebersprungener_Wanduhrzeit_Wirft_Nicht`, M2 (Schutz in `LearningDay.cs:66` aus) → rot.
  Review (`pugling-reviewer`): 🟢 Kopf von `WeekCoinsAsync` noch alt → behoben (ohne Zählung); 🟢 `AddHours(1)` nimmt eine Umstellung um eine Stunde an → im Kommentar benannt (ohne Zählung).
- **2026-11-23** — abgenommen (`e3f51ee7`, Sprint 117). Verifikation: Backend 2126/2126 (Release, nach der letzten
  Änderung), Mutanten M1–M13 alle rot, Grundlinie danach grün; `dotnet format --verify-no-changes` meldet nur vier Dateien
  außerhalb des Diffs (Altbestand). Rollengang: Sohn (Wochenvergleich) bzw. Vater (Titel) maschinell über die
  Integrationstests der Berlin-Fabrik – der Fall braucht eine eingefrorene Uhr in einer Umstellungswoche, die ein laufender
  Server nicht hat; die anderen Rollen Regressionszeugen (volle Suite grün, keine Oberfläche im Diff).
- **2026-11-23** — nachgeschaut (Retro Sprint 118, Selbst-Check). Prüfpunkte: `SameTimeDaysEarlier` hat genau einen Aufrufer (`WalletService.cs:63`); die Grenzen `lastWeekStart ≤ lastWeekNow < weekStart` nachgerechnet für Frühjahr (21.03. 11:00Z) und Herbst (24.10. 10:00Z). Kein Befund.
