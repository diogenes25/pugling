# PM-Sitzung: Lauf AD – Sperrmeldungen sagen, was sperrt

**Datum:** 2026-11-08  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Ein Lauf mit B-411, B-413 und B-414 gebündelt („B-411/413/414 bündeln", Auftrag im Dialog), danach Retrospektive mit Nachschau
zu Sprint 20 (B-409).

## Refinement

Alle drei Stories von `idee` bis `geschaetzt` am Code (Defekt bzw. Aufräumen, im Lauf gegrillt, Entscheidungen beschriftet). Messung
für B-411: 33 Codes mit 409, 9 mit deutschem Text; 17 der übrigen erreicht ein Erwachsener, 6 nur das Kind, einer ist der unbenutzte
Default. Die sechs Kind-Codes laufen in der Arcade über `problem.*` in i18n, nicht über die deutsche Tabelle → neue Story
[B-415](backlog/B-415-arcade-sperrcodes-ohne-text.md), bewusst nicht im Sprint.

Beim Lesen der Texte fiel auf, dass `pm-loop` die Meldungsregel schärfer formulierte als entschieden („never a way out" statt „keine
Auswege über die eigene Sperre hinaus", BZ). Berichtigt in `pm-loop` Step 5 und in beiden Reviewer-Briefs, bevor ein Text dagegen
geschrieben wurde.

## Sprint 21 — Ziel & Umfang

**Sprint-Ziel:** Jede Sperre, die der Vater an einem eigenen Knopf auslöst, meldet sich auf Deutsch und sagt, was sperrt – ohne ihn an
eine Stelle oder über einen Weg zu schicken, der selbst gesperrt ist; ein neuer 409-Code ohne Text kommt nicht mehr durch.
**Umfang:** B-413, B-414, B-411 (in dieser Reihenfolge).
**Entwickler-Brief:** Quelle der Wahrheit sind die `ApiErrors`-Codes und `GERMAN_PROBLEM_TEXT`. B-413 trennt das Löschen einer Unit/Reihe
in einen eigenen Code (`unit_content_in_use`), damit sein Text keine Fläche nennt, die die Unit-Etappen nicht zeigt. B-414 schreibt die
Texte um, deren Ausweg durch eine zweite Sperre führt (DE und EN). B-411 ergänzt 17 deutsche Texte und das Tor `ProblemTextGuardTests`
(alle 409-Codes: Text oder begründete Ausnahme, beide Richtungen, Selbstschutz). Keine Migration. Testweg: `CatalogManagementTests`,
`ReiheFachwechselTests`, `ExerciseUsageScopeTests`, `errorMessage.test.ts`, das neue Tor, `creator-lehrwerk-weg.spec.ts`.

## Iteration 21 — umgesetzt

**Backend (API-First):** neuer Code `unit_content_in_use` für Unit-/Reihen-Löschen (B-413); englische Sperrsätze ohne Ausweg über eine zweite
Sperre (`Explain`, `ExerciseItemsLocked`, `SeriesSubjectInUse`, B-414); Tor `ProblemTextGuardTests` (B-411). **Frontend:** 18 neue deutsche
Texte, vier umgeschrieben, Kopfkommentar der Tabelle nach der Regel. **Doku:** `endpunkt-beziehungen.md`, `tutorial-supervisor.md`,
Swagger-Summaries der Lösch-Actions, OpenAPI und API-Beispiele neu.
**Verifikation:** Backend 1126/1126, Vitest 675/675, `tsc -b` sauber, Playwright **119 passed** (erster Lauf 117/2, siehe Runde 3).

**Belege** – `.claude/scripts/mutant-probe.py`, nach dem letzten Test-Edit:

Gemessen an `c910cf8 + Arbeitsbaum geändert`, 2026-11-08 13:04.

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| B413-1 Unit löschen mit altem Code | backend/Pugling.Api/Controllers/Creator/SeriesUnitsController.cs:157 | red | red |  |
| B413-2 Reihe löschen mit altem Code | backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:299 | red | red |  |
| B414-1 Explain mit Ausweg | backend/Pugling.Api/Services/Shared/ExerciseUsageQueries.cs:158 | red | red |  |
| B414-2 Wörter-Sperre mit Ausweg | backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:311 | red | red |  |
| B414-3 Fach leeren mit Ausweg | backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:207 | red | red |  |
| B414-4 Fachwechsel mit Ausweg | backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:208 | red | red |  |
| B411-G1 deutscher Text fehlt | frontend/src/lib/api.ts:170 | red | red |  |
| B411-G2 überflüssige Ausnahme | backend/Pugling.Api.Tests/ProblemTextGuardTests.cs:29 | red | red |  |
| B411-G3 Tabelle nicht gelesen (Selbstschutz) | backend/Pugling.Api.Tests/ProblemTextGuardTests.cs:92 | red | red |  |
| B414-DE1 Übungs-Sperre mit Ausweg | frontend/src/lib/api.ts:140 | red | red |  |
| B413-DE Unit-Etappe fehlt im Satz | frontend/src/lib/api.ts:145 | red | red |  |
| B414-DE2 Fachwechsel mit Löschen | frontend/src/lib/api.ts:125 | red | red |  |
| B414-DE4 Entfernen-Fall als Tatsache | frontend/src/lib/api.ts:125 | red | red |  |
| B414-DE3 Wörter-Sperre mit Ausweg | frontend/src/lib/api.ts:149 | red | red |  |
| B411-DE duplicate_key fehlt | frontend/src/lib/api.ts:154 | red | red |  |
| B411-DE duplicate_achievement fehlt | frontend/src/lib/api.ts:155 | red | red |  |
| B411-DE duplicate_vocabulary_in_exercise fehlt | frontend/src/lib/api.ts:156 | red | red |  |
| B411-DE duplicate_key_result fehlt | frontend/src/lib/api.ts:157 | red | red |  |
| B411-DE media_variant_exists fehlt | frontend/src/lib/api.ts:158 | red | red |  |
| B411-DE media_already_linked fehlt | frontend/src/lib/api.ts:159 | red | red |  |
| B411-DE duplicate_email fehlt | frontend/src/lib/api.ts:160 | red | red |  |
| B411-DE duplicate_profile_name fehlt | frontend/src/lib/api.ts:162 | red | red |  |
| B411-DE duplicate_category_name fehlt | frontend/src/lib/api.ts:163 | red | red |  |
| B411-DE duplicate_publisher fehlt | frontend/src/lib/api.ts:165 | red | red |  |
| B411-DE grammar_topic_in_use fehlt | frontend/src/lib/api.ts:166 | red | red |  |
| B411-DE last_owner fehlt | frontend/src/lib/api.ts:167 | red | red |  |
| B411-DE vocabulary_in_use fehlt | frontend/src/lib/api.ts:168 | red | red |  |
| B411-DE position_has_data fehlt | frontend/src/lib/api.ts:169 | red | red |  |
| B411-DE timetable_slot_taken fehlt | frontend/src/lib/api.ts:170 | red | red |  |
| B411-DE purchase_not_open fehlt | frontend/src/lib/api.ts:171 | red | red |  |
| B411-DE activation_not_pending fehlt | frontend/src/lib/api.ts:172 | red | red |  |
| B413-E2E Reihe löschen mit altem Code | backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs:299 | red | red |  |

Grundlinie nach dem Zurückspielen: green.

Nachgemessen nach dem Review (Text kam dort dazu), 2026-11-08 13:05: `B411-DE concurrency_conflict fehlt` red, `B411-G4 Text für
concurrency_conflict fehlt (Tor)` red, Grundlinie grün. Zusammen **34 von 34**.

Nach der letzten Textänderung („bisherigen" zurück in Zeile 125, wo B414-DE2/-DE4 mutieren) den Vitest-Teil auf dem Endstand neu
gemessen, 2026-11-08 13:25: **23 von 23** rot am eigenen Fall, Grundlinie grün. Die Backend-Dateien sind seit ihrer Messung unverändert.

## Runde 2 — Review

- `pugling-reviewer` (Backend): keine 🔴. 🟡 `concurrency_conflict` erreicht auch den Vater (Stornieren/Genehmigen/Ablehnen im Shop) – die
  Ausnahme „child-only" war falsch, meine Messung in B-411 auch → deutscher Text, Ausnahme gestrichen, B-411 berichtigt. 🟡 zwei Doku-Stellen
  nannten noch `exercise_in_use` fürs Unit-Löschen → berichtigt. 🟢 Selbstschutz auch für die zweite Torrichtung, Begründungen der
  Medien-Ausnahmen, Swagger-Summaries → umgesetzt; 🟢 Capture für `unit_content_in_use` → offen gelassen. Nebenbefund:
  `backend/Pugling.Api/CLAUDE.md` nennt `concurrency_conflict` „für Clients unerreichbar" → [B-417](backlog/B-417-concurrency-conflict-doch-erreichbar.md).
- `frontend-reviewer`: keine 🔴. 🟡 `series_subject_in_use` beim **Entfernen** als Tatsache falsch → als Bedingung; 🟡 Wörter weichen von der
  Oberfläche ab („Schlüssel"/„Key", „Kennzahl"/„Messlatte"/„Ziel-Metrik", „Besitzer"/„Verwalten") → angeglichen. 🟢 Schlüsselwörter je Satz
  eindeutig, `position_has_data` „Entfernen", Profilname „auch ein inaktives", „steckt in einem Lehrplan", Ausnahmebegründungen → umgesetzt.
  Nebenbefund: die Bestätigungsfrage beim Entfernen einer Position verspricht einen Verlust, den es nie gibt →
  [B-418](backlog/B-418-position-entfernen-verspricht-verlust.md).

**Zähler 4 von 5.** Kein Delta-Review – ein fünfter Fund hätte den Lauf an Freigabe 3 beendet; die Review-Änderungen sind über die
Nachmessung der Mutanten und den zweiten Playwright-Lauf gedeckt.

## Runde 3 — Abnahme

Der erste Playwright-Lauf nach dem Review war **117 passed / 2 failed**: `reihe-fachwechsel-gesperrt.spec.ts` suchte „hängt noch an ihrem
bisherigen Fach" – das Wort war beim Umschreiben verloren gegangen (zurück im Text); `verlag-fehler-am-platz-der-seite.spec.ts` erwartete die
englische Server-Zeile, die seit B-411 Deutsch ist (Spec umgestellt). Zweiter Lauf auf dem letzten Stand: **119 passed**.

- **Vater:** Rollengang als E2E – Reihe löschen (`creator-lehrwerk-weg.spec.ts`, neuer Satz ohne Verweis), Fachwechsel
  (`reihe-fachwechsel-gesperrt.spec.ts`), Verlags-Dublette (`verlag-fehler-am-platz-der-seite.spec.ts`, deutscher Satz). Sprint-Ziel
  gehalten: jede Sperre, die der Vater am eigenen Knopf auslöst, spricht Deutsch und nennt, was sperrt; ein neuer Code ohne Text ist rot.
- **Creator, Sohn:** Regressionszeugen – ganze Suite grün; die Arcade-Codes sind bewusst nicht berührt (B-415).

B-411, B-413, B-414 `abgenommen`.

## Retrospektive

**Nachschau:** Sprint 20 (B-409, `c910cf8`), frischer `pugling-reviewer`, read-only: kein 🔴; ein 🟡 – das Swagger-Summary von „Übung
löschen" nennt die Ziel-Etappen nicht, dazu `docs/uebergabe-vokabeln.md:52-55` – am Code nachgeprüft → [B-416](backlog/B-416-loeschen-summary-und-ziel-id-ungeprueft.md),
`entgangen_bei: [B-409]` (mit zwei 🟢: `objectiveId` ungeprüft, kein Kind mit zwei Betreuern). B-409 `nachgeschaut: 2026-11-08`. Increment
eines früheren Laufs (AC) – kein Limit 2 aus der Nachschau.

**Was die eigenen Tore durchgelassen haben:**
1. **Eine Messung nach Ordner statt nach Pfad.** „Nur das Kind" für `concurrency_conflict` habe ich aus `Controllers/Student` geschlossen und
   den geteilten `ShopService` übersehen – genau die Unschärfe, die ich als Begründung für Entscheidung 1 (Tor ohne Ebenen-Erkennung)
   selbst aufgeschrieben hatte. Das Tor selbst war davon nicht betroffen, nur die Begründung einer Ausnahme; die prüft kein Tor.
2. **Umschreiben machte eine Bedingung zur Tatsache.** Der alte Satz war als „geht erst, wenn …" gebaut und darum in beiden Fällen wahr; die
   Umformung zur Behauptung war für den Entfernen-Fall falsch. Ein semantischer Fehler – kein Grep findet ihn.
3. **Alte Sätze in `e2e/` nicht gesucht.** Vor dem Umschreiben habe ich `src/` nach Zusicherungen auf die alten Sätze durchsucht, `e2e/`
   nicht. Gefangen hat es der Playwright-Lauf – das Tor wirkte, nur spät.

**Der eine Mechanismus:** das Tor `ProblemTextGuardTests` selbst (B-411) – ein neuer 409-Code ohne deutschen Text oder begründete Ausnahme ist
ab jetzt rot. **Neues Tor → Limit 2: der Lauf endet hier** (`docs/backlog/README.md`, „Der Backlog-Lauf"); er war ohnehin ein Sprint.

**Bewusst ungedeckt:** (1) ob eine Ausnahmebegründung wahr ist, (2) ob ein umgeschriebener Satz für jeden Rückgabeweg wahr ist. Beides
liest heute nur der Reviewer. **Vorschlag (nicht gelandet, Limit 2):** (CA) wer einen Meldungstext ändert, sucht seine alten Wendungen in
`frontend/src` **und** `frontend/e2e` (ein Grep, im Proben-Skript als Vorab-Schritt denkbar).

## Offene Roadmap

Neu: B-415 (Arcade-Codes), B-416 (Nachschau B-409), B-417 (Doku `concurrency_conflict`), B-418 (Position entfernen). Weiter offen: B-404/B-407
(Fragen), B-408 (Wunsch), B-410 (Tor null-Element), B-412 (Nachschau B-270). Nichts gepusht.

## Nachtrag — Vorschlag (CA) gelandet

Auf Wunsch des Nutzers („Ja, landen") nach dem Ende des Laufs. Statt einer weiteren Prosaregel ein Werkzeug:
`.claude/scripts/old-phrases.py [rev]` liest aus `git diff` die String-Literale entfernter Zeilen in den Meldungs-Quellen (deutsche
Tabelle, i18n, `backend/Pugling.Api`), bildet Vier-Wort-Fenster, behält die, die nirgends mehr vorkommen, und sucht sie in Tests,
E2E-Specs und handgeschriebener Doku. `pm-loop` Step 5 ruft es in der Meldungsregel auf; das Verfahrensbudget ist bezahlt (eine ältere
Zeile gekürzt, 31.998 B).

**Erprobt:** sauberer Baum → kein Treffer. Der nachgestellte Fehler aus Sprint 21 („bisherigen" entfernt) → genau ein Treffer,
`e2e/reihe-fachwechsel-gesperrt.spec.ts:43` – in Sekunden statt nach einem siebenminütigen Playwright-Lauf. Sprint 21 gegen `c910cf8` →
ein **echter Rest**: das Summary von `ReiheFachwechselTests.Ausweg_Loeschen_…` zitierte noch den mit B-414 entfernten Satz „only while nothing
uses it" (berichtigt); dazu drei Kandidaten, die in Ordnung sind (zwei Kommentare zitieren den alten Satz bewusst, eine Allerweltswendung).
Zwei Fehlalarm-Quellen im ersten Versuch behoben: Strings über mehrere Zeilen (Literale je Seite zusammengelegt) und Satzzeichen am Wortende.
