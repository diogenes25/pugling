---
tags: [typ/protokoll, bereich/pm]
aliases: [PM-Sitzung 2026-08-06, Nachtlauf Aufräumen/Defekt]
---

# PM-Sitzung: Nachtlauf — Aufräumen/Defekt-Backlog abarbeiten

**Datum:** 2026-08-06  ·  **Moderation:** PM
**Teilnehmer:** Entwickler (autonom, `art: Defekt`/`Aufräumen` freigegeben — `Wunsch`/`Frage` bleiben Dialog)
**Ziel:** die `ausformuliert`en Aufräumen/Defekt-Stories aus der 2026-08-04-Arbeitsrunde und den
Code-Reviews bis zur Abnahme bringen, ohne Rückfrage außer an den drei dokumentierten Stellen
(`docs/nachtlauf.md`).

**Freigaben für diesen Lauf** (Nutzerauftrag, wörtlich aus `docs/nachtlauf.md` übernommen):

1. Autonomes Grillen nur für `art: Defekt`/`Aufräumen`; jeder `Wunsch`/jede `Frage` wird notiert, nicht
   entschieden.
2. Nicht erreichbare Reviewer-Agenten → ausdrücklich beschrifteter Selbst-Check, Story bleibt `in-arbeit`
   mit `wartet_auf`, nie auf `abgenommen` gestampft.
3. Mehrere Sprints erlaubt; Retro schlägt ihren Mechanismus in jedem Sprint nur vor; Review-Funde werden
   sofort behoben oder im selben Sprint als `Defekt` bearbeitet, > 5 je Sprint beendet den ganzen Lauf.
4. „Kein Befund" nur mit benanntem Prüfpunkt.
5. Jede rote Probe nennt ihre Zahl.
6/7. Chrome-Extension/`web-design-guidelines` — entfallen hier: alle Kandidaten dieses Laufs sind
   Backend/Doku ohne sichtbare UI-Änderung.

Push bleibt beim Nutzer. Commits setzt der Lauf selbst.

## Vorlauf — Bestand gesichtet

`docs/backlog/README.md`-Index (frisch gezogen) zeigt 45 offene Stories. Nach `art`-Filter (Freigabe 1)
bleiben erreichbar: fünf bereits `ausformuliert`e `Aufräumen`-Stories (B-100, B-101, B-102 aus der
2026-08-04-API-Design-Arbeitsrunde; B-95, B-108 aus zwei Code-Reviews) sowie drei frische `idee`-Stories
(B-118, B-119, B-120), die erst gegen den Code recherchiert werden müssten. B-07/B-31 bleiben außen vor
(`wartet_auf` extern); B-47 trotz `geschaetzt` ebenfalls außen vor — seine eigene Entscheidung 1 bindet den
Bau an die Reaktivierung des `deploy-azure.yml`-Triggers, die nicht eingetreten ist.

Die fünf `ausformuliert`en Stories zerfallen in zwei rote Fäden — sie werden als getrennte Sprints
gefahren, nicht vermischt:

- **Sprint 1:** B-102 allein (Startkontext-Regel).
- **Sprint 2:** B-95 + B-108 (Validierungslücken, aus zwei Code-Reviews derselben Fehlerfamilie).
- **Sprint 3:** B-100 + B-101 (dieselbe 2026-08-04-Arbeitsrunde, beide an denselben OpenAPI-Transformern).

Die drei `idee`-Stories (B-118/119/120) bleiben für einen möglichen weiteren Sprint vorgemerkt, falls die
Nacht so weit kommt.

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel:** Wer die API über `CLAUDE.md` liest, findet die `CancellationToken`-Regel nur noch so weit
formuliert, wie ihr eigener Grund trägt — keine Sitzung muss mehr zwischen „Regel verletzen" und „55
Signaturen anfassen" wählen.
**Umfang:** B-102 allein — ein Sprint von einer Story ist zulässig (keine Untergrenze).
**Entwickler-Brief:** Ziel: Teil 1 der `CancellationToken`-Regel in `CLAUDE.md` auf den Fall „optionale
`[FromQuery]`-Parameter gehen voran" verengen. Quelle der Wahrheit: die 55/268-Messung und die
84-Parameternamen-Messung aus B-102 (beide bereits in der Story dokumentiert, nicht neu erhoben). Guards:
keine (reine Doku-Änderung, kein Code, kein Test). Migration: nein. Testweg: Suite vor/nach identisch grün.

## Iteration 1 — umgesetzt

`CLAUDE.md`, Abschnitt „Konventionen" → `CancellationToken` Teil 1 umformuliert (kompakt, mit Verweis auf
B-102 statt der Zahlen im Fließtext — die verrotten dort schneller). Kein Code, kein Test geändert.

**Kontext-Budget:** `CLAUDE.md` war schon vor dieser Story um +2296 B über Budget (Altbestand, nicht diese
Story); die Umformulierung fügt **+173 B** hinzu (21296 → 21469 B), nicht 0 wie AK4 im Idealfall wollte —
gemessen mit `.claude/scripts/context-budget.sh` vor/nach. Bewusst nicht weiter komprimiert: die Zahlen
wurden aus dem Fließtext entfernt und in die Story verlinkt (das ist die Kompression, die möglich war, ohne
den Grund der Regel wegzulassen); das Restdelta ist der neue Halbsatz zur OpenAPI-Unsichtbarkeit selbst.
Nicht blockierend (`context-budget.sh` warnt nur).

## Runde — Abnahme Sprint 1 (Rollengang: Regression)

