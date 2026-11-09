---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Datumsformat, toLocaleDateString ohne Locale]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-427 · Datumsformat: zwölf Stellen formatieren ohne Locale, daneben stehen ISO-Strings

Im Rollengang vom 2026-11-08 (Browser-Locale en-US) stand im Kind-Hub „Betreuer seit **11/8/2026**" und im
Konto „**11/8/2026**", während die Plan-Seite „**2026-11-08 – 2026-11-21**" und der Tagesverlauf ISO-Daten
zeigen. Drei Schreibweisen für dasselbe Datum in einer deutschen Oberfläche; welche erscheint, hängt vom
Browser ab.

## User Story

Als *Vater* möchte ich jedes Datum in **einer** deutschen Schreibweise sehen („08.11.2026"), unabhängig von der
Browser-Einstellung, damit ich Termine nicht umdenken muss.

## Ist-Stand am Code

- Zwölf Aufrufe `toLocaleDateString()` ohne Locale: `ChildCarePanels.tsx:68`, `VaterKonto.tsx:71`,
  `VaterLernstand.tsx:183/224/359`, `VaterProfil.tsx:78`, `VaterShop.tsx:408/454`,
  `VaterAnmerkungen.tsx:202/224/295`, `RemarkWidget.tsx:317`.
- ISO-Rohstrings in der Anzeige: `VaterPlanDetail.tsx:63` (`{p.startDate} – {p.endDate}`),
  `VaterPlaene.tsx:83`, der Tagesverlauf auf der Plan-Seite; der Assistent-Überblick zeigt „2026-11-08 · 14
  Tage" (Rollengang).
- Keine gemeinsame Formatfunktion (kein Treffer für `formatDate`/`datum(` in `frontend/src/lib`).
- `DateOnly`-Werte kommen vom Server als `YYYY-MM-DD` (UTC-Tagesregel, `CLAUDE.md` „Zeit/UTC"); Zeitstempel
  als ISO-8601 mit Uhrzeit.

## Die echte Lücke

Kein gemeinsamer Formatierer, darum zwölf implizite Entscheidungen und eine Handvoll gar nicht formatierter
Werte. Aufräumen: kein Verhalten ändert sich außer der Schreibweise.

## Offene Punkte

Keine Produktfrage; Aufräumen, autonom grillbar. Empfehlung: `lib/datum.ts` mit `formatDate(iso)` →
`Intl.DateTimeFormat("de-DE", { day: "2-digit", month: "2-digit", year: "numeric" })` und
`formatDateRange(a, b)`; `DateOnly`-Strings ohne `new Date()`-Umweg parsen (sonst springt der Tag in
westlichen Zeitzonen). Rohes ISO bleibt nur in `<input type="date">`-Werten. Die Arcade (`t()`) gleich
mitnehmen, falls dort Daten stehen (prüfen: `grep -n "toLocaleDateString" frontend/src/sohn`).

## Akzeptanzkriterien (Entwurf)

1. Kein `toLocaleDateString(`/`toLocaleString(` ohne Locale mehr in `frontend/src` (Tor: Vitest-Fall, der die
   Quellen greppt, wie `lesefehler.test.ts`).
2. Plan-Zeitraum und Tagesverlauf zeigen „08.11.2026 – 21.11.2026".
3. Ein `DateOnly` „2026-11-08" wird in jeder Zeitzone als 08.11.2026 gezeigt (Test mit `TZ`-Variation oder
   reiner String-Parse).
4. `old-phrases.py` findet keine Spec mehr, die auf das alte Format zusichert.

## Entscheidungen

1. **`lib/datum.ts` mit `formatDate`, `formatDateTime`, `formatDateRange`**, Locale fest `de-DE`; ein `DateOnly` wird als Text zerlegt,
   nie über `new Date()`. *Entscheidung im Lauf (Aufräumen), getroffen von Claude.*
2. **Aufrufe mit Uhrzeit bleiben mit Uhrzeit** (`toLocaleString()` → `formatDateTime`), damit Anmerkungen und Item-Historie ihre
   Zeit behalten – nur die Schreibweise ändert sich.
3. **Die Arcade bleibt außen vor**: sie formatiert schon mit der gewählten Sprache (`SohnShop`, `toLocaleDateString(i18n.language)`)
   – das Tor erlaubt jede Locale, verbietet nur den Aufruf **ohne**.
4. **Tor als Vitest-Quellsuche** wie `lesefehler.test.ts`, mit Selbsttest je Form.

## Schätzung

**Größe: S** — ein Modul, zwölf Aufrufe, vier ISO-Anzeigen, ein Tor. Testweg: `datum.test.ts`, E2E `konto-waehrung-und-datum.spec.ts`.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Vater ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Aufräumen, Freigabe 1).
- **2026-11-09** — gegrillt (Aufräumen, Freigabe 1), geschätzt und gebaut in Lauf AG, Sprint 27. **Rote Probe mit Zahl:** das Tor gegen
  den Ausgangsstand → 12 Funde (erwartet 12, wie im Ist-Stand gezählt), danach 0. Umgebaut: die zwölf Aufrufe plus Plan-Zeitraum in
  `VaterPlaene`/`VaterPlanDetail`, Tagesverlauf (`VaterPlanDetail`) und Assistent-Überblick. `old-phrases.py`: 0 Fundstellen (AK 4).
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Edit an `453a740 + Arbeitsbaum`, 2026-11-09 15:44:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 1 Tor | `datum.test.ts` Selbsttest „Uhrzeit ohne Locale" | MF2 Muster ohne Leerraum (`datum.test.ts:42`) | red, am Fall |
  | AK 3 Zeitzone | `datum.test.ts` „zeigt einen DateOnly …" unter `TZ=America/New_York` | MF3 Textzerlegung aus (`datum.ts:25`) | red, am Fall |
  | AK 2 Konto-Datum | `e2e/konto-waehrung-und-datum.spec.ts` (Locale en-US) | ME3 wieder `toLocaleDateString()` (`VaterKonto.tsx:72`) | red, am Fall |
  | AK 2 Plan-Zeitraum | dieselbe | ME4 Zeitraum wieder ISO (`VaterPlanDetail.tsx:64`) | red, am Fall |

  Grundlinie nach dem Zurückspielen grün. Die übrigen elf Umbau-Stellen hält das Tor (ein Rückfall auf `toLocale…()` ist rot), nicht
  je eine E2E.
