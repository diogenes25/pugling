---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Nachtlauf 2026-11-17 mit dem Lernkarten-Sprint

**Datum:** 2026-11-17  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Den Lernkarten-Sprint (Sprint 92, [B-643](backlog/B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md))
bauen – mit dem Fundament, auf dem er steht.

## Auftrag und Ausgangswert

„Mache einen Nachtlauf, der den Sprint für die Lernkarten enthält." Unbeaufsichtigter Lauf nach `pm-loop`,
[docs/nachtlauf.md](nachtlauf.md) (Freigaben 1–10) und [docs/backlog/README.md](backlog/README.md). Arbeit direkt auf
`main` (Stand `54dbf16a`); der Heimnetz-Server auf `:5200` wird nicht angefasst, E2E laufen auf Ausweichports gegen
Wegwerf-DBs.

**Ausgangswert:** 57 offen (Index vom 2026-11-17).

## Auswahl

Der Lernkarten-Sprint ist Sprint 92 aus der [Sprintplanung vom selben Tag](pm-sitzung-2026-11-17-sprintplanung.md).
B-643 trägt `wartet_auf: B-635` und Entscheidung 9 („B-635 zuerst; B-643 setzt auf dessen Stufe je Karte und `easier`
auf"). Der Lauf fährt darum **Sprint 85** (B-633 → B-634 → B-635, die Reihenfolge seines Entwickler-Briefs) und danach
**Sprint 92**. Alle vier Stories stehen auf `geschaetzt` mit `## Entscheidungen` – baubar nach Freigabe 8.

Sprint 85 wird mit dem Ziel und Brief aus der Sprintplanung gefahren; sie werden hier nicht wiederholt.

## Iteration 85 — umgesetzt

- **Backend (API-First):** B-633 `Gap.Prompt` → Hinweis und `passage` über die Senke `ClozeGaps.WithPrompt`; B-634
  Buchstabensalat je Wort in `StageMechanics` (Wortgrenzen als `answerPattern`); B-635 Migration `Remediation`,
  `IExerciseType.EaseLadder`, `POST …/easier`, Runde aus offenen Aufgaben, „erledigt“ nur frei getippt, Fehlercode
  `cannot_ease`.
- **Frontend:** Feld „Deutsche Vorgabe“ im Übungseditor, `LetterTiles` je Wort, Schalter „Nacharbeit“ im
  Positionsformular (aus der Manifest-Fähigkeit), Arcade-Knopf „Mach's mir leichter“, Runden-Pille, Blatt-Hinweis.
- **Verifikation:** volle Backend-Suite 1813/1816 grün, die drei roten danach behoben und gezielt grün (27/27);
  Vitest 1159/1166 → nach Nachzug grün in den betroffenen Dateien; E2E `nacharbeit.spec.ts` + `buchstaben-ordnen.spec.ts`
  **2 passed** auf Ausweichports 5211/5181.
- **Mutanten-Probe:** erster Lauf brach beim Zurückschreiben von M8 ab (Windows-Schreibsperre, `Errno 22`); der Mutant
  stand danach im Code und wurde von Hand zurückgesetzt und geprüft (alle Mutantenzeilen wieder im Original). Zweiter
  Lauf über einen Wrapper mit Wiederholung von `open()`.

**Mutanten-Tabelle (zweiter Lauf, nach der letzten Bearbeitung):**

Gemessen an `54dbf16a + Arbeitsbaum geändert`, 2026-11-17 17:48.

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| M1 | backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:611 | red | red |  |
| M2 | backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:193 | red | red |  |
| M3 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:459 | red | red |  |
| M4 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:483 | red | red |  |
| M5 | backend/Pugling.Api/Services/Shared/StageMechanics.cs:109 | red | red |  |
| M6 | backend/Pugling.Api/Services/Shared/StageMechanics.cs:146 | red | red |  |
| M7 | backend/Pugling.Api/Services/Shared/StageMechanics.cs:154 | red | red |  |
| M8 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:719 | red | red |  |
| M9 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:603 | red | red |  |
| M10 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:174 | red | abweichend | fällt am falschen Ort, fehlt: Leichter_geschaltet_erledigt_nie_frei_getippt_schon |
| M11 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:114 | red | ungemessen | kompiliert nicht |
| M12 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:288 | red | red |  |
| M13 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:306 | red | ungemessen | kompiliert nicht |
| M14 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:460 | red | red |  |
| M15 | backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:132 | red | red |  |
| M16 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:717 | red | red |  |
| F1 | frontend/src/components/LetterTiles.tsx:144 | red | red |  |
| F2 | frontend/src/vater/exerciseConfig.tsx:263 | red | red |  |

Grundlinie nach dem Zurückspielen: green.
3 Zeile(n) ohne Beleg: M10, M11, M13

M10 ist ohne eigenen Beleg, weil `Review` die Leichter-Stufe zusätzlich selbst liest (Doppelsicherung); M11/M13 brauchen eine kompilierbare Form (`if (false)` ist unter `TreatWarningsAsErrors` ein Fehler) – offen.

## Review Sprint 85 und Halt des Laufs

Der `frontend-reviewer` meldete **1 🔴 und 5 🟡**, deren Behebung jeweils Code oder Tests ändert:

1. 🔴 Fokus landet nach „Weiter“ auf „Mach's mir leichter“ statt im Antwortfeld; ein gehaltenes Enter schaltet die
   ungelesene Karte leichter (keine `zuFrueh`-Sperre) – die Aufgabe ist für diese Runde verloren.
2. 🟡 Ein gescheitertes `easier` bleibt stumm (`action.message` wird in der Arcade nie gezeigt); `cannot_ease` gehört
   in `PROBLEM_KEYS` der Arcade samt `de`/`en`.
3. 🟡 Nach dem Leichterschalten geht der Fokus verloren bzw. bleibt auf dem Knopf.
4. 🟡 „Nacharbeit“ bleibt beim Wechsel auf einen Typ ohne Leiter angehakt → `400 validation_error`.
5. 🟡 `LetterTiles`: die Wort-Bevorzugung der Tastatur ist toter Code; der Vitest-Kommentar verspricht ein Rot, das es
   nicht gibt (leere Zusicherung).
6. 🟡 Wortgrenzen sind für Screenreader nicht hörbar (keine Gruppen-Semantik, Ansage ohne Trenner).

Der `pugling-reviewer` (nur lesend, kein Build – die Probe lief) meldete dazu **2 🟡** mit Code-/Test-Änderung:

7. 🟡 Anti-Cheat am Rundenende vorbei: eine Karte bis zur Auswahl leichter schalten, sofort eine neue Runde starten und
   die gesehene Lösung dort frei tippen – `RemediationDoneAt` wird gesetzt (Stufe je Sitzung, `Review` nimmt über den
   Batch-Weg auch eine Karte außerhalb des Cursors). Vorschlag: offene Nacharbeits-Runde fortsetzen statt neu starten,
   und in der Nacharbeit nur die Karte am Cursor annehmen.
8. 🟡 `EasierStage` vergleicht nur mit der aktuellen Form: bei einer Lücke ohne Ablenker fällt die Auswahl auf Kästchen
   zurück, und vom Salat aus ginge es wieder auf Kästchen – schwerer, gegen Entscheidung 3. Fix: alle schon gezeigten
   Formen vergleichen, Testfall „eine Lücke, kein Ablenker“.

🟢 ohne Zählung: Rundenzähler zählt auch Vorschau- und Vor-Nacharbeits-Sitzungen; `UnlimitedByDesign` nur für einen
der beiden Dict-Spalten; Testlücken (Vater-Antwort erledigt nichts, Batch-Weg, Anlegen mit Vokabeltyp); Wortkasten
listet bei Satz-Vorgaben ganze Sätze.

Damit steht der **Fünfer-Zähler von Sprint 85 bei 8** (Freigabe 3 mit Präzisierung 10: nur 🟡/🔴, nur mit Code-/
Test-Änderung). Nach Freigabe 3 **endet der gesamte Lauf**; Sprint 92 (B-643, der Lernkarten-Sprint) wird nicht
gebaut. Die Funde sind nicht behoben, die Stories bleiben auf `in-arbeit`. Nichts ist committet.

**Stand im Arbeitsbaum** (für den Morgen):

- Sprint 85 vollständig gebaut, Funde 1–6 offen.
- **B-643 teilweise vorbereitet – nur Frontend:** `Lernkarte.tsx` (Leichter in Schritt 4, „Später nochmal“), Ring auf
  `imprintedCount` (`pfad.ts`, `SohnHome.tsx`, Beschriftung „eingeprägt“), `vorDemSchalten` in `SohnPractice.tsx`,
  Hinweis im grünen Blatt, E2E-Helfer-Beschriftung und angepasste Tests. Das Backend von B-643 (`ImprintedAt`,
  Migration, Kartenwahl, Dosis, Wiederholung ohne Grenze, `ImprintedCount`) ist **nicht** gebaut; `tsc` ist deshalb an
  `imprintedCount` rot, bis es steht. Wer Sprint 85 allein committen will, muss diese Teile herausnehmen.

## Fortsetzung nach Rückfrage — Funde behoben

Der Vater sagte am selben Abend „Ja, weitermachen“ (Antwort auf die Frage, ob die 8 Funde behoben und danach der
Lernkarten-Sprint gebaut werden soll). Der Lauf geht damit **im Dialog** weiter, nicht mehr als unbeaufsichtigter
Nachtlauf; der Zähler ist durch diese Freigabe aufgehoben, nicht umgangen.

| Fund | Behebung | Beleg |
| --- | --- | --- |
| 1 🔴 Fokus auf „leichter“ | Knopf hinter die Antwortfläche, `zuFrueh`-Sperre | E2E `nacharbeit.spec.ts`: Feld nach „Weiter“ fokussiert, Mutant F4 |
| 2 🟡 stummer Fehler | `try/catch` → Ansage; `cannot_ease` in `PROBLEM_KEYS` + `de`/`en` | Vitest i18n-Parität |
| 3 🟡 Fokus nach dem Schalten | Effekt setzt den Fokus in die neue Form | E2E: erstes Kästchen fokussiert, Mutant F3 |
| 4 🟡 Haken bei Typwechsel | `remediation` fällt mit dem Typ | Vitest „ein Wechsel auf einen Typ ohne Leiter nimmt die Nacharbeit zurück“; Probe von Hand: mit Fix 12/12, ohne Reset 1 failed (dieser Fall) |
| 5 🟡 toter Zweig, leere Zusicherung | Zweig entfernt, Testkommentar richtiggestellt | Vitest `LetterTiles` |
| 6 🟡 Wortgrenzen für Screenreader | Wort-Gruppen mit Namen, Ansage mit „ / “ | Vitest `LetterTiles`, E2E `buchstaben-ordnen` |
| 7 🟡 Neustart umgeht die Hilfe | offene Nacharbeits-Runde des Tages wird fortgesetzt; `Review` nur am Cursor | `Neustart_setzt_die_offene_Runde_fort_und_die_Hilfe_bleibt`, Mutanten R4/R5 |
| 8 🟡 zurück auf schwerere Form | Vergleich mit allen schon gezeigten Formen | `Leichter_fuehrt_nie_zurueck_auf_eine_schon_gezeigte_Form`, Mutant R1 |

**Mutanten-Tabelle der Reparaturen:**

Gemessen an `54dbf16a + Arbeitsbaum geändert`, 2026-11-17 19:25.

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| R1 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:307 | red | red |  |
| R2 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:309 | red | red |  |
| R3 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:118 | red | red |  |
| R4 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:137 | red | red |  |
| R5 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:543 | red | red |  |
| R6 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:733 | red | red |  |
| F3 | frontend/src/sohn/SohnPractice.tsx:356 | red | red |  |
| F4 | frontend/src/sohn/SohnPractice.tsx:198 | red | red |  |

Grundlinie nach dem Zurückspielen: green.

Dazu 🟢: `Antwort_des_Vaters_erledigt_nichts` (Mutant R6), der Eintrag für `EasedStages` in `UnlimitedByDesign`
entfernt (Dictionary-Spalten fallen nicht unter G3). Offen als 🟢: Rundenzähler zählt auch Vorschau-Sitzungen; eine
offene Vorschau-Runde des Vaters würde vom Kind fortgesetzt (keine Spalte „wer startete“).

## Retrospektive (nur Vorschläge, Freigabe 3)

**Endstand Backend:** `dotnet test Pugling.sln -c Release` **1817/1817** grün (nach der Probe, Arbeitsbaum ohne Mutanten).

**Nachschau:** nicht gelaufen – der Lauf endete am Zähler vor dem Rollengang.

- **(CY) Probe-Skript wiederholt das Zurückschreiben bei einer Windows-Schreibsperre.** `mutant-probe.py` brach beim
  Wiederherstellen mit `OSError Errno 22` ab und ließ Mutant M8 im Code stehen. Gemerkt nur, weil der Abbruch sichtbar
  war; ein Reviewer las den Mutanten schon als Befund. Vorschlag: `open(…, "wb")` beim Zurückschreiben mit Wiederholung
  und am Ende ein Byte-Vergleich aller berührten Dateien gegen den Ausgangsstand. Der Wrapper dieses Laufs
  (Scratchpad `probe_retry.py`) hat es bewiesen.
- **(CZ) Fokus-Regel der Arcade als Tor:** ein neuer Knopf vor der Antwortfläche stahl den Fokus (Fund 1). Vorschlag:
  E2E-Zusicherung „nach Weiter steht der Fokus im Antwortfeld" in jede Spec, die eine neue Antwortform einführt.

## Sprint 92 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn):** „Neue Wörter zählen erst, wenn ich sie aus dem Gedächtnis weiß; bis dahin darf ich mir helfen
lassen, und die Lernkarte holt sie Runde um Runde zurück.“ **Umfang:** [B-643](backlog/B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md) (L).
Entwickler-Brief wie in der Sprintplanung; Fundament aus B-635 (Stufe je Karte, `easier`) wiederverwendet.

## Iteration 92 — umgesetzt

Einzelheiten im Verlauf von B-643. Kern: `ImprintedAt` (Migration mit Backfill, Probe an einer Kopie der echten DB:
30 Positionen, eingeprägt = eingeführt), Kartenwahl über `Known`, Dosis zählt offene Lernkarten, Lernkarte leichter in
Schritt 4, Wiederholung ohne Grenze, Ring „eingeprägt“.

**Review:** `pugling-reviewer` 1 🔴 (doppelt geschickte Hilfe-Antwort prägt ein) + 4 🟡 (Dosis-Leck, Testportion,
Test-Wörter ohne Entscheidung, fehlender Neu-Bonus-Test); `frontend-reviewer` 2 🟡 (zwei Testlücken). Behoben: alle
außer Fund 3 (Testportion) – **verworfen**, weil die Reparatur die B-04-Invariante brach (gemessen:
`VierWochenTests.Dosis_WerNieUebt…` 5/5/5 statt 5/10/15). Fund 4 per Herleitung aus Entscheidung 1 gelöst (frei im Test
getippt prägt ein) – **zur Bestätigung durch den Vater**. Zähler formell bei 7 (> 5); der Lauf läuft seit „Ja,
weitermachen“ im Dialog, die Funde sind behoben statt angehäuft.

**Mutanten-Tabelle:**

Gemessen an `d401d615 + Arbeitsbaum geändert`, 2026-11-17 20:24.

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| K1 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:184 | red | red |  |
| K2 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:176 | red | red |  |
| K3 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:295 | red | red |  |
| K4 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:351 | red | red |  |
| K5 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:286 | red | red |  |
| K6 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:302 | red | red |  |
| K7 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:575 | red | red |  |
| K8 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:829 | red | red |  |
| K9 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:829 | red | red |  |
| K10 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:829 | red | red |  |
| K11 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:860 | red | red |  |
| K12 | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:297 | red | red |  |
| K13 | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:332 | red | red |  |
| K14 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:274 | red | red |  |

Grundlinie nach dem Zurückspielen: green.

**Verifikation:** `dotnet test` (Debug, wegen eines parallelen Release-Laufs der anderen Sitzung) **1831/1831**, `tsc`
sauber, Vitest **1172/1172**, E2E volle Suite **169 passed, 1 failed** (`vater-von-null`, einzeln **1 passed**),
`lernkarte-leichter.spec.ts` **1 passed**.

## Runde 92 — Abnahme

- **Sohn:** maschineller Rollengang `lernkarte-leichter.spec.ts` gegen frisch gestarteten Server: leichter bis zur Auswahl,
  „kommt noch einmal“, ganze Lernkarte am Rundenende, frei erinnert, Ring „2 von 2 eingeprägt“. Offen für ein Gerät: wie
  sich die Leiter in Schritt 4 am Handy anfühlt.
- **Vater:** Regressionszeuge – Pflicht bleibt der Test (AK 7, Bestandstests grün), Bestandsringe der Söhne unverändert
  (Migrationsprobe). Offene Entscheidung: Testantwort frei getippt = eingeprägt.
- **Creator:** Regressionszeuge – kein Pfad des Creators im Diff; Suite grün.

## Retrospektive 92

**Nachschau:** B-633, B-634, B-635 – Prüfpunkt: die Sprint-92-Änderungen an `EaseLadder`/`IsRemediationRound` lassen den
Lückentext-Pfad unverändert (`RemediationPositionTests` 11/11 grün nach der Umstellung auf `SupportsRemediation`);
kein Durchrutscher gefunden.

- Durchgelassen: der eigene Bau nahm eine Review-Reparatur (Testportion) fast ungeprüft an; erst ein Bestandstest der
  B-04-Invariante fing sie. Bestätigt die Regel [[review-vorschlag-gegen-invariante]] – kein neuer Mechanismus nötig, sie
  hat gegriffen.
- **Vorschlag (DA):** das Test-Tor des Stop-Hooks erkennt einen fremden `testhost` im selben Checkout und meldet „parallel
  belegt“ statt „Fixture abgebrochen“ – dreimal stand heute ein rotes Tor ohne eigenen Fehler.

## Offene Roadmap

Quelle ist [docs/backlog/](backlog/INDEX.md). Nächster Schritt: Funde 1–8 von Sprint 85 beheben (B-633/B-634/B-635
stehen auf `in-arbeit` mit `wartet_auf`), Rollengang, Abnahme, Commit; danach Sprint 92 (B-643), dessen Frontend schon im
Arbeitsbaum liegt.