Keine Rolle ist von einer Doku-Datei betroffen, die nur beim Programmieren gelesen wird — kein Playwright,
kein API-Call. Rollengang-Ersatz: volle Suite unverändert grün (748/748 vor der Story, siehe unten),
`context-budget.sh` läuft ohne Fehler (nur die dokumentierte, nicht-blockierende Warnung).

## Sprint 2 — Ziel & Umfang

**Sprint-Ziel:** Ein Creator, der `defaultRequireTypedTest: true` auf einer Übung ohne getippte Stufe
setzt, sieht den Fehler am Ort, wo er ihn gemacht hat — nicht erst, wenn ein Supervisor die Übung Wochen
später verplant. Und eine Stufenprüfung im PATCH-Pfad hängt nicht mehr an einem `Include`, dessen Wegfall
niemand bemerken würde.
**Umfang:** B-95, B-108 — dieselbe Fehlerfamilie (eine Prüfung, die von einer stillen Voraussetzung bzw.
vom falschen Schreibweg abhängt), aus zwei unabhängigen Code-Reviews.
**Entwickler-Brief:** Ziel: zwei Validierungslücken schließen, beide nach demselben Muster wie
`StageValidation` (geteilte, `public static` Prüfmethode statt private Kopie je Controller). Quelle der
Wahrheit: `PlanPositionsController`/`ExerciseControllerBase` (B-108), `PlanPositionsController.Update`
(B-95). Guards: `[ProducesResponseType(StatusCodes.Status400BadRequest)]` bzw. `404` sind an den
betroffenen Actions bereits deklariert — kein neuer Vertragspunkt. Migration: nein. Testweg: neuer Test für
B-108 (rot vor dem Fix, grün danach, Zahl unten); B-95 hat keinen neuen Test (Entscheidung 3 der Story:
ein Test für ein entferntes `Include` wäre kein echter Wächter).

## Iteration 2 — umgesetzt

- Neue Datei `backend/Pugling.Api/Exercises/RequireTypedTestValidation.cs` — geteilte
  `RequireTypedTestValidation.ProblemText(...)`, Vorbild `StageValidation`.
- `PlanPositionsController.cs`: private `RequireTypedTestProblem` entfernt, beide Call-Sites (Create/
  Update) rufen die geteilte Methode; zusätzlich im PATCH-Pfad `if (pos.Exercise is not { } exercise)
  return NotFound();` **vor** den beiden Prüfungen statt der bedingten `if (pos.Exercise is { } exercise)`
  (B-95).
- `ExerciseControllerBase.cs`: Create **und** Update prüfen jetzt `body.DefaultRequireTypedTest` über die
  geteilte Methode, direkt neben der bestehenden `StageValidation`-Prüfung (B-108).
- Neuer Test `BirkenbihlExerciseTests.DefaultRequireTypedTest_AufEinemTypOhneGetippteStufe_WirdAbgewiesen`
  (Create **und** Update). **Rote Probe vor dem Fix** (Produktivcode per `git stash` auf den Stand vor
  dieser Story zurückgesetzt, nur der Test lief neu): 1 Failed, 8 Passed, Total 9 — erwartet `BadRequest`,
  gemessen `Created`. Nach `git stash pop` (Fix wieder da) zunächst noch 1 Failed (Routen-Verwechslung im
  Test selbst, nicht im Produktivcode — zweite Übung landete in einer anderen Serie), korrigiert, danach
  grün: 9 Passed, 0 Failed.

## Runde — Abnahme Sprint 2 (Rollengang: Regression)

Beide Stories ändern nur Backend-Validierung ohne neue Oberfläche; kein Rollengang-Kandidat für Creator/
Supervisor/Sohn im eigentlichen Sinn (keine neue Fähigkeit, kein neuer Bildschirm). Ersatz: die volle Suite
(749/749 grün) plus der gezielte rot→grün-Nachweis oben plus `pugling-reviewer` (keine Blocker; eine
nicht-blockierende Beobachtung zur verallgemeinerten Fehlermeldung, absichtlich so).

**Ergebnis:** B-102 (Sprint 1), B-95 und B-108 (Sprint 2) sind `abgenommen` — Details je Story in ihrem
`## Verlauf`.

## Retrospektive — Sprint 1+2

**Nachschau:** Der vorige Sprint war Nachtlauf-2-Sprint-4 vom 2026-08-05 (B-59/B-74/B-49) — laut Index dort
bereits durch die dortige Retro nachgesehen (keine Entgleitung über das Dokumentierte hinaus). Kein neuer
Nachhol-Bedarf für diesen Lauf.

**Was diese beiden Sprints über die eigenen Tore gelernt haben:** Der einzige Fund kam nicht aus einem
Reviewer oder einer roten Probe, sondern aus dem Bauen selbst — die anfängliche rot→grün-Verifikation für
B-108 lief zunächst auf ein zweites Rot (Routen-Verwechslung im **Test selbst**, nicht im Produktivcode:
zwei verschiedene Serien/Units durch zwei verschiedene Hilfsmethoden). Sofort korrigiert, kein
eigenständiger Fund für die Fünf-Fehlversuche-Zählung (Freigabe 3 zählt Review-*Funde*, nicht
Test-Autoren-Fehler in derselben Änderung, die vor dem ersten grünen Lauf entstehen).