- **2026-11-09** — Review `frontend-reviewer`: 🟡 drei rohe ISO-Daten übrig (Klassenarbeits-Termin `VaterClassTests.tsx`, Ziel-Frist
  `VaterZiele.tsx`, „Fällig" im Positions-Report `PlanPositions.tsx`) → `formatDate` (**Zähler 1**); 🟡 der Server liefert gelesene
  Zeitstempel ohne Offset (EF/SQLite, kein UTC-Konverter – **hergeleitet**, nicht am Endpunkt gemessen; Indiz
  `RemarkExportService.cs:157`), `new Date()` läse sie als Ortszeit → `augenblick()` ergänzt ein `Z` (**Zähler 2**); 🟡 Tagesverlauf,
  Planliste und Assistent-Überblick ohne Test → E2E-Zusicherungen (**Zähler 3**). 🟢 ohne Zählung: Tor erkennt jetzt auch
  `toLocaleDateString(undefined, …)`, `([])` und `Intl.DateTimeFormat()` ohne Locale; Meldung nennt für die Arcade `i18n.language`;
  Tag-Grenze Gerät vs. Server als Entscheidung in `datum.ts`; die Spec in drei Fälle je Fläche geteilt. 🟢 Rohe ISO-Daten in der Arcade
  → [B-447](B-447-arcade-zeigt-iso-daten.md). nachgemessen nach dem letzten Edit an `453a740 + Arbeitsbaum`, 2026-11-09 16:14 (`mutant-probe.py`, alle Zeilen der Tabelle oben red, dazu:)

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | Review: Zeitstempel ohne Offset | `datum.test.ts` „liest einen Zeitstempel ohne Offset als UTC" unter `TZ=Europe/Berlin` | MF5 kein `Z` (`datum.ts:28`) | red, am Fall |
  | AK 1 Tor, neue Formen | Selbsttest „Locale ausdrücklich offen" | MF2 altes Muster (`datum.test.ts:50`) | red, am Fall |
  | AK 2 Tagesverlauf | `e2e/konto-waehrung-und-datum.spec.ts` Fall 2 | ME5 (`VaterPlanDetail.tsx:95`) | red, am Fall |
  | AK 2 Planliste | dieselbe | ME6 (`VaterPlaene.tsx:84`) | red, am Fall |
  | Review: Klassenarbeits-Termin | dieselbe, Fall 3 | ME8 (`VaterClassTests.tsx:126`) | red, am Fall |
  | Review: Ziel-Frist | dieselbe, Fall 3 | ME9 (`VaterZiele.tsx:281`) | red, am Fall |
  | Assistent-Überblick | `e2e/assistent.spec.ts` | ME7 (`VaterWizard.tsx:645`) | red, am Fall |

  **Ungemessen:** „Fällig" im Positions-Report (`PlanPositions.tsx`) – der Fall braucht Lernfortschritt einer Position; keine Zusicherung
  hält ihn, nur die Umstellung selbst.
- **2026-11-09** — abgenommen. Backend 1183/1183, Vitest 715/715, `tsc -b` sauber, Playwright **125 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet). **Rollengang** Vater = `e2e/konto-waehrung-und-datum.spec.ts` Fälle 1–3 (Konto-Datum, Plan-Zeitraum, Tagesverlauf, Planliste, Klassenarbeits-Termin, Ziel-Frist, Browser-Locale en-US) und `e2e/assistent.spec.ts` (Überblick). Sohn und Creator als Regressionszeugen: Suite grün; die Arcade formatiert weiter mit ihrer Sprache. Commit: siehe `git log -- docs/backlog/B-427-datumsformat-uneinheitlich.md` (Lauf AG, Sprint 27).