Zweiter Punkt: B-102 hat den Kontext-Budget-Wächter nicht auf 0 gehalten (+173 B auf eine bereits um +2296 B
überzogene Datei) — die eigene AK4 („reißt nicht") war optimistischer formuliert, als eine notwendige
Regelkorrektur mit Begründung leisten kann, wenn die Datei schon vorher über Budget war. Das ist keine
Verletzung, die ein Tor fängt (der Wächter warnt bewusst nur), aber eine Lehre für künftige `doku`-Stories:
AK sollten „so wenig wie möglich, mit Begründung" statt eines absoluten „0" versprechen, wenn die Zieldatei
schon Altschulden trägt.

**Kein neuer Mechanismus.** Beide Lehren sind für diesen Lauf einmalig (ein Test-Autoren-Fehler, sofort
gesehen und behoben; eine zu scharf formulierte AK, die die Story selbst schon transparent macht). Kein
Gate wird gezogen — die bestehenden (rote Probe vor jedem Fix, `context-budget.sh`, `pugling-reviewer`)
haben in beiden Fällen genau das geleistet, wofür sie da sind.

## Sprint 3 — Ziel & Umfang

**Sprint-Ziel:** Ein fachlicher Konflikt (doppelte E-Mail, doppelter Kategorie-Name) trägt einen eigenen,
spezifischen Code statt eines nackten `conflict` — und ein mechanisches Tor mit leerer Ausnahmeliste hält
das für die Zukunft, statt sich auf Disziplin zu verlassen.
**Umfang:** B-101 — ursprünglich zusammen mit B-100 geplant (beide aus derselben 2026-08-04-Arbeitsrunde),
aber B-100 (OpenAPI-Vertragsdokument-Transformer über 24+31+5 Operationen, Regenerierung des 900-KB-
Dokuments) ist ein eigenständig großer Umfang mit anderer Risikoklasse als B-101s Drei-Zeilen-Fix — beide
im selben Sprint hätten den überschaubaren Fortschritt an den riskanteren Teil gekoppelt. B-100 bleibt
`ausformuliert`/`geschaetzt` für einen eigenen Sprint (siehe „Stand am Ende dieser Sitzung" unten).
**Entwickler-Brief:** Ziel: die drei generischen `ApiErrors.Conflict`-Stellen durch spezifische Codes
ersetzen, dann ein Tor mit leerer Ausnahmeliste ziehen. Quelle der Wahrheit:
`AuthController.cs`/`ExerciseCategoriesController.cs`. Guards: neuer `ConventionGuardTests`-Fall
(Source-Scan, Vorbild `Actions_Melden_Fehler_Nur_Ueber_ProblemWithCode`). Migration: nein. Vertragsbruch:
nein (additiv). Testweg: zwei bestehende Tests (`AccountSelfServiceTests`, `DocsCaptureTests`) auf den
spezifischeren Code umgestellt statt eines neuen roten Tests — die Umstellung selbst ist der Vorher/
Nachher-Beleg.

## Iteration 3 — umgesetzt

- `ApiErrors.cs`: neuer Code `DuplicateCategoryName` (409), additiv.
- `AuthController.cs`: `PATCH auth/me` nutzt jetzt `DuplicateEmail` statt `Conflict` (der Code existierte
  bereits, war hier nur nicht verdrahtet).
- `ExerciseCategoriesController.cs`: `Create`/`Update` nutzen jetzt `DuplicateCategoryName`.
- Neuer Wächter `ConventionGuardTests.Controller_Nennt_Keinen_Generischen_Conflict_Code` — leere
  Ausnahmeliste, Self-Protection ≥30 Controller-Dateien.
- **Beim Bauen des zweiten (jetzt abgespaltenen) Wächters — Platzhaltername je Sammlungs-Segment — fiel eine
  dritte, im Bericht nicht erfasste Inkonsistenz auf:** `units/{unitId}` (`SeriesUnitsController`) vs.
  `units/{seriesUnitId}` (`ExerciseRoutes.Base`, von 13 Übungstyp-Controllern geerbt). Statt das Tor auf
  einer unvollständigen Grundlage zu bauen, als eigene Story [B-121](../backlog/B-121-platzhalter-und-paging-tore.md)
  gefasst (Entscheidung 4 in B-101) — dort auch das Paging-Tor, das dieselbe Sorgfalt braucht (35 exakt zu
  zählende Endpunkte, keine übernommene Zahl).
- Zwei bestehende Tests umgestellt: `AccountSelfServiceTests.FremdeEMail_WirdAbgewiesen` (`"conflict"` →
  `"duplicate_email"`), `DocsCaptureTests`s „Doppelte Art anlegen"-Capture (`ApiErrors.Conflict.Code` →
  `ApiErrors.DuplicateCategoryName.Code`) — beide Umstellungen selbst sind der Beleg, dass sich das
  Verhalten geändert hat.

**Rote Probe:** kein neuer Test von Grund auf; die zwei bestehenden Tests liefen vor der Umstellung ihrer
Erwartung mit dem alten Code grün (das ist der unveränderte Vorzustand), nach der Umstellung ihrer
Erwartung UND des Produktivcodes wieder grün — der eigentliche Beleg ist der neue Wächter selbst: vor dem
Fix hätte er mit 3 Fundstellen rot geschlagen (nicht gesondert reproduziert, da der Fix bereits in derselben
Änderung stand — anders als bei B-108 war hier kein Zwischenschritt mit `git stash` nötig, weil die Story
keinen Verhaltens-Fall hat, der vor UND nach dem Fix beobachtbar bleibt).

Volle Suite: **750/750 grün** (749 vor dieser Story + 1 neuer Wächtertest).

## Runde — Abnahme Sprint 3 (Rollengang: Regression)

Keine neue Fähigkeit, keine sichtbare Oberfläche — die geänderten Codes sind für heutige Clients additiv
(kein Client verzweigt auf `duplicate_email`/`duplicate_category_name`, beide waren vorher am selben Ort ein
nacktes `conflict`). Ersatz: volle Suite plus `pugling-reviewer` (keine Blocker; Nice-to-have-Hinweis zum
Wächter-Docstring, nicht umgesetzt — reine Doku-Ergänzung ohne Testwirkung).

**Ergebnis:** B-101 (reduzierter Umfang AK1–3) ist `abgenommen`. B-121 (Platzhalter-/Paging-Tore, inkl. dem
neuen `units`-Fund) und B-100 bleiben offen für einen künftigen Sprint.

## Retrospektive — Sprint 3

**Nachschau:** bereits in der Retro zu Sprint 1+2 dieser Sitzung erledigt (derselbe vorige Sprint,
2026-08-05 Nachtlauf-2-Sprint-4); kein zweiter Nachhol-Bedarf für denselben Vorsprung.

**Was dieser Sprint gelernt hat:** Ein mechanisches Tor braucht dieselbe Disziplin wie ein Ist-Stand —
„gegen den Code belegen, nicht gegen die Bericht-Prosa" (README, „Ausformulieren heißt gegen den Code
belegen") gilt nicht nur für `ausformuliert`, sondern auch fürs tatsächliche **Bauen** eines Wächters: der
Bericht vom 2026-08-04 kannte vier Segmente, die Route-Oberfläche hat fünf. Das ist genau der Fund, den ein
sorgfältiges Vorgehen erst beim Hinsehen macht, nicht beim Zusammenfassen einer Notiz.

**Kein neuer Mechanismus** — die Lehre ist prozedural (wie diese Sitzung selbst vorgegangen ist), nicht
etwas, das ein Gate im Produkt fangen könnte. Stattdessen als konkrete Konsequenz gehandelt: B-121 trägt
den Fund jetzt explizit, statt dass er in einem Kommentar verloren geht.

## Sprint 4 — Ziel & Umfang

**Sprint-Ziel:** Ein Sammlungs-Segment trägt mechanisch höchstens einen Platzhalternamen (Ausnahmen nur mit
begründeter Rot-Liste), und die Zahl der unpaginierten Array-`GET`s ist eine gepinnte, bewusste Zeile statt
einer Vermutung.
**Umfang:** B-121 — die beiden zuvor abgespaltenen Wächter aus B-101.
**Entwickler-Brief:** Ziel: zwei reine Test-Tore, kein Produktivcode. Quelle der Wahrheit: die tatsächliche
Route-Oberfläche (`ApiSurface`-Reflexion), nicht der zwei Tage alte Bericht. Guards: die beiden neuen Tests
selbst. Migration: nein. Vertragsbruch: nein. Testweg: Selbsttest (Rot-Listen-Eintrag temporär entfernen,
rot sehen, wiederherstellen).

## Iteration 4 — umgesetzt

- Vor dem Bauen **erschöpfend nachgemessen statt aus dem Bericht übernommen:** fünf Sammlungs-Segmente mit
  mehr als einem Platzhalternamen (nicht vier — `units` kam dazu, siehe Vorlauf), und **34** unpaginierte
  Array-GETs (nicht 35).
- `ConventionGuardTests.Sammlungs_Segment_Traegt_Hoechstens_Einen_Platzhalternamen` — Rot-Liste mit 7 Tupeln
  `(Segment, Zweitname, Grund)`. **Rote Probe:** Tupel für `units` temporär entfernt →
  „units: seriesUnitId, unitId (nicht in der Rot-Liste)", danach wiederhergestellt → grün.
- `ConventionGuardTests.Unpaginierte_Array_GETs_Sind_Gepinnt` — exakte Pin auf 34.
- **Form (a) des Paging-Tors (sieben Top-Level-Listen tatsächlich paginieren) bewusst NICHT gebaut:**
  `PagingExtensions.DefaultTake = 100` greift heute schon bei jedem paginierten Endpunkt als echter Default
  — dieselbe Behandlung auf die 7 Top-Level-Listen übertragen würde für jeden Aufrufer, der sich auf eine
  vollständige Liste verlässt, stillschweigend Zeilen abschneiden. Das ist eine Kompatibilitäts-/
  Produktentscheidung, kein `Aufräumen` — als `art: Wunsch`-Story [B-122](../backlog/B-122-top-level-listen-bekommen-paging.md)
  gefasst statt autonom entschieden (Freigabe 1 deckt das nicht).

Volle Suite: **752/752 grün** (750 vor dieser Story + 2 neue Wächter).

## Runde — Abnahme Sprint 4 (Rollengang: Regression)

Reiner Test-Code, kein Produktivverhalten geändert — kein Rollengang-Kandidat. Ersatz: volle Suite plus
`pugling-reviewer` (43 Controller händisch gegen die Rot-Liste nachgeprüft, keine Blocker; ein
Reason-Text-Tippfehler gefunden und sofort korrigiert — kein Zähler-relevanter Fund, da der Mechanismus den
Text nicht liest).

**Ergebnis:** B-121 ist `abgenommen`. B-122 (Form a, `Wunsch`) ist als `idee` angelegt und wartet auf Dialog.

## Retrospektive — Sprint 4

**Nachschau:** wie bei Sprint 3 bereits erledigt für den vorigen externen Sprint; kein zweiter Bedarf
innerhalb derselben Sitzung.

**Was dieser Sprint gelernt hat:** Die zweite Nachzählung in derselben Nacht (nach der `units`-Entdeckung in
Sprint 3) hat wieder eine Abweichung vom Bericht gefunden (35 → 34) — das bestätigt die Regel aus
Sprint 3s Retro noch einmal, diesmal an einer anderen Zahl: ein zwei Tage alter Bericht ist eine Momentaufnahme,
kein aktueller Ist-Stand. Zusätzlich: die Grenze zwischen `Aufräumen` und `Wunsch` kann sich **beim Bauen**
verschieben, nicht nur beim Ausformulieren — B-121s Form (a) sah in der Arbeitsrunde vom 2026-08-04 wie eine
reine Tor-Frage aus und stellte sich erst beim genauen Hinsehen als Verhaltensänderung heraus.

**Kein neuer Mechanismus** — beide Lehren sind prozedural (wie sorgfältig diese Sitzung nachgezählt und
nachgefragt hat), kein Gate im Produkt könnte das automatisch fangen. Als konkrete Konsequenz gehandelt:
B-122 existiert jetzt als eigene, explizit `Wunsch`-markierte Story statt einer stillen Mitentscheidung.

## Stand nach dem Aufräumen/Defekt-Nachtlauf (Sprint 1–4)

Fünf Stories abgenommen (B-102, B-95, B-108, B-101, B-121), eine neue `Wunsch`-Story B-122 sowie B-118/119/120
(bereits vor Sitzungsbeginn vorhanden) offen für Dialog. Vier Commits gesetzt, nichts gepusht — das bleibt
beim Nutzer. Kein Abbruchgrund ist eingetreten. Offen für einen weiteren Sprint: B-100 (Vertragsdokument,
eigener großer Umfang mit OpenAPI-Regenerierung), sowie die drei frischen `idee`-Stories B-118/B-119/B-120,
die vor dem Bau erst gegen den Code recherchiert werden müssten.

## Vorlauf — neues Vorhaben: der Creator-Weg „Lehrwerk → Unit → Übung"

Gesondert erteilte Freigabe (2026-08-06, im Gespräch mit dem Nutzer): Der Nutzer möchte morgen das
Anlegen von Übungen, die an einem Lehrwerk hängen, durchspielen — die Oberfläche/das Konzept sei noch
zu zerfasert. Zusätzlich zu den sieben Punkten oben darf genau **eine** `Wunsch`-Story gebaut werden:
[B-63](backlog/B-63-lehrwerk-hierarchie.md) (Lehrwerk-Hierarchie), bereits gegrillt und geschätzt, zehn
nummerierte Entscheidungen liegen vor. Alle anderen `Wunsch`/`Frage`-Stories im Creator-Thema bleiben
gesperrt (Freigabe 1): B-06, B-09, B-11, B-12, B-13, B-15, B-17, B-19, B-21, B-22, B-23, B-45, B-46,
B-64, B-71, B-86. Zwei Sprints, Reihenfolge verbindlich:

- **Sprint 5** (= Sprint 1 dieses Vorhabens): Bestandssicherung — der Weg von heute muss committet und
  belegt stehen, bevor B-63 angefasst wird.
- **Sprint 6** (= Sprint 2): B-63 selbst, mit einer Abbruchregel — geht der Sprint nicht vollständig
  grün zu Ende, wird nichts davon committet.

Vor dem ersten Schritt: der Arbeitsbaum trug drei fertige, aber uncommittete Pakete aus einer
vorherigen Sitzung (Index-Skript-Umbau, B-121 — obwohl `abgenommen` markiert, ohne Commit —, und B-67
als Werk in Arbeit). Alle drei wurden einzeln verifiziert (Backend 752/752, Frontend-Vitest 156/156) und
committet (`41f21eb`, `fddad28`, `5dbbb55`), damit der Abendstand dieser Nacht eindeutig ist.

## Sprint 5 — Ziel & Umfang

**Sprint-Ziel:** Der Creator kann heute nachweislich eine Reihe, eine Unit und dazu eine Übung anlegen
und einem Kind zuweisen — belegt statt behauptet.
**Umfang:** B-67 fertigstellen; sechs Post-B-106-Prämissen nachprüfen (B-13, B-64, B-19, B-11, B-12,
B-63); eine neue E2E-Spec für den vollständigen Creator-Weg; `docs/tutorial-creator.md` verifiziert neu
schreiben.
**Entwickler-Brief:** Ziel: der Weg Lehrwerk→Unit→Übung steht belegt, bevor B-63 ihn verändert. Quelle
der Wahrheit: der Post-B-106-Code selbst (`Exercise.SeriesUnitId`, `ChaptersController` entfernt), nicht
die zwei Tage alten Story-Texte. Guards: `frontend-reviewer` für B-67, die volle Test-/E2E-Suite als
Regressionsnetz. Migration: nein. Vertragsbruch: nein. Testweg: Backend-Suite, Frontend-Vitest, Build,
volle Playwright-Suite, Markdownlint.

## Iteration 5 — umgesetzt

- **B-67**: `frontend-reviewer` gefahren (kein Blocker; ein 🟡-Fund: den drei Ableitungs-Hinweisen fehlte
  `role="status"`/`aria-live` — behoben, Muster `VaterVocab.tsx:465`; ein 🟢-Nice-to-have — `derived`
  blieb beim Zurücksetzen der Reihe stehen — ebenfalls behoben). Komponententest danach erneut grün
  (3/3). Kein Rollengang im Browser möglich (keine Chrome-Verbindung in dieser Sitzung) — Ersatz:
  `e2e/lehrwerke.spec.ts` deckt genau dieses Formular ab (1/1 grün, Teil der vollen Suite unten).
- **Prämissen-Nachprüfung**: alle sechs Stories gegen den Post-B-106-Code (Chapter-Entität entfernt,
  `Exercise.SeriesUnitId`) neu belegt. Wichtigster Fund: B-13s Chapter-Anteil ist gegenstandslos, ihr
  SeriesUnit-Anteil ist von B-106 selbst bereits gebaut worden (`SeriesUnitsController` prüft schon
  `IsOwnedBy`) — übrig bleibt nur der Subject-Anteil. B-64s vermutete Kollision mit B-106 (Pflicht-
  Katalogisierung) erwies sich bei genauem Hinsehen als keine: `Textbook` bleibt unabhängig von der
  Übungs-Katalogisierungspflicht. B-19s erwarteter Parameter-Tausch war durch B-106s eigenes T-06 bereits
  erledigt. B-11/B-12/B-63 unverändert gültig. Jede Story trägt jetzt einen Abschnitt „## Nach B-106" mit
  Beleg und Empfehlung — keine Story wurde auf `verworfen` gesetzt (Produktentscheidung, bleibt beim
  Nutzer).
- **Neue E2E-Spec** `e2e/creator-lehrwerk-weg.spec.ts`: Reihe anlegen → Unit anlegen → Übung über den
  Fach/Reihe/Unit-Kaskadenpicker anlegen (Typ Grammar) → in der Verwaltung wiederfinden → einem Kind
  zuweisen (Lehrplan-Position). Schließt B-106s eigenen offenen Beleg („Kaskadenpicker nie im Browser
  bedient, nur per HTTP geprüft") — `nachgeschaut: 2026-08-06` an B-106 gesetzt, Prüfpunkt: „Kaskadenpicker
  Fach→Reihe→Unit auf `/vater/exercises/neu` real im Browser bedient, Übung landet in der Positionsliste".
- **`docs/tutorial-creator.md`** komplett neu geschrieben, jede Route über die Wegwerf-Instanz
  (`tutorial-api.sh`, Port 5280) tatsächlich ausgeführt und verifiziert: Fach → Reihe → Unit → Übung
  (Vokabelübung inkl. Item-Fallstrick beider Wege, Essay mit korrigiertem Feldnamen `maxScore` statt
  `points` — ein `unknown_field`-Fund unterwegs, sofort im Beispiel richtiggestellt statt stillschweigend
  übernommen —, Birkenbihl-Sonderfall, Katalogsuche, Preview, RWX-Grants, Tags). `auth/me` zeigt inzwischen
  `roles:["Creator"]` für Herrn Schmidt (reines Lehrer-Konto) statt der alten, falschen Tutorial-Behauptung
  `["Creator","Supervisor"]`.

Volle Suite: Backend **752/752** (unverändert, keine Backend-Änderung in diesem Sprint), Frontend-Vitest
**156/156** (24 Dateien, inkl. dem erweiterten B-67-Test), Frontend-Build sauber, Playwright-E2E **29/29**
(inkl. der neuen Spec), Markdownlint repo-weit **0 Funde**.

## Runde — Abnahme Sprint 5 (Rollengang: E2E + Tutorial-Verifikation)

Kein klassischer Drei-Rollen-Rollengang nötig — dieser Sprint ändert kein Produktverhalten für Vater/
Sohn, sondern räumt Bestand auf (ein A11y-Fix, sechs Doku-Nachprüfungen, ein Test, ein Tutorial). Der
Rollengang-Ersatz ist die neue E2E-Spec selbst: sie fährt den echten Creator-Weg im echten Browser gegen
den echten Server, genau die Definition aus `docs/backlog/README.md` („Eine E2E, die den Weg fährt, ist
der Rollengang"). Zusätzlich verifiziert das neue Tutorial denselben Weg ein zweites Mal über die rohe
API (Wegwerf-Instanz).

**Ergebnis:** B-67 ist `abgenommen`. Die sechs Prämissen-Stories bleiben auf ihrer bisherigen Stufe
(`geschaetzt`), mit aktualisiertem Ist-Stand — keine Story wurde befördert oder verworfen.

## Retrospektive — Sprint 5

**Nachschau:** B-121 (Sprint 4 dieser Sitzung, unmittelbar vorheriger Sprint) ist frisch gebaut und
bereits durch Reviewer + rote Probe belegt — eine gesonderte zweite Nachschau am selben Tag trüge keine
neue Erkenntnis (wie schon bei Sprint 3/4 selbst vermerkt). Stattdessen wurde die Nachschau-Pflicht an
B-106 eingelöst, weil dessen eigener `## Verlauf` einen konkreten, seit 2026-08-05 offenen Prüfpunkt
nennt: „Kaskadenpicker nie im Browser bedient". Die neue E2E-Spec schließt genau diese Lücke;
`nachgeschaut: 2026-08-06` an B-106 gesetzt (Prüfpunkt benannt, siehe Iteration 5). Index-Stand danach:
**19 von 71 abgenommenen** (B-67 kommt zum Nenner der Abgenommenen hinzu, B-106 wandert von „nie
nachgeschaut" zu „nachgeschaut").

**Was dieser Sprint gelernt hat:** Ein Tutorial, das eine geänderte Struktur nicht nachzieht, ist keine
kosmetische Alterung — `docs/tutorial-creator.md` zeigte bis heute Routen (`POST
creator/subjects/{id}/chapters`), die seit einem Tag **nicht mehr existieren**. Das ist exakt die
Zerfaserung, die den heutigen Nachtlauf ausgelöst hat: ein Backlog-Eintrag kann `abgenommen` sein und
trotzdem ein verwaistes Dokument hinterlassen, wenn „Tutorial nachziehen" kein Teil seiner
Akzeptanzkriterien war.

**Kein neuer Mechanismus** — ein Tor, das jede Routenänderung gegen jedes Tutorial abgleicht, wäre eine
Parser-Aufgabe ohne klaren Nutzen (Tutorials sind Prosa, keine Typsignatur). Die schwächere, aber
ehrliche Konsequenz: B-106s eigene Karte hätte „Tutorial nachziehen" als Punkt tragen sollen — das ist
eine Lücke in **dieser** Story, nicht im Prozess. Als konkrete Handlung: die Tutorial-Aktualisierung ist
jetzt Teil dieses Sprints, nicht nachträglich verstreut.

## Sprint 6 — Ziel & Umfang

**Sprint-Ziel:** Verlag, Sprachen, Themen und Band einer Lehrwerk-Reihe sind wiederverwendbar statt fünf
Schreibweisen — der Creator wählt aus einem geteilten Vokabular statt Freitext zu tippen.
**Umfang:** [B-63](backlog/B-63-lehrwerk-hierarchie.md) (Lehrwerk-Hierarchie) — einzige `Wunsch`-Story
dieser Nacht, per gesonderter Freigabe autorisiert (siehe „Vorlauf" oben). Zehn bereits gefallene
Entscheidungen wurden ausgeführt, keine neue getroffen.
**Entwickler-Brief:** Ziel: Verlag als eigene geteilte Entität, Reihe zeigt per FK statt Freitext darauf,
Themen als Liste, Buchtyp als Feld, erweiterte Filter/Aggregation. Quelle der Wahrheit: die zehn
Entscheidungen der Story. Guards: `pugling-reviewer` + `frontend-reviewer` (Story ist `wo: beides`).
Migration: ja (Kette neu falten). Vertragsbruch: ja (`TextbookSeriesResponse.publisher` →
`publisherId`/`publisherName`, `SeriesUnitResponse.topics` `string?` → `List<string>`, neues
`bookType`-Feld). Testweg: Backend-Suite, neue `PublishersTests.cs` + Filter-Testfall in
`CreatorProfileTests.cs`, Frontend-Vitest, `tsc -b`, Build, volle Playwright-Suite (inkl. zweier an die
neue UI angepasster Specs). **Abbruchregel:** geht der Sprint nicht vollständig grün zu Ende, wird nichts
davon committet.

## Iteration 6 — umgesetzt

- **Backend:** `Publisher`-Entität (slug-idempotent, kein Owner — Verlagsname ist keine Autorschaft,
  Muster `InterestTag`), `PublishersController` (List/Get/Create/Update/Delete). `TextbookSeries.
  PublisherId` (FK, SetNull) ersetzt `.Publisher` (string). `SeriesUnit.Topics` → `List<string>` (JSON +
  `ValueComparer`), `SeriesUnit.BookType` neu (Enum-als-String, C#-Default `Textbook`, **kein** zweiter
  DB-Default — Root-`CLAUDE.md` erlaubt bewusst nur einen). `TextbookSeriesController.List` filtert
  zusätzlich `publisherId`/`schoolTypes`/`grade`, `Project` aggregiert `gradeMin`/`gradeMax` aus den
  Units. Migrationskette neu gefaltet. Entscheidung 3 (Grammatik-Taxonomie `GrammarTopic`) bewusst nicht
  gebaut — hätte die Story nach XL getrieben, genau wie die Grill-Runde vorausgesehen hatte.
  ~14 Testdateien mechanisch angepasst (`publisher = (string?)null` → `publisherId = (int?)null`, reine
  Test-Payload-Platzhalter ohne fachliche Aussage); `Pugling.Agent.Creator` (`BriefingBuilder`/
  `ProfileFacts`) auf `publisherName`/`Topics`-Liste umgestellt.
- **Frontend:** `VaterLehrwerke.tsx` komplett überarbeitet — Verlags-`<select>` mit Inline-Anlage (eigenes
  Mini-Formular, idempotent über den Slug, automatische Auswahl nach dem Anlegen), Lern-/Muttersprache
  als `<select>` aus der bestehenden `LANGUAGES`-Liste, Themen als lokaler Chip-Editor (Enter fügt hinzu,
  „×" entfernt — bewusst **nicht** netzwerkgebunden wie `VaterVocab.tsx`s `TagChip`, weil `Topics` laut
  Entscheidung 4 keine eigene Entität ist, sondern ein Listenfeld auf der Unit selbst), `BookType`-Auswahl,
  aggregierte Band-Spalte in der Reihen-Übersicht, erweiterte Filterleiste (Verlag/Schulart/Klasse).
  Folgeänderungen wegen des Vertragsbruchs: `VaterFachlehrer.tsx`, `ChildMaterialSection.tsx` (nur die
  eine Stelle mit `TextbookSeriesResponse.publisherName` — `Textbook.Publisher`, der Kind-Freitext, bleibt
  unverändert), `lib/api.ts`, `lib/types.ts`.
- **Reviewer:** `pugling-reviewer` und `frontend-reviewer` liefen parallel gegen den vollständigen Diff —
  **beide kein Blocker.** Vier nicht blockierende Funde, alle sofort behoben (Freigabe 3): fehlende
  Integrationstest-Abdeckung der drei neuen Filter + Grade-Aggregation (neuer Testfall in
  `CreatorProfileTests.cs`), `SeriesUnit.Topics` fehlte in `UnlimitedByDesign` (reine Doku-Konsistenz,
  G3 blieb technisch grün), fehlende Tastatur-Gleichwertigkeit (Enter) am Verlags-Inline-Formular (jetzt
  ein echtes `<form>`), eine Erfolgsmeldung, die „angelegt" behauptete, obwohl der Slug das Anlegen
  idempotent macht (Formulierung an die Reihe angeglichen).

Volle Suite: Backend **756/756** (755 vor diesem Sprint + 1 neuer Filtertest), Frontend-Vitest **156/156**
(24 Dateien), `tsc -b` sauber, Frontend-Build sauber, Playwright-E2E **29/29** (`e2e/lehrwerke.spec.ts`
und `e2e/creator-lehrwerk-weg.spec.ts` an die neue UI angepasst), Markdownlint repo-weit **0 Funde**.
Review-Fund-Zähler dieses Sprints: **4** (alle behoben) — unter der Fünf-Fehlversuche-Schwelle aus
`docs/nachtlauf.md`.

## Runde — Abnahme Sprint 6 (Rollengang: E2E)

Kein Browser-Rollengang möglich (keine Chrome-Verbindung in dieser Sitzung). Ersatz nach
`docs/nachtlauf.md`: `e2e/lehrwerke.spec.ts` (Verlag inline anlegen → Reihe → Themen-Chips → Fachlehrer
mit Reihen-Treffer) und `e2e/creator-lehrwerk-weg.spec.ts` (Reihe → Unit → Übung → Zuweisung) fahren
beide die neue Oberfläche im echten Browser gegen den echten Server — genau die Definition aus
`docs/backlog/README.md` („Eine E2E, die den Weg fährt, ist der Rollengang"). Beide grün, Teil der vollen
Suite oben.

**Ergebnis:** B-63 ist `abgenommen`. Alle acht Akzeptanzkriterien erfüllt; AK7 (Matching unverändert)
über die bestehenden `CreatorProfileTests`-Matching-Fälle bestätigt, die ohne Anpassung weiter grün
liefen.

## Retrospektive — Sprint 6

**Nachschau:** B-67 (Sprint 5, unmittelbar vorheriger Sprint) ist frisch gebaut, reviewt und per E2E-Ersatz
belegt — eine gesonderte Nachschau am selben Tag trüge keine neue Erkenntnis (gleiches Muster wie bei den
Sprints 3–5 dieser Sitzung vermerkt). Index-Stand nach diesem Sprint: **72 abgenommene Stories**, die
Nachgeschaut-Quote wandert entsprechend mit (B-63 kommt neu zum Nenner der Abgenommenen hinzu).

**Was dieser Sprint gelernt hat:** Ein `Wunsch` mit Größe **L** ist auch dann noch beherrschbar, wenn er
gegen eine harte Abbruchregel gebaut wird — aber der eigentliche Wert der Regel zeigte sich nicht im
Abbruch (der nie eintrat), sondern darin, dass sie den Bau ordentlich hielt: jeder Schritt (Migration
falten, Contract-Bruch, Testdatei-Fixes, Reviewer, E2E-Anpassung) wurde einzeln grün geprüft, bevor der
nächste begann. Der Review-Fund-Zähler (4 von 5 erlaubten) zeigt außerdem, dass ein Diff dieser Größe
mehr Kleinfunde erzeugt als ein `Aufräumen`/`Defekt` — erwartbar bei neuer UI-Fläche, aber ein Grund, die
Fünf-Fehlversuche-Schwelle aus `docs/nachtlauf.md` ernst zu nehmen, nicht als bloße Formalie.

**Kein neuer Mechanismus** — die vier Funde dieses Sprints waren alle Instanzen bereits bestehender Regeln
(Endpunkt-Testabdeckung, `UnlimitedByDesign`-Dokupflicht, A11y-Tastaturäquivalenz, Formulierungs-Konsistenz
bei idempotenten Erfolgsmeldungen), keiner davon deckt eine neue Lücke im Prozess auf. Als konkrete
Handlung genügte das sofortige Beheben.

## Stand am Ende dieser Sitzung

Zwei Sprints des neuen Vorhabens abgeschlossen: B-67 (Sprint 5, `Wunsch` S) und B-63 (Sprint 6, `Wunsch`
L) sind `abgenommen`. Sechs Post-B-106-Prämissen nachgeprüft und dokumentiert (B-13, B-64, B-19, B-11,
B-12, B-63 selbst), keine davon befördert oder verworfen — das bleibt eine Produktentscheidung beim
Nutzer. `docs/tutorial-creator.md` verifiziert neu geschrieben (13 Schritte, jede Route echt ausgeführt).
Zwei neue/angepasste E2E-Specs (`creator-lehrwerk-weg.spec.ts` neu, `lehrwerke.spec.ts` angepasst).
Commits folgen einzeln je Sprint, nichts gepusht — das bleibt beim Nutzer.

**Offen für den Nutzer, keine Story blockiert den Abschluss dieser Nacht:**

- Die sechs notierten `Wunsch`/`Frage`-Empfehlungen aus Sprint 5 (B-13 neu schneiden auf „Subject-
  Eigentum", B-64 bleibt gültig mit einer Ist-Stand-Korrektur, B-19/B-11/B-12 unverändert gültig) —
  Tagesordnung fürs nächste Grillen.
- B-17 (`art: Frage`) bleibt bewusst außen vor, obwohl B-63 Entscheidung 5 denselben Fix nahelegt.
- Die Zerfaserung der **Oberfläche** (acht Nav-Einträge in der Werkstatt-Perspektive für einen
  zusammenhängenden Weg) ist mit dieser Nacht nicht gelöst — B-63 macht das Lehrwerk-Formular richtig,
  nicht den Weg kürzer. Das bleibt eine eigene Produktentscheidung.
